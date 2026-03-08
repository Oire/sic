namespace Oire.Sic;

partial class SettingsDialog {
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
        outputFolderLabel = new Label();
        outputFolderTextBox = new TextBox();
        browseButton = new Button();
        clearOutputFolderButton = new Button();
        languageLabel = new Label();
        languageComboBox = new ComboBox();
        okButton = new Button();
        cancelButton = new Button();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 3-row x 4-column grid
        //
        mainLayout.ColumnCount = 4;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15F));
        mainLayout.RowCount = 4;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        // Row 0: Output folder
        mainLayout.Controls.Add(outputFolderLabel, 0, 0);
        mainLayout.Controls.Add(outputFolderTextBox, 1, 0);
        mainLayout.Controls.Add(browseButton, 2, 0);
        mainLayout.Controls.Add(clearOutputFolderButton, 3, 0);

        // Row 1: Language
        mainLayout.Controls.Add(languageLabel, 0, 1);
        mainLayout.Controls.Add(languageComboBox, 1, 1);

        // Row 2: Confirm exit with non-empty queue
        confirmExitCheckBox = new CheckBox();
        mainLayout.Controls.Add(confirmExitCheckBox, 0, 2);
        mainLayout.SetColumnSpan(confirmExitCheckBox, 4);

        // Row 3: OK / Cancel (right-aligned in cols 2-3)
        mainLayout.Controls.Add(okButton, 2, 3);
        mainLayout.Controls.Add(cancelButton, 3, 3);

        //
        // outputFolderLabel
        //
        outputFolderLabel.Text = "Output &Folder:";
        outputFolderLabel.AutoSize = true;
        outputFolderLabel.Anchor = AnchorStyles.Left;
        outputFolderLabel.Name = "outputFolderLabel";
        outputFolderLabel.TabIndex = 0;

        //
        // outputFolderTextBox
        //
        outputFolderTextBox.Dock = DockStyle.Fill;
        outputFolderTextBox.ReadOnly = true;
        outputFolderTextBox.Name = "outputFolderTextBox";
        outputFolderTextBox.TabIndex = 1;

        //
        // browseButton
        //
        browseButton.Text = "&Browse...";
        browseButton.Dock = DockStyle.Fill;
        browseButton.Name = "browseButton";
        browseButton.TabIndex = 2;

        //
        // clearOutputFolderButton
        //
        clearOutputFolderButton.Text = "&Reset";
        clearOutputFolderButton.Dock = DockStyle.Fill;
        clearOutputFolderButton.Name = "clearOutputFolderButton";
        clearOutputFolderButton.TabIndex = 3;

        //
        // languageLabel
        //
        languageLabel.Text = "&Language:";
        languageLabel.AutoSize = true;
        languageLabel.Anchor = AnchorStyles.Left;
        languageLabel.Name = "languageLabel";
        languageLabel.TabIndex = 4;

        //
        // languageComboBox
        //
        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.Dock = DockStyle.Fill;
        languageComboBox.Name = "languageComboBox";
        languageComboBox.TabIndex = 5;

        //
        // confirmExitCheckBox
        //
        confirmExitCheckBox.Text = "Confirm on exit with non-empty &queue";
        confirmExitCheckBox.AutoSize = true;
        confirmExitCheckBox.Anchor = AnchorStyles.Left;
        confirmExitCheckBox.Name = "confirmExitCheckBox";
        confirmExitCheckBox.TabIndex = 6;

        //
        // okButton
        //
        okButton.Text = "&OK";
        okButton.Dock = DockStyle.Fill;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 7;

        //
        // cancelButton
        //
        cancelButton.Text = "&Cancel";
        cancelButton.Dock = DockStyle.Fill;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 8;

        //
        // SettingsDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(450, 190);
        Controls.Add(mainLayout);
        Name = "SettingsDialog";
        Text = "Settings";
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
    private Label outputFolderLabel;
    private TextBox outputFolderTextBox;
    private Button browseButton;
    private Button clearOutputFolderButton;
    private Label languageLabel;
    private ComboBox languageComboBox;
    private CheckBox confirmExitCheckBox;
    private Button okButton;
    private Button cancelButton;
}
