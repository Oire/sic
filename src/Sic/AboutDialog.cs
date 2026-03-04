using System.Globalization;
using GetText.WindowsForms;
using Microsoft.Win32;
using Oire.Sic.Utils;
using Oire.Sic.Utils.Constants;
using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

public partial class AboutDialog: Form {
    public AboutDialog() {
        InitializeComponent();
        Localizer.Localize(this, Localization.Catalog);
        versionLabel.Text = _("Version {0}", Application.ProductVersion);
        copyrightLabel.Text = _("\u00a9 {0} Oire Software", DateTime.Now.Year);
        repoLink.LinkClicked += RepoLink_LinkClicked;
        copyInfoButton.Click += CopyInfoButton_Click;
        copyInfoStatusTimer.Tick += CopyInfoStatusTimer_Tick;
        ActiveControl = okButton;
    }

    private void RepoLink_LinkClicked(object? sender, LinkLabelLinkClickedEventArgs e) {
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
            FileName = App.RepoUrl,
            UseShellExecute = true,
        });
    }

    private void CopyInfoButton_Click(object? sender, EventArgs e) {
        var info = string.Join(Environment.NewLine,
            "SIC! \u2014 Simple Image Converter",
            $"Version: {Application.ProductVersion}",
            $"OS: {GetFriendlyOsVersion()}",
            $".NET: {Environment.Version}",
            $"OS locale: {CultureInfo.InstalledUICulture.DisplayName}",
            $"App locale: {Localization.GetCurrentCulture().EnglishName}"
        );
        Clipboard.SetText(info);
        copyInfoStatusLabel.Text = _("Copied!");
        copyInfoStatusTimer.Start();
    }

    private static string GetFriendlyOsVersion() {
        try {
            using var key = Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion");
            if (key != null) {
                var productName = key.GetValue("ProductName") as string;
                var displayVersion = key.GetValue("DisplayVersion") as string;
                var buildNumber = key.GetValue("CurrentBuildNumber") as string;
                if (productName != null) {
                    // Registry ProductName is stuck at "Windows 10" on many Windows 11 machines.
                    // Windows 11 starts at build 22000.
                    if (int.TryParse(buildNumber, out var build) && build >= 22000) {
                        productName = productName.Replace("Windows 10", "Windows 11");
                    }
                    var parts = productName;
                    if (displayVersion != null) {
                        parts += $" {displayVersion}";
                    }
                    if (buildNumber != null) {
                        parts += $" (Build {buildNumber})";
                    }
                    return parts;
                }
            }
        } catch {
            // Fall through to default
        }
        return Environment.OSVersion.ToString();
    }

    private void CopyInfoStatusTimer_Tick(object? sender, EventArgs e) {
        copyInfoStatusTimer.Stop();
        copyInfoStatusLabel.Text = "";
    }
}
