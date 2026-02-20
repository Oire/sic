using static Oire.Sic.Utils.Localization;

namespace Oire.Sic;

partial class AddUrlDialog {
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
        urlLabel = new Label();
        urlTextBox = new TextBox();
        okButton = new Button();
        cancelButton = new Button();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 2-row x 3-column grid
        //
        mainLayout.ColumnCount = 3;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.RowCount = 2;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        // Row 0: Label + URL text box (spanning cols 1-2)
        mainLayout.Controls.Add(urlLabel, 0, 0);
        mainLayout.Controls.Add(urlTextBox, 1, 0);
        mainLayout.SetColumnSpan(urlTextBox, 2);

        // Row 1: OK in col 0, gap in col 1, Cancel in col 2
        mainLayout.Controls.Add(okButton, 0, 1);
        mainLayout.Controls.Add(cancelButton, 2, 1);

        //
        // urlLabel
        //
        urlLabel.Text = _("URL:");
        urlLabel.AutoSize = true;
        urlLabel.Anchor = AnchorStyles.Left;
        urlLabel.Name = "urlLabel";
        urlLabel.TabIndex = 0;

        //
        // urlTextBox
        //
        urlTextBox.Dock = DockStyle.Fill;
        urlTextBox.Name = "urlTextBox";
        urlTextBox.TabIndex = 1;

        //
        // okButton
        //
        okButton.Text = _("OK");
        okButton.Anchor = AnchorStyles.Left;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 2;

        //
        // cancelButton
        //
        cancelButton.Text = _("Cancel");
        cancelButton.Anchor = AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 3;

        //
        // AddUrlDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(480, 100);
        Controls.Add(mainLayout);
        Name = "AddUrlDialog";
        Text = _("Add Image from URL");
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        AcceptButton = okButton;
        CancelButton = cancelButton;

        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private Label urlLabel;
    private TextBox urlTextBox;
    private Button okButton;
    private Button cancelButton;
}
