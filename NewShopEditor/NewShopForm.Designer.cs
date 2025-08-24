namespace NewShopEditor
{
    partial class NewShopForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.Label lblShopId;
        private System.Windows.Forms.TextBox txtShopId;
        private System.Windows.Forms.Label lblShopName;
        private System.Windows.Forms.TextBox txtShopName;
        private System.Windows.Forms.GroupBox grpType;
        private System.Windows.Forms.RadioButton rbTypeGold;
        private System.Windows.Forms.RadioButton rbTypeEp;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null)) components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private void InitializeComponent()
        {
            lblShopId = new Label();
            txtShopId = new TextBox();
            lblShopName = new Label();
            txtShopName = new TextBox();
            grpType = new GroupBox();
            rbTypeEp = new RadioButton();
            rbTypeGold = new RadioButton();
            btnOK = new Button();
            btnCancel = new Button();
            grpType.SuspendLayout();
            SuspendLayout();
            // 
            // lblShopId
            // 
            lblShopId.AutoSize = true;
            lblShopId.Location = new Point(16, 18);
            lblShopId.Name = "lblShopId";
            lblShopId.Size = new Size(51, 15);
            lblShopId.TabIndex = 0;
            lblShopId.Text = "ShopID :";
            // 
            // txtShopId
            // 
            txtShopId.Location = new Point(73, 15);
            txtShopId.Name = "txtShopId";
            txtShopId.Size = new Size(180, 23);
            txtShopId.TabIndex = 1;
            // 
            // lblShopName
            // 
            lblShopName.AutoSize = true;
            lblShopName.Location = new Point(22, 54);
            lblShopName.Name = "lblShopName";
            lblShopName.Size = new Size(45, 15);
            lblShopName.TabIndex = 2;
            lblShopName.Text = "Name :";
            // 
            // txtShopName
            // 
            txtShopName.Location = new Point(73, 51);
            txtShopName.Name = "txtShopName";
            txtShopName.Size = new Size(180, 23);
            txtShopName.TabIndex = 3;
            // 
            // grpType
            // 
            grpType.Controls.Add(rbTypeEp);
            grpType.Controls.Add(rbTypeGold);
            grpType.Location = new Point(73, 90);
            grpType.Name = "grpType";
            grpType.Size = new Size(180, 56);
            grpType.TabIndex = 4;
            grpType.TabStop = false;
            grpType.Text = "Type";
            // 
            // rbTypeEp
            // 
            rbTypeEp.AutoSize = true;
            rbTypeEp.Location = new Point(107, 22);
            rbTypeEp.Name = "rbTypeEp";
            rbTypeEp.Size = new Size(67, 19);
            rbTypeEp.TabIndex = 1;
            rbTypeEp.Tag = "5";
            rbTypeEp.Text = "EP shop";
            rbTypeEp.UseVisualStyleBackColor = true;
            // 
            // rbTypeGold
            // 
            rbTypeGold.AutoSize = true;
            rbTypeGold.Location = new Point(14, 22);
            rbTypeGold.Name = "rbTypeGold";
            rbTypeGold.Size = new Size(79, 19);
            rbTypeGold.TabIndex = 0;
            rbTypeGold.TabStop = true;
            rbTypeGold.Tag = "2";
            rbTypeGold.Text = "Gold shop";
            rbTypeGold.UseVisualStyleBackColor = true;
            // 
            // btnOK
            // 
            btnOK.Location = new Point(73, 152);
            btnOK.Name = "btnOK";
            btnOK.Size = new Size(75, 28);
            btnOK.TabIndex = 5;
            btnOK.Text = "Add";
            btnOK.UseVisualStyleBackColor = true;
            btnOK.Click += btnOK_Click;
            // 
            // btnCancel
            // 
            btnCancel.DialogResult = DialogResult.Cancel;
            btnCancel.Location = new Point(172, 152);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(75, 28);
            btnCancel.TabIndex = 6;
            btnCancel.Text = "Cancel";
            btnCancel.UseVisualStyleBackColor = true;
            // 
            // NewShopForm
            // 
            AcceptButton = btnOK;
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = btnCancel;
            ClientSize = new Size(273, 193);
            Controls.Add(btnCancel);
            Controls.Add(btnOK);
            Controls.Add(grpType);
            Controls.Add(txtShopName);
            Controls.Add(lblShopName);
            Controls.Add(txtShopId);
            Controls.Add(lblShopId);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "NewShopForm";
            StartPosition = FormStartPosition.CenterParent;
            Text = "Add New Shop";
            grpType.ResumeLayout(false);
            grpType.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }
        #endregion
    }
}
