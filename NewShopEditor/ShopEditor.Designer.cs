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

        private System.Windows.Forms.TextBox txtClientPath;
        private System.Windows.Forms.Button btnFindPath;
        private System.Windows.Forms.Button btnLoad;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TabPage tabShop;
        private System.Windows.Forms.TabPage tabNewShop;
        private System.Windows.Forms.TabPage tabNewShopMx;
        // SHOP.DAT
        private System.Windows.Forms.Label lblShopList;
        private System.Windows.Forms.Label lblItemList;
        private System.Windows.Forms.DataGridView dgvShops;
        private System.Windows.Forms.DataGridView dgvItems;

        private System.Windows.Forms.Label lblCategoryList;
        private System.Windows.Forms.Label lblNewShopItems;
        private System.Windows.Forms.TreeView tvCategories;
        private System.Windows.Forms.DataGridView dgvNewShopItems;

        private void InitializeComponent()
        {
            txtClientPath = new TextBox();
            btnFindPath = new Button();
            btnLoad = new Button();
            btnSave = new Button();
            tabControl = new TabControl();
            tabShop = new TabPage();
            lblShopList = new Label();
            lblItemList = new Label();
            dgvShops = new DataGridView();
            dgvItems = new DataGridView();
            tabNewShop = new TabPage();
            lblCategoryList = new Label();
            tvCategories = new TreeView();
            lblNewShopItems = new Label();
            dgvNewShopItems = new DataGridView();
            tabNewShopMx = new TabPage();
            tabControl.SuspendLayout();
            tabShop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvShops).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).BeginInit();
            tabNewShop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvNewShopItems).BeginInit();
            SuspendLayout();
            // 
            // txtClientPath
            // 
            txtClientPath.Location = new Point(20, 20);
            txtClientPath.Name = "txtClientPath";
            txtClientPath.Size = new Size(350, 23);
            txtClientPath.TabIndex = 0;
            // 
            // btnFindPath
            // 
            btnFindPath.Location = new Point(380, 20);
            btnFindPath.Name = "btnFindPath";
            btnFindPath.Size = new Size(60, 23);
            btnFindPath.TabIndex = 1;
            btnFindPath.Text = "Find...";
            // 
            // btnLoad
            // 
            btnLoad.Location = new Point(450, 20);
            btnLoad.Name = "btnLoad";
            btnLoad.Size = new Size(60, 23);
            btnLoad.TabIndex = 2;
            btnLoad.Text = "Load";
            // 
            // btnSave
            // 
            btnSave.Location = new Point(520, 20);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(60, 23);
            btnSave.TabIndex = 3;
            btnSave.Text = "Save";
            // 
            // tabControl
            // 
            tabControl.Controls.Add(tabShop);
            tabControl.Controls.Add(tabNewShop);
            tabControl.Controls.Add(tabNewShopMx);
            tabControl.Location = new Point(10, 60);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1341, 801);
            tabControl.TabIndex = 4;
            // 
            // tabShop
            // 
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
            // lblShopList
            // 
            lblShopList.Location = new Point(20, 10);
            lblShopList.Name = "lblShopList";
            lblShopList.Size = new Size(100, 23);
            lblShopList.TabIndex = 0;
            lblShopList.Text = "Shop List:";
            // 
            // lblItemList
            // 
            lblItemList.Location = new Point(400, 10);
            lblItemList.Name = "lblItemList";
            lblItemList.Size = new Size(100, 23);
            lblItemList.TabIndex = 1;
            lblItemList.Text = "Item List:";
            // 
            // dgvShops
            // 
            dgvShops.Location = new Point(20, 36);
            dgvShops.Name = "dgvShops";
            dgvShops.Size = new Size(350, 334);
            dgvShops.TabIndex = 2;
            // 
            // dgvItems
            // 
            dgvItems.Location = new Point(400, 36);
            dgvItems.Name = "dgvItems";
            dgvItems.Size = new Size(350, 334);
            dgvItems.TabIndex = 3;
            // 
            // tabNewShop
            // 
            tabNewShop.Controls.Add(lblCategoryList);
            tabNewShop.Controls.Add(tvCategories);
            tabNewShop.Controls.Add(lblNewShopItems);
            tabNewShop.Controls.Add(dgvNewShopItems);
            tabNewShop.Location = new Point(4, 24);
            tabNewShop.Name = "tabNewShop";
            tabNewShop.Size = new Size(1333, 773);
            tabNewShop.TabIndex = 1;
            tabNewShop.Text = "NewShop.dat";
            // 
            // lblCategoryList
            // 
            lblCategoryList.Location = new Point(20, 10);
            lblCategoryList.Name = "lblCategoryList";
            lblCategoryList.Size = new Size(100, 23);
            lblCategoryList.TabIndex = 0;
            lblCategoryList.Text = "Category List:";
            // 
            // tvCategories
            // 
            tvCategories.Location = new Point(20, 36);
            tvCategories.Name = "tvCategories";
            tvCategories.Size = new Size(304, 713);
            tvCategories.TabIndex = 1;
            tvCategories.AfterSelect += tvCategories_AfterSelect;
            // 
            // lblNewShopItems
            // 
            lblNewShopItems.Location = new Point(290, 10);
            lblNewShopItems.Name = "lblNewShopItems";
            lblNewShopItems.Size = new Size(150, 23);
            lblNewShopItems.TabIndex = 2;
            lblNewShopItems.Text = "NewShop Items:";
            // 
            // dgvNewShopItems
            // 
            dgvNewShopItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNewShopItems.Location = new Point(349, 36);
            dgvNewShopItems.MultiSelect = false;
            dgvNewShopItems.Name = "dgvNewShopItems";
            dgvNewShopItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNewShopItems.Size = new Size(939, 713);
            dgvNewShopItems.TabIndex = 3;
            // 
            // tabNewShopMx
            // 
            tabNewShopMx.Location = new Point(4, 24);
            tabNewShopMx.Name = "tabNewShopMx";
            tabNewShopMx.Size = new Size(772, 382);
            tabNewShopMx.TabIndex = 2;
            tabNewShopMx.Text = "NewShopMx.dat";
            // 
            // ShopEditor
            // 
            ClientSize = new Size(1363, 873);
            Controls.Add(txtClientPath);
            Controls.Add(btnFindPath);
            Controls.Add(btnLoad);
            Controls.Add(btnSave);
            Controls.Add(tabControl);
            Name = "ShopEditor";
            Text = "EO Shop/NewShop Editor";
            tabControl.ResumeLayout(false);
            tabShop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvShops).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).EndInit();
            tabNewShop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvNewShopItems).EndInit();
            ResumeLayout(false);
            PerformLayout();

        }

        #endregion
    }
}
