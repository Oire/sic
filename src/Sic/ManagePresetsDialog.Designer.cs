namespace Oire.Sic;

partial class ManagePresetsDialog {
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
        presetsLabel = new Label();
        presetsListBox = new ListBox();
        addButton = new Button();
        editButton = new Button();
        removeButton = new Button();
        closeButton = new Button();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 3-row x 4-column grid
        //
        mainLayout.ColumnCount = 4;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25F));
        mainLayout.RowCount = 3;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        // Row 0: presets label spanning all columns
        mainLayout.Controls.Add(presetsLabel, 0, 0);
        mainLayout.SetColumnSpan(presetsLabel, 4);

        // Row 1: presets list spanning all columns
        mainLayout.Controls.Add(presetsListBox, 0, 1);
        mainLayout.SetColumnSpan(presetsListBox, 4);

        // Row 2: Add, Edit, Remove, Close
        mainLayout.Controls.Add(addButton, 0, 2);
        mainLayout.Controls.Add(editButton, 1, 2);
        mainLayout.Controls.Add(removeButton, 2, 2);
        mainLayout.Controls.Add(closeButton, 3, 2);

        //
        // presetsLabel
        //
        presetsLabel.Text = "Pre&sets:";
        presetsLabel.AutoSize = true;
        presetsLabel.Dock = DockStyle.Fill;
        presetsLabel.TextAlign = ContentAlignment.MiddleLeft;
        presetsLabel.Name = "presetsLabel";
        presetsLabel.TabIndex = 0;

        //
        // presetsListBox — a 64-character name plus its summary is wider than the dialog
        //
        presetsListBox.Dock = DockStyle.Fill;
        presetsListBox.SelectionMode = SelectionMode.One;
        presetsListBox.HorizontalScrollbar = true;
        presetsListBox.IntegralHeight = false;
        presetsListBox.Name = "presetsListBox";
        presetsListBox.TabIndex = 1;

        //
        // addButton
        //
        addButton.Text = "&Add...";
        addButton.Dock = DockStyle.Fill;
        addButton.Name = "addButton";
        addButton.TabIndex = 2;

        //
        // editButton
        //
        editButton.Text = "&Edit...";
        editButton.Dock = DockStyle.Fill;
        editButton.Enabled = false;
        editButton.Name = "editButton";
        editButton.TabIndex = 3;

        //
        // removeButton
        //
        removeButton.Text = "&Remove";
        removeButton.Dock = DockStyle.Fill;
        removeButton.Enabled = false;
        removeButton.Name = "removeButton";
        removeButton.TabIndex = 4;

        //
        // closeButton
        //
        closeButton.Text = "&Close";
        closeButton.Dock = DockStyle.Fill;
        closeButton.DialogResult = DialogResult.Cancel;
        closeButton.Name = "closeButton";
        closeButton.TabIndex = 5;

        //
        // ManagePresetsDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(440, 320);
        Controls.Add(mainLayout);
        Name = "ManagePresetsDialog";
        Text = "Manage Presets";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        AccessibleRole = AccessibleRole.Dialog;
        MaximizeBox = false;
        MinimizeBox = false;
        CancelButton = closeButton;

        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private Label presetsLabel;
    private ListBox presetsListBox;
    private Button addButton;
    private Button editButton;
    private Button removeButton;
    private Button closeButton;
}
