using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Windows.Forms;
using MySql.Data.MySqlClient;

namespace NewShopEditor
{
    public partial class ShopEditor : Form
    {
        // ========== SHOP.DAT ==========
        private List<ShopInfo> allShops = new List<ShopInfo>();
        private string shopDatPath = "";

        // ========== NewShop.dat ==========
        private List<string> newShopRawLines = new List<string>();
        private string newShopDatPath = "";
        private Dictionary<string, Dictionary<string, string>> newShopItems = new Dictionary<string, Dictionary<string, string>>();

        // ========== NewShopMx.dat (NEW) ==========
        private List<string> newShopMxRawLines = new List<string>();
        private string newShopMxDatPath = "";
        private Dictionary<string, Dictionary<string, string>> newShopMxItems = new Dictionary<string, Dictionary<string, string>>();
        private Dictionary<string, Dictionary<string, string>> newShopMxCategories = new();
        private readonly HashSet<string> hiddenMxShops = new() { "9995", "28", "26" };
        private MySqlConnection? conn = null;


        public ShopEditor()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            btnFindPath.Click += BtnFindPath_Click;
            btnSave.Click += BtnSave_Click;
            dgvShops.SelectionChanged += DgvShops_SelectionChanged;
            tabControl.Selected += TabControl_Selected;
            btnConnect.Click += BtnConnect_Click;

            // NEW: hook for NewShopMx tab TreeView
            tvMxCategories.AfterSelect += tvMxCategories_AfterSelect;
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
                        btnSave.Enabled = true;
                    }
                    else
                    {
                        shopDatPath = "";
                        btnSave.Enabled = false;
                        MessageBox.Show("shop.dat tak jumpa dalam subfolder \\ini !", "Ralat");
                    }
                }
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
                dt.Rows.Add(i + 1, filteredShops[i].ShopID, filteredShops[i].Name, filteredShops[i].Type, (uint)filteredShops[i].ItemIDs.Count);
            }
            dgvShops.DataSource = dt;
            if (dt.Rows.Count > 0)
                dgvShops.Rows[0].Selected = true;
        }

        private static readonly Dictionary<int, string> MxCatalogMap = new()
{
    { 0, "Other" },
    { 1, "Eudemon" },
    { 2, "Equipment" },
    { 3, "Item" },
    { 4, "Casual" },
    { 5, "Collection" },
    { 6, "Unknown" }
};


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
            if (conn == null || conn.State != System.Data.ConnectionState.Open)
            {
                MessageBox.Show("Sila sambung ke MySQL dahulu sebelum menyimpan!", "Sambungan Diperlukan");
                return;
            }

            if (string.IsNullOrEmpty(shopDatPath) || allShops == null || allShops.Count == 0)
            {
                MessageBox.Show("Tiada data untuk disimpan!");
                return;
            }

            try
            {
                // Simpan Shop.dat
                ShopDatHandler.Save(shopDatPath, allShops);

                // Simpan newshop.dat
                if (!string.IsNullOrEmpty(newShopDatPath) && newShopRawLines.Count > 0)
                {
                    NewShopDatHandler.Write(newShopDatPath, newShopRawLines);
                    // NOTA: Jangan delete .ini
                }

                // Simpan newshopmx.dat
                if (!string.IsNullOrEmpty(newShopMxDatPath) && newShopMxCategories.Count > 0)
                {
                    NewShopMxDatHandler.Write(newShopMxDatPath, newShopMxCategories);
                    // NOTA: Jangan delete .ini
                }

                MessageBox.Show("Semua data berjaya disimpan!");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal simpan: " + ex.Message);
            }
        }




        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            // hanya handle tabNewShopMx sahaja sekarang
            if (e.TabPage == tabNewShopMx && newShopMxCategories.Count == 0)
            {
                var iniPath = Path.Combine(txtClientPath.Text, "ini", "newshopmx.ini");
                var datPath = Path.Combine(txtClientPath.Text, "ini", "newshopmx.dat");

                if (!File.Exists(iniPath))
                {
                    if (File.Exists(datPath))
                    {
                        newShopMxRawLines = NewShopMxDatHandler.Read(datPath);
                        SaveNewShopMxIni(iniPath);
                    }
                    else
                    {
                        MessageBox.Show("newshopmx.ini & newshopmx.dat tak dijumpai!", "Ralat");
                        return;
                    }
                }
                else
                {
                    newShopMxRawLines = new List<string>(File.ReadAllLines(iniPath));
                }

                newShopMxCategories = ParseIniFile(iniPath);
                LoadMxIniCategoriesToTreeView();
            }
        }



        private void LoadMxIniCategoriesToTreeView()
        {
            tvMxCategories.Nodes.Clear();
            var catalogNodes = new Dictionary<int, TreeNode>();

            foreach (var entry in newShopMxCategories)
            {
                if (!int.TryParse(entry.Key, out _)) continue;
                if (hiddenMxShops.Contains(entry.Key)) continue;

                var data = entry.Value;

                int catalog = data.TryGetValue("Catalog", out var catStr) && int.TryParse(catStr, out var c) ? c : -1;
                string catalogName = MxCatalogMap.TryGetValue(catalog, out var mappedName) ? mappedName : $"Catalog {catalog}";

                if (!catalogNodes.TryGetValue(catalog, out var parent))
                {
                    parent = tvMxCategories.Nodes.Add($"cat_{catalog}", catalogName);
                    catalogNodes[catalog] = parent;
                }

                string label = data.TryGetValue("Name", out var name) ? name : $"[{entry.Key}]";
                if (data.TryGetValue("Amount", out var amtStr) && !string.IsNullOrEmpty(amtStr))
                {
                    label += $" ({amtStr})";
                }

                parent.Nodes.Add(entry.Key, label);
            }

            tvMxCategories.ExpandAll();
        }




        private Dictionary<string, Dictionary<string, string>> ParseIniFile(string path)
        {
            var result = new Dictionary<string, Dictionary<string, string>>();
            string? currentSection = null;
            Dictionary<string, string>? current = null;

            foreach (var raw in File.ReadAllLines(path))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || line.StartsWith(";")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (currentSection != null && current != null)
                        result[currentSection] = current;

                    currentSection = line[1..^1];
                    current = new Dictionary<string, string>();
                }
                else if (current != null && line.Contains('='))
                {
                    var parts = line.Split('=', 2);
                    current[parts[0].Trim()] = parts[1].Trim();
                }
            }

            if (currentSection != null && current != null)
                result[currentSection] = current;

            return result;
        }

        // ==================== NewShop.dat helpers (ASAL) ====================




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
                //MessageBox.Show("NewShop.ini berjaya disimpan ke:\n" + savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal simpan NewShop.ini: " + ex.Message);
            }
        }


        // ==================== NewShopMx.dat helpers (NEW) ====================


        private void tvMxCategories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Parent == null) return; // abaikan parent

            string sectionKey = e.Node.Name;
            if (!newShopMxCategories.TryGetValue(sectionKey, out var category)) return;

            var itemIds = new List<string>();

            // Abaikan ItemSource — terus je scan semua ItemIDx
            foreach (var kv in category)
            {
                if (kv.Key.StartsWith("ItemID"))
                {
                    itemIds.Add(kv.Value.Trim());
                }
            }

            dgvNewShopMxItems.DataSource = CreateItemGrid(itemIds);
        }



        private DataTable CreateItemGrid(List<string> itemIds)
        {
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

            for (int i = 0; i < itemIds.Count; i++)
            {
                string key = "Item" + itemIds[i];
                if (newShopItems.TryGetValue(key, out var data))
                {
                    dt.Rows.Add(i + 1,
                        itemIds[i],
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
            }

            return dt;
        }

        private void SaveNewShopMxIni(string savePath)
        {
            try
            {
                File.WriteAllLines(savePath, newShopMxRawLines, Encoding.UTF8);
                //MessageBox.Show("NewShopMx.ini berjaya disimpan ke:\n" + savePath);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Gagal simpan NewShopMx.ini: " + ex.Message);
            }
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (conn != null && conn.State == System.Data.ConnectionState.Open)
            {
                // Disconnect
                try
                {
                    conn.Close();
                    conn.Dispose();
                    conn = null;
                    btnConnect.Text = "Connect";
                    MessageBox.Show("Disconnected dari MySQL.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal disconnect: " + ex.Message);
                }
                return;
            }

            // Check path
            string clientPath = txtClientPath.Text.Trim();
            if (string.IsNullOrEmpty(clientPath) || !Directory.Exists(clientPath))
            {
                MessageBox.Show("Sila pilih folder client terlebih dahulu.");
                return;
            }

            string shopPath = Path.Combine(clientPath, "ini", "shop.dat");
            string newshopPath = Path.Combine(clientPath, "ini", "newshop.dat");

            if (!File.Exists(shopPath) || !File.Exists(newshopPath))
            {
                MessageBox.Show("shop.dat atau newshop.dat tidak dijumpai dalam folder \\ini!");
                return;
            }

            // Connection details
            string host = txtHost.Text.Trim();
            string port = txtPort.Text.Trim();
            string user = txtUser.Text.Trim();
            string pass = txtPass.Text.Trim();
            string db = txtDb.Text.Trim();

            if (string.IsNullOrEmpty(host) || string.IsNullOrEmpty(port) ||
                string.IsNullOrEmpty(user) || string.IsNullOrEmpty(db))
            {
                MessageBox.Show("Sila isi semua maklumat sambungan MySQL.");
                return;
            }

            string connStr = $"server={host};port={port};uid={user};pwd={pass};database={db};";

            try
            {
                conn = new MySqlConnection(connStr);
                conn.Open();

                if (conn.State == System.Data.ConnectionState.Open)
                {
                    btnConnect.Text = "Disconnect";

                    // Load semua data
                    LoadAllFiles();
                }
            }
            catch (Exception ex)
            {
                conn = null;
                MessageBox.Show("Gagal sambung ke MySQL:\n" + ex.Message);
            }
        }


        private void LoadAllFiles()
        {
            if (string.IsNullOrEmpty(shopDatPath) || !File.Exists(shopDatPath))
            {
                MessageBox.Show("Path shop.dat tak sah!", "Ralat");
                return;
            }

            // ========== SHOP.DAT ==========
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

            // ========== NEWSHOP.DAT ==========
            var newshopPath = Path.Combine(txtClientPath.Text, "ini", "newshop.dat");
            if (File.Exists(newshopPath))
            {
                newShopDatPath = newshopPath;
                newShopRawLines = NewShopDatHandler.Read(newShopDatPath);
                ParseNewShopItems(); // masih perlu untuk item lookup
                SaveNewShopIni(Path.Combine(txtClientPath.Text, "ini", "newshop.ini"));
            }

            // ========== NEWSHOPMX.DAT ==========
            newShopMxDatPath = Path.Combine(txtClientPath.Text, "ini", "newshopmx.dat");
            var mxIniPath = Path.Combine(txtClientPath.Text, "ini", "newshopmx.ini");

            if (!File.Exists(mxIniPath))
            {
                if (File.Exists(newShopMxDatPath))
                {
                    newShopMxRawLines = NewShopMxDatHandler.Read(newShopMxDatPath);
                    SaveNewShopMxIni(mxIniPath);
                }
            }
            else
            {
                newShopMxRawLines = new List<string>(File.ReadAllLines(mxIniPath));
            }

            if (File.Exists(mxIniPath))
            {
                newShopMxCategories = ParseIniFile(mxIniPath);
                LoadMxIniCategoriesToTreeView();
            }
        }


    }
}
