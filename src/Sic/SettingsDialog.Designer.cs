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
        tabControl = new TabControl();

        // General tab
        generalTab = new TabPage();
        generalLayout = new TableLayoutPanel();
        languageLabel = new Label();
        languageComboBox = new ComboBox();
        confirmExitCheckBox = new CheckBox();
        checkUpdatesOnStartupCheckBox = new CheckBox();
        updateIntervalLabel = new Label();
        updateIntervalComboBox = new ComboBox();

        // Images tab
        imagesTab = new TabPage();
        imagesLayout = new TableLayoutPanel();
        outputFolderLabel = new Label();
        outputFolderTextBox = new TextBox();
        browseButton = new Button();
        clearOutputFolderButton = new Button();
        saveToSourceFolderCheckBox = new CheckBox();
        detectClipboardCheckBox = new CheckBox();
        formatsGroupBox = new GroupBox();
        formatsPanel = new TableLayoutPanel();

        okButton = new Button();
        cancelButton = new Button();

        mainLayout.SuspendLayout();
        tabControl.SuspendLayout();
        generalTab.SuspendLayout();
        generalLayout.SuspendLayout();
        imagesTab.SuspendLayout();
        imagesLayout.SuspendLayout();
        formatsGroupBox.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — 2 cols × 2 rows. Tab control fills row 0 spanning both cols;
        // OK (col 0) / Cancel (col 1) sit directly in row 1. No nested button row.
        //
        mainLayout.ColumnCount = 2;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.RowCount = 2;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        mainLayout.Controls.Add(tabControl, 0, 0);
        mainLayout.SetColumnSpan(tabControl, 2);
        mainLayout.Controls.Add(okButton, 0, 1);
        mainLayout.Controls.Add(cancelButton, 1, 1);

        //
        // tabControl
        //
        tabControl.Dock = DockStyle.Fill;
        tabControl.Name = "tabControl";
        tabControl.TabPages.AddRange(new TabPage[] { generalTab, imagesTab });
        tabControl.TabIndex = 0;

        //
        // generalTab — language, exit confirmation, and update preferences.
        // TabPage.Text doesn't interpret & as a mnemonic (unlike menus/buttons) — it would
        // render literally as "&General". Tab navigation uses Ctrl+Tab / Ctrl+PgUp/Dn,
        // which screen readers announce correctly.
        //
        generalTab.Text = "General";
        generalTab.Name = "generalTab";
        generalTab.UseVisualStyleBackColor = true;
        generalTab.Padding = new Padding(8);
        generalTab.Controls.Add(generalLayout);

        // generalLayout — 2 cols × 5 rows. Col 0 AutoSize for labels, col 1 stretches.
        // Last row is a stretcher that absorbs extra vertical space.
        generalLayout.ColumnCount = 2;
        generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        generalLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        generalLayout.RowCount = 5;
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: language
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: confirm on exit
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: check updates on startup
        generalLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: background update interval
        generalLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 4: filler
        generalLayout.Dock = DockStyle.Fill;
        generalLayout.Name = "generalLayout";

        generalLayout.Controls.Add(languageLabel, 0, 0);
        generalLayout.Controls.Add(languageComboBox, 1, 0);
        generalLayout.Controls.Add(confirmExitCheckBox, 0, 1);
        generalLayout.SetColumnSpan(confirmExitCheckBox, 2);
        generalLayout.Controls.Add(checkUpdatesOnStartupCheckBox, 0, 2);
        generalLayout.SetColumnSpan(checkUpdatesOnStartupCheckBox, 2);
        generalLayout.Controls.Add(updateIntervalLabel, 0, 3);
        generalLayout.Controls.Add(updateIntervalComboBox, 1, 3);

        //
        // languageLabel
        //
        languageLabel.Text = "&Language:";
        languageLabel.AutoSize = true;
        languageLabel.Anchor = AnchorStyles.Left;
        languageLabel.Margin = new Padding(3, 6, 8, 3);
        languageLabel.Name = "languageLabel";
        languageLabel.TabIndex = 0;

        //
        // languageComboBox
        //
        languageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        languageComboBox.Dock = DockStyle.Fill;
        languageComboBox.Name = "languageComboBox";
        languageComboBox.TabIndex = 1;

        //
        // confirmExitCheckBox
        //
        confirmExitCheckBox.Text = "Confirm on exit with non-empty &queue";
        confirmExitCheckBox.AutoSize = true;
        confirmExitCheckBox.Anchor = AnchorStyles.Left;
        confirmExitCheckBox.Margin = new Padding(3, 8, 3, 3);
        confirmExitCheckBox.Name = "confirmExitCheckBox";
        confirmExitCheckBox.TabIndex = 2;

        //
        // checkUpdatesOnStartupCheckBox
        //
        checkUpdatesOnStartupCheckBox.Text = "Check for &updates on startup";
        checkUpdatesOnStartupCheckBox.AutoSize = true;
        checkUpdatesOnStartupCheckBox.Anchor = AnchorStyles.Left;
        checkUpdatesOnStartupCheckBox.Margin = new Padding(3, 8, 3, 3);
        checkUpdatesOnStartupCheckBox.Name = "checkUpdatesOnStartupCheckBox";
        checkUpdatesOnStartupCheckBox.TabIndex = 3;

        //
        // updateIntervalLabel
        //
        updateIntervalLabel.Text = "Check for updates in the back&ground:";
        updateIntervalLabel.AutoSize = true;
        updateIntervalLabel.Anchor = AnchorStyles.Left;
        updateIntervalLabel.Margin = new Padding(3, 9, 8, 3);
        updateIntervalLabel.Name = "updateIntervalLabel";
        updateIntervalLabel.TabIndex = 4;

        //
        // updateIntervalComboBox
        //
        updateIntervalComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        updateIntervalComboBox.Dock = DockStyle.Fill;
        updateIntervalComboBox.Margin = new Padding(3, 6, 3, 3);
        updateIntervalComboBox.Name = "updateIntervalComboBox";
        updateIntervalComboBox.TabIndex = 5;

        //
        // imagesTab — where images come from and where converted files go.
        //
        imagesTab.Text = "Images";
        imagesTab.Name = "imagesTab";
        imagesTab.UseVisualStyleBackColor = true;
        imagesTab.Padding = new Padding(8);
        imagesTab.Controls.Add(imagesLayout);

        // imagesLayout — 4 cols × 4 rows. Row 0 is the output-folder line (label, path,
        // Browse, Reset); row 1 is the save-next-to-original toggle; row 2 is the clipboard
        // toggle; row 3 is the target-formats group box, which stretches to fill the tab.
        // Rows 1-3 span all columns.
        imagesLayout.ColumnCount = 4;
        imagesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));     // label
        imagesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F)); // path
        imagesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));     // browse
        imagesLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));     // reset
        imagesLayout.RowCount = 4;
        imagesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: output folder
        imagesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: save to source folder
        imagesLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: detect clipboard
        imagesLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F)); // 3: formats group
        imagesLayout.Dock = DockStyle.Fill;
        imagesLayout.Name = "imagesLayout";

        imagesLayout.Controls.Add(outputFolderLabel, 0, 0);
        imagesLayout.Controls.Add(outputFolderTextBox, 1, 0);
        imagesLayout.Controls.Add(browseButton, 2, 0);
        imagesLayout.Controls.Add(clearOutputFolderButton, 3, 0);
        imagesLayout.Controls.Add(saveToSourceFolderCheckBox, 0, 1);
        imagesLayout.SetColumnSpan(saveToSourceFolderCheckBox, 4);
        imagesLayout.Controls.Add(detectClipboardCheckBox, 0, 2);
        imagesLayout.SetColumnSpan(detectClipboardCheckBox, 4);
        imagesLayout.Controls.Add(formatsGroupBox, 0, 3);
        imagesLayout.SetColumnSpan(formatsGroupBox, 4);

        //
        // outputFolderLabel
        //
        outputFolderLabel.Text = "Output &Folder:";
        outputFolderLabel.AutoSize = true;
        outputFolderLabel.Anchor = AnchorStyles.Left;
        outputFolderLabel.Margin = new Padding(3, 7, 8, 3);
        outputFolderLabel.Name = "outputFolderLabel";
        outputFolderLabel.TabIndex = 0;

        //
        // outputFolderTextBox
        //
        outputFolderTextBox.Dock = DockStyle.Fill;
        outputFolderTextBox.ReadOnly = true;
        outputFolderTextBox.Margin = new Padding(3, 4, 3, 3);
        outputFolderTextBox.Name = "outputFolderTextBox";
        outputFolderTextBox.TabIndex = 1;

        //
        // browseButton
        //
        browseButton.Text = "&Browse...";
        browseButton.AutoSize = true;
        browseButton.Anchor = AnchorStyles.Left;
        browseButton.Name = "browseButton";
        browseButton.TabIndex = 2;

        //
        // clearOutputFolderButton
        //
        clearOutputFolderButton.Text = "&Reset";
        clearOutputFolderButton.AutoSize = true;
        clearOutputFolderButton.Anchor = AnchorStyles.Left;
        clearOutputFolderButton.Name = "clearOutputFolderButton";
        clearOutputFolderButton.TabIndex = 3;

        //
        // saveToSourceFolderCheckBox — when ticked, converted files go next to their originals
        // (issue #33); the output folder above still applies to clipboard and downloaded images.
        //
        saveToSourceFolderCheckBox.Text = "&Save converted images in the same folder as the original";
        saveToSourceFolderCheckBox.AutoSize = true;
        saveToSourceFolderCheckBox.Anchor = AnchorStyles.Left;
        saveToSourceFolderCheckBox.Margin = new Padding(3, 10, 3, 3);
        saveToSourceFolderCheckBox.Name = "saveToSourceFolderCheckBox";
        saveToSourceFolderCheckBox.TabIndex = 4;

        //
        // detectClipboardCheckBox
        //
        detectClipboardCheckBox.Text = "&Detect images in clipboard";
        detectClipboardCheckBox.AutoSize = true;
        detectClipboardCheckBox.Anchor = AnchorStyles.Left;
        detectClipboardCheckBox.Margin = new Padding(3, 10, 3, 3);
        detectClipboardCheckBox.Name = "detectClipboardCheckBox";
        detectClipboardCheckBox.TabIndex = 5;

        //
        // formatsGroupBox — its caption is the accessible group name screen readers announce when
        // focus enters the format checkboxes; the mnemonic jumps focus to the first checkbox.
        //
        formatsGroupBox.Text = "&Target formats to show in the list";
        formatsGroupBox.Dock = DockStyle.Fill;
        formatsGroupBox.Margin = new Padding(3, 9, 3, 3);
        formatsGroupBox.Padding = new Padding(8, 4, 8, 8);
        formatsGroupBox.Name = "formatsGroupBox";
        formatsGroupBox.TabIndex = 6;
        formatsGroupBox.Controls.Add(formatsPanel);

        //
        // formatsPanel — host for one real CheckBox per supported target format, added at runtime in
        // PopulateFormats(). Real checkboxes (rather than a CheckedListBox) are used so screen readers
        // announce each toggle; the boxes stack in a single auto-sizing column that scrolls if needed.
        //
        formatsPanel.Dock = DockStyle.Fill;
        formatsPanel.AutoScroll = true;
        formatsPanel.ColumnCount = 1;
        formatsPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        formatsPanel.Name = "formatsPanel";

        //
        // okButton
        //
        okButton.Text = "&OK";
        okButton.AutoSize = true;
        okButton.Anchor = AnchorStyles.Right;
        okButton.Margin = new Padding(0, 8, 6, 0);
        okButton.DialogResult = DialogResult.OK;
        okButton.Name = "okButton";
        okButton.TabIndex = 1;

        //
        // cancelButton
        //
        cancelButton.Text = "&Cancel";
        cancelButton.AutoSize = true;
        cancelButton.Anchor = AnchorStyles.Left;
        cancelButton.Margin = new Padding(6, 8, 0, 0);
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 2;

        //
        // SettingsDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(470, 380);
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
        tabControl.ResumeLayout(false);
        generalTab.ResumeLayout(false);
        generalTab.PerformLayout();
        generalLayout.ResumeLayout(false);
        generalLayout.PerformLayout();
        imagesTab.ResumeLayout(false);
        imagesTab.PerformLayout();
        imagesLayout.ResumeLayout(false);
        imagesLayout.PerformLayout();
        formatsGroupBox.ResumeLayout(false);
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private TabControl tabControl;

    private TabPage generalTab;
    private TableLayoutPanel generalLayout;
    private Label languageLabel;
    private ComboBox languageComboBox;
    private CheckBox confirmExitCheckBox;
    private CheckBox checkUpdatesOnStartupCheckBox;
    private Label updateIntervalLabel;
    private ComboBox updateIntervalComboBox;

    private TabPage imagesTab;
    private TableLayoutPanel imagesLayout;
    private Label outputFolderLabel;
    private TextBox outputFolderTextBox;
    private Button browseButton;
    private Button clearOutputFolderButton;
    private CheckBox saveToSourceFolderCheckBox;
    private CheckBox detectClipboardCheckBox;
    private GroupBox formatsGroupBox;
    private TableLayoutPanel formatsPanel;

    private Button okButton;
    private Button cancelButton;
}
