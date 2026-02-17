namespace Oire.Sic;

public partial class AboutDialog: Form {
    public AboutDialog() {
        InitializeComponent();
        versionLabel.Text = $"Version {Application.ProductVersion}";
        copyrightLabel.Text = $"\u00a9 {DateTime.Now.Year} Oire Software";
    }
}
