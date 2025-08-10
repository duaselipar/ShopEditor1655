using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;

namespace NewShopEditor
{
    public partial class ShopEditor : Form
    {
        private List<ShopInfo> allShops = new List<ShopInfo>();
        private string shopDatPath = "";

        private List<string> newShopRawLines = new List<string>();
        private string newShopDatPath = "";

        private Dictionary<string, Dictionary<string, string>> newShopItems = new Dictionary<string, Dictionary<string, string>>();

        public ShopEditor()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            btnFindPath.Click += BtnFindPath_Click;
            btnLoad.Click += BtnLoad_Click;
            btnSave.Click += BtnSave_Click;
            dgvShops.SelectionChanged += DgvShops_SelectionChanged;
            tabControl.Selected += TabControl_Selected;
            tvCategories.AfterSelect += tvCategories_AfterSelect;
        }

        private void BtnFindPath_Click(object sender, EventArgs e)
        {
            using (var fbd = new FolderBrowserDialog())
            {
                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    txtClientPath.Text = fbd.SelectedPath;
                    var possiblePath = Path.Combine(fbd.SelectedPath, "ini", "shop.dat");
                    if (File.Exists(possiblePath))
                    {
                        shopDatPath = possiblePath;
                        btnLoad.Enabled = true;
                        btnSave.Enabled = true;
                    }
                    else
                    {
                        shopDatPath = "";
                        btnLoad.Enabled = false;
                        btnSave.Enabled = false;
                        MessageBox.Show("shop.dat tak jumpa dalam subfolder \\ini !", "Ralat");
                    }
                }
            }
        }

        private void BtnLoad_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(shopDatPath) || !File.Exists(shopDatPath))
            {
                MessageBox.Show("Path shop.dat tak sah!", "Ralat");
                return;
            }
            allShops = ShopDatHandler.Read(shopDatPath);
            LoadShopsToGrid();

            if (ShopDatHandler.hiddenShopIDs.Count > 0)
            {
                MessageBox.Show(
                    "Shop berikut di-hide (untouched):\n" +
                    string.Join(", ", ShopDatHandler.hiddenShopIDs),
                    "Maklumat ShopID Hidden"
                );
            }
        }

        private void LoadShopsToGrid()
        {
            dgvShops.DataSource = null;
            dgvItems.DataSource = null;
            var dt = new DataTable();
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ShopID", typeof(uint));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("Type", typeof(uint));
            dt.Columns.Add("ItemCount", typeof(uint));

            var filteredShops = allShops.FindAll(s => !ShopDatHandler.hiddenShopIDs.Contains(s.ShopID));

            for (int i = 0; i < filteredShops.Count; i++)
            {
                dt.Rows.Add(i + 1, filteredShops[i].ShopID, filteredShops[i].Name, filteredShops[i].Type, filteredShops[i].ItemIDs.Count);
            }
            dgvShops.DataSource = dt;
            if (dt.Rows.Count > 0)
                dgvShops.Rows[0].Selected = true;
        }

        private void DgvShops_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvShops.SelectedRows.Count > 0)
            {
                uint shopId = 0;
                if (dgvShops.SelectedRows[0].Cells["ShopID"].Value != null)
                    shopId = (uint)dgvShops.SelectedRows[0].Cells["ShopID"].Value;

                var shop = allShops.Find(s => s.ShopID == shopId);
                if (shop == null || ShopDatHandler.hiddenShopIDs.Contains(shop.ShopID))
                {
                    dgvItems.DataSource = null;
                    return;
                }

                var dt = new DataTable();
                dt.Columns.Add("No", typeof(int));
                dt.Columns.Add("ItemID", typeof(uint));
                dt.Columns.Add("Reserved1", typeof(uint));
                dt.Columns.Add("Reserved2", typeof(uint));
                for (int i = 0; i < shop.ItemIDs.Count; i++)
                {
                    uint res1 = (shop.Reserved1.Count > i) ? shop.Reserved1[i] : 0;
                    uint res2 = (shop.Reserved2.Count > i) ? shop.Reserved2[i] : 0;
                    dt.Rows.Add(i + 1, shop.ItemIDs[i], res1, res2);
                }
                dgvItems.DataSource = dt;
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(shopDatPath) || allShops == null || allShops.Count == 0)
            {
                MessageBox.Show("Tiada data untuk disimpan!");
                return;
            }
            try
            {
                ShopDatHandler.Save(shopDatPath, allShops);
                MessageBox.Show("Shop.dat berjaya disimpan!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal simpan: " + ex.Message);
            }
        }

        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == tabNewShop && string.IsNullOrEmpty(newShopDatPath))
            {
                var possibleNewShopPath = Path.Combine(txtClientPath.Text, "ini", "newshop.dat");
                if (File.Exists(possibleNewShopPath))
                {
                    newShopDatPath = possibleNewShopPath;
                    newShopRawLines = NewShopDatHandler.Read(newShopDatPath);
                    ParseNewShopItems();
                    LoadCategoriesToTreeView();
                    SaveNewShopIni(Path.Combine(txtClientPath.Text, "ini", "newshop.ini"));
                }
                else
                {
                    MessageBox.Show("newshop.dat tak dijumpai dalam folder \\ini!", "Ralat");
                }
            }
        }

        private void LoadCategoriesToTreeView()
        {
            tvCategories.Nodes.Clear();

            foreach (var section in newShopItems.Keys)
            {
                if (section.StartsWith("ItemSort"))
                {
                    tvCategories.Nodes.Add(section);
                }
            }

            if (tvCategories.Nodes.Count > 0)
                tvCategories.Nodes[0].EnsureVisible();
        }

        private void tvCategories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null) return;
            string selectedCategory = e.Node.Text;

            var items = new List<string>();

            if (newShopItems.TryGetValue(selectedCategory, out var sortData))
            {
                foreach (var entry in sortData)
                {
                    if (entry.Key.StartsWith("ItemID"))
                        items.Add(entry.Value.Trim());
                }
            }

            var dt = new DataTable();
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemID", typeof(string));
            dt.Columns.Add("ItemType", typeof(string));
            dt.Columns.Add("CostType", typeof(string));
            dt.Columns.Add("Version", typeof(string));
            dt.Columns.Add("New", typeof(string));
            dt.Columns.Add("Commend", typeof(string));
            dt.Columns.Add("OEM", typeof(string));
            dt.Columns.Add("Describe", typeof(string));
            dt.Columns.Add("ReturnEmoney", typeof(string));
            dt.Columns.Add("BeginTime", typeof(string));
            dt.Columns.Add("EndTime", typeof(string));
            dt.Columns.Add("Tip", typeof(string));

            for (int i = 0; i < items.Count; i++)
            {
                string key = "Item" + items[i];
                if (newShopItems.TryGetValue(key, out var data))
                {
                    dt.Rows.Add(i + 1,
                        items[i],
                        data.TryGetValue("ItemType", out var itemType) ? itemType : "",
                        data.TryGetValue("CostType", out var costType) ? costType : "",
                        data.TryGetValue("Version", out var version) ? version : "",
                        data.TryGetValue("New", out var isNew) ? isNew : "",
                        data.TryGetValue("Commend", out var commend) ? commend : "",
                        data.TryGetValue("OEM", out var oem) ? oem : "",
                        data.TryGetValue("Describe", out var desc) ? desc : "",
                        data.TryGetValue("ReturnEmoney", out var remoney) ? remoney : "",
                        data.TryGetValue("BeginTime", out var beginTime) ? beginTime : "",
                        data.TryGetValue("EndTime", out var endTime) ? endTime : "",
                        data.TryGetValue("Tip", out var tip) ? tip : ""
                    );
                }
                else
                {
                    dt.Rows.Add(i + 1, items[i], "(Data not found)");
                }
            }

            dgvNewShopItems.DataSource = dt;
        }

        private void ParseNewShopItems()
        {
            newShopItems.Clear();

            string currentSection = "";
            Dictionary<string, string> currentItem = null;

            foreach (var line in newShopRawLines)
            {
                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (currentItem != null && !string.IsNullOrEmpty(currentSection))
                    {
                        newShopItems[currentSection] = currentItem;
                    }

                    currentSection = line.Trim('[', ']');
                    currentItem = new Dictionary<string, string>();
                    continue;
                }

                if (currentItem != null && line.Contains("="))
                {
                    var parts = line.Split(new char[] { '=' }, 2);
                    currentItem[parts[0].Trim()] = parts.Length > 1 ? parts[1].Trim() : "";
                }
            }

            if (currentItem != null && !string.IsNullOrEmpty(currentSection))
            {
                newShopItems[currentSection] = currentItem;
            }
        }

        private void SaveNewShopIni(string savePath)
        {
            try
            {
                File.WriteAllLines(savePath, newShopRawLines, Encoding.UTF8);
                MessageBox.Show("NewShop.ini berjaya disimpan ke:\n" + savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal simpan NewShop.ini: " + ex.Message);
            }
        }
    }
}
