namespace Oire.Sic;

partial class FitToSizeResultsDialog {
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
        introLabel = new Label();
        resultsListBox = new ListBox();
        convertButton = new Button();
        cancelButton = new Button();

        mainLayout.SuspendLayout();
        SuspendLayout();

        //
        // mainLayout — flat 3-row x 3-column grid
        //
        mainLayout.ColumnCount = 3;
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
        mainLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        mainLayout.RowCount = 3;
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        mainLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        mainLayout.Dock = DockStyle.Fill;
        mainLayout.Padding = new Padding(12);
        mainLayout.Name = "mainLayout";

        // Row 0: intro label spanning all columns
        mainLayout.Controls.Add(introLabel, 0, 0);
        mainLayout.SetColumnSpan(introLabel, 3);

        // Row 1: results list spanning all columns
        mainLayout.Controls.Add(resultsListBox, 0, 1);
        mainLayout.SetColumnSpan(resultsListBox, 3);

        // Row 2: Convert in col 0, gap in col 1, Cancel in col 2
        mainLayout.Controls.Add(convertButton, 0, 2);
        mainLayout.Controls.Add(cancelButton, 2, 2);

        //
        // introLabel
        //
        introLabel.Text = "C&hoose a result to convert to:";
        introLabel.AutoSize = true;
        introLabel.Anchor = AnchorStyles.Left;
        introLabel.Name = "introLabel";
        introLabel.TabIndex = 0;

        //
        // resultsListBox
        //
        resultsListBox.Dock = DockStyle.Fill;
        resultsListBox.SelectionMode = SelectionMode.One;
        resultsListBox.Name = "resultsListBox";
        resultsListBox.TabIndex = 1;

        //
        // convertButton
        //
        convertButton.Text = "Con&vert";
        convertButton.Anchor = AnchorStyles.Left;
        convertButton.DialogResult = DialogResult.OK;
        convertButton.Name = "convertButton";
        convertButton.TabIndex = 2;

        //
        // cancelButton
        //
        cancelButton.Text = "&Cancel";
        cancelButton.Anchor = AnchorStyles.Right;
        cancelButton.DialogResult = DialogResult.Cancel;
        cancelButton.Name = "cancelButton";
        cancelButton.TabIndex = 3;

        //
        // FitToSizeResultsDialog
        //
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(440, 280);
        Controls.Add(mainLayout);
        Name = "FitToSizeResultsDialog";
        Text = "Fit to File Size — Results";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition = FormStartPosition.CenterParent;
        AccessibleRole = AccessibleRole.Dialog;
        MaximizeBox = false;
        MinimizeBox = false;
        AcceptButton = convertButton;
        CancelButton = cancelButton;

        mainLayout.ResumeLayout(false);
        mainLayout.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private TableLayoutPanel mainLayout;
    private Label introLabel;
    private ListBox resultsListBox;
    private Button convertButton;
    private Button cancelButton;
}
