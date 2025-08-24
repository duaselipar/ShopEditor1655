namespace NewShopEditor
{
    partial class NewShopMxItem
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            lblSearchMx = new Label();
            txtSearchMx = new TextBox();
            dgvMx = new DataGridView();
            lblItemId = new Label();
            txtItemId = new TextBox();
            lblItemType = new Label();
            txtItemType = new TextBox();
            lblName = new Label();
            txtName = new TextBox();
            lblEmoney = new Label();
            txtEmoney = new TextBox();
            lblCostType = new Label();
            cboCostType = new ComboBox();
            lblVersion = new Label();
            cboVersion = new ComboBox();
            chkNew = new CheckBox();
            chkCommend = new CheckBox();
            lblOEM = new Label();
            cboOEM = new ComboBox();
            lblDescribe = new Label();
            txtDescribe = new TextBox();
            lblReturnEmoney = new Label();
            txtReturnEmoney = new TextBox();
            lblBegin = new Label();
            txtBegin = new TextBox();
            lblEnd = new Label();
            txtEnd = new TextBox();
            lblTip = new Label();
            cboTip1 = new ComboBox();
            cboTip2 = new ComboBox();
            cboTip3 = new ComboBox();
            btnOK = new Button();
            btnCancel = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvMx).BeginInit();
            SuspendLayout();
            // 
            // lblSearchMx
            // 
            lblSearchMx.AutoSize = true;
            lblSearchMx.Location = new Point(12, 12);
            lblSearchMx.Name = "lblSearchMx";
            lblSearchMx.Size = new Size(45, 15);
            lblSearchMx.TabIndex = 0;
            lblSearchMx.Text = "Search:";
            // 
            // txtSearchMx
            // 
            txtSearchMx.Location = new Point(65, 9);
            txtSearchMx.Name = "txtSearchMx";
            txtSearchMx.Size = new Size(307, 23);
            txtSearchMx.TabIndex = 1;
            // 
            // dgvMx
            // 
            dgvMx.AllowUserToAddRows = false;
            dgvMx.AllowUserToDeleteRows = false;
            dgvMx.AllowUserToResizeRows = false;
            dgvMx.Location = new Point(12, 40);
            dgvMx.MultiSelect = false;
            dgvMx.Name = "dgvMx";
            dgvMx.ReadOnly = true;
            dgvMx.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvMx.Size = new Size(360, 537);
            dgvMx.TabIndex = 2;
            // 
            // lblItemId
            // 
            lblItemId.AutoSize = true;
            lblItemId.Location = new Point(424, 384);
            lblItemId.Name = "lblItemId";
            lblItemId.Size = new Size(45, 15);
            lblItemId.TabIndex = 3;
            lblItemId.Text = "ItemID:";
            // 
            // txtItemId
            // 
            txtItemId.Enabled = false;
            txtItemId.Location = new Point(479, 381);
            txtItemId.Name = "txtItemId";
            txtItemId.Size = new Size(172, 23);
            txtItemId.TabIndex = 4;
            // 
            // lblItemType
            // 
            lblItemType.AutoSize = true;
            lblItemType.Location = new Point(378, 40);
            lblItemType.Name = "lblItemType";
            lblItemType.Size = new Size(59, 15);
            lblItemType.TabIndex = 5;
            lblItemType.Text = "ItemType:";
            // 
            // txtItemType
            // 
            txtItemType.Enabled = false;
            txtItemType.Location = new Point(455, 37);
            txtItemType.Name = "txtItemType";
            txtItemType.Size = new Size(195, 23);
            txtItemType.TabIndex = 6;
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(378, 73);
            lblName.Name = "lblName";
            lblName.Size = new Size(42, 15);
            lblName.TabIndex = 7;
            lblName.Text = "Name:";
            // 
            // txtName
            // 
            txtName.Location = new Point(454, 70);
            txtName.Name = "txtName";
            txtName.ReadOnly = true;
            txtName.Size = new Size(196, 23);
            txtName.TabIndex = 8;
            // 
            // lblEmoney
            // 
            lblEmoney.AutoSize = true;
            lblEmoney.Location = new Point(378, 106);
            lblEmoney.Name = "lblEmoney";
            lblEmoney.Size = new Size(53, 15);
            lblEmoney.TabIndex = 9;
            lblEmoney.Text = "Emoney:";
            // 
            // txtEmoney
            // 
            txtEmoney.Location = new Point(455, 103);
            txtEmoney.Name = "txtEmoney";
            txtEmoney.Size = new Size(195, 23);
            txtEmoney.TabIndex = 10;
            // 
            // lblCostType
            // 
            lblCostType.AutoSize = true;
            lblCostType.Location = new Point(378, 139);
            lblCostType.Name = "lblCostType";
            lblCostType.Size = new Size(59, 15);
            lblCostType.TabIndex = 11;
            lblCostType.Text = "CostType:";
            // 
            // cboCostType
            // 
            cboCostType.DropDownStyle = ComboBoxStyle.DropDownList;
            cboCostType.Location = new Point(455, 136);
            cboCostType.Name = "cboCostType";
            cboCostType.Size = new Size(195, 23);
            cboCostType.TabIndex = 12;
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Location = new Point(378, 172);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(48, 15);
            lblVersion.TabIndex = 13;
            lblVersion.Text = "Version:";
            // 
            // cboVersion
            // 
            cboVersion.DropDownStyle = ComboBoxStyle.DropDownList;
            cboVersion.Location = new Point(455, 169);
            cboVersion.Name = "cboVersion";
            cboVersion.Size = new Size(195, 23);
            cboVersion.TabIndex = 14;
            // 
            // chkNew
            // 
            chkNew.AutoSize = true;
            chkNew.Location = new Point(455, 202);
            chkNew.Name = "chkNew";
            chkNew.Size = new Size(50, 19);
            chkNew.TabIndex = 15;
            chkNew.Text = "New";
            // 
            // chkCommend
            // 
            chkCommend.AutoSize = true;
            chkCommend.Location = new Point(518, 202);
            chkCommend.Name = "chkCommend";
            chkCommend.Size = new Size(83, 19);
            chkCommend.TabIndex = 16;
            chkCommend.Text = "Commend";
            // 
            // lblOEM
            // 
            lblOEM.AutoSize = true;
            lblOEM.Location = new Point(433, 413);
            lblOEM.Name = "lblOEM";
            lblOEM.Size = new Size(36, 15);
            lblOEM.TabIndex = 17;
            lblOEM.Text = "OEM:";
            // 
            // cboOEM
            // 
            cboOEM.DropDownStyle = ComboBoxStyle.DropDownList;
            cboOEM.Location = new Point(479, 410);
            cboOEM.Name = "cboOEM";
            cboOEM.Size = new Size(171, 23);
            cboOEM.TabIndex = 18;
            // 
            // lblDescribe
            // 
            lblDescribe.AutoSize = true;
            lblDescribe.Location = new Point(414, 446);
            lblDescribe.Name = "lblDescribe";
            lblDescribe.Size = new Size(55, 15);
            lblDescribe.TabIndex = 19;
            lblDescribe.Text = "Describe:";
            // 
            // txtDescribe
            // 
            txtDescribe.Location = new Point(479, 443);
            txtDescribe.Name = "txtDescribe";
            txtDescribe.ReadOnly = true;
            txtDescribe.Size = new Size(172, 23);
            txtDescribe.TabIndex = 20;
            // 
            // lblReturnEmoney
            // 
            lblReturnEmoney.AutoSize = true;
            lblReturnEmoney.Location = new Point(381, 479);
            lblReturnEmoney.Name = "lblReturnEmoney";
            lblReturnEmoney.Size = new Size(88, 15);
            lblReturnEmoney.TabIndex = 21;
            lblReturnEmoney.Text = "ReturnEmoney:";
            // 
            // txtReturnEmoney
            // 
            txtReturnEmoney.Location = new Point(479, 476);
            txtReturnEmoney.Name = "txtReturnEmoney";
            txtReturnEmoney.ReadOnly = true;
            txtReturnEmoney.Size = new Size(171, 23);
            txtReturnEmoney.TabIndex = 22;
            // 
            // lblBegin
            // 
            lblBegin.AutoSize = true;
            lblBegin.Location = new Point(402, 512);
            lblBegin.Name = "lblBegin";
            lblBegin.Size = new Size(67, 15);
            lblBegin.TabIndex = 23;
            lblBegin.Text = "BeginTime:";
            // 
            // txtBegin
            // 
            txtBegin.Location = new Point(479, 509);
            txtBegin.Name = "txtBegin";
            txtBegin.Size = new Size(172, 23);
            txtBegin.TabIndex = 24;
            // 
            // lblEnd
            // 
            lblEnd.AutoSize = true;
            lblEnd.Location = new Point(412, 545);
            lblEnd.Name = "lblEnd";
            lblEnd.Size = new Size(57, 15);
            lblEnd.TabIndex = 25;
            lblEnd.Text = "EndTime:";
            // 
            // txtEnd
            // 
            txtEnd.Location = new Point(479, 542);
            txtEnd.Name = "txtEnd";
            txtEnd.Size = new Size(171, 23);
            txtEnd.TabIndex = 26;
            // 
            // lblTip
            // 
            lblTip.AutoSize = true;
            lblTip.Location = new Point(400, 227);
            lblTip.Name = "lblTip";
            lblTip.Size = new Size(27, 15);
            lblTip.TabIndex = 27;
            lblTip.Text = "Tip:";
            // 
            // cboTip1
            // 
            cboTip1.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTip1.Location = new Point(455, 227);
            cboTip1.Name = "cboTip1";
            cboTip1.Size = new Size(195, 23);
            cboTip1.TabIndex = 28;
            // 
            // cboTip2
            // 
            cboTip2.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTip2.Location = new Point(455, 256);
            cboTip2.Name = "cboTip2";
            cboTip2.Size = new Size(195, 23);
            cboTip2.TabIndex = 29;
            // 
            // cboTip3
            // 
            cboTip3.DropDownStyle = ComboBoxStyle.DropDownList;
            cboTip3.Location = new Point(455, 285);
            cboTip3.Name = "cboTip3";
            cboTip3.Size = new Size(195, 23);
            cboTip3.TabIndex = 30;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(455, 326);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(90, 30);
            btnOK.TabIndex = 31;
            btnOK.Text = "OK";
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(560, 326);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(90, 30);
            btnCancel.TabIndex = 32;
            btnCancel.Text = "Cancel";
            // 
            // NewShopMxItem
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(663, 590);
            Controls.Add(lblSearchMx);
            Controls.Add(txtSearchMx);
            Controls.Add(dgvMx);
            Controls.Add(lblItemId);
            Controls.Add(txtItemId);
            Controls.Add(lblItemType);
            Controls.Add(txtItemType);
            Controls.Add(lblName);
            Controls.Add(txtName);
            Controls.Add(lblEmoney);
            Controls.Add(txtEmoney);
            Controls.Add(lblCostType);
            Controls.Add(cboCostType);
            Controls.Add(lblVersion);
            Controls.Add(cboVersion);
            Controls.Add(chkNew);
            Controls.Add(chkCommend);
            Controls.Add(lblOEM);
            Controls.Add(cboOEM);
            Controls.Add(lblDescribe);
            Controls.Add(txtDescribe);
            Controls.Add(lblReturnEmoney);
            Controls.Add(txtReturnEmoney);
            Controls.Add(lblBegin);
            Controls.Add(txtBegin);
            Controls.Add(lblEnd);
            Controls.Add(txtEnd);
            Controls.Add(lblTip);
            Controls.Add(cboTip1);
            Controls.Add(cboTip2);
            Controls.Add(cboTip3);
            Controls.Add(btnOK);
            Controls.Add(btnCancel);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NewShopMxItem";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add Mall Item (NewShopMx)";
            ((System.ComponentModel.ISupportInitialize)dgvMx).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.Label lblSearchMx;
        private System.Windows.Forms.TextBox txtSearchMx;
        private System.Windows.Forms.DataGridView dgvMx;

        private System.Windows.Forms.Label lblItemId;
        private System.Windows.Forms.TextBox txtItemId;
        private System.Windows.Forms.Label lblItemType;
        private System.Windows.Forms.TextBox txtItemType;
        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblEmoney;
        private System.Windows.Forms.TextBox txtEmoney;
        private System.Windows.Forms.Label lblCostType;
        private System.Windows.Forms.ComboBox cboCostType;
        private System.Windows.Forms.Label lblVersion;
        private System.Windows.Forms.ComboBox cboVersion;
        private System.Windows.Forms.CheckBox chkNew;
        private System.Windows.Forms.CheckBox chkCommend;
        private System.Windows.Forms.Label lblOEM;
        private System.Windows.Forms.ComboBox cboOEM;
        private System.Windows.Forms.Label lblDescribe;
        private System.Windows.Forms.TextBox txtDescribe;
        private System.Windows.Forms.Label lblReturnEmoney;
        private System.Windows.Forms.TextBox txtReturnEmoney;
        private System.Windows.Forms.Label lblBegin;
        private System.Windows.Forms.TextBox txtBegin;
        private System.Windows.Forms.Label lblEnd;
        private System.Windows.Forms.TextBox txtEnd;
        private System.Windows.Forms.Label lblTip;
        private System.Windows.Forms.ComboBox cboTip1;
        private System.Windows.Forms.ComboBox cboTip2;
        private System.Windows.Forms.ComboBox cboTip3;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}
