namespace Oire.Sic;

partial class IcoPresetDialog {
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
        presetGroupBox = new GroupBox();
        presetFlowLayout = new FlowLayoutPanel();
        faviconRadioButton = new RadioButton();
        appIconRadioButton = new RadioButton();
        customRadioButton = new RadioButton();
        sizesLabel = new Label();
        sizesListBox = new ListBox();
        addSizeButton = new Button();
        removeSizeButton = new Button();
        okButton = new Button();
        cancelButton = new Button();

        mainLayout.SuspendLayout();
        presetGroupBox.SuspendLayout();
        presetFlowLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — 4 rows x 3 columns
        //
        mainLayout.ColumnCount = 3;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.RowCount = 4;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        // Row 0: Preset group box spanning all 3 columns
        mainLayout.Controls.Add(presetGroupBox, 0, 0);
        mainLayout.SetColumnSpan(presetGroupBox, 3);

        // Row 1: Sizes label, list box (rowspan 2), add button
        mainLayout.Controls.Add(sizesLabel, 0, 1);
        mainLayout.Controls.Add(sizesListBox, 1, 1);
        mainLayout.SetRowSpan(sizesListBox, 2);
        mainLayout.Controls.Add(addSizeButton, 2, 1);

        // Row 2: (empty col 0), (listbox continues), remove button
        mainLayout.Controls.Add(removeSizeButton, 2, 2);

        // Row 3: OK button (col 0), cancel button (col 2)
        mainLayout.Controls.Add(okButton, 0, 3);
        mainLayout.Controls.Add(cancelButton, 2, 3);

        //
        // presetGroupBox
        //
        presetGroupBox.Text = "Pre&set:";
        presetGroupBox.AutoSize = true;
        presetGroupBox.Dock = DockStyle.Fill;
        presetGroupBox.Name = "presetGroupBox";
        presetGroupBox.TabIndex = 0;
        presetGroupBox.Controls.Add(presetFlowLayout);

        //
        // presetFlowLayout
        //
        presetFlowLayout.AutoSize = true;
        presetFlowLayout.Dock = DockStyle.Fill;
        presetFlowLayout.FlowDirection = FlowDirection.LeftToRight;
        presetFlowLayout.Name = "presetFlowLayout";
        presetFlowLayout.Controls.Add(faviconRadioButton);
        presetFlowLayout.Controls.Add(appIconRadioButton);
        presetFlowLayout.Controls.Add(customRadioButton);

        //
        // faviconRadioButton
        //
        faviconRadioButton.Text = "&Favicon";
        faviconRadioButton.AutoSize = true;
        faviconRadioButton.Checked = true;
        faviconRadioButton.Name = "faviconRadioButton";
        faviconRadioButton.TabIndex = 0;

        //
        // appIconRadioButton
        //
        appIconRadioButton.Text = "Application &Icon";
        appIconRadioButton.AutoSize = true;
        appIconRadioButton.Name = "appIconRadioButton";
        appIconRadioButton.TabIndex = 1;

        //
        // customRadioButton
        //
        customRadioButton.Text = "C&ustom";
        customRadioButton.AutoSize = true;
        customRadioButton.Name = "customRadioButton";
        customRadioButton.TabIndex = 2;

        //
        // sizesLabel
        //
        sizesLabel.Text = "Si&zes:";
        sizesLabel.AutoSize = true;
        sizesLabel.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        sizesLabel.Name = "sizesLabel";
        sizesLabel.Enabled = false;
        sizesLabel.TabIndex = 3;

        //
        // sizesListBox
        //
        sizesListBox.Dock = DockStyle.Fill;
        sizesListBox.SelectionMode = SelectionMode.One;
        sizesListBox.Name = "sizesListBox";
        sizesListBox.Enabled = false;
        sizesListBox.TabIndex = 4;

        //
        // addSizeButton
        //
        addSizeButton.Text = "&Add...";
        addSizeButton.AutoSize = true;
        addSizeButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        addSizeButton.Name = "addSizeButton";
        addSizeButton.Enabled = false;
        addSizeButton.TabIndex = 5;

        //
        // removeSizeButton
        //
        removeSizeButton.Text = "&Remove";
        removeSizeButton.AutoSize = true;
        removeSizeButton.Anchor = AnchorStyles.Left | AnchorStyles.Top;
        removeSizeButton.Name = "removeSizeButton";
        removeSizeButton.Enabled = false;
        removeSizeButton.TabIndex = 6;

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
        // IcoPresetDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(420, 320);
        Controls.Add(mainLayout);
        Name = "IcoPresetDialog";
        Text = "Create Multi-size ICO";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        AccessibleRole = AccessibleRole.Dialog;
        StartPosition = FormStartPosition.CenterParent;
        MaximizeBox = false;
        MinimizeBox = false;
        AcceptButton = okButton;
        CancelButton = cancelButton;

        presetFlowLayout.ResumeLayout(false);
        presetFlowLayout.PerformLayout();
        presetGroupBox.ResumeLayout(false);
        presetGroupBox.PerformLayout();
        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private GroupBox presetGroupBox;
    private FlowLayoutPanel presetFlowLayout;
    private RadioButton faviconRadioButton;
    private RadioButton appIconRadioButton;
    private RadioButton customRadioButton;
    private Label sizesLabel;
    private ListBox sizesListBox;
    private Button addSizeButton;
    private Button removeSizeButton;
    private Button okButton;
    private Button cancelButton;
}
