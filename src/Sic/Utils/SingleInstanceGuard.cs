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
/// the mutex disappears, so nothing is left behind and the next launch creates it afresh.
/// </remarks>
public sealed class SingleInstanceGuard: IDisposable {
    private readonly Lock _lock = new();
    private Mutex? _mutex;

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
    /// Asks the instance running for <paramref name="dataFolder"/> to bring its window forward.
    /// Best effort: Windows may only flash the taskbar button, and blocks the message entirely
    /// when the running copy is elevated.
    /// </summary>
    public static void ActivateExisting(string dataFolder) {
        Log.Information("SingleInstance: another instance is running, asking it to come forward");

        // This process was just started by the user, so Windows lets it take the foreground; pass
        // that right on to the running instance.
        AllowSetForegroundWindow(ASFW_ANY);

        var message = GetActivationMessage(dataFolder);
        if (message == 0 || !PostMessage(HWND_BROADCAST, (uint)message, IntPtr.Zero, IntPtr.Zero)) {
            Log.Warning("SingleInstance: unable to post the activation message (error {Error})",
                Marshal.GetLastPInvokeError());
        }
    }

    /// <summary>
    /// The window message that asks the instance for <paramref name="dataFolder"/> to come
    /// forward. The same folder always yields the same message, system-wide.
    /// </summary>
    /// <returns>The message, or 0 if Windows couldn't register it.</returns>
    public static int GetActivationMessage(string dataFolder) =>
        (int)RegisterWindowMessage($"Oire.Sic.Activate.{GetInstanceKey(dataFolder)}");

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
    public void Dispose() {
        lock (_lock) {
            _mutex?.Dispose();
            _mutex = null;
        }
    }

    private static readonly IntPtr HWND_BROADCAST = new(0xFFFF);
    private const uint ASFW_ANY = unchecked((uint)-1);

    [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern uint RegisterWindowMessage(string lpString);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool PostMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool AllowSetForegroundWindow(uint dwProcessId);
}
