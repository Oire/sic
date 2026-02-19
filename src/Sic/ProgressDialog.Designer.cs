using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

partial class ProgressDialog {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
        if (disposing && (components != null)) {
            components.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        MainLayout = new TableLayoutPanel();
        MessageLabel = new Label();
        ProgressBar = new ProgressBar();
        MainLayout.SuspendLayout();
        SuspendLayout();
        //
        // MainLayout
        //
        MainLayout.ColumnCount = 1;
        MainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        MainLayout.Controls.Add(MessageLabel, 0, 0);
        MainLayout.Controls.Add(ProgressBar, 0, 1);
        MainLayout.Dock = DockStyle.Fill;
        MainLayout.Location = new Point(0, 0);
        MainLayout.Name = "MainLayout";
        MainLayout.Padding = new Padding(20);
        MainLayout.RowCount = 2;
        MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 60F));
        MainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        MainLayout.Size = new Size(484, 161);
        MainLayout.TabIndex = 0;
        //
        // MessageLabel
        //
        MessageLabel.Anchor = AnchorStyles.None;
        MessageLabel.AutoSize = true;
        MessageLabel.Font = new Font("Segoe UI", 12F);
        MessageLabel.Location = new Point(191, 41);
        MessageLabel.Name = "MessageLabel";
        MessageLabel.Size = new Size(101, 21);
        MessageLabel.TabIndex = 0;
        MessageLabel.Text = "Please wait...";
        MessageLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // ProgressBar
        //
        ProgressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        ProgressBar.Location = new Point(23, 107);
        ProgressBar.Name = "ProgressBar";
        ProgressBar.Size = new Size(438, 30);
        ProgressBar.Style = ProgressBarStyle.Marquee;
        ProgressBar.TabIndex = 1;
        ProgressBar.MarqueeAnimationSpeed = 30;
        //
        // ProgressDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(484, 161);
        ControlBox = false;
        Controls.Add(MainLayout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ProgressDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = _("Converting...");
        AccessibleRole = AccessibleRole.Dialog;
        MainLayout.ResumeLayout(false);
        MainLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel MainLayout;
    private Label MessageLabel;
    private ProgressBar ProgressBar;
}
