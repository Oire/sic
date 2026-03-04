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
        repoLink = new LinkLabel();
        copyInfoButton = new Button();
        copyInfoStatusLabel = new Label();
        okButton = new Button();
        copyInfoStatusTimer = new System.Windows.Forms.Timer();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 7-row x 1-column grid
        //
        mainLayout.ColumnCount = 1;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.RowCount = 7;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
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
        mainLayout.Controls.Add(repoLink, 0, 3);
        mainLayout.Controls.Add(copyInfoButton, 0, 4);
        mainLayout.Controls.Add(copyInfoStatusLabel, 0, 5);
        mainLayout.Controls.Add(okButton, 0, 6);

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
        copyrightLabel.Padding = new Padding(0, 0, 0, 8);
        copyrightLabel.TabIndex = 2;

        //
        // repoLink
        //
        repoLink.Text = "GitHub repository";
        repoLink.AutoSize = true;
        repoLink.Anchor = AnchorStyles.None;
        repoLink.Name = "repoLink";
        repoLink.Padding = new Padding(0, 0, 0, 8);
        repoLink.TabIndex = 3;

        //
        // copyInfoButton
        //
        copyInfoButton.Text = "&Copy Info";
        copyInfoButton.MinimumSize = new Size(100, 0);
        copyInfoButton.Anchor = AnchorStyles.None;
        copyInfoButton.Name = "copyInfoButton";
        copyInfoButton.TabIndex = 4;

        //
        // copyInfoStatusLabel
        //
        copyInfoStatusLabel.Text = "";
        copyInfoStatusLabel.AutoSize = true;
        copyInfoStatusLabel.Anchor = AnchorStyles.None;
        copyInfoStatusLabel.Name = "copyInfoStatusLabel";
        copyInfoStatusLabel.Padding = new Padding(0, 0, 0, 8);
        copyInfoStatusLabel.TabIndex = 5;

        //
        // okButton
        //
        okButton.Text = "&OK";
        okButton.MinimumSize = new Size(80, 0);
        okButton.Anchor = AnchorStyles.None;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 6;

        //
        // copyInfoStatusTimer
        //
        copyInfoStatusTimer.Interval = 2000;

        //
        // AboutDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(380, 280);
        Controls.Add(mainLayout);
        Name = "AboutDialog";
        Text = "About SIC!";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        AcceptButton = okButton;
        CancelButton = okButton;
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
    private LinkLabel repoLink;
    private Button copyInfoButton;
    private Label copyInfoStatusLabel;
    private Button okButton;
    private System.Windows.Forms.Timer copyInfoStatusTimer;
}
