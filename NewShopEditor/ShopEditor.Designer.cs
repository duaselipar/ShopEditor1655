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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ShopEditor));
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
            btnUptName = new Button();
            btnNewItem = new Button();
            btnNewShop = new Button();
            lblShopList = new Label();
            lblItemList = new Label();
            dgvShops = new DataGridView();
            dgvItems = new DataGridView();
            tabNewShopMx = new TabPage();
            btnNewMallItem = new Button();
            lblMxCategoryList = new Label();
            tvMxCategories = new TreeView();
            lblNewShopMxItems = new Label();
            dgvNewShopMxItems = new DataGridView();
            tabPage1 = new TabPage();
            tabWardrobe = new TabControl();
            tabCasual = new TabPage();
            dgvWBCasual = new DataGridView();
            tabWeapon = new TabPage();
            dgvWBWeapon = new DataGridView();
            tabAvatar = new TabPage();
            dgvWBAvatar = new DataGridView();
            tabDecor = new TabPage();
            dgvWBDecor = new DataGridView();
            tabToy = new TabPage();
            dgvWBToy = new DataGridView();
            tabHair = new TabPage();
            dgvWBHair = new DataGridView();
            tabFollowPet = new TabPage();
            dgvWBFPet = new DataGridView();
            tabEudSkin = new TabPage();
            dgvWBEudSkin = new DataGridView();
            tabServant = new TabPage();
            btnNewServerItem = new Button();
            tabControl1 = new TabControl();
            tabGift = new TabPage();
            dgvGift = new DataGridView();
            tabSpirit = new TabPage();
            dgvSpirit = new DataGridView();
            tabEventShop = new TabPage();
            btnNewEventItem = new Button();
            tabControl2 = new TabControl();
            tabAstraShop = new TabPage();
            dgvAstraShop = new DataGridView();
            tabHonorShop = new TabPage();
            dgvHonorShop = new DataGridView();
            tabPlaneShop = new TabPage();
            dgvPlaneShop = new DataGridView();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            label4 = new Label();
            label5 = new Label();
            label6 = new Label();
            tabControl.SuspendLayout();
            tabShop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvShops).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).BeginInit();
            tabNewShopMx.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvNewShopMxItems).BeginInit();
            tabPage1.SuspendLayout();
            tabWardrobe.SuspendLayout();
            tabCasual.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBCasual).BeginInit();
            tabWeapon.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBWeapon).BeginInit();
            tabAvatar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBAvatar).BeginInit();
            tabDecor.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBDecor).BeginInit();
            tabToy.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBToy).BeginInit();
            tabHair.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBHair).BeginInit();
            tabFollowPet.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBFPet).BeginInit();
            tabEudSkin.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvWBEudSkin).BeginInit();
            tabServant.SuspendLayout();
            tabControl1.SuspendLayout();
            tabGift.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvGift).BeginInit();
            tabSpirit.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvSpirit).BeginInit();
            tabEventShop.SuspendLayout();
            tabControl2.SuspendLayout();
            tabAstraShop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvAstraShop).BeginInit();
            tabHonorShop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvHonorShop).BeginInit();
            tabPlaneShop.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvPlaneShop).BeginInit();
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
            txtClientPath.Size = new Size(541, 23);
            txtClientPath.TabIndex = 11;
            // 
            // btnFindPath
            // 
            btnFindPath.Location = new Point(643, 59);
            btnFindPath.Name = "btnFindPath";
            btnFindPath.Size = new Size(85, 23);
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
            tabControl.Controls.Add(tabPage1);
            tabControl.Controls.Add(tabServant);
            tabControl.Controls.Add(tabEventShop);
            tabControl.Location = new Point(10, 95);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1258, 714);
            tabControl.TabIndex = 15;
            // 
            // tabShop
            // 
            tabShop.Controls.Add(btnUptName);
            tabShop.Controls.Add(btnNewItem);
            tabShop.Controls.Add(btnNewShop);
            tabShop.Controls.Add(lblShopList);
            tabShop.Controls.Add(lblItemList);
            tabShop.Controls.Add(dgvShops);
            tabShop.Controls.Add(dgvItems);
            tabShop.Location = new Point(4, 24);
            tabShop.Name = "tabShop";
            tabShop.Size = new Size(1250, 686);
            tabShop.TabIndex = 0;
            tabShop.Text = "All Shop";
            // 
            // btnUptName
            // 
            btnUptName.Location = new Point(101, 15);
            btnUptName.Name = "btnUptName";
            btnUptName.Size = new Size(110, 23);
            btnUptName.TabIndex = 6;
            btnUptName.Text = "Update Name";
            btnUptName.UseVisualStyleBackColor = true;
            // 
            // btnNewItem
            // 
            btnNewItem.Location = new Point(570, 15);
            btnNewItem.Name = "btnNewItem";
            btnNewItem.Size = new Size(75, 23);
            btnNewItem.TabIndex = 5;
            btnNewItem.Text = "New Item";
            btnNewItem.UseVisualStyleBackColor = true;
            btnNewItem.Click += btnNewItem_Click;
            // 
            // btnNewShop
            // 
            btnNewShop.Location = new Point(20, 15);
            btnNewShop.Name = "btnNewShop";
            btnNewShop.Size = new Size(75, 23);
            btnNewShop.TabIndex = 4;
            btnNewShop.Text = "New Shop";
            btnNewShop.UseVisualStyleBackColor = true;
            btnNewShop.Click += btnNewShop_Click;
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
            lblItemList.Location = new Point(570, 45);
            lblItemList.Name = "lblItemList";
            lblItemList.Size = new Size(100, 23);
            lblItemList.TabIndex = 1;
            lblItemList.Text = "Item List:";
            // 
            // dgvShops
            // 
            dgvShops.Location = new Point(20, 71);
            dgvShops.Name = "dgvShops";
            dgvShops.Size = new Size(538, 595);
            dgvShops.TabIndex = 2;
            // 
            // dgvItems
            // 
            dgvItems.Location = new Point(570, 71);
            dgvItems.Name = "dgvItems";
            dgvItems.Size = new Size(667, 595);
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
            tabNewShopMx.Size = new Size(1250, 686);
            tabNewShopMx.TabIndex = 1;
            tabNewShopMx.Text = "VIP Shop/Mall";
            // 
            // btnNewMallItem
            // 
            btnNewMallItem.Location = new Point(264, 10);
            btnNewMallItem.Name = "btnNewMallItem";
            btnNewMallItem.Size = new Size(75, 23);
            btnNewMallItem.TabIndex = 4;
            btnNewMallItem.Text = "New Item";
            btnNewMallItem.UseVisualStyleBackColor = true;
            btnNewMallItem.Click += btnNewMallItem_Click;
            // 
            // lblMxCategoryList
            // 
            lblMxCategoryList.Location = new Point(20, 36);
            lblMxCategoryList.Name = "lblMxCategoryList";
            lblMxCategoryList.Size = new Size(120, 23);
            lblMxCategoryList.TabIndex = 0;
            lblMxCategoryList.Text = "Mx Category List:";
            // 
            // tvMxCategories
            // 
            tvMxCategories.Location = new Point(20, 62);
            tvMxCategories.Name = "tvMxCategories";
            tvMxCategories.Size = new Size(236, 610);
            tvMxCategories.TabIndex = 1;
            // 
            // lblNewShopMxItems
            // 
            lblNewShopMxItems.Location = new Point(264, 36);
            lblNewShopMxItems.Name = "lblNewShopMxItems";
            lblNewShopMxItems.Size = new Size(160, 23);
            lblNewShopMxItems.TabIndex = 2;
            lblNewShopMxItems.Text = "NewShopMx Items:";
            // 
            // dgvNewShopMxItems
            // 
            dgvNewShopMxItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvNewShopMxItems.Location = new Point(264, 62);
            dgvNewShopMxItems.MultiSelect = false;
            dgvNewShopMxItems.Name = "dgvNewShopMxItems";
            dgvNewShopMxItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNewShopMxItems.Size = new Size(971, 610);
            dgvNewShopMxItems.TabIndex = 3;
            // 
            // tabPage1
            // 
            tabPage1.Controls.Add(tabWardrobe);
            tabPage1.Location = new Point(4, 24);
            tabPage1.Name = "tabPage1";
            tabPage1.Padding = new Padding(3);
            tabPage1.Size = new Size(1250, 686);
            tabPage1.TabIndex = 2;
            tabPage1.Text = "Wardrobe Buy";
            tabPage1.UseVisualStyleBackColor = true;
            // 
            // tabWardrobe
            // 
            tabWardrobe.Controls.Add(tabCasual);
            tabWardrobe.Controls.Add(tabWeapon);
            tabWardrobe.Controls.Add(tabAvatar);
            tabWardrobe.Controls.Add(tabDecor);
            tabWardrobe.Controls.Add(tabToy);
            tabWardrobe.Controls.Add(tabHair);
            tabWardrobe.Controls.Add(tabFollowPet);
            tabWardrobe.Controls.Add(tabEudSkin);
            tabWardrobe.Location = new Point(8, 6);
            tabWardrobe.Name = "tabWardrobe";
            tabWardrobe.SelectedIndex = 0;
            tabWardrobe.Size = new Size(1236, 674);
            tabWardrobe.TabIndex = 0;
            // 
            // tabCasual
            // 
            tabCasual.Controls.Add(dgvWBCasual);
            tabCasual.Location = new Point(4, 24);
            tabCasual.Name = "tabCasual";
            tabCasual.Padding = new Padding(3);
            tabCasual.Size = new Size(1228, 646);
            tabCasual.TabIndex = 0;
            tabCasual.Text = "Casual";
            tabCasual.UseVisualStyleBackColor = true;
            // 
            // dgvWBCasual
            // 
            dgvWBCasual.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBCasual.Location = new Point(6, 6);
            dgvWBCasual.Name = "dgvWBCasual";
            dgvWBCasual.Size = new Size(1216, 634);
            dgvWBCasual.TabIndex = 0;
            // 
            // tabWeapon
            // 
            tabWeapon.Controls.Add(dgvWBWeapon);
            tabWeapon.Location = new Point(4, 24);
            tabWeapon.Name = "tabWeapon";
            tabWeapon.Padding = new Padding(3);
            tabWeapon.Size = new Size(1228, 646);
            tabWeapon.TabIndex = 1;
            tabWeapon.Text = "Weapon Soul";
            tabWeapon.UseVisualStyleBackColor = true;
            // 
            // dgvWBWeapon
            // 
            dgvWBWeapon.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBWeapon.Location = new Point(6, 6);
            dgvWBWeapon.Name = "dgvWBWeapon";
            dgvWBWeapon.Size = new Size(1216, 634);
            dgvWBWeapon.TabIndex = 0;
            // 
            // tabAvatar
            // 
            tabAvatar.Controls.Add(dgvWBAvatar);
            tabAvatar.Location = new Point(4, 24);
            tabAvatar.Name = "tabAvatar";
            tabAvatar.Padding = new Padding(3);
            tabAvatar.Size = new Size(1228, 646);
            tabAvatar.TabIndex = 2;
            tabAvatar.Text = "Avatar";
            tabAvatar.UseVisualStyleBackColor = true;
            // 
            // dgvWBAvatar
            // 
            dgvWBAvatar.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBAvatar.Location = new Point(6, 6);
            dgvWBAvatar.Name = "dgvWBAvatar";
            dgvWBAvatar.Size = new Size(1216, 634);
            dgvWBAvatar.TabIndex = 0;
            // 
            // tabDecor
            // 
            tabDecor.Controls.Add(dgvWBDecor);
            tabDecor.Location = new Point(4, 24);
            tabDecor.Name = "tabDecor";
            tabDecor.Padding = new Padding(3);
            tabDecor.Size = new Size(1228, 646);
            tabDecor.TabIndex = 3;
            tabDecor.Text = "Decoration";
            tabDecor.UseVisualStyleBackColor = true;
            // 
            // dgvWBDecor
            // 
            dgvWBDecor.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBDecor.Location = new Point(6, 6);
            dgvWBDecor.Name = "dgvWBDecor";
            dgvWBDecor.Size = new Size(1216, 634);
            dgvWBDecor.TabIndex = 0;
            // 
            // tabToy
            // 
            tabToy.Controls.Add(dgvWBToy);
            tabToy.Location = new Point(4, 24);
            tabToy.Name = "tabToy";
            tabToy.Padding = new Padding(3);
            tabToy.Size = new Size(1228, 646);
            tabToy.TabIndex = 4;
            tabToy.Text = "Toy";
            tabToy.UseVisualStyleBackColor = true;
            // 
            // dgvWBToy
            // 
            dgvWBToy.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBToy.Location = new Point(6, 6);
            dgvWBToy.Name = "dgvWBToy";
            dgvWBToy.Size = new Size(1216, 634);
            dgvWBToy.TabIndex = 0;
            // 
            // tabHair
            // 
            tabHair.Controls.Add(dgvWBHair);
            tabHair.Location = new Point(4, 24);
            tabHair.Name = "tabHair";
            tabHair.Padding = new Padding(3);
            tabHair.Size = new Size(1228, 646);
            tabHair.TabIndex = 5;
            tabHair.Text = "Hair";
            tabHair.UseVisualStyleBackColor = true;
            // 
            // dgvWBHair
            // 
            dgvWBHair.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBHair.Location = new Point(6, 6);
            dgvWBHair.Name = "dgvWBHair";
            dgvWBHair.Size = new Size(1216, 634);
            dgvWBHair.TabIndex = 0;
            // 
            // tabFollowPet
            // 
            tabFollowPet.Controls.Add(dgvWBFPet);
            tabFollowPet.Location = new Point(4, 24);
            tabFollowPet.Name = "tabFollowPet";
            tabFollowPet.Padding = new Padding(3);
            tabFollowPet.Size = new Size(1228, 646);
            tabFollowPet.TabIndex = 6;
            tabFollowPet.Text = "Follow Pet";
            tabFollowPet.UseVisualStyleBackColor = true;
            // 
            // dgvWBFPet
            // 
            dgvWBFPet.AllowUserToAddRows = false;
            dgvWBFPet.AllowUserToDeleteRows = false;
            dgvWBFPet.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBFPet.Location = new Point(6, 6);
            dgvWBFPet.Name = "dgvWBFPet";
            dgvWBFPet.Size = new Size(1216, 634);
            dgvWBFPet.TabIndex = 0;
            // 
            // tabEudSkin
            // 
            tabEudSkin.Controls.Add(dgvWBEudSkin);
            tabEudSkin.Location = new Point(4, 24);
            tabEudSkin.Name = "tabEudSkin";
            tabEudSkin.Padding = new Padding(3);
            tabEudSkin.Size = new Size(1228, 646);
            tabEudSkin.TabIndex = 7;
            tabEudSkin.Text = "Eudemon Skin";
            tabEudSkin.UseVisualStyleBackColor = true;
            // 
            // dgvWBEudSkin
            // 
            dgvWBEudSkin.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvWBEudSkin.Location = new Point(6, 6);
            dgvWBEudSkin.Name = "dgvWBEudSkin";
            dgvWBEudSkin.Size = new Size(1216, 634);
            dgvWBEudSkin.TabIndex = 0;
            // 
            // tabServant
            // 
            tabServant.Controls.Add(btnNewServerItem);
            tabServant.Controls.Add(tabControl1);
            tabServant.Location = new Point(4, 24);
            tabServant.Name = "tabServant";
            tabServant.Padding = new Padding(3);
            tabServant.Size = new Size(1250, 686);
            tabServant.TabIndex = 3;
            tabServant.Text = "Servant Craft";
            tabServant.UseVisualStyleBackColor = true;
            // 
            // btnNewServerItem
            // 
            btnNewServerItem.Location = new Point(6, 6);
            btnNewServerItem.Name = "btnNewServerItem";
            btnNewServerItem.Size = new Size(79, 23);
            btnNewServerItem.TabIndex = 1;
            btnNewServerItem.Text = "New Item";
            btnNewServerItem.UseVisualStyleBackColor = true;
            btnNewServerItem.Click += btnNewServerItem_Click;
            // 
            // tabControl1
            // 
            tabControl1.Controls.Add(tabGift);
            tabControl1.Controls.Add(tabSpirit);
            tabControl1.Location = new Point(6, 35);
            tabControl1.Name = "tabControl1";
            tabControl1.SelectedIndex = 0;
            tabControl1.Size = new Size(1238, 645);
            tabControl1.TabIndex = 0;
            // 
            // tabGift
            // 
            tabGift.Controls.Add(dgvGift);
            tabGift.Location = new Point(4, 24);
            tabGift.Name = "tabGift";
            tabGift.Padding = new Padding(3);
            tabGift.Size = new Size(1230, 617);
            tabGift.TabIndex = 0;
            tabGift.Text = "Gift Master";
            tabGift.UseVisualStyleBackColor = true;
            // 
            // dgvGift
            // 
            dgvGift.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvGift.Location = new Point(6, 6);
            dgvGift.Name = "dgvGift";
            dgvGift.Size = new Size(1218, 599);
            dgvGift.TabIndex = 0;
            // 
            // tabSpirit
            // 
            tabSpirit.Controls.Add(dgvSpirit);
            tabSpirit.Location = new Point(4, 24);
            tabSpirit.Name = "tabSpirit";
            tabSpirit.Padding = new Padding(3);
            tabSpirit.Size = new Size(1230, 617);
            tabSpirit.TabIndex = 1;
            tabSpirit.Text = "Spirit Master";
            tabSpirit.UseVisualStyleBackColor = true;
            // 
            // dgvSpirit
            // 
            dgvSpirit.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvSpirit.Location = new Point(6, 6);
            dgvSpirit.Name = "dgvSpirit";
            dgvSpirit.Size = new Size(1218, 599);
            dgvSpirit.TabIndex = 0;
            // 
            // tabEventShop
            // 
            tabEventShop.Controls.Add(btnNewEventItem);
            tabEventShop.Controls.Add(tabControl2);
            tabEventShop.Location = new Point(4, 24);
            tabEventShop.Name = "tabEventShop";
            tabEventShop.Padding = new Padding(3);
            tabEventShop.Size = new Size(1250, 686);
            tabEventShop.TabIndex = 4;
            tabEventShop.Text = "Event Shop";
            tabEventShop.UseVisualStyleBackColor = true;
            // 
            // btnNewEventItem
            // 
            btnNewEventItem.Location = new Point(6, 6);
            btnNewEventItem.Name = "btnNewEventItem";
            btnNewEventItem.Size = new Size(75, 23);
            btnNewEventItem.TabIndex = 1;
            btnNewEventItem.Text = "New Item";
            btnNewEventItem.UseVisualStyleBackColor = true;
            // 
            // tabControl2
            // 
            tabControl2.Controls.Add(tabAstraShop);
            tabControl2.Controls.Add(tabHonorShop);
            tabControl2.Controls.Add(tabPlaneShop);
            tabControl2.Location = new Point(6, 35);
            tabControl2.Name = "tabControl2";
            tabControl2.SelectedIndex = 0;
            tabControl2.Size = new Size(1238, 645);
            tabControl2.TabIndex = 0;
            // 
            // tabAstraShop
            // 
            tabAstraShop.Controls.Add(dgvAstraShop);
            tabAstraShop.Location = new Point(4, 24);
            tabAstraShop.Name = "tabAstraShop";
            tabAstraShop.Padding = new Padding(3);
            tabAstraShop.Size = new Size(1230, 617);
            tabAstraShop.TabIndex = 0;
            tabAstraShop.Text = "Astra Shop";
            tabAstraShop.UseVisualStyleBackColor = true;
            // 
            // dgvAstraShop
            // 
            dgvAstraShop.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvAstraShop.Location = new Point(6, 6);
            dgvAstraShop.Name = "dgvAstraShop";
            dgvAstraShop.Size = new Size(1218, 605);
            dgvAstraShop.TabIndex = 0;
            // 
            // tabHonorShop
            // 
            tabHonorShop.Controls.Add(dgvHonorShop);
            tabHonorShop.Location = new Point(4, 24);
            tabHonorShop.Name = "tabHonorShop";
            tabHonorShop.Padding = new Padding(3);
            tabHonorShop.Size = new Size(1230, 617);
            tabHonorShop.TabIndex = 1;
            tabHonorShop.Text = "Honor Shop";
            tabHonorShop.UseVisualStyleBackColor = true;
            // 
            // dgvHonorShop
            // 
            dgvHonorShop.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvHonorShop.Location = new Point(6, 6);
            dgvHonorShop.Name = "dgvHonorShop";
            dgvHonorShop.Size = new Size(1218, 605);
            dgvHonorShop.TabIndex = 0;
            // 
            // tabPlaneShop
            // 
            tabPlaneShop.Controls.Add(dgvPlaneShop);
            tabPlaneShop.Location = new Point(4, 24);
            tabPlaneShop.Name = "tabPlaneShop";
            tabPlaneShop.Padding = new Padding(3);
            tabPlaneShop.Size = new Size(1230, 617);
            tabPlaneShop.TabIndex = 2;
            tabPlaneShop.Text = "Plane Shop";
            tabPlaneShop.UseVisualStyleBackColor = true;
            // 
            // dgvPlaneShop
            // 
            dgvPlaneShop.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvPlaneShop.Location = new Point(6, 6);
            dgvPlaneShop.Name = "dgvPlaneShop";
            dgvPlaneShop.Size = new Size(1218, 605);
            dgvPlaneShop.TabIndex = 0;
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
            label6.Location = new Point(15, 61);
            label6.Name = "label6";
            label6.Size = new Size(71, 15);
            label6.TabIndex = 21;
            label6.Text = "Client Path :";
            // 
            // ShopEditor
            // 
            ClientSize = new Size(1272, 813);
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
            FormBorderStyle = FormBorderStyle.FixedSingle;
            Icon = (Icon)resources.GetObject("$this.Icon");
            MaximizeBox = false;
            Name = "ShopEditor";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "EO Shop 1655 Editor - DuaSelipar";
            tabControl.ResumeLayout(false);
            tabShop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvShops).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvItems).EndInit();
            tabNewShopMx.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvNewShopMxItems).EndInit();
            tabPage1.ResumeLayout(false);
            tabWardrobe.ResumeLayout(false);
            tabCasual.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBCasual).EndInit();
            tabWeapon.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBWeapon).EndInit();
            tabAvatar.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBAvatar).EndInit();
            tabDecor.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBDecor).EndInit();
            tabToy.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBToy).EndInit();
            tabHair.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBHair).EndInit();
            tabFollowPet.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBFPet).EndInit();
            tabEudSkin.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvWBEudSkin).EndInit();
            tabServant.ResumeLayout(false);
            tabControl1.ResumeLayout(false);
            tabGift.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvGift).EndInit();
            tabSpirit.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvSpirit).EndInit();
            tabEventShop.ResumeLayout(false);
            tabControl2.ResumeLayout(false);
            tabAstraShop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvAstraShop).EndInit();
            tabHonorShop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvHonorShop).EndInit();
            tabPlaneShop.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvPlaneShop).EndInit();
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
        private Button btnUptName;
        private TabPage tabPage1;
        private TabControl tabWardrobe;
        private TabPage tabCasual;
        private TabPage tabWeapon;
        private TabPage tabAvatar;
        private TabPage tabDecor;
        private TabPage tabToy;
        private TabPage tabHair;
        private DataGridView dgvWBWeapon;
        private DataGridView dgvWBHair;
        private DataGridView dgvWBCasual;
        private DataGridView dgvWBAvatar;
        private DataGridView dgvWBDecor;
        private DataGridView dgvWBToy;
        private TabPage tabFollowPet;
        private DataGridView dgvWBFPet;
        private TabPage tabEudSkin;
        private DataGridView dgvWBEudSkin;
        private TabPage tabServant;
        private TabControl tabControl1;
        private TabPage tabGift;
        private DataGridView dgvGift;
        private TabPage tabSpirit;
        private DataGridView dgvSpirit;
        private Button btnNewServerItem;
        private TabPage tabEventShop;
        private Button btnNewEventItem;
        private TabControl tabControl2;
        private TabPage tabAstraShop;
        private TabPage tabHonorShop;
        private TabPage tabPlaneShop;
        private DataGridView dgvAstraShop;
        private DataGridView dgvHonorShop;
        private DataGridView dgvPlaneShop;
    }
}
