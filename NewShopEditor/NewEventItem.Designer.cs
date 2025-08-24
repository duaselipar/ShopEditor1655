using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace NewShopEditor
{
    partial class NewEventItem
    {
        /// <summary>Required designer variable.</summary>
        private IContainer components = null;

        // Controls declaration
        private TextBox txtSearch;
        private DataGridView dgvSearch;
        private Button btnAdd;
        private Button btnCancel;
        private GroupBox groupBox1;
        private GroupBox groupShopType;
        private RadioButton rbAstra;
        private RadioButton rbHonor;
        private RadioButton rbPlane;

        /// <summary>Clean up any resources being used.</summary>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            txtSearch = new TextBox();
            dgvSearch = new DataGridView();
            dataGridViewTextBoxColumn1 = new DataGridViewTextBoxColumn();
            dataGridViewTextBoxColumn2 = new DataGridViewTextBoxColumn();
            btnAdd = new Button();
            btnCancel = new Button();
            groupBox1 = new GroupBox();
            groupShopType = new GroupBox();
            rbAstra = new RadioButton();
            rbHonor = new RadioButton();
            rbPlane = new RadioButton();
            groupBox2 = new GroupBox();
            txtPrice = new TextBox();
            ((ISupportInitialize)dgvSearch).BeginInit();
            groupBox1.SuspendLayout();
            groupShopType.SuspendLayout();
            groupBox2.SuspendLayout();
            SuspendLayout();
            // 
            // txtSearch
            // 
            txtSearch.Location = new Point(12, 12);
            txtSearch.Name = "txtSearch";
            txtSearch.PlaceholderText = "Search item id/name...";
            txtSearch.Size = new Size(299, 23);
            txtSearch.TabIndex = 1;
            // 
            // dgvSearch
            // 
            dgvSearch.AllowUserToAddRows = false;
            dgvSearch.AllowUserToDeleteRows = false;
            dgvSearch.AllowUserToResizeColumns = false;
            dgvSearch.AllowUserToResizeRows = false;
            dgvSearch.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvSearch.Columns.AddRange(new DataGridViewColumn[] { dataGridViewTextBoxColumn1, dataGridViewTextBoxColumn2 });
            dgvSearch.Location = new Point(6, 22);
            dgvSearch.MultiSelect = false;
            dgvSearch.Name = "dgvSearch";
            dgvSearch.ReadOnly = true;
            dgvSearch.RowHeadersVisible = false;
            dgvSearch.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
            dgvSearch.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSearch.Size = new Size(284, 482);
            dgvSearch.TabIndex = 2;
            // 
            // dataGridViewTextBoxColumn1
            // 
            dataGridViewTextBoxColumn1.HeaderText = "Item ID";
            dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            dataGridViewTextBoxColumn1.ReadOnly = true;
            dataGridViewTextBoxColumn1.Width = 80;
            // 
            // dataGridViewTextBoxColumn2
            // 
            dataGridViewTextBoxColumn2.HeaderText = "Name";
            dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            dataGridViewTextBoxColumn2.ReadOnly = true;
            dataGridViewTextBoxColumn2.Width = 184;
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(142, 676);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(80, 27);
            btnAdd.TabIndex = 3;
            btnAdd.Text = "Add";
            btnAdd.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(228, 676);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(80, 27);
            btnCancel.TabIndex = 4;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            groupBox1.Controls.Add(dgvSearch);
            groupBox1.Location = new Point(12, 160);
            groupBox1.Name = "groupBox1";
            groupBox1.Size = new Size(299, 510);
            groupBox1.TabIndex = 5;
            groupBox1.TabStop = false;
            groupBox1.Text = "Select Item (Double Click To Select)";
            // 
            // groupShopType
            // 
            groupShopType.Controls.Add(rbAstra);
            groupShopType.Controls.Add(rbHonor);
            groupShopType.Controls.Add(rbPlane);
            groupShopType.Location = new Point(12, 41);
            groupShopType.Name = "groupShopType";
            groupShopType.Size = new Size(299, 50);
            groupShopType.TabIndex = 6;
            groupShopType.TabStop = false;
            groupShopType.Text = "Select Shop Type";
            // 
            // rbAstra
            // 
            rbAstra.AutoSize = true;
            rbAstra.Checked = true;
            rbAstra.Location = new Point(22, 20);
            rbAstra.Name = "rbAstra";
            rbAstra.Size = new Size(82, 19);
            rbAstra.TabIndex = 0;
            rbAstra.TabStop = true;
            rbAstra.Tag = "1";
            rbAstra.Text = "Astra Shop";
            rbAstra.UseVisualStyleBackColor = true;
            // 
            // rbHonor
            // 
            rbHonor.AutoSize = true;
            rbHonor.Location = new Point(110, 20);
            rbHonor.Name = "rbHonor";
            rbHonor.Size = new Size(89, 19);
            rbHonor.TabIndex = 1;
            rbHonor.Tag = "2";
            rbHonor.Text = "Honor Shop";
            rbHonor.UseVisualStyleBackColor = true;
            // 
            // rbPlane
            // 
            rbPlane.AutoSize = true;
            rbPlane.Location = new Point(205, 20);
            rbPlane.Name = "rbPlane";
            rbPlane.Size = new Size(84, 19);
            rbPlane.TabIndex = 2;
            rbPlane.Tag = "4";
            rbPlane.Text = "Plane Shop";
            rbPlane.UseVisualStyleBackColor = true;
            // 
            // groupBox2
            // 
            groupBox2.Controls.Add(txtPrice);
            groupBox2.Location = new Point(12, 97);
            groupBox2.Name = "groupBox2";
            groupBox2.Size = new Size(299, 57);
            groupBox2.TabIndex = 7;
            groupBox2.TabStop = false;
            groupBox2.Text = "Price";
            // 
            // txtPrice
            // 
            txtPrice.Location = new Point(6, 22);
            txtPrice.Name = "txtPrice";
            txtPrice.Size = new Size(283, 23);
            txtPrice.TabIndex = 0;
            txtPrice.Text = "0";
            // 
            // NewEventItem
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(320, 715);
            Controls.Add(groupBox2);
            Controls.Add(groupBox1);
            Controls.Add(groupShopType);
            Controls.Add(btnCancel);
            Controls.Add(btnAdd);
            Controls.Add(txtSearch);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NewEventItem";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add New Item";
            ((ISupportInitialize)dgvSearch).EndInit();
            groupBox1.ResumeLayout(false);
            groupShopType.ResumeLayout(false);
            groupShopType.PerformLayout();
            groupBox2.ResumeLayout(false);
            groupBox2.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private GroupBox groupBox2;
        private TextBox txtPrice;
    }
}
