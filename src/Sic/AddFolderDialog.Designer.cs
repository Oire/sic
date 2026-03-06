namespace Oire.Sic;

partial class AddFolderDialog {
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
        folderLabel = new Label();
        folderTextBox = new TextBox();
        browseButton = new Button();
        filterLabel = new Label();
        filterComboBox = new ComboBox();
        includeSubfoldersCheckBox = new CheckBox();
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

        // Row 0: Folder
        mainLayout.Controls.Add(folderLabel, 0, 0);
        mainLayout.Controls.Add(folderTextBox, 1, 0);
        mainLayout.Controls.Add(browseButton, 2, 0);

        // Row 1: Filter (spans cols 1-2)
        mainLayout.Controls.Add(filterLabel, 0, 1);
        mainLayout.Controls.Add(filterComboBox, 1, 1);
        mainLayout.SetColumnSpan(filterComboBox, 2);

        // Row 2: Include subfolders (spans cols 1-2)
        mainLayout.Controls.Add(includeSubfoldersCheckBox, 1, 2);
        mainLayout.SetColumnSpan(includeSubfoldersCheckBox, 2);

        // Row 3: OK / Cancel (in cols 1-2)
        mainLayout.Controls.Add(okButton, 1, 3);
        mainLayout.Controls.Add(cancelButton, 2, 3);

        //
        // folderLabel
        //
        folderLabel.Text = "&Folder:";
        folderLabel.AutoSize = true;
        folderLabel.Anchor = AnchorStyles.Left;
        folderLabel.Name = "folderLabel";
        folderLabel.TabIndex = 0;

        //
        // folderTextBox
        //
        folderTextBox.Dock = DockStyle.Fill;
        folderTextBox.ReadOnly = true;
        folderTextBox.Name = "folderTextBox";
        folderTextBox.TabIndex = 1;

        //
        // browseButton
        //
        browseButton.Text = "&Browse...";
        browseButton.Name = "browseButton";
        browseButton.TabIndex = 2;

        //
        // filterLabel
        //
        filterLabel.Text = "Fi&lter:";
        filterLabel.AutoSize = true;
        filterLabel.Anchor = AnchorStyles.Left;
        filterLabel.Name = "filterLabel";
        filterLabel.TabIndex = 3;

        //
        // filterComboBox
        //
        filterComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        filterComboBox.Dock = DockStyle.Fill;
        filterComboBox.Name = "filterComboBox";
        filterComboBox.TabIndex = 4;

        //
        // includeSubfoldersCheckBox
        //
        includeSubfoldersCheckBox.Text = "Include &All Subfolders";
        includeSubfoldersCheckBox.AutoSize = true;
        includeSubfoldersCheckBox.Anchor = AnchorStyles.Left;
        includeSubfoldersCheckBox.Name = "includeSubfoldersCheckBox";
        includeSubfoldersCheckBox.TabIndex = 5;
        includeSubfoldersCheckBox.Padding = new Padding(0, 4, 0, 4);

        //
        // okButton
        //
        okButton.Text = "&OK";
        okButton.Anchor = AnchorStyles.Right;
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 6;

        //
        // cancelButton
        //
        cancelButton.Text = "&Cancel";
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 7;

        //
        // AddFolderDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(480, 190);
        Controls.Add(mainLayout);
        Name = "AddFolderDialog";
        Text = "Add Folder";
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
    private Label folderLabel;
    private TextBox folderTextBox;
    private Button browseButton;
    private Label filterLabel;
    private ComboBox filterComboBox;
    private CheckBox includeSubfoldersCheckBox;
    private Button okButton;
    private Button cancelButton;
}
