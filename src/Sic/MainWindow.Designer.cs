namespace Oire.Sic;

partial class MainWindow {
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
        mainLayout = new TableLayoutPanel();
        imageListView = new ListView();
        colFileName = new ColumnHeader();
        colFormat = new ColumnHeader();
        colDimensions = new ColumnHeader();
        colSize = new ColumnHeader();
        previewPictureBox = new PictureBox();
        controlsLayout = new TableLayoutPanel();
        formatLabel = new Label();
        formatComboBox = new ComboBox();
        resizeCheckBox = new CheckBox();
        widthLabel = new Label();
        widthNumeric = new NumericUpDown();
        dimensionSeparatorLabel = new Label();
        heightLabel = new Label();
        heightNumeric = new NumericUpDown();
        buttonsLayout = new TableLayoutPanel();
        addFileButton = new Button();
        addUrlButton = new Button();
        removeButton = new Button();
        settingsButton = new Button();
        convertButton = new Button();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();
        progressBar = new ToolStripProgressBar();

        mainLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        controlsLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)widthNumeric).BeginInit();
        ((System.ComponentModel.ISupportInitialize)heightNumeric).BeginInit();
        buttonsLayout.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout
        //
        mainLayout.ColumnCount = 2;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.RowCount = 4;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(0, 0);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(8);
        mainLayout.Controls.Add(imageListView, 0, 0);
        mainLayout.Controls.Add(previewPictureBox, 1, 0);
        mainLayout.Controls.Add(controlsLayout, 0, 1);
        mainLayout.Controls.Add(buttonsLayout, 0, 2);
        mainLayout.Controls.Add(statusStrip, 0, 3);
        mainLayout.SetColumnSpan(controlsLayout, 2);
        mainLayout.SetColumnSpan(buttonsLayout, 2);
        mainLayout.SetColumnSpan(statusStrip, 2);

        //
        // imageListView
        //
        imageListView.Columns.AddRange(new ColumnHeader[] { colFileName, colFormat, colDimensions, colSize });
        imageListView.Dock = DockStyle.Fill;
        imageListView.FullRowSelect = true;
        imageListView.View = View.Details;
        imageListView.Name = "imageListView";
        imageListView.AccessibleName = "Image list";
        imageListView.AllowDrop = true;
        imageListView.TabIndex = 0;

        //
        // Column Headers
        //
        colFileName.Text = "File Name";
        colFileName.Width = 180;
        colFormat.Text = "Format";
        colFormat.Width = 60;
        colDimensions.Text = "Dimensions";
        colDimensions.Width = 90;
        colSize.Text = "Size";
        colSize.Width = 70;

        //
        // previewPictureBox
        //
        previewPictureBox.Dock = DockStyle.Fill;
        previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewPictureBox.BorderStyle = BorderStyle.FixedSingle;
        previewPictureBox.Name = "previewPictureBox";
        previewPictureBox.AccessibleName = "Image preview";
        previewPictureBox.TabIndex = 1;
        previewPictureBox.TabStop = false;

        //
        // controlsLayout
        //
        controlsLayout.ColumnCount = 8;
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Format label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F)); // Format combo
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Resize checkbox
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Width label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F)); // Width numeric
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // x label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Height label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F)); // Height numeric
        controlsLayout.RowCount = 1;
        controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlsLayout.Dock = DockStyle.Fill;
        controlsLayout.AutoSize = true;
        controlsLayout.Name = "controlsLayout";
        controlsLayout.Padding = new Padding(0, 4, 0, 4);
        controlsLayout.Controls.Add(formatLabel, 0, 0);
        controlsLayout.Controls.Add(formatComboBox, 1, 0);
        controlsLayout.Controls.Add(resizeCheckBox, 2, 0);
        controlsLayout.Controls.Add(widthLabel, 3, 0);
        controlsLayout.Controls.Add(widthNumeric, 4, 0);
        controlsLayout.Controls.Add(dimensionSeparatorLabel, 5, 0);
        controlsLayout.Controls.Add(heightLabel, 6, 0);
        controlsLayout.Controls.Add(heightNumeric, 7, 0);

        //
        // formatLabel
        //
        formatLabel.Text = "Target format:";
        formatLabel.AutoSize = true;
        formatLabel.Anchor = AnchorStyles.Left;
        formatLabel.Name = "formatLabel";
        formatLabel.TabIndex = 2;

        //
        // formatComboBox
        //
        formatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        formatComboBox.Dock = DockStyle.Fill;
        formatComboBox.Name = "formatComboBox";
        formatComboBox.AccessibleName = "Target format";
        formatComboBox.TabIndex = 3;

        //
        // resizeCheckBox
        //
        resizeCheckBox.Text = "Resize";
        resizeCheckBox.AutoSize = true;
        resizeCheckBox.Anchor = AnchorStyles.Left;
        resizeCheckBox.Name = "resizeCheckBox";
        resizeCheckBox.AccessibleName = "Resize images";
        resizeCheckBox.TabIndex = 4;
        resizeCheckBox.Padding = new Padding(8, 0, 0, 0);

        //
        // widthLabel
        //
        widthLabel.Text = "W:";
        widthLabel.AutoSize = true;
        widthLabel.Anchor = AnchorStyles.Left;
        widthLabel.Name = "widthLabel";
        widthLabel.Enabled = false;
        widthLabel.TabIndex = 5;

        //
        // widthNumeric
        //
        widthNumeric.Minimum = 1;
        widthNumeric.Maximum = 65535;
        widthNumeric.Value = 128;
        widthNumeric.Dock = DockStyle.Fill;
        widthNumeric.Enabled = false;
        widthNumeric.Name = "widthNumeric";
        widthNumeric.AccessibleName = "Width in pixels";
        widthNumeric.TabIndex = 6;

        //
        // dimensionSeparatorLabel
        //
        dimensionSeparatorLabel.Text = "x";
        dimensionSeparatorLabel.AutoSize = true;
        dimensionSeparatorLabel.Anchor = AnchorStyles.Left;
        dimensionSeparatorLabel.Name = "dimensionSeparatorLabel";
        dimensionSeparatorLabel.Enabled = false;
        dimensionSeparatorLabel.TabIndex = 7;

        //
        // heightLabel
        //
        heightLabel.Text = "H:";
        heightLabel.AutoSize = true;
        heightLabel.Anchor = AnchorStyles.Left;
        heightLabel.Name = "heightLabel";
        heightLabel.Enabled = false;
        heightLabel.TabIndex = 8;

        //
        // heightNumeric
        //
        heightNumeric.Minimum = 1;
        heightNumeric.Maximum = 65535;
        heightNumeric.Value = 128;
        heightNumeric.Dock = DockStyle.Fill;
        heightNumeric.Enabled = false;
        heightNumeric.Name = "heightNumeric";
        heightNumeric.AccessibleName = "Height in pixels";
        heightNumeric.TabIndex = 9;

        //
        // buttonsLayout
        //
        buttonsLayout.ColumnCount = 5;
        buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        buttonsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20F));
        buttonsLayout.RowCount = 1;
        buttonsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        buttonsLayout.Dock = DockStyle.Fill;
        buttonsLayout.AutoSize = true;
        buttonsLayout.Name = "buttonsLayout";
        buttonsLayout.Padding = new Padding(0, 4, 0, 4);
        buttonsLayout.Controls.Add(addFileButton, 0, 0);
        buttonsLayout.Controls.Add(addUrlButton, 1, 0);
        buttonsLayout.Controls.Add(removeButton, 2, 0);
        buttonsLayout.Controls.Add(settingsButton, 3, 0);
        buttonsLayout.Controls.Add(convertButton, 4, 0);

        //
        // addFileButton
        //
        addFileButton.Text = "Add File...";
        addFileButton.Dock = DockStyle.Fill;
        addFileButton.Name = "addFileButton";
        addFileButton.AccessibleName = "Add image file";
        addFileButton.TabIndex = 10;

        //
        // addUrlButton
        //
        addUrlButton.Text = "Add URL...";
        addUrlButton.Dock = DockStyle.Fill;
        addUrlButton.Name = "addUrlButton";
        addUrlButton.AccessibleName = "Add image from URL";
        addUrlButton.TabIndex = 11;

        //
        // removeButton
        //
        removeButton.Text = "Remove";
        removeButton.Dock = DockStyle.Fill;
        removeButton.Name = "removeButton";
        removeButton.AccessibleName = "Remove selected images";
        removeButton.TabIndex = 12;

        //
        // settingsButton
        //
        settingsButton.Text = "Settings...";
        settingsButton.Dock = DockStyle.Fill;
        settingsButton.Name = "settingsButton";
        settingsButton.AccessibleName = "Open settings";
        settingsButton.TabIndex = 13;

        //
        // convertButton
        //
        convertButton.Text = "Convert";
        convertButton.Dock = DockStyle.Fill;
        convertButton.Name = "convertButton";
        convertButton.AccessibleName = "Convert all images";
        convertButton.TabIndex = 14;
        convertButton.Font = new Font(convertButton.Font, FontStyle.Bold);

        //
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel, progressBar });
        statusStrip.Name = "statusStrip";
        statusStrip.TabIndex = 15;
        statusStrip.Dock = DockStyle.Fill;

        //
        // statusLabel
        //
        statusLabel.Name = "statusLabel";
        statusLabel.Text = "Ready";
        statusLabel.Spring = true;
        statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
        statusLabel.AccessibleName = "Status";

        //
        // progressBar
        //
        progressBar.Name = "progressBar";
        progressBar.Visible = false;
        progressBar.Size = new Size(150, 16);
        progressBar.AccessibleName = "Conversion progress";

        //
        // MainWindow
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 550);
        Controls.Add(mainLayout);
        Name = "MainWindow";
        Text = "SIC! — Simple Image Converter";
        MinimumSize = new Size(640, 400);

        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        controlsLayout.ResumeLayout(false);
        controlsLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)widthNumeric).EndInit();
        ((System.ComponentModel.ISupportInitialize)heightNumeric).EndInit();
        buttonsLayout.ResumeLayout(false);
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private ListView imageListView;
    private ColumnHeader colFileName;
    private ColumnHeader colFormat;
    private ColumnHeader colDimensions;
    private ColumnHeader colSize;
    private PictureBox previewPictureBox;
    private TableLayoutPanel controlsLayout;
    private Label formatLabel;
    private ComboBox formatComboBox;
    private CheckBox resizeCheckBox;
    private Label widthLabel;
    private NumericUpDown widthNumeric;
    private Label dimensionSeparatorLabel;
    private Label heightLabel;
    private NumericUpDown heightNumeric;
    private TableLayoutPanel buttonsLayout;
    private Button addFileButton;
    private Button addUrlButton;
    private Button removeButton;
    private Button settingsButton;
    private Button convertButton;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;
    private ToolStripProgressBar progressBar;
}
