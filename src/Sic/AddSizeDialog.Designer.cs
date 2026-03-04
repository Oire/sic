namespace Oire.Sic;

partial class AddSizeDialog {
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
        sizeLabel = new Label();
        sizeTextBox = new TextBox();
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

        // Row 0: Label + size text box (spanning cols 1-2)
        mainLayout.Controls.Add(sizeLabel, 0, 0);
        mainLayout.Controls.Add(sizeTextBox, 1, 0);
        mainLayout.SetColumnSpan(sizeTextBox, 2);

        // Row 1: OK in col 0, gap in col 1, Cancel in col 2
        mainLayout.Controls.Add(okButton, 0, 1);
        mainLayout.Controls.Add(cancelButton, 2, 1);

        //
        // sizeLabel
        //
        sizeLabel.Text = "Si&ze (16–512):";
        sizeLabel.AutoSize = true;
        sizeLabel.Anchor = AnchorStyles.Left;
        sizeLabel.Name = "sizeLabel";
        sizeLabel.TabIndex = 0;

        //
        // sizeTextBox
        //
        sizeTextBox.Dock = DockStyle.Fill;
        sizeTextBox.Name = "sizeTextBox";
        sizeTextBox.TabIndex = 1;

        //
        // okButton
        //
        okButton.Text = "&OK";
        okButton.Anchor = AnchorStyles.Left;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 2;

        //
        // cancelButton
        //
        cancelButton.Text = "&Cancel";
        cancelButton.Anchor = AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 3;

        //
        // AddSizeDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(300, 100);
        Controls.Add(mainLayout);
        Name = "AddSizeDialog";
        Text = "Add Size";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        AccessibleRole = AccessibleRole.Dialog;
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
    private Label sizeLabel;
    private TextBox sizeTextBox;
    private Button okButton;
    private Button cancelButton;
}
