using System.Drawing;
using System.Windows.Forms;

namespace NewShopEditor
{
    partial class NewShopItemForm
    {
        private System.ComponentModel.IContainer components = null;

        private SplitContainer splitMain;
        private TextBox txtSearch;
        private Label lblSearch;
        private DataGridView dgvFdb;

        private Label lblSelName;
        private TextBox txtSelName;

        private Label lblGold;
        private TextBox txtGold;
        private Label lblEmoney;
        private TextBox txtEmoney;

        private Button btnOK;
        private Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            splitMain = new SplitContainer();
            dgvFdb = new DataGridView();
            txtSearch = new TextBox();
            lblSearch = new Label();
            lblSelName = new Label();
            txtSelName = new TextBox();
            lblGold = new Label();
            txtGold = new TextBox();
            lblEmoney = new Label();
            txtEmoney = new TextBox();
            btnOK = new Button();
            btnCancel = new Button();
            lblReserved2 = new Label();
            lblReserved1 = new Label();
            cboReserved1 = new ComboBox();
            cboReserved2 = new ComboBox();
            ((System.ComponentModel.ISupportInitialize)splitMain).BeginInit();
            splitMain.Panel1.SuspendLayout();
            splitMain.Panel2.SuspendLayout();
            splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFdb).BeginInit();
            SuspendLayout();
            // 
            // splitMain
            // 
            splitMain.Dock = DockStyle.Fill;
            splitMain.Location = new Point(0, 0);
            splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            splitMain.Panel1.Controls.Add(dgvFdb);
            splitMain.Panel1.Controls.Add(txtSearch);
            splitMain.Panel1.Controls.Add(lblSearch);
            // 
            // splitMain.Panel2
            // 
            splitMain.Panel2.Controls.Add(lblSelName);
            splitMain.Panel2.Controls.Add(txtSelName);
            splitMain.Panel2.Controls.Add(lblGold);
            splitMain.Panel2.Controls.Add(txtGold);
            splitMain.Panel2.Controls.Add(lblEmoney);
            splitMain.Panel2.Controls.Add(txtEmoney);
            splitMain.Panel2.Controls.Add(lblReserved1);
            splitMain.Panel2.Controls.Add(cboReserved1);
            splitMain.Panel2.Controls.Add(lblReserved2);
            splitMain.Panel2.Controls.Add(cboReserved2);
            splitMain.Panel2.Controls.Add(btnOK);
            splitMain.Panel2.Controls.Add(btnCancel);
            splitMain.Panel2.Padding = new Padding(12);
            splitMain.Size = new Size(824, 520);
            splitMain.SplitterDistance = 486;
            splitMain.TabIndex = 0;
            // 
            // dgvFdb
            // 
            dgvFdb.AllowUserToAddRows = false;
            dgvFdb.AllowUserToDeleteRows = false;
            dgvFdb.AllowUserToOrderColumns = true;
            dgvFdb.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvFdb.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFdb.Location = new Point(8, 40);
            dgvFdb.MultiSelect = false;
            dgvFdb.Name = "dgvFdb";
            dgvFdb.ReadOnly = true;
            dgvFdb.RowHeadersVisible = false;
            dgvFdb.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFdb.Size = new Size(463, 470);
            dgvFdb.TabIndex = 1;
            // 
            // txtSearch
            // 
            txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            txtSearch.Location = new Point(65, 7);
            txtSearch.Name = "txtSearch";
            txtSearch.Size = new Size(406, 23);
            txtSearch.TabIndex = 0;
            // 
            // lblSearch
            // 
            lblSearch.AutoSize = true;
            lblSearch.Location = new Point(8, 10);
            lblSearch.Name = "lblSearch";
            lblSearch.Size = new Size(42, 15);
            lblSearch.TabIndex = 2;
            lblSearch.Text = "Search";
            // 
            // lblSelName
            // 
            lblSelName.AutoSize = true;
            lblSelName.Location = new Point(12, 15);
            lblSelName.Name = "lblSelName";
            lblSelName.Size = new Size(39, 15);
            lblSelName.TabIndex = 0;
            lblSelName.Text = "Name";
            // 
            // txtSelName
            // 
            txtSelName.Location = new Point(100, 12);
            txtSelName.Name = "txtSelName";
            txtSelName.ReadOnly = true;
            txtSelName.Size = new Size(219, 23);
            txtSelName.TabIndex = 1;
            // 
            // lblGold
            // 
            lblGold.AutoSize = true;
            lblGold.Location = new Point(12, 50);
            lblGold.Name = "lblGold";
            lblGold.Size = new Size(69, 15);
            lblGold.TabIndex = 2;
            lblGold.Text = "Gold (Price)";
            // 
            // txtGold
            // 
            txtGold.Location = new Point(100, 47);
            txtGold.Name = "txtGold";
            txtGold.Size = new Size(219, 23);
            txtGold.TabIndex = 3;
            // 
            // lblEmoney
            // 
            lblEmoney.AutoSize = true;
            lblEmoney.Location = new Point(12, 85);
            lblEmoney.Name = "lblEmoney";
            lblEmoney.Size = new Size(74, 15);
            lblEmoney.TabIndex = 4;
            lblEmoney.Text = "Emoney (EP)";
            // 
            // txtEmoney
            // 
            txtEmoney.Location = new Point(100, 82);
            txtEmoney.Name = "txtEmoney";
            txtEmoney.Size = new Size(219, 23);
            txtEmoney.TabIndex = 5;
            // 
            // btnOK
            // 
            btnOK.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnOK.Location = new Point(100, 190);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(105, 28);
            btnOK.TabIndex = 10;
            btnOK.Text = "Add";
            // 
            // btnCancel
            // 
            btnCancel.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(211, 190);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(105, 28);
            btnCancel.TabIndex = 11;
            btnCancel.Text = "Cancel";
            // 
            // lblReserved2
            // 
            lblReserved2.AutoSize = true;
            lblReserved2.Location = new Point(12, 155);
            lblReserved2.Name = "lblReserved2";
            lblReserved2.Size = new Size(60, 15);
            lblReserved2.TabIndex = 8;
            lblReserved2.Text = "Reserved2";
            // 
            // lblReserved1
            // 
            lblReserved1.AutoSize = true;
            lblReserved1.Location = new Point(12, 120);
            lblReserved1.Name = "lblReserved1";
            lblReserved1.Size = new Size(60, 15);
            lblReserved1.TabIndex = 6;
            lblReserved1.Text = "Reserved1";
            // 
            // cboReserved1
            // 
            cboReserved1.DropDownStyle = ComboBoxStyle.DropDownList;
            cboReserved1.Location = new Point(100, 117);
            cboReserved1.Name = "cboReserved1";
            cboReserved1.Size = new Size(219, 23);
            cboReserved1.TabIndex = 7;
            // 
            // cboReserved2
            // 
            cboReserved2.DropDownStyle = ComboBoxStyle.DropDownList;
            cboReserved2.Location = new Point(100, 152);
            cboReserved2.Name = "cboReserved2";
            cboReserved2.Size = new Size(219, 23);
            cboReserved2.TabIndex = 9;
            // 
            // NewShopItemForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(824, 520);
            Controls.Add(splitMain);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NewShopItemForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add Item from itemtype.fdb";
            splitMain.Panel1.ResumeLayout(false);
            splitMain.Panel1.PerformLayout();
            splitMain.Panel2.ResumeLayout(false);
            splitMain.Panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)splitMain).EndInit();
            splitMain.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvFdb).EndInit();
            ResumeLayout(false);
        }
        #endregion

        private Label lblReserved1;
        private ComboBox cboReserved1;
        private Label lblReserved2;
        private ComboBox cboReserved2;
    }
}
