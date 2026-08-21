namespace Oire.Sic;

partial class FitToSizeDialog {
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
        maxSizeLabel = new Label();
        maxSizeTextBox = new TextBox();
        limitWidthCheckBox = new CheckBox();
        maxWidthLabel = new Label();
        maxWidthTextBox = new TextBox();
        okButton = new Button();
        cancelButton = new Button();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 4-row x 3-column grid
        //
        mainLayout.ColumnCount = 3;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.RowCount = 4;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        // Row 0: max file size label + text box (spanning cols 1-2)
        mainLayout.Controls.Add(maxSizeLabel, 0, 0);
        mainLayout.Controls.Add(maxSizeTextBox, 1, 0);
        mainLayout.SetColumnSpan(maxSizeTextBox, 2);

        // Row 1: limit-width check box (spanning all columns)
        mainLayout.Controls.Add(limitWidthCheckBox, 0, 1);
        mainLayout.SetColumnSpan(limitWidthCheckBox, 3);

        // Row 2: max width label + text box (spanning cols 1-2)
        mainLayout.Controls.Add(maxWidthLabel, 0, 2);
        mainLayout.Controls.Add(maxWidthTextBox, 1, 2);
        mainLayout.SetColumnSpan(maxWidthTextBox, 2);

        // Row 3: OK in col 0, gap in col 1, Cancel in col 2
        mainLayout.Controls.Add(okButton, 0, 3);
        mainLayout.Controls.Add(cancelButton, 2, 3);

        //
        // maxSizeLabel
        //
        maxSizeLabel.Text = "Maximum file size (&KB):";
        maxSizeLabel.AutoSize = true;
        maxSizeLabel.Anchor = AnchorStyles.Left;
        maxSizeLabel.Name = "maxSizeLabel";
        maxSizeLabel.TabIndex = 0;

        //
        // maxSizeTextBox
        //
        maxSizeTextBox.Dock = DockStyle.Fill;
        maxSizeTextBox.Name = "maxSizeTextBox";
        maxSizeTextBox.TabIndex = 1;

        //
        // limitWidthCheckBox
        //
        limitWidthCheckBox.Text = "&Limit width (keeps proportions)";
        limitWidthCheckBox.AutoSize = true;
        limitWidthCheckBox.Anchor = AnchorStyles.Left;
        limitWidthCheckBox.Name = "limitWidthCheckBox";
        limitWidthCheckBox.TabIndex = 2;

        //
        // maxWidthLabel
        //
        maxWidthLabel.Text = "Maximum &width (px):";
        maxWidthLabel.AutoSize = true;
        maxWidthLabel.Anchor = AnchorStyles.Left;
        maxWidthLabel.Name = "maxWidthLabel";
        maxWidthLabel.Enabled = false;
        maxWidthLabel.TabIndex = 3;

        //
        // maxWidthTextBox
        //
        maxWidthTextBox.Dock = DockStyle.Fill;
        maxWidthTextBox.Name = "maxWidthTextBox";
        maxWidthTextBox.Enabled = false;
        maxWidthTextBox.TabIndex = 4;

        //
        // okButton
        //
        okButton.Text = "&OK";
        okButton.Anchor = AnchorStyles.Left;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 5;

        //
        // cancelButton
        //
        cancelButton.Text = "&Cancel";
        cancelButton.Anchor = AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 6;

        //
        // FitToSizeDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(340, 150);
        Controls.Add(mainLayout);
        Name = "FitToSizeDialog";
        Text = "Fit to File Size";
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
    private Label maxSizeLabel;
    private TextBox maxSizeTextBox;
    private CheckBox limitWidthCheckBox;
    private Label maxWidthLabel;
    private TextBox maxWidthTextBox;
    private Button okButton;
    private Button cancelButton;
}
