using System.Runtime.InteropServices;
using Serilog;

namespace Oire.Sic.Utils;

/// <summary>
/// The running GUI's answer to a second launch for the same data folder: a hidden message-only
/// window that <see cref="SingleInstanceGuard.RequestActivationTarget"/> finds by its title.
/// Program creates it on the UI thread right after taking the guard, so it answers from the
/// first message box on, before <c>MainWindow</c> exists, until the app exits.
/// </summary>
/// <remarks>
/// It only gets the window ready: it restores a minimized one and names the window to focus. The
/// second process does the focusing, because Windows lets the process the user just started take
/// the foreground, and refuses one that has been running for a while.
/// </remarks>
internal sealed class ActivationListener: NativeWindow, IDisposable {
    private readonly int _message;

    /// <summary>Creates the listener for <paramref name="dataFolder"/> on the calling thread,
    /// whose windows it will offer.</summary>
    public ActivationListener(string dataFolder) {
        _message = SingleInstanceGuard.GetActivationMessage(dataFolder);
        CreateHandle(new CreateParams {
            Caption = SingleInstanceGuard.GetListenerTitle(dataFolder),
            Parent = SingleInstanceGuard.HWND_MESSAGE,
        });

        // A normal second launch may still reach an elevated instance this way.
        if (_message != 0 && !ChangeWindowMessageFilterEx(Handle, (uint)_message, MSGFLT_ALLOW, IntPtr.Zero)) {
            Log.Debug("SingleInstance: unable to allow the activation message (error {Error})",
                Marshal.GetLastPInvokeError());
        }
    }

    /// <inheritdoc />
    protected override void WndProc(ref Message m) {
        if (_message != 0 && m.Msg == _message) {
            m.Result = PrepareActivation();
            return;
        }

        base.WndProc(ref m);
    }

    /// <summary>Destroys the window, so a later launch no longer finds it.</summary>
    public void Dispose() => DestroyHandle();

    /// <summary>
    /// Finds this thread's main window, the main window or a startup message box, restores it if
    /// it's minimized, keeping a maximized window maximized, and returns its last active popup, so
    /// an open modal dialog gets focus rather than the disabled window behind it.
    /// </summary>
    /// <returns>The window to focus, or <see cref="IntPtr.Zero"/> when this thread shows none.</returns>
    private IntPtr PrepareActivation() {
        var root = FindMainWindow();
        if (root == IntPtr.Zero) {
            return IntPtr.Zero;
        }

        if (IsIconic(root)) {
            ShowWindow(root, SW_RESTORE);
        }

        var popup = GetLastActivePopup(root);
        return popup == IntPtr.Zero ? root : popup;
    }

    /// <summary>
    /// The first visible top-level window of this thread that no visible window owns. A form with
    /// <see cref="Form.ShowInTaskbar"/> off is owned by a hidden window, so it still counts.
    /// </summary>
    private IntPtr FindMainWindow() {
        var found = IntPtr.Zero;

        EnumThreadWindows(GetCurrentThreadId(), (window, _) => {
            if (window == Handle || !IsWindowVisible(window)) {
                return true;
            }

            var owner = GetWindow(window, GW_OWNER);
            if (owner != IntPtr.Zero && IsWindowVisible(owner)) {
                return true;
            }

            if ((GetWindowLongPtr(window, GWL_EXSTYLE).ToInt64() & WS_EX_TOOLWINDOW) != 0) {
                return true;
            }

            found = window;
            return false;
        }, IntPtr.Zero);

        return found;
    }

    private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

    private const uint MSGFLT_ALLOW = 1;
    private const int SW_RESTORE = 9;
    private const uint GW_OWNER = 4;
    private const int GWL_EXSTYLE = -20;
    private const long WS_EX_TOOLWINDOW = 0x80;

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool ChangeWindowMessageFilterEx(IntPtr hWnd, uint message, uint action, IntPtr pChangeFilterStruct);

    [DllImport("user32.dll")]
    private static extern bool EnumThreadWindows(uint dwThreadId, EnumWindowsProc lpfn, IntPtr lParam);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern bool IsWindowVisible(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

    [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
    private static extern IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex);

    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

    [DllImport("user32.dll")]
    private static extern IntPtr GetLastActivePopup(IntPtr hWnd);
}
