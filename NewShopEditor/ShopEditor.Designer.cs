using System.Drawing;
using System.Windows.Forms;

namespace NewShopEditor
{
    partial class ShopEditor
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code
        private TextBox txtHost;
        private TextBox txtPort;
        private TextBox txtUser;
        private TextBox txtPass;
        private TextBox txtDb;
        private Button btnConnect;

        // Existing
        private TextBox txtClientPath;
        private Button btnFindPath;
        private Button btnSave;
        private TabControl tabControl;
        private TabPage tabShop;
        private TabPage tabNewShopMx;

        // SHOP.DAT
        private Label lblShopList;
        private Label lblItemList;
        private DataGridView dgvShops;
        private DataGridView dgvItems;

        // NewShopMx.dat
        private Label lblMxCategoryList;
        private Label lblNewShopMxItems;
        private TreeView tvMxCategories;
        private DataGridView dgvNewShopMxItems;

        private void InitializeComponent()
        {
            txtHost = new TextBox();
            txtPort = new TextBox();
            txtUser = new TextBox();
            txtPass = new TextBox();
            txtDb = new TextBox();
            btnConnect = new Button();
            txtClientPath = new TextBox();
            btnFindPath = new Button();
            btnSave = new Button();
            tabControl = new TabControl();
            tabShop = new TabPage();
            btnNewItem = new Button();
            btnNewShop = new Button();
            lblShopList = new Label();
            lblItemList = new Label();
            dgvShops = new DataGridView();
            dgvItems = new DataGridView();
            tabNewShopMx = new TabPage();
            lblMxCategoryList = new Label();
            tvMxCategories = new TreeView();
            lblNewShopMxItems = new Label();
            dgvNewShopMxItems = new DataGridView();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            btnNewMallItem = new Button();
            tabControl.SuspendLayout();
            tabShop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvShops).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).BeginInit();
            tabNewShopMx.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvNewShopMxItems).BeginInit();
            SuspendLayout();
            // 
            // txtHost
            // 
            txtHost.Location = new Point(74, 21);
            txtHost.Name = "txtHost";
            txtHost.Size = new Size(83, 23);
            txtHost.TabIndex = 5;
            txtHost.Text = "127.0.0.1";
            // 
            // txtPort
            // 
            txtPort.Location = new Point(204, 21);
            txtPort.Name = "txtPort";
            txtPort.Size = new Size(66, 23);
            txtPort.TabIndex = 6;
            txtPort.Text = "3306";
            // 
            // txtUser
            // 
            txtUser.Location = new Point(320, 21);
            txtUser.Name = "txtUser";
            txtUser.Size = new Size(87, 23);
            txtUser.TabIndex = 7;
            txtUser.Text = "test";
            // 
            // txtPass
            // 
            txtPass.Location = new Point(482, 21);
            txtPass.Name = "txtPass";
            txtPass.PasswordChar = '*';
            txtPass.Size = new Size(87, 23);
            txtPass.TabIndex = 8;
            txtPass.Text = "test123";
            // 
            // txtDb
            // 
            txtDb.Location = new Point(643, 21);
            txtDb.Name = "txtDb";
            txtDb.Size = new Size(87, 23);
            txtDb.TabIndex = 9;
            txtDb.Text = "newdb1";
            // 
            // btnConnect
            // 
            btnConnect.Location = new Point(747, 21);
            btnConnect.Name = "btnConnect";
            btnConnect.Size = new Size(80, 61);
            btnConnect.TabIndex = 10;
            btnConnect.Text = "Connect";
            // 
            // txtClientPath
            // 
            txtClientPath.Location = new Point(96, 58);
            txtClientPath.Name = "txtClientPath";
            txtClientPath.Size = new Size(523, 23);
            txtClientPath.TabIndex = 11;
            // 
            // btnFindPath
            // 
            btnFindPath.Location = new Point(643, 58);
            btnFindPath.Name = "btnFindPath";
            btnFindPath.Size = new Size(87, 23);
            btnFindPath.TabIndex = 12;
            btnFindPath.Text = "Find...";
            // 
            // btnSave
            // 
            btnSave.Location = new Point(849, 21);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(80, 60);
            btnSave.TabIndex = 14;
            btnSave.Text = "Save";
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabShop);
            tabControl.Controls.Add(tabNewShopMx);
            tabControl.Location = new Point(10, 95);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1341, 801);
            tabControl.TabIndex = 15;
            // 
            // tabShop
            // 
            tabShop.Controls.Add(btnNewItem);
            tabShop.Controls.Add(btnNewShop);
            tabShop.Controls.Add(lblShopList);
            tabShop.Controls.Add(lblItemList);
            tabShop.Controls.Add(dgvShops);
            tabShop.Controls.Add(dgvItems);
            tabShop.Location = new Point(4, 24);
            tabShop.Name = "tabShop";
            tabShop.Size = new Size(1333, 773);
            tabShop.TabIndex = 0;
            tabShop.Text = "Shop.dat";
            // 
            // btnNewItem
            // 
            btnNewItem.Location = new Point(616, 15);
            btnNewItem.Name = "btnNewItem";
            btnNewItem.Size = new Size(75, 23);
            btnNewItem.TabIndex = 5;
            btnNewItem.Text = "New Item";
            btnNewItem.UseVisualStyleBackColor = true;
            // 
            // btnNewShop
            // 
            btnNewShop.Location = new Point(20, 15);
            btnNewShop.Name = "btnNewShop";
            btnNewShop.Size = new Size(75, 23);
            btnNewShop.TabIndex = 4;
            btnNewShop.Text = "New Shop";
            btnNewShop.UseVisualStyleBackColor = true;
            // 
            // lblShopList
            // 
            lblShopList.Location = new Point(20, 45);
            lblShopList.Name = "lblShopList";
            lblShopList.Size = new Size(100, 23);
            lblShopList.TabIndex = 0;
            lblShopList.Text = "Shop List:";
            // 
            // lblItemList
            // 
            lblItemList.Location = new Point(611, 45);
            lblItemList.Name = "lblItemList";
            lblItemList.Size = new Size(100, 23);
            lblItemList.TabIndex = 1;
            lblItemList.Text = "Item List:";
            // 
            // dgvShops
            // 
            dgvShops.Location = new Point(20, 71);
            dgvShops.Name = "dgvShops";
            dgvShops.Size = new Size(585, 702);
            dgvShops.TabIndex = 2;
            // 
            // dgvItems
            // 
            dgvItems.Location = new Point(611, 71);
            dgvItems.Name = "dgvItems";
            dgvItems.Size = new Size(719, 702);
            dgvItems.TabIndex = 3;
            // 
            // tabNewShopMx
            // 
            tabNewShopMx.Controls.Add(btnNewMallItem);
            tabNewShopMx.Controls.Add(lblMxCategoryList);
            tabNewShopMx.Controls.Add(tvMxCategories);
            tabNewShopMx.Controls.Add(lblNewShopMxItems);
            tabNewShopMx.Controls.Add(dgvNewShopMxItems);
            tabNewShopMx.Location = new Point(4, 24);
            tabNewShopMx.Name = "tabNewShopMx";
            tabNewShopMx.Size = new Size(1333, 773);
            tabNewShopMx.TabIndex = 1;
            tabNewShopMx.Text = "NewShopMx.dat";
            // 
            // lblMxCategoryList
            // 
            lblMxCategoryList.Location = new Point(20, 10);
            lblMxCategoryList.Name = "lblMxCategoryList";
            lblMxCategoryList.Size = new Size(120, 23);
            lblMxCategoryList.TabIndex = 0;
            lblMxCategoryList.Text = "Mx Category List:";
            // 
            // tvMxCategories
            // 
            tvMxCategories.Location = new Point(20, 28);
            tvMxCategories.Name = "tvMxCategories";
            tvMxCategories.Size = new Size(304, 721);
            tvMxCategories.TabIndex = 1;
            // 
            // lblNewShopMxItems
            // 
            lblNewShopMxItems.Location = new Point(349, 54);
            lblNewShopMxItems.Name = "lblNewShopMxItems";
            lblNewShopMxItems.Size = new Size(160, 23);
            lblNewShopMxItems.TabIndex = 2;
            lblNewShopMxItems.Text = "NewShopMx Items:";
            // 
            // dgvNewShopMxItems
            // 
            dgvNewShopMxItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNewShopMxItems.Location = new Point(349, 80);
            dgvNewShopMxItems.MultiSelect = false;
            dgvNewShopMxItems.Name = "dgvNewShopMxItems";
            dgvNewShopMxItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNewShopMxItems.Size = new Size(981, 669);
            dgvNewShopMxItems.TabIndex = 3;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(15, 24);
            label1.Name = "label1";
            label1.Size = new Size(53, 15);
            label1.TabIndex = 16;
            label1.Text = "IP/Host :";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(163, 24);
            label2.Name = "label2";
            label2.Size = new Size(35, 15);
            label2.TabIndex = 17;
            label2.Text = "Port :";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(278, 24);
            label3.Name = "label3";
            label3.Size = new Size(36, 15);
            label3.TabIndex = 18;
            label3.Text = "User :";
            // 
            // label4
            // 
            label4.AutoSize = true;
            label4.Location = new Point(413, 24);
            label4.Name = "label4";
            label4.Size = new Size(63, 15);
            label4.TabIndex = 19;
            label4.Text = "Password :";
            // 
            // label5
            // 
            label5.AutoSize = true;
            label5.Location = new Point(576, 24);
            label5.Name = "label5";
            label5.Size = new Size(61, 15);
            label5.TabIndex = 20;
            label5.Text = "Database :";
            // 
            // label6
            // 
            label6.AutoSize = true;
            label6.Location = new Point(19, 61);
            label6.Name = "label6";
            label6.Size = new Size(71, 15);
            label6.TabIndex = 21;
            label6.Text = "Client Path :";
            // 
            // btnNewMallItem
            // 
            btnNewMallItem.Location = new Point(349, 28);
            btnNewMallItem.Name = "btnNewMallItem";
            btnNewMallItem.Size = new Size(75, 23);
            btnNewMallItem.TabIndex = 4;
            btnNewMallItem.Text = "New Item";
            btnNewMallItem.UseVisualStyleBackColor = true;
            // 
            // ShopEditor
            // 
            ClientSize = new Size(1363, 928);
            Controls.Add(label6);
            Controls.Add(label5);
            Controls.Add(label4);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(txtHost);
            Controls.Add(txtPort);
            Controls.Add(txtUser);
            Controls.Add(txtPass);
            Controls.Add(txtDb);
            Controls.Add(btnConnect);
            Controls.Add(txtClientPath);
            Controls.Add(btnFindPath);
            Controls.Add(btnSave);
            Controls.Add(tabControl);
            Name = "ShopEditor";
            Text = "EO Shop/NewShop Editor";
            tabControl.ResumeLayout(false);
            tabShop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvShops).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).EndInit();
            tabNewShopMx.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvNewShopMxItems).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;
        private Label label5;
        private Label label6;
        private Button btnNewItem;
        private Button btnNewShop;
        private Button btnNewMallItem;
    }
}
