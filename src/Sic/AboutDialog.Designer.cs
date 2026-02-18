namespace Oire.Sic;

partial class AboutDialog {
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing) {
        if (disposing && (components != null)) {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent() {
        mainLayout = new TableLayoutPanel();
        appNameLabel = new Label();
        versionLabel = new Label();
        copyrightLabel = new Label();
        okButton = new Button();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 4-row x 1-column grid
        //
        mainLayout.ColumnCount = 1;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowCount = 4;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(16);
        mainLayout.Name = "mainLayout";

        mainLayout.Controls.Add(appNameLabel, 0, 0);
        mainLayout.Controls.Add(versionLabel, 0, 1);
        mainLayout.Controls.Add(copyrightLabel, 0, 2);
        mainLayout.Controls.Add(okButton, 0, 3);

        //
        // appNameLabel
        //
        appNameLabel.Text = "SIC! \u2014 Simple Image Converter";
        appNameLabel.AutoSize = true;
        appNameLabel.Font = new Font(appNameLabel.Font.FontFamily, 14F, FontStyle.Bold);
        appNameLabel.Anchor = AnchorStyles.None;
        appNameLabel.Name = "appNameLabel";
        appNameLabel.Padding = new Padding(0, 0, 0, 8);
        appNameLabel.TabIndex = 0;

        //
        // versionLabel
        //
        versionLabel.Text = "Version";
        versionLabel.AutoSize = true;
        versionLabel.Anchor = AnchorStyles.None;
        versionLabel.Name = "versionLabel";
        versionLabel.Padding = new Padding(0, 0, 0, 4);
        versionLabel.TabIndex = 1;

        //
        // copyrightLabel
        //
        copyrightLabel.Text = "Oire Software";
        copyrightLabel.AutoSize = true;
        copyrightLabel.Anchor = AnchorStyles.None;
        copyrightLabel.Name = "copyrightLabel";
        copyrightLabel.Padding = new Padding(0, 0, 0, 12);
        copyrightLabel.TabIndex = 2;

        //
        // okButton
        //
        okButton.Text = "OK";
        okButton.MinimumSize = new Size(80, 0);
        okButton.Anchor = AnchorStyles.None;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 3;

        //
        // AboutDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(380, 180);
        Controls.Add(mainLayout);
        Name = "AboutDialog";
        Text = "About SIC!";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        AcceptButton = okButton;
        AccessibleRole = AccessibleRole.Dialog;

        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private Label appNameLabel;
    private Label versionLabel;
    private Label copyrightLabel;
    private Button okButton;
}
