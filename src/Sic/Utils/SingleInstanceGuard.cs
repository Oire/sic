using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Serilog;

namespace Oire.Sic.Utils;

/// <summary>
/// Keeps the GUI to one instance per data folder. Two windows sharing one <c>sic.json</c> would
/// overwrite each other's changes, so a second launch hands over to the running window instead.
/// Everything is keyed by the data folder, so separate portable copies, or a portable and an
/// installed copy, each with their own config, still run side by side. The CLI never takes it.
/// </summary>
/// <remarks>
/// The guard is a named kernel mutex in the session's <c>Local\</c> namespace. It's never waited
/// on and never released, only closed: when the last handle closes, including a crashed process's,
/// the mutex disappears, so nothing is left behind and the next launch creates it afresh. The
/// running instance answers a second launch through its <see cref="ActivationListener"/>.
/// </remarks>
public sealed class SingleInstanceGuard: IDisposable {
    /// <summary>How long a second launch looks for the listener: the first instance creates it
    /// right after taking the guard, so it may not exist yet.</summary>
    private static readonly TimeSpan ListenerWait = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan ListenerPollDelay = TimeSpan.FromMilliseconds(50);

    /// <summary>How long the running instance has to answer: it only answers while its UI thread
    /// pumps messages, which it starts doing at its first message box or window.</summary>
    private const uint ReplyTimeoutMilliseconds = 3000;

    private readonly Mutex _mutex;

    private SingleInstanceGuard(Mutex mutex) {
        _mutex = mutex;
    }

    /// <summary>
    /// Takes the guard for <paramref name="dataFolder"/>.
    /// </summary>
    /// <returns>
    /// The guard, to keep for the app's lifetime, or <see langword="null"/> when another instance
    /// already holds it, or when it can't be opened because an elevated copy created it.
    /// </returns>
    public static SingleInstanceGuard? TryAcquire(string dataFolder) {
        var name = $@"Local\Oire.Sic.{GetInstanceKey(dataFolder)}";

        try {
            var mutex = new Mutex(initiallyOwned: true, name, out var createdNew);
            if (!createdNew) {
                mutex.Dispose();
                return null;
            }

            return new SingleInstanceGuard(mutex);
        } catch (UnauthorizedAccessException ex) {
            // An elevated copy owns the mutex, and its security keeps a normal process out. That
            // still means SIC! is running, so treat it as taken rather than as a startup failure.
            Log.Warning("SingleInstance: unable to open the instance mutex {Name}: {Error}", name, ex.Message);
            return null;
        }
    }

    /// <summary>
    /// Brings the instance running for <paramref name="dataFolder"/> forward. The running instance
    /// restores its window and names the one to focus, and this process, which the user just
    /// started and which Windows therefore lets take the foreground, focuses it.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when a window of the running instance came forward;
    /// <see langword="false"/> when none answered or Windows refused, so the caller can say SIC!
    /// is already running instead of exiting without a word.
    /// </returns>
    public static bool ActivateExisting(string dataFolder) {
        Log.Information("SingleInstance: another instance is running, asking it to come forward");

        var target = RequestActivationTarget(dataFolder, ListenerWait);
        if (target == IntPtr.Zero) {
            return false;
        }

        if (!SetForegroundWindow(target)) {
            Log.Warning("SingleInstance: SetForegroundWindow was refused");
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds the running instance's <see cref="ActivationListener"/>, waiting up to
    /// <paramref name="wait"/> for it to appear, and asks it to get its window ready.
    /// </summary>
    /// <returns>The window to focus, or <see cref="IntPtr.Zero"/> when no listener answered or it
    /// had no window to offer.</returns>
    internal static IntPtr RequestActivationTarget(string dataFolder, TimeSpan wait) {
        var message = GetActivationMessage(dataFolder);
        if (message == 0) {
            Log.Warning("SingleInstance: unable to register the activation message (error {Error})",
                Marshal.GetLastPInvokeError());
            return IntPtr.Zero;
        }

        var title = GetListenerTitle(dataFolder);
        var stopwatch = Stopwatch.StartNew();
        IntPtr listener;

        while ((listener = FindWindowEx(HWND_MESSAGE, IntPtr.Zero, null, title)) == IntPtr.Zero) {
            if (stopwatch.Elapsed >= wait) {
                Log.Warning("SingleInstance: the running instance has no activation listener");
                return IntPtr.Zero;
            }

            Thread.Sleep(ListenerPollDelay);
        }

        if (SendMessageTimeout(listener, (uint)message, IntPtr.Zero, IntPtr.Zero, SMTO_ABORTIFHUNG,
                ReplyTimeoutMilliseconds, out var target) == IntPtr.Zero) {
            Log.Warning("SingleInstance: the running instance didn't answer (error {Error})",
                Marshal.GetLastPInvokeError());
            return IntPtr.Zero;
        }

        if (target == IntPtr.Zero) {
            Log.Warning("SingleInstance: the running instance has no window to bring forward");
        }

        return target;
    }

    /// <summary>
    /// The window message that asks the instance for <paramref name="dataFolder"/> to come
    /// forward. The same folder always yields the same message, system-wide.
    /// </summary>
    /// <returns>The message, or 0 if Windows couldn't register it.</returns>
    public static int GetActivationMessage(string dataFolder) =>
        (int)RegisterWindowMessage($"Oire.Sic.Activate.{GetInstanceKey(dataFolder)}");

    /// <summary>The title of the instance's message-only <see cref="ActivationListener"/>, which
    /// a second launch looks it up by.</summary>
    internal static string GetListenerTitle(string dataFolder) => $"Oire.Sic.Listener.{GetInstanceKey(dataFolder)}";

    /// <summary>
    /// A short, stable key for <paramref name="dataFolder"/>: the first 16 hex digits of the
    /// SHA-256 of its full path, upper-cased and without a trailing backslash, so paths differing
    /// only by case or a trailing separator share a key.
    /// </summary>
    public static string GetInstanceKey(string dataFolder) {
        var normalized = Path.GetFullPath(dataFolder).TrimEnd('\\').ToUpperInvariant();
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)))[..16];
    }

    /// <summary>
    /// Closes the mutex handle, which frees the guard once no other handle is open. It never calls
    /// <see cref="Mutex.ReleaseMutex"/>, which throws on any thread but the owner, so it's safe
    /// from any thread, and calling it again does nothing.
    /// </summary>
    public void Dispose() => _mutex.Dispose();

    internal static readonly IntPtr HWND_MESSAGE = new(-3);
    private const uint SMTO_ABORTIFHUNG = 0x0002;

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern IntPtr FindWindowEx(IntPtr hWndParent, IntPtr hWndChildAfter, string? lpszClass, string? lpszWindow);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern IntPtr SendMessageTimeout(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam,
        uint fuFlags, uint uTimeout, out IntPtr lpdwResult);

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
}
