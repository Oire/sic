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
        menuStrip = new MenuStrip();

        // File menu
        fileMenu = new ToolStripMenuItem();
        addImageMenuItem = new ToolStripMenuItem();
        addFolderMenuItem = new ToolStripMenuItem();
        addFromUrlMenuItem = new ToolStripMenuItem();
        removeMenuItem = new ToolStripMenuItem();
        removeAllMenuItem = new ToolStripMenuItem();
        optionsMenuItem = new ToolStripMenuItem();
        exitMenuItem = new ToolStripMenuItem();

        // Help menu
        helpMenu = new ToolStripMenuItem();
        userGuideMenuItem = new ToolStripMenuItem();
        aboutMenuItem = new ToolStripMenuItem();

        mainLayout = new TableLayoutPanel();
        imageListView = new ListView();
        colFileName = new ColumnHeader();
        colFormat = new ColumnHeader();
        colDimensions = new ColumnHeader();
        colSize = new ColumnHeader();
        colStatus = new ColumnHeader();
        previewPictureBox = new PictureBox();
        controlsLayout = new TableLayoutPanel();
        formatLabel = new Label();
        formatComboBox = new ComboBox();
        resizeCheckBox = new CheckBox();
        widthLabel = new Label();
        widthTextBox = new TextBox();
        dimensionSeparatorLabel = new Label();
        heightLabel = new Label();
        heightTextBox = new TextBox();
        convertButton = new Button();
        keepProportionsRadioButton = new RadioButton();
        cropRadioButton = new RadioButton();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();



        menuStrip.SuspendLayout();
        mainLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        controlsLayout.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        //
        // menuStrip
        //
        menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, helpMenu });
        menuStrip.Name = "menuStrip";

        //
        // fileMenu
        //
        fileMenu.Text = "&File";
        fileMenu.Name = "fileMenu";
        fileMenu.DropDownItems.AddRange(new ToolStripItem[] {
            addImageMenuItem,
            addFolderMenuItem,
            addFromUrlMenuItem,
            new ToolStripSeparator(),
            removeMenuItem,
            removeAllMenuItem,
            new ToolStripSeparator(),
            optionsMenuItem,
            new ToolStripSeparator(),
            exitMenuItem,
        });

        //
        // addImageMenuItem
        //
        addImageMenuItem.Text = "Add &Image...";
        addImageMenuItem.ShortcutKeys = Keys.Control | Keys.O;
        addImageMenuItem.Name = "addImageMenuItem";

        //
        // addFolderMenuItem
        //
        addFolderMenuItem.Text = "Add F&older...";
        addFolderMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.O;
        addFolderMenuItem.Name = "addFolderMenuItem";

        //
        // addFromUrlMenuItem
        //
        addFromUrlMenuItem.Text = "Add from &URL...";
        addFromUrlMenuItem.ShortcutKeys = Keys.Control | Keys.U;
        addFromUrlMenuItem.Name = "addFromUrlMenuItem";

        //
        // removeMenuItem
        //
        removeMenuItem.Text = "&Remove";
        removeMenuItem.ShortcutKeyDisplayString = "Del";
        removeMenuItem.Name = "removeMenuItem";
        removeMenuItem.Enabled = false;

        //
        // removeAllMenuItem
        //
        removeAllMenuItem.Text = "Remove &All";
        removeAllMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.Delete;
        removeAllMenuItem.Name = "removeAllMenuItem";
        removeAllMenuItem.Enabled = false;

        //
        // optionsMenuItem
        //
        optionsMenuItem.Text = "&Options...";
        optionsMenuItem.ShortcutKeys = Keys.Control | Keys.Oemcomma;
        optionsMenuItem.Name = "optionsMenuItem";

        //
        // exitMenuItem
        //
        exitMenuItem.Text = "E&xit";
        exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
        exitMenuItem.Name = "exitMenuItem";

        //
        // helpMenu
        //
        helpMenu.Text = "&Help";
        helpMenu.Name = "helpMenu";
        helpMenu.DropDownItems.AddRange(new ToolStripItem[] {
            userGuideMenuItem,
            new ToolStripSeparator(),
            aboutMenuItem,
        });

        //
        // userGuideMenuItem
        //
        userGuideMenuItem.Text = "&User Guide";
        userGuideMenuItem.ShortcutKeys = Keys.F1;
        userGuideMenuItem.Name = "userGuideMenuItem";

        //
        // aboutMenuItem
        //
        aboutMenuItem.Text = "&About SIC!...";
        aboutMenuItem.ShortcutKeys = Keys.Shift | Keys.F1;
        aboutMenuItem.Name = "aboutMenuItem";

        //
        // mainLayout
        //
        mainLayout.ColumnCount = 2;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        mainLayout.RowCount = 3;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(0, 0);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(8);
        mainLayout.Controls.Add(imageListView, 0, 0);
        mainLayout.Controls.Add(previewPictureBox, 1, 0);
        mainLayout.Controls.Add(controlsLayout, 0, 1);
        mainLayout.Controls.Add(statusStrip, 0, 2);
        mainLayout.SetColumnSpan(controlsLayout, 2);
        mainLayout.SetColumnSpan(statusStrip, 2);

        //
        // imageListView
        //
        imageListView.Columns.AddRange(new ColumnHeader[] { colFileName, colFormat, colDimensions, colSize, colStatus });
        imageListView.Dock = DockStyle.Fill;
        imageListView.FullRowSelect = true;
        imageListView.MultiSelect = false;
        imageListView.View = View.Details;
        imageListView.Name = "imageListView";
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
        colStatus.Text = "Status";
        colStatus.Width = 90;

        //
        // previewPictureBox
        //
        previewPictureBox.Dock = DockStyle.Fill;
        previewPictureBox.SizeMode = PictureBoxSizeMode.Zoom;
        previewPictureBox.BorderStyle = BorderStyle.FixedSingle;
        previewPictureBox.Name = "previewPictureBox";
        previewPictureBox.TabIndex = 1;
        previewPictureBox.TabStop = false;

        //
        // controlsLayout
        //
        controlsLayout.ColumnCount = 9;
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Format label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 100F)); // Format combo
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Resize checkbox
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Width label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F)); // Width numeric
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // x label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Height label
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 80F)); // Height numeric
        controlsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); // Convert button
        controlsLayout.RowCount = 2;
        controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        controlsLayout.Dock = DockStyle.Fill;
        controlsLayout.AutoSize = true;
        controlsLayout.Name = "controlsLayout";
        controlsLayout.Padding = new Padding(0, 4, 0, 4);
        controlsLayout.Controls.Add(formatLabel, 0, 0);
        controlsLayout.Controls.Add(formatComboBox, 1, 0);
        controlsLayout.Controls.Add(resizeCheckBox, 2, 0);
        controlsLayout.Controls.Add(widthLabel, 3, 0);
        controlsLayout.Controls.Add(widthTextBox, 4, 0);
        controlsLayout.Controls.Add(dimensionSeparatorLabel, 5, 0);
        controlsLayout.Controls.Add(heightLabel, 6, 0);
        controlsLayout.Controls.Add(heightTextBox, 7, 0);
        controlsLayout.Controls.Add(convertButton, 8, 0);
        controlsLayout.Controls.Add(keepProportionsRadioButton, 2, 1);
        controlsLayout.SetColumnSpan(keepProportionsRadioButton, 3);
        controlsLayout.Controls.Add(cropRadioButton, 5, 1);
        controlsLayout.SetColumnSpan(cropRadioButton, 3);

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
        formatComboBox.TabIndex = 3;

        //
        // resizeCheckBox
        //
        resizeCheckBox.Text = "Resize";
        resizeCheckBox.AutoSize = true;
        resizeCheckBox.Anchor = AnchorStyles.Left;
        resizeCheckBox.Name = "resizeCheckBox";
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
        widthLabel.TabIndex = 7;

        //
        // widthTextBox
        //
        widthTextBox.Text = "";
        widthTextBox.Dock = DockStyle.Fill;
        widthTextBox.Enabled = false;
        widthTextBox.Name = "widthTextBox";
        widthTextBox.TabIndex = 8;

        //
        // dimensionSeparatorLabel
        //
        dimensionSeparatorLabel.Text = "x";
        dimensionSeparatorLabel.AutoSize = true;
        dimensionSeparatorLabel.Anchor = AnchorStyles.Left;
        dimensionSeparatorLabel.Name = "dimensionSeparatorLabel";
        dimensionSeparatorLabel.Enabled = false;
        dimensionSeparatorLabel.TabIndex = 9;

        //
        // heightLabel
        //
        heightLabel.Text = "H:";
        heightLabel.AutoSize = true;
        heightLabel.Anchor = AnchorStyles.Left;
        heightLabel.Name = "heightLabel";
        heightLabel.Enabled = false;
        heightLabel.TabIndex = 10;

        //
        // heightTextBox
        //
        heightTextBox.Text = "";
        heightTextBox.Dock = DockStyle.Fill;
        heightTextBox.Enabled = false;
        heightTextBox.Name = "heightTextBox";
        heightTextBox.TabIndex = 11;

        //
        // convertButton
        //
        convertButton.Text = "Convert";
        convertButton.Name = "convertButton";
        convertButton.Font = new Font(convertButton.Font, FontStyle.Bold);
        convertButton.Padding = new Padding(8, 0, 8, 0);
        convertButton.TabIndex = 12;

        //
        // keepProportionsRadioButton
        //
        keepProportionsRadioButton.Text = "Keep proportions";
        keepProportionsRadioButton.AutoSize = true;
        keepProportionsRadioButton.Anchor = AnchorStyles.Left;
        keepProportionsRadioButton.Name = "keepProportionsRadioButton";
        keepProportionsRadioButton.Checked = true;
        keepProportionsRadioButton.Enabled = false;
        keepProportionsRadioButton.TabIndex = 5;
        keepProportionsRadioButton.Padding = new Padding(8, 0, 0, 0);

        //
        // cropRadioButton
        //
        cropRadioButton.Text = "Crop";
        cropRadioButton.AutoSize = true;
        cropRadioButton.Anchor = AnchorStyles.Left;
        cropRadioButton.Name = "cropRadioButton";
        cropRadioButton.Enabled = false;
        cropRadioButton.TabIndex = 6;

        //
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Name = "statusStrip";
        statusStrip.TabIndex = 13;
        statusStrip.Dock = DockStyle.Fill;

        //
        // statusLabel
        //
        statusLabel.Name = "statusLabel";
        statusLabel.Text = "Ready";
        statusLabel.Spring = true;
        statusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;

        //
        // MainWindow
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(900, 550);
        Controls.Add(mainLayout);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "MainWindow";
        Text = "SIC! \u2014 Simple Image Converter";
        MinimumSize = new Size(640, 400);

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        controlsLayout.ResumeLayout(false);
        controlsLayout.PerformLayout();
        statusStrip.ResumeLayout(false);
        statusStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private MenuStrip menuStrip;
    private ToolStripMenuItem fileMenu;
    private ToolStripMenuItem addImageMenuItem;
    private ToolStripMenuItem addFolderMenuItem;
    private ToolStripMenuItem addFromUrlMenuItem;
    private ToolStripMenuItem removeMenuItem;
    private ToolStripMenuItem removeAllMenuItem;
    private ToolStripMenuItem optionsMenuItem;
    private ToolStripMenuItem exitMenuItem;
    private ToolStripMenuItem helpMenu;
    private ToolStripMenuItem userGuideMenuItem;
    private ToolStripMenuItem aboutMenuItem;
    private TableLayoutPanel mainLayout;
    private ListView imageListView;
    private ColumnHeader colFileName;
    private ColumnHeader colFormat;
    private ColumnHeader colDimensions;
    private ColumnHeader colSize;
    private ColumnHeader colStatus;
    private PictureBox previewPictureBox;
    private TableLayoutPanel controlsLayout;
    private Label formatLabel;
    private ComboBox formatComboBox;
    private CheckBox resizeCheckBox;
    private Label widthLabel;
    private TextBox widthTextBox;
    private Label dimensionSeparatorLabel;
    private Label heightLabel;
    private TextBox heightTextBox;
    private Button convertButton;
    private RadioButton keepProportionsRadioButton;
    private RadioButton cropRadioButton;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;


}
