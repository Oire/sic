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
        if (disposing) {
            components?.Dispose();
            _cts.Dispose();
        }
        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        mainLayout = new TableLayoutPanel();
        messageLabel = new Label();
        progressBar = new ProgressBar();
        cancelOperationButton = new Button();
        mainLayout.SuspendLayout();
        SuspendLayout();
        //
        // mainLayout
        //
        mainLayout.ColumnCount = 1;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.Controls.Add(messageLabel, 0, 0);
        mainLayout.Controls.Add(progressBar, 0, 1);
        mainLayout.Controls.Add(cancelOperationButton, 0, 2);
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(0, 0);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(20);
        mainLayout.RowCount = 3;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 40F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 30F));
        mainLayout.Size = new Size(484, 201);
        mainLayout.TabIndex = 0;
        //
        // messageLabel
        //
        messageLabel.Anchor = AnchorStyles.None;
        messageLabel.AutoSize = true;
        messageLabel.Font = new Font("Segoe UI", 12F);
        messageLabel.Location = new Point(191, 41);
        messageLabel.Name = "messageLabel";
        messageLabel.Size = new Size(101, 21);
        messageLabel.TabIndex = 0;
        messageLabel.Text = "Please wait...";
        messageLabel.TextAlign = ContentAlignment.MiddleCenter;
        //
        // progressBar
        //
        progressBar.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        progressBar.Location = new Point(23, 107);
        progressBar.Name = "progressBar";
        progressBar.Size = new Size(438, 30);
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.TabIndex = 1;
        progressBar.TabStop = false;
        progressBar.MarqueeAnimationSpeed = 30;
        //
        // cancelOperationButton
        //
        cancelOperationButton.Anchor = AnchorStyles.None;
        cancelOperationButton.AutoSize = true;
        cancelOperationButton.Name = "cancelOperationButton";
        cancelOperationButton.Size = new Size(90, 30);
        cancelOperationButton.TabIndex = 2;
        cancelOperationButton.Text = "&Cancel";
        cancelOperationButton.UseVisualStyleBackColor = true;
        //
        // ProgressDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(484, 201);
        ControlBox = false;
        Controls.Add(mainLayout);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        Name = "ProgressDialog";
        StartPosition = FormStartPosition.CenterParent;
        Text = "Please wait...";
        AccessibleRole = AccessibleRole.Dialog;
        CancelButton = cancelOperationButton;
        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ResumeLayout(false);
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private Label messageLabel;
    private ProgressBar progressBar;
    private Button cancelOperationButton;
}
