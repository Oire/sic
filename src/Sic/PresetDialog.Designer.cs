namespace Oire.Sic;

partial class PresetDialog {
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
        nameLabel = new Label();
        nameTextBox = new TextBox();
        formatLabel = new Label();
        formatComboBox = new ComboBox();
        resizeCheckBox = new CheckBox();
        resizeModeLabel = new Label();
        resizeModeComboBox = new ComboBox();
        widthLabel = new Label();
        widthTextBox = new TextBox();
        heightLabel = new Label();
        heightTextBox = new TextBox();
        hintLabel = new Label();
        okButton = new Button();
        cancelButton = new Button();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 8-row x 3-column grid
        //
        mainLayout.ColumnCount = 3;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.RowCount = 8;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        // Row 0: name label + text box (spanning cols 1-2)
        mainLayout.Controls.Add(nameLabel, 0, 0);
        mainLayout.Controls.Add(nameTextBox, 1, 0);
        mainLayout.SetColumnSpan(nameTextBox, 2);

        // Row 1: format label + dropdown (spanning cols 1-2)
        mainLayout.Controls.Add(formatLabel, 0, 1);
        mainLayout.Controls.Add(formatComboBox, 1, 1);
        mainLayout.SetColumnSpan(formatComboBox, 2);

        // Row 2: resize check box (spanning all columns)
        mainLayout.Controls.Add(resizeCheckBox, 0, 2);
        mainLayout.SetColumnSpan(resizeCheckBox, 3);

        // Row 3: resize mode label + dropdown (spanning cols 1-2)
        mainLayout.Controls.Add(resizeModeLabel, 0, 3);
        mainLayout.Controls.Add(resizeModeComboBox, 1, 3);
        mainLayout.SetColumnSpan(resizeModeComboBox, 2);

        // Row 4: width label + text box (spanning cols 1-2)
        mainLayout.Controls.Add(widthLabel, 0, 4);
        mainLayout.Controls.Add(widthTextBox, 1, 4);
        mainLayout.SetColumnSpan(widthTextBox, 2);

        // Row 5: height label + text box (spanning cols 1-2)
        mainLayout.Controls.Add(heightLabel, 0, 5);
        mainLayout.Controls.Add(heightTextBox, 1, 5);
        mainLayout.SetColumnSpan(heightTextBox, 2);

        // Row 6: hint (spanning all columns), absorbing spare height so the buttons keep theirs
        mainLayout.Controls.Add(hintLabel, 0, 6);
        mainLayout.SetColumnSpan(hintLabel, 3);

        // Row 7: empty cell, OK, Cancel
        mainLayout.Controls.Add(okButton, 1, 7);
        mainLayout.Controls.Add(cancelButton, 2, 7);

        //
        // nameLabel
        //
        nameLabel.Text = "&Name:";
        nameLabel.AutoSize = true;
        nameLabel.Dock = DockStyle.Fill;
        nameLabel.TextAlign = ContentAlignment.MiddleLeft;
        nameLabel.Name = "nameLabel";
        nameLabel.TabIndex = 0;

        //
        // nameTextBox
        //
        nameTextBox.Dock = DockStyle.Fill;
        nameTextBox.Name = "nameTextBox";
        nameTextBox.TabIndex = 1;

        //
        // formatLabel
        //
        formatLabel.Text = "Target &Format:";
        formatLabel.AutoSize = true;
        formatLabel.Dock = DockStyle.Fill;
        formatLabel.TextAlign = ContentAlignment.MiddleLeft;
        formatLabel.Name = "formatLabel";
        formatLabel.TabIndex = 2;

        //
        // formatComboBox
        //
        formatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        formatComboBox.Dock = DockStyle.Fill;
        formatComboBox.Name = "formatComboBox";
        formatComboBox.TabIndex = 3;

        //
        // resizeCheckBox
        //
        resizeCheckBox.Text = "Resi&ze";
        resizeCheckBox.AutoSize = true;
        resizeCheckBox.Dock = DockStyle.Fill;
        resizeCheckBox.Name = "resizeCheckBox";
        resizeCheckBox.TabIndex = 4;

        //
        // resizeModeLabel
        //
        resizeModeLabel.Text = "Resize &Mode:";
        resizeModeLabel.AutoSize = true;
        resizeModeLabel.Dock = DockStyle.Fill;
        resizeModeLabel.TextAlign = ContentAlignment.MiddleLeft;
        resizeModeLabel.Name = "resizeModeLabel";
        resizeModeLabel.Enabled = false;
        resizeModeLabel.TabIndex = 5;

        //
        // resizeModeComboBox
        //
        resizeModeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        resizeModeComboBox.Dock = DockStyle.Fill;
        resizeModeComboBox.Name = "resizeModeComboBox";
        resizeModeComboBox.Enabled = false;
        resizeModeComboBox.TabIndex = 6;

        //
        // widthLabel
        //
        widthLabel.Text = "&Width:";
        widthLabel.AutoSize = true;
        widthLabel.Dock = DockStyle.Fill;
        widthLabel.TextAlign = ContentAlignment.MiddleLeft;
        widthLabel.Name = "widthLabel";
        widthLabel.Enabled = false;
        widthLabel.TabIndex = 7;

        //
        // widthTextBox
        //
        widthTextBox.Dock = DockStyle.Fill;
        widthTextBox.Name = "widthTextBox";
        widthTextBox.Enabled = false;
        widthTextBox.TabIndex = 8;

        //
        // heightLabel
        //
        heightLabel.Text = "&Height:";
        heightLabel.AutoSize = true;
        heightLabel.Dock = DockStyle.Fill;
        heightLabel.TextAlign = ContentAlignment.MiddleLeft;
        heightLabel.Name = "heightLabel";
        heightLabel.Enabled = false;
        heightLabel.TabIndex = 9;

        //
        // heightTextBox
        //
        heightTextBox.Dock = DockStyle.Fill;
        heightTextBox.Name = "heightTextBox";
        heightTextBox.Enabled = false;
        heightTextBox.TabIndex = 10;

        //
        // hintLabel — not auto-sized, so it wraps inside its row
        //
        hintLabel.Text = "With Keep proportions, fill in one size to scale every image to it, or both to fit images inside that box.";
        hintLabel.AutoSize = false;
        hintLabel.Dock = DockStyle.Fill;
        hintLabel.TextAlign = ContentAlignment.TopLeft;
        hintLabel.Name = "hintLabel";
        hintLabel.TabIndex = 11;

        //
        // okButton
        //
        okButton.Text = "&OK";
        okButton.Dock = DockStyle.Fill;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 12;

        //
        // cancelButton
        //
        cancelButton.Text = "&Cancel";
        cancelButton.Dock = DockStyle.Fill;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 13;

        //
        // PresetDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(440, 300);
        Controls.Add(mainLayout);
        Name = "PresetDialog";
        Text = "Add Preset";
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
    private Label nameLabel;
    private TextBox nameTextBox;
    private Label formatLabel;
    private ComboBox formatComboBox;
    private CheckBox resizeCheckBox;
    private Label resizeModeLabel;
    private ComboBox resizeModeComboBox;
    private Label widthLabel;
    private TextBox widthTextBox;
    private Label heightLabel;
    private TextBox heightTextBox;
    private Label hintLabel;
    private Button okButton;
    private Button cancelButton;
}
