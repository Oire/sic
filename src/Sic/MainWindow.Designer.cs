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
        if (disposing) {
            components?.Dispose();
            _previewDebounceTimer?.Dispose();
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
        optionsMenuItem = new ToolStripMenuItem();
        exitMenuItem = new ToolStripMenuItem();

        // Edit menu
        editMenu = new ToolStripMenuItem();
        removeMenuItem = new ToolStripMenuItem();
        removeAllMenuItem = new ToolStripMenuItem();

        // Convert menu
        convertMenu = new ToolStripMenuItem();
        convertSelectedMenuItem = new ToolStripMenuItem();
        convertAllMenuItem = new ToolStripMenuItem();
        createMultiSizeIcoMenuItem = new ToolStripMenuItem();

        // Help menu
        helpMenu = new ToolStripMenuItem();
        userGuideMenuItem = new ToolStripMenuItem();
        supportDevelopmentMenuItem = new ToolStripMenuItem();
        aboutMenuItem = new ToolStripMenuItem();

        mainLayout = new TableLayoutPanel();
        imageListView = new ListView();
        colFileName = new ColumnHeader();
        colFormat = new ColumnHeader();
        colDimensions = new ColumnHeader();
        colSize = new ColumnHeader();
        colStatus = new ColumnHeader();
        previewPictureBox = new PictureBox();
        formatLabel = new Label();
        formatComboBox = new ComboBox();
        resizeCheckBox = new CheckBox();
        resizeModeGroupBox = new GroupBox();
        resizeModeFlowLayout = new FlowLayoutPanel();
        resizeFieldsLayout = new TableLayoutPanel();
        keepProportionsRadioButton = new RadioButton();
        cropRadioButton = new RadioButton();
        widthLabel = new Label();
        widthTextBox = new TextBox();
        heightLabel = new Label();
        heightTextBox = new TextBox();
        convertSelectedButton = new Button();
        convertButton = new Button();
        statusStrip = new StatusStrip();
        statusLabel = new ToolStripStatusLabel();

        menuStrip.SuspendLayout();
        mainLayout.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).BeginInit();
        resizeModeGroupBox.SuspendLayout();
        resizeModeFlowLayout.SuspendLayout();
        resizeFieldsLayout.SuspendLayout();
        statusStrip.SuspendLayout();
        SuspendLayout();

        //
        // menuStrip
        //
        menuStrip.Items.AddRange(new ToolStripItem[] { fileMenu, editMenu, convertMenu, helpMenu });
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
            optionsMenuItem,
            new ToolStripSeparator(),
            exitMenuItem,
        });

        //
        // addImageMenuItem
        //
        addImageMenuItem.Text = "Add &Image...";
        addImageMenuItem.ShortcutKeys = Keys.Control | Keys.N;
        addImageMenuItem.Name = "addImageMenuItem";

        //
        // addFolderMenuItem
        //
        addFolderMenuItem.Text = "Add F&older...";
        addFolderMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.N;
        addFolderMenuItem.Name = "addFolderMenuItem";

        //
        // addFromUrlMenuItem
        //
        addFromUrlMenuItem.Text = "Add Image by &Link...";
        addFromUrlMenuItem.ShortcutKeys = Keys.Control | Keys.L;
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
        optionsMenuItem.Text = "&Settings...";
        optionsMenuItem.ShortcutKeys = Keys.Control | Keys.Oemcomma;
        optionsMenuItem.ShortcutKeyDisplayString = "Ctrl+,";
        optionsMenuItem.Name = "optionsMenuItem";

        //
        // exitMenuItem
        //
        exitMenuItem.Text = "E&xit";
        exitMenuItem.ShortcutKeys = Keys.Alt | Keys.F4;
        exitMenuItem.Name = "exitMenuItem";

        //
        // editMenu
        //
        editMenu.Text = "&Edit";
        editMenu.Name = "editMenu";
        editMenu.DropDownItems.AddRange(new ToolStripItem[] {
            removeMenuItem,
            removeAllMenuItem,
        });

        //
        // convertMenu
        //
        convertMenu.Text = "&Convert";
        convertMenu.Name = "convertMenu";
        convertMenu.DropDownItems.AddRange(new ToolStripItem[] {
            convertSelectedMenuItem,
            convertAllMenuItem,
            new ToolStripSeparator(),
            createMultiSizeIcoMenuItem,
        });

        //
        // convertSelectedMenuItem
        //
        convertSelectedMenuItem.Text = "Convert &Selected";
        convertSelectedMenuItem.ShortcutKeys = Keys.F5;
        convertSelectedMenuItem.Name = "convertSelectedMenuItem";
        convertSelectedMenuItem.Enabled = false;

        //
        // convertAllMenuItem
        //
        convertAllMenuItem.Text = "Convert &All";
        convertAllMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.F5;
        convertAllMenuItem.Name = "convertAllMenuItem";
        convertAllMenuItem.Enabled = false;

        //
        // createMultiSizeIcoMenuItem
        //
        createMultiSizeIcoMenuItem.Text = "Create Multi-size &ICO...";
        createMultiSizeIcoMenuItem.Name = "createMultiSizeIcoMenuItem";
        createMultiSizeIcoMenuItem.ShortcutKeys = Keys.Control | Keys.Alt | Keys.F5;
        createMultiSizeIcoMenuItem.Enabled = false;

        //
        // helpMenu
        //
        helpMenu.Text = "&Help";
        helpMenu.Name = "helpMenu";
        helpMenu.DropDownItems.AddRange(new ToolStripItem[] {
            userGuideMenuItem,
            new ToolStripSeparator(),
            supportDevelopmentMenuItem,
            aboutMenuItem,
        });

        //
        // userGuideMenuItem
        //
        userGuideMenuItem.Text = "Read User &Manual";
        userGuideMenuItem.ShortcutKeys = Keys.F1;
        userGuideMenuItem.Name = "userGuideMenuItem";

        //
        // supportDevelopmentMenuItem
        //
        supportDevelopmentMenuItem.Text = "Support &Development...";
        supportDevelopmentMenuItem.Name = "supportDevelopmentMenuItem";
        supportDevelopmentMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.D;

        //
        // aboutMenuItem
        //
        aboutMenuItem.Text = "&About SIC!...";
        aboutMenuItem.ShortcutKeys = Keys.Shift | Keys.F1;
        aboutMenuItem.Name = "aboutMenuItem";

        //
        // mainLayout
        //
        mainLayout.ColumnCount = 4;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.RowCount = 5;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Location = new Point(0, 0);
        mainLayout.Name = "mainLayout";
        mainLayout.Padding = new Padding(8);
        // Row 0: ListView + Preview
        mainLayout.Controls.Add(imageListView, 0, 0);
        mainLayout.SetColumnSpan(imageListView, 2);
        mainLayout.Controls.Add(previewPictureBox, 2, 0);
        mainLayout.SetColumnSpan(previewPictureBox, 2);
        // Row 1: Format + Resize checkbox
        mainLayout.Controls.Add(formatLabel, 0, 1);
        mainLayout.Controls.Add(formatComboBox, 1, 1);
        mainLayout.Controls.Add(resizeCheckBox, 2, 1);
        mainLayout.SetColumnSpan(resizeCheckBox, 2);
        // Row 2: Resize mode GroupBox + Convert Selected
        mainLayout.Controls.Add(resizeModeGroupBox, 0, 2);
        mainLayout.SetColumnSpan(resizeModeGroupBox, 3);
        mainLayout.Controls.Add(convertSelectedButton, 3, 2);
        // Row 3: Width/Height fields + Convert All
        mainLayout.Controls.Add(resizeFieldsLayout, 0, 3);
        mainLayout.SetColumnSpan(resizeFieldsLayout, 3);
        mainLayout.Controls.Add(convertButton, 3, 3);
        // Row 4: Status strip
        mainLayout.Controls.Add(statusStrip, 0, 4);
        mainLayout.SetColumnSpan(statusStrip, 4);

        //
        // imageListView
        //
        imageListView.Columns.AddRange(new ColumnHeader[] { colFileName, colFormat, colDimensions, colSize, colStatus });
        imageListView.Dock = DockStyle.Fill;
        imageListView.FullRowSelect = true;
        imageListView.MultiSelect = false;
        imageListView.View = View.Details;
        imageListView.Name = "imageListView";
        imageListView.AccessibleName = "Images list";
        imageListView.AllowDrop = true;
        imageListView.TabIndex = 0;

        //
        // Column Headers
        //
        colFileName.Text = "File Name";
        colFileName.Width = -2;
        colFormat.Text = "Format";
        colFormat.Width = -2;
        colDimensions.Text = "Dimensions";
        colDimensions.Width = -2;
        colSize.Text = "Size";
        colSize.Width = -2;
        colStatus.Text = "Status";
        colStatus.Width = -2;

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
        // formatLabel
        //
        formatLabel.Text = "Target &Format:";
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
        resizeCheckBox.Text = "Resi&ze";
        resizeCheckBox.AutoSize = true;
        resizeCheckBox.Anchor = AnchorStyles.Left;
        resizeCheckBox.Name = "resizeCheckBox";
        resizeCheckBox.TabIndex = 4;

        //
        // resizeModeGroupBox
        //
        resizeModeGroupBox.Text = "Resize &Mode:";
        resizeModeGroupBox.AutoSize = true;
        resizeModeGroupBox.Dock = DockStyle.Fill;
        resizeModeGroupBox.Name = "resizeModeGroupBox";
        resizeModeGroupBox.Enabled = false;
        resizeModeGroupBox.TabIndex = 5;
        resizeModeGroupBox.Controls.Add(resizeModeFlowLayout);

        //
        // resizeModeFlowLayout
        //
        resizeModeFlowLayout.AutoSize = true;
        resizeModeFlowLayout.Dock = DockStyle.Fill;
        resizeModeFlowLayout.FlowDirection = FlowDirection.LeftToRight;
        resizeModeFlowLayout.Name = "resizeModeFlowLayout";
        resizeModeFlowLayout.Controls.Add(keepProportionsRadioButton);
        resizeModeFlowLayout.Controls.Add(cropRadioButton);

        //
        // widthLabel
        //
        widthLabel.Text = "&Width:";
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
        // heightLabel
        //
        heightLabel.Text = "&Height:";
        heightLabel.AutoSize = true;
        heightLabel.Anchor = AnchorStyles.Left;
        heightLabel.Name = "heightLabel";
        heightLabel.Enabled = false;
        heightLabel.TabIndex = 9;

        //
        // heightTextBox
        //
        heightTextBox.Text = "";
        heightTextBox.Dock = DockStyle.Fill;
        heightTextBox.Enabled = false;
        heightTextBox.Name = "heightTextBox";
        heightTextBox.TabIndex = 10;

        //
        // resizeFieldsLayout
        //
        resizeFieldsLayout.ColumnCount = 4;
        resizeFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        resizeFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        resizeFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        resizeFieldsLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
        resizeFieldsLayout.RowCount = 1;
        resizeFieldsLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        resizeFieldsLayout.AutoSize = true;
        resizeFieldsLayout.Dock = DockStyle.Fill;
        resizeFieldsLayout.Margin = new Padding(0);
        resizeFieldsLayout.Name = "resizeFieldsLayout";
        resizeFieldsLayout.Controls.Add(widthLabel, 0, 0);
        resizeFieldsLayout.Controls.Add(widthTextBox, 1, 0);
        resizeFieldsLayout.Controls.Add(heightLabel, 2, 0);
        resizeFieldsLayout.Controls.Add(heightTextBox, 3, 0);

        //
        // convertSelectedButton
        //
        convertSelectedButton.Text = "Convert &Selected";
        convertSelectedButton.AutoSize = true;
        convertSelectedButton.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
        convertSelectedButton.Name = "convertSelectedButton";
        convertSelectedButton.Enabled = false;
        convertSelectedButton.TabIndex = 11;

        //
        // convertButton
        //
        convertButton.Text = "Convert &All";
        convertButton.AutoSize = true;
        convertButton.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        convertButton.Name = "convertButton";
        convertButton.Font = new Font(convertButton.Font, FontStyle.Bold);
        convertButton.TabIndex = 12;

        //
        // keepProportionsRadioButton
        //
        keepProportionsRadioButton.Text = "&Keep proportions";
        keepProportionsRadioButton.AutoSize = true;
        keepProportionsRadioButton.Name = "keepProportionsRadioButton";
        keepProportionsRadioButton.Checked = true;
        keepProportionsRadioButton.TabIndex = 0;

        //
        // cropRadioButton
        //
        cropRadioButton.Text = "&Crop";
        cropRadioButton.AutoSize = true;
        cropRadioButton.Name = "cropRadioButton";
        cropRadioButton.TabIndex = 1;

        //
        // statusStrip
        //
        statusStrip.Items.AddRange(new ToolStripItem[] { statusLabel });
        statusStrip.Name = "statusStrip";
        statusStrip.TabIndex = 14;
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
        Text = "SIC! — Simple Image Converter";
        MinimumSize = new Size(640, 400);

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)previewPictureBox).EndInit();
        resizeFieldsLayout.ResumeLayout(false);
        resizeFieldsLayout.PerformLayout();
        resizeModeFlowLayout.ResumeLayout(false);
        resizeModeFlowLayout.PerformLayout();
        resizeModeGroupBox.ResumeLayout(false);
        resizeModeGroupBox.PerformLayout();
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
    private ToolStripMenuItem optionsMenuItem;
    private ToolStripMenuItem exitMenuItem;
    private ToolStripMenuItem editMenu;
    private ToolStripMenuItem removeMenuItem;
    private ToolStripMenuItem removeAllMenuItem;
    private ToolStripMenuItem convertMenu;
    private ToolStripMenuItem convertSelectedMenuItem;
    private ToolStripMenuItem convertAllMenuItem;
    private ToolStripMenuItem createMultiSizeIcoMenuItem;
    private ToolStripMenuItem helpMenu;
    private ToolStripMenuItem userGuideMenuItem;
    private ToolStripMenuItem supportDevelopmentMenuItem;
    private ToolStripMenuItem aboutMenuItem;
    private TableLayoutPanel mainLayout;
    private ListView imageListView;
    private ColumnHeader colFileName;
    private ColumnHeader colFormat;
    private ColumnHeader colDimensions;
    private ColumnHeader colSize;
    private ColumnHeader colStatus;
    private PictureBox previewPictureBox;
    private Label formatLabel;
    private ComboBox formatComboBox;
    private CheckBox resizeCheckBox;
    private GroupBox resizeModeGroupBox;
    private FlowLayoutPanel resizeModeFlowLayout;
    private TableLayoutPanel resizeFieldsLayout;
    private RadioButton keepProportionsRadioButton;
    private RadioButton cropRadioButton;
    private Label widthLabel;
    private TextBox widthTextBox;
    private Label heightLabel;
    private TextBox heightTextBox;
    private Button convertSelectedButton;
    private Button convertButton;
    private StatusStrip statusStrip;
    private ToolStripStatusLabel statusLabel;
}
