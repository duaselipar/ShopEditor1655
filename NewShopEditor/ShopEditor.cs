using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

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

        // ========== NewShopMx.dat ==========
        private List<string> newShopMxRawLines = new List<string>();
        private string newShopMxDatPath = "";
        private Dictionary<string, Dictionary<string, string>> newShopMxItems = new();
        private Dictionary<string, Dictionary<string, string>> newShopMxCategories = new();
        private readonly HashSet<string> hiddenMxShops = new() { "9995", "28", "26" };

        private MySqlConnection? conn = null;

        // Itemtype cache (uID/szName/uPrice/uEPrice) – shared
        private ItemtypeCache _it;

        public int Gold { get; private set; }
        public int Emoney { get; private set; }


        private const int SHOP_NAME_MAX_BYTES = 16;


        // EXTRA: limits untuk shop [25] (cq_collectiongoods)
        private readonly Dictionary<uint, (int MaxCount, int Supplement)> _mxShop25Limits = new();


        // guard untuk elak reentrancy masa rebind dgvItems
        private bool _rebindingItems = false;

        // drag state
        private int _dragStartRow = -1;
        private Rectangle _dragBoxFromMouseDown = Rectangle.Empty;

        // context menu
        private ContextMenuStrip? _itemsMenu;

        // MX grid state
        private string _currentMxSectionKey = "";
        private ContextMenuStrip? _mxItemsMenu;
        private bool _mxCostBtnPending = false;


        // MX drag & drop state
        private int _mxDragStartRow = -1;
        private Rectangle _mxDragBoxFromMouseDown = Rectangle.Empty;

        private bool _mxCostBtnEnsuring = false;
        private bool _mxChkEnsuring = false;



        private readonly Dictionary<(int Type, uint Key, int Gender), string> hairNameByRelation = new(); // key = (2/4, RelationItem, 1/2)
        private readonly Dictionary<(int Type, uint Key, int Gender), string> hairNameByResource = new(); // key = (2/4, ResourceID, 1/2)


        // [A] FIELDS (add)
        private readonly Dictionary<(int Type, uint RelationItem, int BaseGender), int> hairGenderFlagByRelation = new(); // flag = 1/2/64/128

        // DTs for Wardrobe tabs (so we can append later)
        private DataTable dtWBCasual, dtWBWeapon, dtWBHair, dtWBAvatar, dtWBDecor, dtWBToy;

        // Index from dressroomitem.ini: ItemID -> (Cat, BaseGender, GenderLbl, Buyable, Describe)
        private readonly Dictionary<uint, (int Cat, int BaseGender, string GenderLbl, bool Buyable, string Describe)> _wardrobeIdx
            = new();


        private DataTable dtWBFPet;
        private static readonly Encoding Gbk = Encoding.GetEncoding(936); // GBK
        private static readonly byte[] FP_PASSWORD = Encoding.ASCII.GetBytes("hfydbeir");


        // Eudemons Skin
        private DataTable dtWBEudSkin;

        // Servant Craft (split)
        private DataTable dtGift, dtSpirit;


        // ====== FIELDS (letak dalam class ShopEditor) ======
        private DataTable dtAstraES, dtHonorES, dtPlaneES;

        private const int EventShopDefaultVersion = 49213;

        public ShopEditor()
        {
            InitializeComponent();
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // --- buttons / tabs ---
            btnFindPath.Click += BtnFindPath_Click;
            btnSave.Click += BtnSave_Click;
            tabControl.Selected += TabControl_Selected;
            btnConnect.Click += BtnConnect_Click;

            // --- shops master grid ---
            dgvShops.SelectionChanged += DgvShops_SelectionChanged;

            // --- MX tree & grid ---
            tvMxCategories.AfterSelect += tvMxCategories_AfterSelect;

            // pastikan tiada duplikasi
            dgvItems.CurrentCellDirtyStateChanged -= Dgv_CommitOnDirty;
            dgvItems.CellEndEdit -= DgvItems_CellEndEdit_Safe;
            dgvNewShopMxItems.CurrentCellDirtyStateChanged -= Dgv_CommitOnDirty;
            dgvNewShopMxItems.CellEndEdit -= DgvNewShopMxItems_CellEndEdit;

            // hook SHOP & MX
            dgvItems.CurrentCellDirtyStateChanged += Dgv_CommitOnDirty;
            dgvItems.CellEndEdit += DgvItems_CellEndEdit_Safe;
            dgvNewShopMxItems.CurrentCellDirtyStateChanged += Dgv_CommitOnDirty;
            dgvNewShopMxItems.CellEndEdit += DgvNewShopMxItems_CellEndEdit;


            dgvItems.DataBindingComplete += (s, e) => HideItemReservedCols();


            // row-only select
            dgvShops.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvShops.MultiSelect = false;
            dgvShops.AllowUserToAddRows = false;
            dgvShops.AllowUserToDeleteRows = false;

            // toggle Type button + format
            dgvShops.CellFormatting += DgvShops_CellFormatting_TypeButton;
            dgvShops.CellClick += DgvShops_CellClick_TypeButton;

            // edit tracking
            dgvShops.CellBeginEdit += DgvShops_CellBeginEdit;
            dgvShops.CellEndEdit += DgvShops_CellEndEdit_UpdateShop;

            // right-click select row + context menu
            dgvShops.MouseDown += DgvShops_MouseDown_SelectOnRightClick;
            InitShopContextMenu();

            btnUptName.Click += btnUptName_Click;


            // === ITEMS GRID BEHAVIOUR ===
            dgvItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect; // (1)(4) row-only select + multi
            dgvItems.MultiSelect = true;
            dgvItems.AllowUserToAddRows = false;
            dgvItems.AllowUserToDeleteRows = false;
            dgvItems.AllowDrop = true;

            // auto apply visibility/readonly rules lepas bind
            dgvItems.DataBindingComplete += (s, e) => { HideItemReservedCols(); ApplyItemGridEditRules(); };

            // drag & drop reorder (3)
            dgvItems.MouseDown += DgvItems_MouseDown;
            dgvItems.MouseMove += DgvItems_MouseMove;
            dgvItems.DragOver += DgvItems_DragOver;
            dgvItems.DragDrop += DgvItems_DragDrop;

            // right-click: select row + context menu (3)(5)
            dgvItems.MouseDown += DgvItems_MouseDown_SelectOnRightClick;
            InitItemsContextMenu();


            // --- NEW: hook grid NewShop secara dinamik (ikut nama control sebenar) ---
            var newShopGrid = FindNewShopGrid();
            if (newShopGrid != null)
            {
                newShopGrid.CurrentCellDirtyStateChanged -= Dgv_CommitOnDirty;
                newShopGrid.CellEndEdit -= DgvNewShopGrid_CellEndEdit;

                newShopGrid.CurrentCellDirtyStateChanged += Dgv_CommitOnDirty;
                newShopGrid.CellEndEdit += DgvNewShopGrid_CellEndEdit;
            }


            // --- items grid: lock sizing ---
            dgvItems.AllowUserToResizeColumns = false;
            dgvItems.AllowUserToResizeRows = false;
            dgvItems.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvItems.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // apply after every bind
            dgvItems.DataBindingComplete += (s, e) =>
            {
                HideItemReservedCols();
                ApplyItemGridEditRules();
                ApplyItemGridSizingRules(); // <- set widths
            };

            // ---- shop grid: lock sizing ----
            dgvShops.AllowUserToResizeColumns = false;
            dgvShops.AllowUserToResizeRows = false;
            dgvShops.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvShops.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // re-apply widths setiap kali bind
            dgvShops.DataBindingComplete += (s, e) => ApplyShopGridSizingRules();

            // ==== NewShopMx items grid ====
            dgvNewShopMxItems.MultiSelect = true;
            dgvNewShopMxItems.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvNewShopMxItems.AllowUserToAddRows = false;
            dgvNewShopMxItems.AllowUserToDeleteRows = false;




            // enable DnD
            dgvNewShopMxItems.AllowDrop = true;
            dgvNewShopMxItems.MouseDown += DgvMxItems_MouseDown_DnD;
            dgvNewShopMxItems.MouseMove += DgvMxItems_MouseMove_DnD;
            dgvNewShopMxItems.DragOver += DgvMxItems_DragOver_DnD;
            dgvNewShopMxItems.DragDrop += DgvMxItems_DragDrop_DnD;

            // right-click same as shop itemlist
            dgvNewShopMxItems.MouseDown += DgvMxItems_MouseDown_SelectOnRightClick;
            InitMxItemsContextMenu();

            // MX: edit rules + checkboxes + writeback
            dgvNewShopMxItems.DataBindingComplete += (s, e) => ApplyMxGridUi();     // already used earlier
            dgvNewShopMxItems.CellFormatting += DgvMxItems_CellFormatting_Checks;   // render New/Commend checkboxes
            dgvNewShopMxItems.CellContentClick += DgvMxItems_CellContentClick_Checks; // toggle + writeback
            dgvNewShopMxItems.CellEndEdit += DgvMxItems_CellEndEdit_WriteBack;      // Version/Tip writeback
            dgvNewShopMxItems.DataError += (s, e) => e.ThrowException = false;

            // CostType button
            dgvNewShopMxItems.CellFormatting += DgvMxItems_CellFormatting_CostTypeBtn;
            dgvNewShopMxItems.CellClick += DgvMxItems_CellClick_CostTypeBtn;

            // --- NEW: lock sizing like shop item list ---
            dgvNewShopMxItems.AllowUserToResizeColumns = false;
            dgvNewShopMxItems.AllowUserToResizeRows = false;
            dgvNewShopMxItems.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            dgvNewShopMxItems.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            // SHOP items: fill sizing
            dgvItems.DataBindingComplete += (s, e) =>
            {
                HideItemReservedCols();
                ApplyItemGridEditRules();     // No/ItemID/Name RO; Gold/Emoney editable
                ApplyItemGridFillSizing();    // <-- fill ikut grid width
            };

            // MX items: fill sizing
            dgvNewShopMxItems.DataBindingComplete += (s, e) =>
            {
                ApplyMxGridUi();              // hide cols + CostTypeBtn + checkbox
                ApplyMxReadOnlyRules();       // No/Name/ItemType RO; Version/Tip editable
                ApplyMxItemGridFillSizing();  // <-- fill ikut grid width
            };


            dgvItems.AllowUserToOrderColumns = false;
            dgvNewShopMxItems.AllowUserToOrderColumns = false;

            // block tukar tab jika tak connected
            tabControl.Selecting += TabControl_Selecting_BlockWhenDisconnected;

            // init UI state (mula-mula belum connect)
            UpdateConnectUi();




            // --- WARDROBE: commit & sync on edit ---
            dgvWBCasual.CurrentCellDirtyStateChanged += Dgv_CommitOnDirty;
            dgvWBWeapon.CurrentCellDirtyStateChanged += Dgv_CommitOnDirty;
            dgvWBDecor.CurrentCellDirtyStateChanged += Dgv_CommitOnDirty;
            dgvWBToy.CurrentCellDirtyStateChanged += Dgv_CommitOnDirty;

            dgvWBCasual.CellEndEdit += Wardrobe_CellEndEdit;
            dgvWBWeapon.CellEndEdit += Wardrobe_CellEndEdit;
            dgvWBDecor.CellEndEdit += Wardrobe_CellEndEdit;
            dgvWBToy.CellEndEdit += Wardrobe_CellEndEdit;

            dgvWBFPet.DataBindingComplete += dgvWBFPet_DataBindingComplete;


            dgvGift.DataBindingComplete -= DgvServant_AfterBind;
            dgvGift.DataBindingComplete += DgvServant_AfterBind;

            dgvSpirit.DataBindingComplete -= DgvServant_AfterBind;
            dgvSpirit.DataBindingComplete += DgvServant_AfterBind;

            btnNewEventItem.Click += btnNewEventItem_Click;

        }


        // Wardrobe Price edit -> write to itemtype cache and refresh other grids
        private void Wardrobe_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView gv) return;
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var col = gv.Columns[e.ColumnIndex].Name;
            if (col != "Price") return; // only sync when Price edited

            uint itemId = ToUInt(gv.Rows[e.RowIndex].Cells["ItemID"]?.Value);
            int newEp = ToInt(gv.Rows[e.RowIndex].Cells["Price"]?.Value);
            if (itemId == 0 || newEp < 0) return;

            // update shared itemtype cache (same style as Shop/NewShop handlers)
            if (_it != null)
            {
                int curGold = 0;
                if (_it.ById.TryGetValue(itemId, out var byId)) curGold = byId.Gold;
                else if (_it.ByType.TryGetValue(itemId, out var byType)) curGold = byType.Gold;

                if (_it.ById != null)
                {
                    if (_it.ById.TryGetValue(itemId, out var info))
                        _it.ById[itemId] = (info.Name, curGold, newEp);
                    else
                        _it.ById[itemId] = ("", curGold, newEp);
                }
                if (_it.ByType != null)
                {
                    if (_it.ByType.TryGetValue(itemId, out var infoT))
                        _it.ByType[itemId] = (infoT.Name, curGold, newEp);
                    else
                        _it.ByType[itemId] = ("", curGold, newEp);
                }
                if (_it.GridTable != null)
                {
                    var rs = _it.GridTable.Select("ID = " + itemId);
                    foreach (var r in rs) r["Emoney"] = newEp;
                    _it.GridTable.AcceptChanges();
                }
            }

            // propagate to Shop list, NewShopMx grid, and other Wardrobe tabs
            if (dgvShops.SelectedRows.Count > 0)
            {
                var v = dgvShops.SelectedRows[0].Cells["ShopID"].Value;
                if (v != null && UInt32.TryParse(v.ToString(), out uint sid))
                    BeginInvoke(new Action(() => RebindItemsForShop_Safe(sid)));
            }
            BeginInvoke(new Action(() => SyncMxGrid(itemId, newEp)));
            BeginInvoke(new Action(() => SyncWardrobePrice(itemId, newEp)));
        }

        // Push EP change to Wardrobe (only 0/1/5/6 tabs)
        // --- Wardrobe sync helpers ---
        // -------- Wardrobe sync helpers --------
        private DataGridView? FindWardrobeGrid(string name)
        {
            var a = this.Controls.Find(name, true);
            return (a.Length > 0 && a[0] is DataGridView gv) ? gv : null;
        }
        private void UpdateWardrobePriceInDt(DataGridView? gv, uint itemId, int newPrice)
        {
            if (gv?.DataSource is DataTable dt && dt.Columns.Contains("ItemID") && dt.Columns.Contains("Price"))
            {
                bool touched = false;
                foreach (DataRow r in dt.Rows)
                {
                    if (Convert.ToUInt32(r["ItemID"]) == itemId) { r["Price"] = newPrice; touched = true; }
                }
                if (touched)
                {
                    try { dt.AcceptChanges(); } catch { }
                    try { gv.Refresh(); } catch { }
                }
            }
        }
        private void SyncWardrobePrice(uint itemId, int ep)
        {
            UpdateWardrobePriceInDt(FindWardrobeGrid("dgvWBCasual"), itemId, ep);
            UpdateWardrobePriceInDt(FindWardrobeGrid("dgvWBWeapon"), itemId, ep);
            UpdateWardrobePriceInDt(FindWardrobeGrid("dgvWBDecor"), itemId, ep);
            UpdateWardrobePriceInDt(FindWardrobeGrid("dgvWBToy"), itemId, ep);
        }



        private void UpdateGridPriceByItemId(DataGridView gv, uint itemId, int newPrice)
        {
            if (gv == null || gv.DataSource == null) return;
            if (!gv.Columns.Contains("ItemID") || !gv.Columns.Contains("Price")) return;

            foreach (DataGridViewRow r in gv.Rows)
            {
                if (r.IsNewRow) continue;
                if (ToUInt(r.Cells["ItemID"]?.Value) == itemId)
                    r.Cells["Price"].Value = newPrice;
            }
        }
        private void TryAppendWardrobeRow(uint itemId, int ep)
        {
            if (!_wardrobeIdx.TryGetValue(itemId, out var info)) return;

            string nm = GetItemName(itemId);
            var (cat, _baseGen, genderLbl, buyable, describe) = info;

            switch (cat)
            {
                case 0: // Casual
                    if (dtWBCasual != null)
                    {
                        dtWBCasual.Rows.Add(dtWBCasual.Rows.Count + 1, itemId, nm, genderLbl, buyable, ep, describe);
                        try { dtWBCasual.AcceptChanges(); dgvWBCasual.Refresh(); } catch { }
                    }
                    break;
                case 1: // Weapon
                    if (dtWBWeapon != null)
                    {
                        dtWBWeapon.Rows.Add(dtWBWeapon.Rows.Count + 1, itemId, nm, buyable, ep, describe);
                        try { dtWBWeapon.AcceptChanges(); dgvWBWeapon.Refresh(); } catch { }
                    }
                    break;
                case 5: // Decor
                    if (dtWBDecor != null)
                    {
                        dtWBDecor.Rows.Add(dtWBDecor.Rows.Count + 1, itemId, nm, buyable, ep, describe);
                        try { dtWBDecor.AcceptChanges(); dgvWBDecor.Refresh(); } catch { }
                    }
                    break;
                case 6: // Toy
                    if (dtWBToy != null)
                    {
                        dtWBToy.Rows.Add(dtWBToy.Rows.Count + 1, itemId, nm, buyable, ep, describe);
                        try { dtWBToy.AcceptChanges(); dgvWBToy.Refresh(); } catch { }
                    }
                    break;
                default:
                    // hair/avatar (2/4) not auto-appended here by design
                    break;
            }
        }
        // (KEKAL) ini biar ada sebagai fallback layer cell


        // ===== [2] Loader hairdata.ini -> maps =====
        private void EnsureHairNamesLoaded()
        {
            if (hairNameByRelation.Count > 0) return;
            LoadHairAvatarNamesFromIni(txtClientPath.Text.Trim());
        }

        // [B] LOADER hairdata.ini (replace your LoadHairAvatarNamesFromIni with this stronger one)
        // FIX: hairdata.ini format = TYPE RelationItem BaseGender Name ResourceID 0 GenderFlag
        private void LoadHairAvatarNamesFromIni(string clientRoot)
        {
            try
            {
                var p1 = Path.Combine(clientRoot, "ini", "hairdata.ini");
                var p2 = Path.Combine(clientRoot, "hairdata.ini");
                var path = File.Exists(p1) ? p1 : (File.Exists(p2) ? p2 : "");
                if (string.IsNullOrEmpty(path)) return;

                hairNameByRelation.Clear();
                hairNameByResource.Clear();
                hairGenderFlagByRelation.Clear();

                var enc = System.Text.Encoding.GetEncoding(936);
                foreach (var raw in File.ReadLines(path, enc))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("//")) continue;

                    var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length < 7) continue;

                    int type = SafeToInt(parts[0]);            // 2=Hair, 4=Avatar
                    if (type != 2 && type != 4) continue;

                    uint relationItem = SafeToUInt(parts[1]);           // <-- match dressroom ItemID
                    int baseGender = SafeToInt(parts[2]);            // 1/2
                    string name = parts[3].Replace("~", " ");      // display name
                    uint resourceId = SafeToUInt(parts[4]);           // resource (fallback key)
                                                                      // parts[5] = 0 (ignore)
                    int genderFlag = SafeToInt(parts[6]);            // 1/2/64/128

                    // store by relation (primary)
                    hairNameByRelation[(type, relationItem, baseGender)] = name;
                    hairGenderFlagByRelation[(type, relationItem, baseGender)] = genderFlag;

                    // also keep by resource (secondary fallback)
                    hairNameByResource[(type, resourceId, baseGender)] = name;
                }
            }
            catch { /* ignore */ }
        }

        private static uint SafeToUInt(string s) => uint.TryParse(s, out var v) ? v : 0;

        // [C] HELPERS (add)
        // ===== Helper: map gender flag (from dressroomitem.ini) =====
        private static string GenderLabelFromFlag(int code) => code switch
        {
            64 => "Male(SS)",
            128 => "Female(SS)",
            1 => "Male",
            2 => "Female",
            _ => ""
        };

        // Find 64/128 anywhere after base fields; fallback to baseGender(1/2)
        private static string ComputeDressroomGenderLabel(string[] parts, int baseGender)
        {
            for (int i = 3; i < parts.Length; i++)
            {
                int v = SafeToInt(parts[i]);
                if (v == 64 || v == 128) return GenderLabelFromFlag(v);
            }
            return GenderLabelFromFlag(baseGender);
        }


        private string GetHairAvatarGenderLabel(int type, uint relationItem, int fallbackBaseGender)
        {
            // Prefer exact match TYPE + RelationItem + BaseGender
            if (hairGenderFlagByRelation.TryGetValue((type, relationItem, fallbackBaseGender), out var flag))
                return GenderLabelFromFlag(flag);

            // Fallback: try any gender we have for that RelationItem
            if (hairGenderFlagByRelation.TryGetValue((type, relationItem, 1), out flag))
                return GenderLabelFromFlag(flag);
            if (hairGenderFlagByRelation.TryGetValue((type, relationItem, 2), out flag))
                return GenderLabelFromFlag(flag);

            // Last resort: use dressroom gender
            return GenderLabelFromFlag(fallbackBaseGender);
        }


        // ===== [3] Helper ambil nama hair/avatar ikut TYPE + GENDER =====
        private string GetHairAvatarName(int type, uint itemId, int gender)
        {
            // 1) match by RelationItem == dressroom ItemID
            if (hairNameByRelation.TryGetValue((type, itemId, gender), out var n1)) return n1;

            // 2) (fallback jarang kena) cuba treat ItemID as ResourceID
            if (hairNameByResource.TryGetValue((type, itemId, gender), out var n2)) return n2;

            return "";
        }

        private readonly Dictionary<uint, string> itemNameMap = new(); // ItemID -> Name

        // ================== A) Ensure names loaded ==================
        private void EnsureItemNamesLoaded()
        {
            if (itemNameMap.Count > 0) return;

            // Prefer shared cache if available
            try
            {
                if (_it != null && _it.ById != null && _it.ById.Count > 0)
                {
                    foreach (var kv in _it.ById)
                    {
                        var id = kv.Key;
                        var name = kv.Value.Name ?? "";
                        if (!itemNameMap.ContainsKey(id))
                            itemNameMap[id] = name;
                    }
                }
            }
            catch { /* ignore */ }

            // If still empty, load via FDB
            if (itemNameMap.Count == 0)
                LoadItemNamesFromFDBSafe(txtClientPath.Text.Trim());
        }

        // ================== B) Stronger FDB name loader ==================
        private void LoadItemNamesFromFDBSafe(string clientRoot)
        {
            itemNameMap.Clear();
            try
            {
                var fdb1 = Path.Combine(clientRoot, "ini", "itemtype.fdb");
                var fdb2 = Path.Combine(clientRoot, "itemtype.fdb");
                var fdbPath = File.Exists(fdb1) ? fdb1 : (File.Exists(fdb2) ? fdb2 : "");
                if (string.IsNullOrEmpty(fdbPath)) return;

                // Try both possible assemblies/namespaces from your repo(s)
                var candidates = new[]
                {
            "FDBRead.FDBLoaderEPLStyle, FDBRead",
            "FDBReadOnly.FDBLoaderEPLStyle, FDBRead-Only",
            "FDBReadOnly.FDBLoaderEPLStyle, FDBReadOnly"
        };

                Type loaderType = null;
                foreach (var c in candidates)
                {
                    loaderType = Type.GetType(c);
                    if (loaderType != null) break;
                }
                if (loaderType == null) return;

                var mi = loaderType.GetMethod("Load", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                if (mi == null) return;

                var result = mi.Invoke(null, new object[] { fdbPath }) as System.Collections.IEnumerable;
                if (result == null) return;

                foreach (var row in result)
                {
                    var t = row.GetType();
                    // Flexible ID/Name property names
                    var idProp = t.GetProperty("Id") ?? t.GetProperty("ID") ?? t.GetProperty("Type") ?? t.GetProperty("TYPE") ?? t.GetProperty("ItemID");
                    var nameProp = t.GetProperty("Name") ?? t.GetProperty("NAME") ?? t.GetProperty("StrName") ?? t.GetProperty("szName");

                    if (idProp == null || nameProp == null) continue;
                    var idObj = idProp.GetValue(row);
                    var nameObj = nameProp.GetValue(row) ?? "";
                    if (idObj == null) continue;

                    uint id = Convert.ToUInt32(idObj);
                    string name = Convert.ToString(nameObj) ?? "";
                    if (!itemNameMap.ContainsKey(id))
                        itemNameMap[id] = name;
                }
            }
            catch { /* ignore */ }
        }
        private string GetItemName(uint itemId)
            => itemNameMap.TryGetValue(itemId, out var n) ? n : "";

        // ==== 1) Table factory (support Gender) ====
        private DataTable CreateWardrobeTable(bool withGender, bool withName)
        {
            var dt = new DataTable();
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemID", typeof(uint));
            if (withName) dt.Columns.Add("Name", typeof(string));   // read-only
            if (withGender) dt.Columns.Add("Gender", typeof(string)); // "Male"/"Female", read-only
            dt.Columns.Add("Buyable", typeof(bool));
            dt.Columns.Add("Price", typeof(int));
            dt.Columns.Add("Describe", typeof(string));
            return dt;
        }


        private DataTable CreateWardrobeTable(bool withGender) => CreateWardrobeTable(withGender, false);
        private DataTable CreateWardrobeTable() => CreateWardrobeTable(false, false);

        // === Wardrobe Buy loader (Price by category) ===
        // Only 0/1/5/6 use Emoney from itemtype.fdb; 2/4 keep original price from dressroom
        // === Wardrobe Buy loader (build index, keep DT references) ===
        private void LoadWardrobeBuyFromIni(string dressroomIniPath)
        {
            EnsureItemNamesLoaded();
            EnsureHairNamesLoaded();
            EnsureItemtypeIndex();

            dtWBCasual = CreateWardrobeTable(withGender: true, withName: true);
            dtWBWeapon = CreateWardrobeTable(withGender: false, withName: true);
            dtWBHair = CreateWardrobeTable(withGender: true, withName: true);
            dtWBAvatar = CreateWardrobeTable(withGender: true, withName: true);
            dtWBDecor = CreateWardrobeTable(withGender: false, withName: true);
            dtWBToy = CreateWardrobeTable(withGender: false, withName: true);

            _wardrobeIdx.Clear();

            int nCasual = 0, nWeapon = 0, nHair = 0, nAvatar = 0, nDecor = 0, nToy = 0;
            var enc = System.Text.Encoding.GetEncoding(936);

            foreach (var raw in System.IO.File.ReadLines(dressroomIniPath, enc))
            {
                var line = raw.Trim();
                if (string.IsNullOrEmpty(line) || !char.IsDigit(line[0])) continue;

                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length < 7) continue;

                if (!int.TryParse(parts[0], out var cat)) continue;
                if (!uint.TryParse(parts[1], out var itemId)) continue;

                int baseGender = SafeToInt(parts[2]);               // 1/2
                bool buyable = parts[3] == "1";
                int priceIni = SafeToInt(parts[4]);
                string describe = parts[6].Replace("~", " ");
                string genderLbl = ComputeDressroomGenderLabel(parts, baseGender); // 1/2/64/128 -> label

                // index for later appends
                _wardrobeIdx[itemId] = (cat, baseGender, genderLbl, buyable, describe);

                // Name
                string nmFDB = GetItemName(itemId);
                // Price policy per category (0/1/5/6 use EP)
                int price = priceIni;
                if (cat == 0 || cat == 1 || cat == 5 || cat == 6)
                {
                    var (_, ep) = GetPriceFor(itemId);
                    if (ep > 0) price = ep;
                }

                switch (cat)
                {
                    case 0: // CASUAL
                        dtWBCasual.Rows.Add(++nCasual, itemId, nmFDB, genderLbl, buyable, price, describe);
                        break;
                    case 1: // WEAPON
                        dtWBWeapon.Rows.Add(++nWeapon, itemId, nmFDB, buyable, price, describe);
                        break;
                    case 2: // HAIR (Name from hairdata.ini fallback FDB)
                        {
                            string nm = GetHairAvatarName(2, itemId, baseGender);
                            if (string.IsNullOrEmpty(nm)) nm = nmFDB;
                            dtWBHair.Rows.Add(++nHair, itemId, nm, genderLbl, buyable, priceIni, describe);
                            break;
                        }
                    case 4: // AVATAR (Name from hairdata.ini fallback FDB)
                        {
                            string nm = GetHairAvatarName(4, itemId, baseGender);
                            if (string.IsNullOrEmpty(nm)) nm = nmFDB;
                            dtWBAvatar.Rows.Add(++nAvatar, itemId, nm, genderLbl, buyable, priceIni, describe);
                            break;
                        }
                    case 5: // DECOR
                        dtWBDecor.Rows.Add(++nDecor, itemId, nmFDB, buyable, price, describe);
                        break;
                    case 6: // TOY
                        dtWBToy.Rows.Add(++nToy, itemId, nmFDB, buyable, price, describe);
                        break;
                }
            }

            BindWardrobeGrid(dgvWBCasual, dtWBCasual);
            BindWardrobeGrid(dgvWBWeapon, dtWBWeapon);
            BindWardrobeGrid(dgvWBHair, dtWBHair);
            BindWardrobeGrid(dgvWBAvatar, dtWBAvatar);
            BindWardrobeGrid(dgvWBDecor, dtWBDecor);
            BindWardrobeGrid(dgvWBToy, dtWBToy);
        }



        // ==== 3) Sizing lepas bind (add Gender weight if exists) ====
        // ===== Dalam WardrobeGrid_DataBindingComplete, tukar ke BeginInvoke =====
        // Tetapkan lebar tetap utk No/ItemID/Name/Gender, lain ikut design
        private void WardrobeGrid_DataBindingComplete(object? sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is not DataGridView gv) return;

            // nilai lebar (px) — ubah ikut selera
            const int W_NO = 60;
            const int W_ITEMID = 110;
            const int W_NAME = 240;
            const int W_GENDER = 110;
            const int W_BUYABLE = 80;
            const int W_PRICE = 100;

            void Fix(string colName, int w)
            {
                if (!gv.Columns.Contains(colName)) return;
                var c = gv.Columns[colName];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // fixed
                c.Width = w;
                c.Resizable = DataGridViewTriState.False;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // set kolum fixed
            Fix("No", W_NO);
            Fix("ItemID", W_ITEMID);
            if (gv.Columns.Contains("Name")) Fix("Name", W_NAME);
            if (gv.Columns.Contains("Gender")) Fix("Gender", W_GENDER);
            if (gv.Columns.Contains("Buyable")) Fix("Buyable", W_BUYABLE);
            if (gv.Columns.Contains("Price")) Fix("Price", W_PRICE);

            // Describe biar flexible (isi ruang)
            if (gv.Columns.Contains("Describe"))
            {
                var d = gv.Columns["Describe"];
                d.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill; // only this one fills
                d.Resizable = DataGridViewTriState.False;
                d.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // kekalkan lock edit yg kita set sebelum ini
            ApplyWardrobeEditableColumns(gv);
        }



        // ==== 4) (unchanged) helper ====
        private static int SafeToInt(string s) => int.TryParse(s, out var v) ? v : 0;

        // ===== Lock kolum No/ItemID/Gender =====
        private void ApplyWardrobeEditableColumns(DataGridView gv)
        {
            foreach (DataGridViewColumn c in gv.Columns)
                c.ReadOnly = false;

            void Lock(string name)
            {
                if (!gv.Columns.Contains(name)) return;
                var c = gv.Columns[name];
                c.ReadOnly = true;
                try { c.DefaultCellStyle.BackColor = System.Drawing.Color.FromArgb(240, 240, 240); } catch { }
            }

            Lock("No");
            Lock("ItemID");
            Lock("Gender"); // exists only on some tabs
            Lock("Name");   // NEW: read-only
        }

        // ===== Sizing helper (tanpa MinimumWidth) =====
        // ===== SAFE SIZING (REPLACE fungsi lama SetWeightSafe) =====
        private void SetWeightSafe(DataGridView gv, string name, float weight, int minWidth)
        {
            if (gv == null || gv.IsDisposed) return;
            if (!gv.IsHandleCreated) return;                 // elak band null
            if (!gv.Columns.Contains(name)) return;

            var c = gv.Columns[name];
            if (c == null) return;

            try
            {
                gv.SuspendLayout();

                // ❌ JANGAN guna MinimumWidth (trigger MinimumThickness)
                // Set sementara ke None, set Width, kemudian Fill balik
                var prevMode = c.AutoSizeMode;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;

                if (minWidth > 0 && c.Width < minWidth)
                    c.Width = minWidth;                      // hanya Width biasa

                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c.FillWeight = (weight <= 0f) ? 1f : weight;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.Resizable = DataGridViewTriState.False;
            }
            catch { /* ignore */ }
            finally
            {
                try { gv.ResumeLayout(); } catch { }
            }
        }


        private void BindWardrobeGrid(DataGridView gv, DataTable dt)
        {
            if (gv == null) return;

            gv.SuspendLayout();
            gv.AutoGenerateColumns = true;
            gv.DataSource = null;
            gv.DataSource = dt;

            gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None; // << fixed widths
            gv.AllowUserToAddRows = false;
            gv.AllowUserToOrderColumns = false;
            gv.AllowUserToResizeColumns = false;
            gv.AllowUserToResizeRows = false;
            gv.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            gv.RowHeadersVisible = false;
            gv.ReadOnly = false;

            // pastikan Buyable checkbox
            if (gv.Columns.Contains("Buyable") && gv.Columns["Buyable"] is not DataGridViewCheckBoxColumn)
            {
                int idx = gv.Columns["Buyable"].Index;
                gv.Columns.Remove("Buyable");
                var chk = new DataGridViewCheckBoxColumn
                {
                    Name = "Buyable",
                    DataPropertyName = "Buyable",
                    HeaderText = "Buyable",
                    ThreeState = false
                };
                gv.Columns.Insert(idx, chk);
            }

            gv.DataBindingComplete -= WardrobeGrid_DataBindingComplete;
            gv.DataBindingComplete += WardrobeGrid_DataBindingComplete;

            // elak crash
            gv.DataError -= (s, e) => { };
            gv.DataError += (s, e) => { e.ThrowException = false; };

            gv.ResumeLayout();
        }



        // Is DB connected?
        private bool IsConnected() => conn != null && conn.State == ConnectionState.Open;

        // Enable/disable tabs & kandungan ikut status connection
        // Enable/disable tabs & kandungan ikut status connection
        private void UpdateConnectUi()
        {
            bool on = IsConnected();

            // TabPages utama
            tabShop.Enabled = on;
            tabNewShopMx.Enabled = on;

            // Wardrobe Buy
            tabPage1.Enabled = on;
            dgvWBCasual.Enabled = on;
            dgvWBWeapon.Enabled = on;
            dgvWBHair.Enabled = on;
            dgvWBAvatar.Enabled = on;
            dgvWBDecor.Enabled = on;
            dgvWBToy.Enabled = on;

            // Servant Craft
            tabServant.Enabled = on;          // <-- tambah ni (nama tab servant kau)
            dgvGift.Enabled = on;
            dgvSpirit.Enabled = on;

            // Shop control
            dgvShops.Enabled = on;
            dgvItems.Enabled = on;
            btnNewItem.Enabled = on;
            btnNewShop.Enabled = on;

            // MX control
            tvMxCategories.Enabled = on;
            dgvNewShopMxItems.Enabled = on;

            // Save hanya bila connected + ada shop.dat
            btnSave.Enabled = on && !string.IsNullOrEmpty(shopDatPath);
        }


        // Block user tukar tab ke Shop/NewShopMx jika tak connected
        private void TabControl_Selecting_BlockWhenDisconnected(object? sender, TabControlCancelEventArgs e)
        {
            if (!IsConnected() &&
               (e.TabPage == tabShop || e.TabPage == tabNewShopMx ||
                e.TabPage == tabPage1 || e.TabPage == tabServant))   // <-- tambah wardobe & servant
            {
                e.Cancel = true;
            }
        }


        // Make columns fill DataGrid width (Shop items)
        private void ApplyItemGridFillSizing()
        {
            var gv = dgvItems;
            if (gv == null || gv.Columns.Count == 0) return;

            gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gv.AllowUserToResizeColumns = false;
            gv.AllowUserToResizeRows = false;
            gv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gv.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            foreach (DataGridViewColumn c in gv.Columns)
            {
                c.Resizable = DataGridViewTriState.False;
                c.MinimumWidth = 40;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;  // <— klik header tak reorder/sort

            }

            // weights (tweak as needed)
            SetWeight(gv, "No", 6, 46);
            SetWeight(gv, "ItemID", 12, 80);
            SetWeight(gv, "Name", 30, 200);
            SetWeight(gv, "Gold", 12, 80);
            SetWeight(gv, "Emoney", 12, 80);

            // hidden reserved stay hidden
            HideItemReservedCols();

            static void SetWeight(DataGridView gvx, string name, float weight, int min)
            {
                if (!gvx.Columns.Contains(name)) return;
                var c = gvx.Columns[name];
                c.FillWeight = weight;
                c.MinimumWidth = min;
            }
        }
        // Make columns fill DataGrid width (NewShopMx items)
        private void ApplyMxItemGridFillSizing()
        {
            var gv = dgvNewShopMxItems;
            if (gv == null || gv.Columns.Count == 0) return;

            gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            gv.AllowUserToResizeColumns = false;
            gv.AllowUserToResizeRows = false;
            gv.AllowUserToOrderColumns = false;                 // <— tak boleh drag header
            gv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gv.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;

            foreach (DataGridViewColumn c in gv.Columns)
            {
                c.Resizable = DataGridViewTriState.False;
                c.MinimumWidth = 40;
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;  // <— klik header tak sort
            }

            // weights – kecilkan Emoney
            SetWeight(gv, "No", 6, 46);
            SetWeight(gv, "Name", 28, 200);
            SetWeight(gv, "ItemType", 14, 100);
            SetWeight(gv, "CostTypeBtn", 16, 110);   // was ~10
            SetWeight(gv, "Version", 10, 70);
            SetWeight(gv, "Tip", 12, 90);   // was ~20 (smaller)
            SetWeight(gv, "NewChk", 6, 60);
            SetWeight(gv, "CommendChk", 6, 70);
            SetWeight(gv, "Emoney", 7, 70);   // <— ramping
            SetWeight(gv, "Price", 7, 70);   // (kalau la kolum guna nama Price)

            // interaktif kekal boleh klik
            if (gv.Columns.Contains("CostTypeBtn")) gv.Columns["CostTypeBtn"].ReadOnly = false;
            if (gv.Columns.Contains("NewChk")) gv.Columns["NewChk"].ReadOnly = false;
            if (gv.Columns.Contains("CommendChk")) gv.Columns["CommendChk"].ReadOnly = false;

            static void SetWeight(DataGridView gvx, string name, float weight, int min)
            {
                if (!gvx.Columns.Contains(name)) return;
                var c = gvx.Columns[name];
                c.FillWeight = weight;
                c.MinimumWidth = min;
            }
        }


        // MX grid: same sizing rules as shop item list
        private void ApplyMxItemGridSizingRules()
        {
            if (dgvNewShopMxItems == null || dgvNewShopMxItems.Columns.Count == 0) return;

            // fixed sizing & block manual resize
            dgvNewShopMxItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            foreach (DataGridViewColumn col in dgvNewShopMxItems.Columns)
                col.Resizable = DataGridViewTriState.False;

            // No smaller
            if (dgvNewShopMxItems.Columns.Contains("No"))
            {
                var c = dgvNewShopMxItems.Columns["No"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 46;
            }
            // Name wider
            if (dgvNewShopMxItems.Columns.Contains("Name"))
            {
                var c = dgvNewShopMxItems.Columns["Name"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 260;
            }
        }

        // Show checkboxes ticked when underlying hidden value == "1"
        private void DgvMxItems_CellFormatting_Checks(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var col = dgvNewShopMxItems.Columns[e.ColumnIndex].Name;
            if (col != "NewChk" && col != "CommendChk") return;

            string dataCol = (col == "NewChk") ? "New" : "Commend";
            var s = dgvNewShopMxItems.Rows[e.RowIndex].Cells[dataCol]?.Value?.ToString() ?? "0";
            e.Value = s == "1";
            e.FormattingApplied = true;
        }
        private void DgvMxItems_CellContentClick_Checks(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var colName = dgvNewShopMxItems.Columns[e.ColumnIndex].Name;
            if (colName != "NewChk" && colName != "CommendChk") return;

            dgvNewShopMxItems.CommitEdit(DataGridViewDataErrorContexts.Commit);

            var row = dgvNewShopMxItems.Rows[e.RowIndex];
            var isNewCol = colName == "NewChk";

            // read current boolean state after click
            bool newChecked = Convert.ToBoolean(row.Cells[isNewCol ? "NewChk" : "CommendChk"].EditedFormattedValue);

            // enforce mutual exclusive
            if (newChecked)
            {
                row.Cells[isNewCol ? "CommendChk" : "NewChk"].Value = false;
                row.Cells[isNewCol ? "Commend" : "New"].Value = "0";
            }

            // write numeric values "1/0" to hidden cols
            row.Cells[isNewCol ? "New" : "Commend"].Value = newChecked ? "1" : "0";

            // write back to map -> key uses "No" index
            int no = 0; int.TryParse(Convert.ToString(row.Cells["No"]?.Value), out no);
            if (no > 0 && !string.IsNullOrEmpty(_currentMxSectionKey) &&
                newShopMxCategories.TryGetValue(_currentMxSectionKey, out var map))
            {
                if (isNewCol)
                {
                    map["New" + no] = newChecked ? "1" : "0";
                    map["Commend" + no] = Convert.ToBoolean(row.Cells["CommendChk"].Value) ? "1" : "0";
                }
                else
                {
                    map["Commend" + no] = newChecked ? "1" : "0";
                    map["New" + no] = Convert.ToBoolean(row.Cells["NewChk"].Value) ? "1" : "0";
                }
            }

            dgvNewShopMxItems.InvalidateRow(e.RowIndex);
        }
        private void DgvMxItems_CellEndEdit_WriteBack(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            var colName = dgvNewShopMxItems.Columns[e.ColumnIndex].Name;
            if (colName != "Version" && colName != "Tip") return;

            var row = dgvNewShopMxItems.Rows[e.RowIndex];
            int no = 0; int.TryParse(Convert.ToString(row.Cells["No"]?.Value), out no);
            if (no <= 0 || string.IsNullOrEmpty(_currentMxSectionKey)) return;

            if (newShopMxCategories.TryGetValue(_currentMxSectionKey, out var map))
            {
                var val = Convert.ToString(row.Cells[colName].Value) ?? "";
                map[colName + no] = val.Trim();
            }
        }

        // begin drag
        private void DgvMxItems_MouseDown_DnD(object? sender, MouseEventArgs e)
        {
            _mxDragStartRow = -1;
            _mxDragBoxFromMouseDown = Rectangle.Empty;

            if (e.Button != MouseButtons.Left) return;
            var hit = dgvNewShopMxItems.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0) return;

            // don't start drag from CostType button cell
            if (hit.ColumnIndex >= 0 && hit.ColumnIndex < dgvNewShopMxItems.Columns.Count)
                if (dgvNewShopMxItems.Columns[hit.ColumnIndex].Name == "CostTypeBtn") return;

            _mxDragStartRow = hit.RowIndex;

            Size dragSize = SystemInformation.DragSize;
            _mxDragBoxFromMouseDown = new Rectangle(
                new Point(e.X - dragSize.Width / 2, e.Y - dragSize.Height / 2),
                dragSize
            );
        }


        private void DgvMxItems_MouseMove_DnD(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_mxDragStartRow < 0) return;
            if (_mxDragBoxFromMouseDown == Rectangle.Empty || _mxDragBoxFromMouseDown.Contains(e.Location)) return;

            // payload = selected row indexes (asc). If none, use start row
            var sel = dgvNewShopMxItems.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderBy(i => i).ToArray();
            if (sel.Length == 0) sel = new[] { _mxDragStartRow };

            dgvNewShopMxItems.DoDragDrop(sel, DragDropEffects.Move);
        }

        private void DgvMxItems_DragOver_DnD(object? sender, DragEventArgs e)
        {
            e.Effect = (e.Data?.GetDataPresent(typeof(int[])) == true) ? DragDropEffects.Move : DragDropEffects.None;
        }

        private void DgvMxItems_DragDrop_DnD(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetData(typeof(int[])) is int[] srcIdx) || srcIdx.Length == 0) return;

            // capture viewport
            int first = SafeFirstRow(dgvNewShopMxItems);
            int hoff = SafeHOffset(dgvNewShopMxItems);

            var pt = dgvNewShopMxItems.PointToClient(new Point(e.X, e.Y));
            var hit = dgvNewShopMxItems.HitTest(pt.X, pt.Y);
            int target = (hit.RowIndex < 0) ? dgvNewShopMxItems.Rows.Count - 1 : hit.RowIndex;

            if (dgvNewShopMxItems.DataSource is DataTable dt)
            {
                var rows = dt.Rows.Cast<DataRow>().ToList();
                var block = new List<DataRow>();

                foreach (var i in srcIdx.Distinct().OrderByDescending(i => i))
                { block.Insert(0, rows[i]); rows.RemoveAt(i); }

                if (target < 0) target = 0;
                if (target > rows.Count) target = rows.Count;
                rows.InsertRange(target, block);

                var dt2 = dt.Clone();
                foreach (var r in rows) dt2.ImportRow(r);
                dgvNewShopMxItems.DataSource = dt2;

                ApplyMxGridUi();            // re-apply hidden cols & costtype btn
                Mx_SaveGridToCategory();    // update data source map (NO rebind UI inside)

                // restore selection (optional)
                dgvNewShopMxItems.ClearSelection();
                for (int i = 0; i < block.Count; i++)
                {
                    int row = target + i;
                    if (row >= 0 && row < dgvNewShopMxItems.Rows.Count)
                        dgvNewShopMxItems.Rows[row].Selected = true;
                }

                // RESTORE viewport
                RestoreViewport(dgvNewShopMxItems, first, hoff);
            }
        }

        // display text for 1/3/19
        private void DgvMxItems_CellFormatting_CostTypeBtn(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dgvNewShopMxItems.Columns.Contains("CostTypeBtn") || !dgvNewShopMxItems.Columns.Contains("CostType")) return;
            if (dgvNewShopMxItems.Columns[e.ColumnIndex].Name != "CostTypeBtn") return;

            var raw = dgvNewShopMxItems.Rows[e.RowIndex].Cells["CostType"]?.Value?.ToString() ?? "0";
            int v = 0; int.TryParse(raw, out v);
            e.Value = v switch
            {
                1 => "EP",
                3 => "EP + PP",
                19 => "EP + PP + Credit",
                _ => raw
            };
            e.FormattingApplied = true;
        }

        // click: toggle 1 <-> 3 (19 just displays; first click => 1)
        private void DgvMxItems_CellClick_CostTypeBtn(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dgvNewShopMxItems.Columns.Contains("CostTypeBtn") || !dgvNewShopMxItems.Columns.Contains("CostType")) return;
            if (dgvNewShopMxItems.Columns[e.ColumnIndex].Name != "CostTypeBtn") return;

            var row = dgvNewShopMxItems.Rows[e.RowIndex];

            int cur = 0; int.TryParse(Convert.ToString(row.Cells["CostType"]?.Value), out cur);
            int next = (cur == 3) ? 1 : 3;
            if (cur == 19) next = 1;

            row.Cells["CostType"].Value = next;

            // write back ke INI map ikut index "No"
            int idx = 0; int.TryParse(Convert.ToString(row.Cells["No"]?.Value), out idx);
            if (idx > 0 && !string.IsNullOrEmpty(_currentMxSectionKey)
                && newShopMxCategories.TryGetValue(_currentMxSectionKey, out var map))
            {
                map["CostType" + idx] = next.ToString();
            }
            dgvNewShopMxItems.InvalidateRow(e.RowIndex);
        }
        private void InitMxItemsContextMenu()
        {
            _mxItemsMenu = new ContextMenuStrip();
            _mxItemsMenu.Items.Add(new ToolStripMenuItem("Move to Top", null, (s, e) => Mx_MoveSelectedTo(0)));
            _mxItemsMenu.Items.Add(new ToolStripMenuItem("Move to Bottom", null, (s, e) => Mx_MoveSelectedTo(int.MaxValue)));
            _mxItemsMenu.Items.Add(new ToolStripSeparator());
            _mxItemsMenu.Items.Add(new ToolStripMenuItem("Delete Selected", null, (s, e) => Mx_DeleteSelected()));
            dgvNewShopMxItems.ContextMenuStrip = _mxItemsMenu;
        }

        // right-click: select row under cursor
        private void DgvMxItems_MouseDown_SelectOnRightClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = dgvNewShopMxItems.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0)
            {
                if (!dgvNewShopMxItems.Rows[hit.RowIndex].Selected)
                {
                    dgvNewShopMxItems.ClearSelection();
                    dgvNewShopMxItems.Rows[hit.RowIndex].Selected = true;
                }
                var cidx = Math.Max(0, hit.ColumnIndex);
                if (cidx < dgvNewShopMxItems.Columns.Count)
                    dgvNewShopMxItems.CurrentCell = dgvNewShopMxItems.Rows[hit.RowIndex].Cells[cidx];
            }
        }
        private void Mx_MoveSelectedTo(int targetIndex)
        {
            if (!(dgvNewShopMxItems.DataSource is DataTable dt)) return;

            // capture viewport
            int first = SafeFirstRow(dgvNewShopMxItems);
            int hoff = SafeHOffset(dgvNewShopMxItems);

            var src = dgvNewShopMxItems.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderBy(i => i).ToList();
            if (src.Count == 0) return;

            var rows = dt.Rows.Cast<DataRow>().ToList();
            var block = new List<DataRow>();
            for (int i = src.Count - 1; i >= 0; i--) { block.Insert(0, rows[src[i]]); rows.RemoveAt(src[i]); }

            if (targetIndex == int.MaxValue) targetIndex = rows.Count;
            if (targetIndex < 0) targetIndex = 0;
            if (targetIndex > rows.Count) targetIndex = rows.Count;

            rows.InsertRange(targetIndex, block);

            var dt2 = dt.Clone();
            foreach (var r in rows) dt2.ImportRow(r);
            dgvNewShopMxItems.DataSource = dt2;

            ApplyMxGridUi();
            Mx_SaveGridToCategory(); // no UI rebind inside

            dgvNewShopMxItems.ClearSelection();
            for (int i = 0; i < block.Count; i++)
            {
                int row = targetIndex + i;
                if (row >= 0 && row < dgvNewShopMxItems.Rows.Count)
                    dgvNewShopMxItems.Rows[row].Selected = true;
            }

            // restore viewport
            RestoreViewport(dgvNewShopMxItems, first, hoff);
        }

        private void Mx_DeleteSelected()
        {
            if (!(dgvNewShopMxItems.DataSource is DataTable dt)) return;

            // capture viewport
            int first = SafeFirstRow(dgvNewShopMxItems);
            int hoff = SafeHOffset(dgvNewShopMxItems);

            var idxs = dgvNewShopMxItems.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).Distinct().OrderByDescending(i => i).ToList();
            if (idxs.Count == 0) return;

            foreach (var i in idxs) dt.Rows.RemoveAt(i);

            Mx_SaveGridToCategory(); // no UI rebind inside

            // restore viewport (grid stayed bound to same dt)
            RestoreViewport(dgvNewShopMxItems, first, hoff);
        }


        // write grid -> newShopMxCategories[_currentMxSectionKey]
        private void Mx_SaveGridToCategory()
        {
            if (string.IsNullOrEmpty(_currentMxSectionKey)) return;
            if (!(dgvNewShopMxItems.DataSource is DataTable dt)) return;
            if (!newShopMxCategories.TryGetValue(_currentMxSectionKey, out var old)) return;

            var nm = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            // keep base keys
            foreach (var k in new[] { "Catalog", "Name", "Pic", "ItemSource" })
                if (old.TryGetValue(k, out var v) && !string.IsNullOrWhiteSpace(v)) nm[k] = v;

            int idx = 0;
            foreach (DataRow r in dt.Rows)
            {
                idx++;
                void Put(string col, string key, bool keepNull = false)
                {
                    var val = Convert.ToString(r[col]) ?? "";
                    if (keepNull)
                    {
                        if (val.Length > 0) nm[key + idx] = val.Trim();
                    }
                    else
                    {
                        if (!string.IsNullOrWhiteSpace(val)) nm[key + idx] = val.Trim();
                    }
                }

                Put("ItemID", "ItemID");
                Put("ItemType", "ItemType");
                Put("CostType", "CostType");
                Put("Version", "Version");
                Put("New", "New");
                Put("Commend", "Commend");
                Put("OEM", "OEM");
                Put("Describe", "Describe", keepNull: true);
                Put("ReturnEmoney", "ReturnEmoney");
                Put("BeginTime", "BeginTime");
                Put("EndTime", "EndTime");
                Put("Tip", "Tip", keepNull: true);
            }
            nm["Amount"] = idx.ToString();

            newShopMxCategories[_currentMxSectionKey] = nm;

            // rebind utk refresh "No" & butang text
            tvMxCategories_AfterSelect(tvMxCategories, new TreeViewEventArgs(tvMxCategories.SelectedNode));
        }

        // Fix widths & disable manual resize for Shop List
        private void ApplyShopGridSizingRules()
        {
            dgvShops.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            foreach (DataGridViewColumn col in dgvShops.Columns)
                col.Resizable = DataGridViewTriState.False;

            if (dgvShops.Columns.Contains("No"))
            {
                var c = dgvShops.Columns["No"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 46; // kecil
            }
            if (dgvShops.Columns.Contains("Name"))
            {
                var c = dgvShops.Columns["Name"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 150; // besar
            }
            if (dgvShops.Columns.Contains("ShopID"))
            {
                var c = dgvShops.Columns["ShopID"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 90;
            }
            if (dgvShops.Columns.Contains("ItemCount"))
            {
                var c = dgvShops.Columns["ItemCount"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 90;
            }
            // if using the Type button column
            if (dgvShops.Columns.Contains("TypeBtn"))
            {
                var c = dgvShops.Columns["TypeBtn"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 100;
            }
        }

        // Fix column widths (No smaller, Name wider) and block manual resizing
        private void ApplyItemGridSizingRules()
        {
            // enforce fixed sizing
            dgvItems.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            if (dgvItems.Columns.Contains("No"))
            {
                var c = dgvItems.Columns["No"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 46; // smaller No
            }
            if (dgvItems.Columns.Contains("Name"))
            {
                var c = dgvItems.Columns["Name"];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                c.Width = 260; // bigger Name
            }

            // keep others as-is but non-resizable
            foreach (DataGridViewColumn col in dgvItems.Columns)
            {
                col.Resizable = DataGridViewTriState.False; // disable drag resize
            }
        }

        // (1)(2) lock columns: No/ItemID/Name readonly; Gold/Emoney editable
        private void ApplyItemGridEditRules()
        {
            if (dgvItems.Columns.Contains("No")) dgvItems.Columns["No"].ReadOnly = true;
            if (dgvItems.Columns.Contains("ItemID")) dgvItems.Columns["ItemID"].ReadOnly = true;
            if (dgvItems.Columns.Contains("Name")) dgvItems.Columns["Name"].ReadOnly = true;

            if (dgvItems.Columns.Contains("Gold")) dgvItems.Columns["Gold"].ReadOnly = false;
            if (dgvItems.Columns.Contains("Emoney")) dgvItems.Columns["Emoney"].ReadOnly = false;

            // hidden but kept for save
            HideItemReservedCols();
        }

        // (UI only) hide Reserved1/Reserved2
        private void HideItemReservedCols()
        {
            if (dgvItems.Columns.Contains("Reserved1")) dgvItems.Columns["Reserved1"].Visible = false;
            if (dgvItems.Columns.Contains("Reserved2")) dgvItems.Columns["Reserved2"].Visible = false;
        }

        // right-click: select row under cursor (keeps multi-select if already selected)
        private void DgvItems_MouseDown_SelectOnRightClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = dgvItems.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0)
            {
                if (!dgvItems.Rows[hit.RowIndex].Selected)
                {
                    dgvItems.ClearSelection();
                    dgvItems.Rows[hit.RowIndex].Selected = true;
                }
                var cidx = Math.Max(0, hit.ColumnIndex);
                if (cidx < dgvItems.Columns.Count)
                    dgvItems.CurrentCell = dgvItems.Rows[hit.RowIndex].Cells[cidx];
            }
        }

        // context menu: Move Top / Move Bottom / Delete Selected
        private void InitItemsContextMenu()
        {
            _itemsMenu = new ContextMenuStrip();
            _itemsMenu.Items.Add(new ToolStripMenuItem("Move to Top", null, (s, e) => MoveSelectedItems(0)));
            _itemsMenu.Items.Add(new ToolStripMenuItem("Move to Bottom", null, (s, e) => MoveSelectedItems(int.MaxValue)));
            _itemsMenu.Items.Add(new ToolStripSeparator());
            _itemsMenu.Items.Add(new ToolStripMenuItem("Delete Selected", null, (s, e) => DeleteSelectedItems()));
            dgvItems.ContextMenuStrip = _itemsMenu;
        }

        // DRAG source
        private void DgvItems_MouseDown(object? sender, MouseEventArgs e)
        {
            _dragStartRow = -1;
            _dragBoxFromMouseDown = Rectangle.Empty;

            if (e.Button != MouseButtons.Left) return;
            var hit = dgvItems.HitTest(e.X, e.Y);
            if (hit.RowIndex < 0) return;

            _dragStartRow = hit.RowIndex;

            Size dragSize = SystemInformation.DragSize;
            _dragBoxFromMouseDown = new Rectangle(
                new Point(e.X - (dragSize.Width / 2), e.Y - (dragSize.Height / 2)),
                dragSize
            );
        }

        // DRAG begin
        private void DgvItems_MouseMove(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            if (_dragStartRow < 0) return;
            if (_dragBoxFromMouseDown == Rectangle.Empty || _dragBoxFromMouseDown.Contains(e.Location)) return;

            // payload = selected rows indices (asc)
            var sel = dgvItems.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderBy(i => i).ToArray();
            if (sel.Length == 0) sel = new[] { _dragStartRow };

            dgvItems.DoDragDrop(sel, DragDropEffects.Move);
        }

        // DRAG over
        private void DgvItems_DragOver(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(typeof(int[])) == true)
                e.Effect = DragDropEffects.Move;
            else
                e.Effect = DragDropEffects.None;
        }

        // DROP reorder
        private void DgvItems_DragDrop(object? sender, DragEventArgs e)
        {
            if (!(e.Data?.GetData(typeof(int[])) is int[] srcIdx) || srcIdx.Length == 0) return;
            var clientPoint = dgvItems.PointToClient(new Point(e.X, e.Y));
            var hit = dgvItems.HitTest(clientPoint.X, clientPoint.Y);
            int target = (hit.RowIndex < 0) ? dgvItems.Rows.Count - 1 : hit.RowIndex;

            ReorderIntoTarget(srcIdx.ToList(), target);
        }

        // (3) Right-click Move Top/Bottom helpers
        private void MoveSelectedItems(int targetIndex)
        {
            var sel = dgvItems.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).OrderBy(i => i).ToList();
            if (sel.Count == 0) return;
            if (targetIndex == int.MaxValue) targetIndex = dgvItems.Rows.Count - 1;
            ReorderIntoTarget(sel, targetIndex);
        }

        // core reorder: update model lists, rebind, renumber No, restore selection
        private void ReorderIntoTarget(List<int> sourceRowIndexes, int targetRowIndex)
        {
            if (dgvShops.SelectedRows.Count == 0) return;
            uint sid = Convert.ToUInt32(dgvShops.SelectedRows[0].Cells["ShopID"].Value);
            var shop = allShops.FirstOrDefault(s => s.ShopID == sid);
            if (shop == null || shop.ItemIDs == null || shop.ItemIDs.Count == 0) return;

            // capture viewport BEFORE rebind
            int first = SafeFirstRow(dgvItems);
            int hoff = SafeHOffset(dgvItems);

            // zip lists
            var tuples = new List<(uint id, uint r1, uint r2)>();
            int n = shop.ItemIDs.Count;
            for (int i = 0; i < n; i++)
            {
                uint r1 = (i < shop.Reserved1.Count) ? shop.Reserved1[i] : 0;
                uint r2 = (i < shop.Reserved2.Count) ? shop.Reserved2[i] : 61;
                tuples.Add((shop.ItemIDs[i], r1, r2));
            }

            // remove selected (desc) -> keep order
            var uniqueSrc = sourceRowIndexes.Distinct().Where(i => i >= 0 && i < tuples.Count).OrderByDescending(i => i).ToList();
            var block = new List<(uint id, uint r1, uint r2)>();
            foreach (var i in uniqueSrc) { block.Insert(0, tuples[i]); tuples.RemoveAt(i); }

            // clamp target
            if (targetRowIndex < 0) targetRowIndex = 0;
            if (targetRowIndex > tuples.Count) targetRowIndex = tuples.Count;

            // insert
            tuples.InsertRange(targetRowIndex, block);

            // split back
            shop.ItemIDs = tuples.Select(t => t.id).ToList();
            shop.Reserved1 = tuples.Select(t => t.r1).ToList();
            shop.Reserved2 = tuples.Select(t => t.r2).ToList();

            // rebind & restore selection
            RebindItemsForShop_Safe(sid);

            dgvItems.ClearSelection();
            for (int i = 0; i < block.Count; i++)
            {
                int row = targetRowIndex + i;
                if (row >= 0 && row < dgvItems.Rows.Count)
                    dgvItems.Rows[row].Selected = true;
            }

            // RESTORE viewport (no jump)
            RestoreViewport(dgvItems, first, hoff);
        }


        // (5) bulk delete selected rows
        private void DeleteSelectedItems()
        {
            if (dgvShops.SelectedRows.Count == 0) return;
            uint sid = Convert.ToUInt32(dgvShops.SelectedRows[0].Cells["ShopID"].Value);
            var shop = allShops.FirstOrDefault(s => s.ShopID == sid);
            if (shop == null) return;

            var idxs = dgvItems.SelectedRows.Cast<DataGridViewRow>().Select(r => r.Index).Distinct().OrderByDescending(i => i).ToList();
            if (idxs.Count == 0) return;

            foreach (var i in idxs)
            {
                if (i >= 0 && i < (shop.ItemIDs?.Count ?? 0)) shop.ItemIDs.RemoveAt(i);
                if (i >= 0 && i < (shop.Reserved1?.Count ?? 0)) shop.Reserved1.RemoveAt(i);
                if (i >= 0 && i < (shop.Reserved2?.Count ?? 0)) shop.Reserved2.RemoveAt(i);
            }

            RebindItemsForShop_Safe(sid);
        }


        // Click: Update shop names from ini/npc.ini (Name -> fallback Note)
        private void btnUptName_Click(object? sender, EventArgs e)
        {
            try
            {
                var clientPath = txtClientPath.Text.Trim();
                var npcIniPath = Path.Combine(clientPath, "ini", "npc.ini");
                if (!File.Exists(npcIniPath))
                {
                    MessageBox.Show("npc.ini not found in ini folder!");
                    return;
                }

                // Parse npc.ini => map[ShopID]=NameOrNote
                var nameMap = ParseNpcIniNames(npcIniPath);

                var enc = Encoding.GetEncoding(936); // GBK
                int updated = 0, skipped = 0;

                // Update in-memory model
                foreach (var shop in allShops)
                {
                    if (!nameMap.TryGetValue(shop.ShopID, out var raw) || string.IsNullOrWhiteSpace(raw))
                    {
                        skipped++;
                        continue;
                    }

                    var trimmed = TruncateToBytes(raw.Trim(), SHOP_NAME_MAX_BYTES, enc);
                    if (string.IsNullOrEmpty(trimmed)) { skipped++; continue; }

                    shop.Name = trimmed;
                    updated++;
                }

                // Update grid (if bound)
                if (dgvShops.DataSource is DataTable dt && dt.Columns.Contains("ShopID") && dt.Columns.Contains("Name"))
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        if (!UInt32.TryParse(Convert.ToString(r["ShopID"]), out var sid)) continue;
                        if (!nameMap.TryGetValue(sid, out var raw) || string.IsNullOrWhiteSpace(raw)) continue;

                        r["Name"] = TruncateToBytes(raw.Trim(), SHOP_NAME_MAX_BYTES, enc);
                    }
                    dt.AcceptChanges();
                }

                MessageBox.Show($"Shop name updated: {updated}, skipped: {skipped}.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Update shop name failed: " + ex.Message);
            }
        }

        // Parse npc.ini -> prefer Name, else Note
        // Parse npc.ini -> prefer Name (tilde stripped), else Note (tilde stripped)
        private Dictionary<uint, string> ParseNpcIniNames(string path)
        {
            var map = new Dictionary<uint, string>();
            uint curId = 0;
            string? curName = null;
            string? curNote = null;

            foreach (var raw in File.ReadLines(path))
            {
                var line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";")) continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    // flush previous section
                    if (curId > 0)
                    {
                        var best = !string.IsNullOrWhiteSpace(curName) ? curName : curNote;
                        if (!string.IsNullOrWhiteSpace(best))
                            map[curId] = NormalizeNpcLabel(best!);
                    }

                    // start new
                    curId = 0; curName = null; curNote = null;
                    var sec = line.Substring(1, line.Length - 2);
                    if (sec.StartsWith("NpcType", StringComparison.OrdinalIgnoreCase))
                        UInt32.TryParse(sec.Substring(7), out curId);
                    continue;
                }

                if (curId == 0) continue;

                var pos = line.IndexOf('=');
                if (pos <= 0) continue;
                var key = line.Substring(0, pos).Trim();
                var val = line.Substring(pos + 1).Trim();

                if (key.Equals("Name", StringComparison.OrdinalIgnoreCase)) curName = NormalizeNpcLabel(val);
                else if (key.Equals("Note", StringComparison.OrdinalIgnoreCase)) curNote = NormalizeNpcLabel(val);
            }

            // flush last
            if (curId > 0)
            {
                var best = !string.IsNullOrWhiteSpace(curName) ? curName : curNote;
                if (!string.IsNullOrWhiteSpace(best))
                    map[curId] = NormalizeNpcLabel(best!);
            }

            return map;
        }

        private static string NormalizeNpcLabel(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace("~", "").Trim();
        }
        // Truncate string to max bytes in given encoding (no half-character cut)
        private static string TruncateToBytes(string s, int maxBytes, Encoding enc)
        {
            if (string.IsNullOrEmpty(s)) return s;
            if (enc.GetByteCount(s) <= maxBytes) return s;

            int low = 0, high = s.Length;
            while (low < high)
            {
                int mid = (low + high + 1) / 2;
                int bc = enc.GetByteCount(s.AsSpan(0, mid).ToString());
                if (bc <= maxBytes) low = mid; else high = mid - 1;
            }
            return s.Substring(0, low);
        }
        // ---------- button column Type ----------
        private void ApplyShopTypeButtonColumn()
        {
            if (!dgvShops.Columns.Contains("Type")) return;

            if (dgvShops.Columns.Contains("TypeBtn"))
                dgvShops.Columns.Remove("TypeBtn");

            var typeCol = dgvShops.Columns["Type"];
            typeCol.Visible = false; // keep numeric data, hide from user
            int insertAt = typeCol.Index;

            var btn = new DataGridViewButtonColumn
            {
                Name = "TypeBtn",
                HeaderText = "Type",
                FlatStyle = FlatStyle.Standard,
                UseColumnTextForButtonValue = false
            };
            if (insertAt < 0 || insertAt > dgvShops.Columns.Count) insertAt = dgvShops.Columns.Count;
            dgvShops.Columns.Insert(insertAt, btn);
        }
        private void DgvShops_CellFormatting_TypeButton(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dgvShops.Columns.Contains("TypeBtn") || !dgvShops.Columns.Contains("Type")) return;
            if (dgvShops.Columns[e.ColumnIndex].Name != "TypeBtn") return;

            var raw = dgvShops.Rows[e.RowIndex].Cells["Type"]?.Value?.ToString() ?? "0";
            int v = 0; int.TryParse(raw, out v);
            e.Value = (v == 5) ? "EP Shop" : "Gold Shop";
            e.FormattingApplied = true;
        }
        private void DgvShops_CellClick_TypeButton(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0) return;
            if (!dgvShops.Columns.Contains("TypeBtn") || !dgvShops.Columns.Contains("Type")) return;
            if (dgvShops.Columns[e.ColumnIndex].Name != "TypeBtn") return;

            var row = dgvShops.Rows[e.RowIndex];
            int cur = 0; int.TryParse(Convert.ToString(row.Cells["Type"]?.Value), out cur);
            int next = (cur == 5) ? 0 : 5;
            row.Cells["Type"].Value = next; // update table cell

            // sync to allShops via ShopID
            if (row.Cells["ShopID"].Value != null)
            {
                uint sid = Convert.ToUInt32(row.Cells["ShopID"].Value);
                var shop = allShops.Find(s => s.ShopID == sid);
                if (shop != null) shop.Type = (uint)next;
            }
            dgvShops.InvalidateRow(e.RowIndex);
        }

        // ---------- read-only rules ----------
        private void SetShopGridReadonly()
        {
            if (dgvShops.Columns.Contains("No")) dgvShops.Columns["No"].ReadOnly = true;
            if (dgvShops.Columns.Contains("ItemCount")) dgvShops.Columns["ItemCount"].ReadOnly = true;
            if (dgvShops.Columns.Contains("Type")) dgvShops.Columns["Type"].ReadOnly = true; // hidden
            if (dgvShops.Columns.Contains("TypeBtn")) dgvShops.Columns["TypeBtn"].ReadOnly = false; // clickable
            if (dgvShops.Columns.Contains("ShopID")) dgvShops.Columns["ShopID"].ReadOnly = false;
            if (dgvShops.Columns.Contains("Name")) dgvShops.Columns["Name"].ReadOnly = false;
        }

        // ---------- edit ShopID/Name sync ----------
        private uint _editShopIdBefore = 0;
        private void DgvShops_CellBeginEdit(object? sender, DataGridViewCellCancelEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (!dgvShops.Columns.Contains("ShopID")) return;
            uint.TryParse(Convert.ToString(dgvShops.Rows[e.RowIndex].Cells["ShopID"].Value), out _editShopIdBefore);
        }
        private void DgvShops_CellEndEdit_UpdateShop(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var colName = dgvShops.Columns[e.ColumnIndex].Name;
            if (colName != "ShopID" && colName != "Name") return;

            var shop = allShops.Find(s => s.ShopID == _editShopIdBefore);
            if (shop == null) return;

            var row = dgvShops.Rows[e.RowIndex];
            if (colName == "ShopID")
            {
                if (uint.TryParse(Convert.ToString(row.Cells["ShopID"].Value), out var newId))
                    shop.ShopID = newId;
                else
                    row.Cells["ShopID"].Value = shop.ShopID; // revert if invalid
            }
            else if (colName == "Name")
            {
                shop.Name = Convert.ToString(row.Cells["Name"].Value) ?? "";
            }
        }

        // ---------- right-click select row + context menu delete ----------
        private ContextMenuStrip? _shopMenu;
        private void InitShopContextMenu()
        {
            _shopMenu = new ContextMenuStrip();
            var miDel = new ToolStripMenuItem("Delete Shop", null, (s, e) => DeleteSelectedShop());
            _shopMenu.Items.Add(miDel);
            dgvShops.ContextMenuStrip = _shopMenu;
        }
        // select the row under mouse on right-click
        private void DgvShops_MouseDown_SelectOnRightClick(object? sender, MouseEventArgs e)
        {
            if (e.Button != MouseButtons.Right) return;
            var hit = dgvShops.HitTest(e.X, e.Y);
            if (hit.RowIndex >= 0)
            {
                dgvShops.ClearSelection();
                dgvShops.Rows[hit.RowIndex].Selected = true;
                // set current cell so edit (ShopID/Name) still works
                var cidx = Math.Max(0, hit.ColumnIndex);
                if (cidx < dgvShops.Columns.Count)
                    dgvShops.CurrentCell = dgvShops.Rows[hit.RowIndex].Cells[cidx];
            }
        }
        // remove selected shop (also clear items & renumber "No")
        private void DeleteSelectedShop()
        {
            if (dgvShops.SelectedRows.Count == 0) return;
            var row = dgvShops.SelectedRows[0];
            if (row.Cells["ShopID"].Value == null) return;

            uint sid = Convert.ToUInt32(row.Cells["ShopID"].Value);
            var shop = allShops.Find(s => s.ShopID == sid);
            if (shop == null) return;

            if (MessageBox.Show($"Delete shop {shop.Name} ({shop.ShopID})?", "Confirm", MessageBoxButtons.YesNo) != DialogResult.Yes)
                return;

            // clear items first, then remove (safer)
            shop.ItemIDs?.Clear();
            shop.Reserved1?.Clear();
            shop.Reserved2?.Clear();
            allShops.Remove(shop);

            // remove from grid and renumber "No"
            if (dgvShops.DataSource is DataTable dt)
            {
                dt.Rows.RemoveAt(row.Index);
                for (int i = 0; i < dt.Rows.Count; i++) dt.Rows[i]["No"] = i + 1;
                dt.AcceptChanges();
            }

            // clear item grid if it was showing this shop
            dgvItems.DataSource = null;
        }

        private void DgvShops_CellFormatting(object? sender, DataGridViewCellFormattingEventArgs e)
        {
            if (e.RowIndex < 0) return;
            if (dgvShops.Columns[e.ColumnIndex].Name != "Type") return;

            var raw = e.Value?.ToString() ?? "0";
            if (int.TryParse(raw, out var v))
            {
                e.Value = (v == 5) ? "EP Shop" : "Gold Shop";
                e.FormattingApplied = true; // guna teks ni untuk display
            }
        }

        private DataGridView? FindNewShopGrid()
        {
            // cuba cari ikut beberapa nama biasa
            string[] candidates = { "dgvNewShopItems", "dgvNewShop", "gridNewShop" };
            foreach (var name in candidates)
            {
                var ctrl = this.Controls.Find(name, true).FirstOrDefault() as DataGridView;
                if (ctrl != null) return ctrl;
            }
            return null;
        }

        // Sync MX grid Emoney/ReturnEmoney untuk item yang sama
        private void SyncMxGrid(uint keyId, int ep)
        {
            if (dgvNewShopMxItems == null || dgvNewShopMxItems.Rows.Count == 0) return;
            bool hasType = dgvNewShopMxItems.Columns.Contains("ItemType");
            bool hasEP = dgvNewShopMxItems.Columns.Contains("Emoney");
            if (!hasType || !hasEP) return;

            bool hasRet = dgvNewShopMxItems.Columns.Contains("ReturnEmoney");
            foreach (DataGridViewRow r in dgvNewShopMxItems.Rows)
            {
                uint mxType = ToUInt(r.Cells["ItemType"]?.Value);
                if (mxType != keyId) continue;

                r.Cells["Emoney"].Value = ep;
                if (hasRet) r.Cells["ReturnEmoney"].Value = (ep < 10) ? "" : (ep / 10).ToString();
            }
        }

        // 2) NewShop grid: when Gold/Emoney edited
        private void DgvNewShopGrid_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (sender is not DataGridView g || e.RowIndex < 0) return;

            var row = g.Rows[e.RowIndex];
            string colName = g.Columns[e.ColumnIndex].Name;

            // detect ItemID column
            uint id = 0;
            if (g.Columns.Contains("ItemID")) id = ToUInt(row.Cells["ItemID"]?.Value);
            else if (g.Columns.Contains("ID")) id = ToUInt(row.Cells["ID"]?.Value);
            else if (g.Columns.Contains("ItemType")) id = ToUInt(row.Cells["ItemType"]?.Value);
            if (id == 0) return;

            // gold/ep from row (flex), fallback to cache for ep
            int gold = g.Columns.Contains("Gold") ? ToInt(row.Cells["Gold"]?.Value) : 0;
            int ep = GetRowEmoneyFlexible(g, row, id);

            // if user edited anything relevant, update itemtype cache
            if (colName.Equals("Gold", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("Emoney", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("EP", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("SellPrice", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("Price", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("ItemID", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("ItemType", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("ID", StringComparison.OrdinalIgnoreCase))
            {
                if (_it != null)
                {
                    if (_it.ById.ContainsKey(id))
                        _it.ById[id] = (_it.ById[id].Name, gold, ep);
                    else
                        _it.ById[id] = ("", gold, ep);

                    if (_it.ByType.ContainsKey(id))
                        _it.ByType[id] = (_it.ByType[id].Name, gold, ep);

                    if (_it.GridTable != null)
                    {
                        var rs = _it.GridTable.Select("ID = " + id);
                        foreach (var r in rs) { r["Gold"] = gold; r["Emoney"] = ep; }
                        _it.GridTable.AcceptChanges();
                    }
                }
            }

            // ReturnEmoney auto (kalau kolum wujud)
            if (g.Columns.Contains("ReturnEmoney"))
                row.Cells["ReturnEmoney"].Value = (ep < 10) ? "" : (ep / 10).ToString();

            // refresh current shop grid
            if (dgvShops.SelectedRows.Count > 0)
            {
                var v = dgvShops.SelectedRows[0].Cells["ShopID"].Value;
                if (v != null && UInt32.TryParse(v.ToString(), out uint sid))
                    BeginInvoke(new Action(() => RebindItemsForShop_Safe(sid)));
            }

            // ❗ always sync Wardrobe after any edit
            BeginInvoke(new Action(() => SyncWardrobePrice(id, ep)));
        }

        private void BtnFindPath_Click(object sender, EventArgs e)
        {
            using var fbd = new FolderBrowserDialog();
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
                    MessageBox.Show("shop.dat not found in ini folder!", "Ralat");
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

            var filtered = allShops.Where(s => !ShopDatHandler.hiddenShopIDs.Contains(s.ShopID)).ToList();
            for (int i = 0; i < filtered.Count; i++)
            {
                dt.Rows.Add(i + 1, filtered[i].ShopID, filtered[i].Name, filtered[i].Type, (uint)filtered[i].ItemIDs.Count);
            }

            dgvShops.DataSource = dt;
            ApplyShopTypeButtonColumn();  // <-- panggil di sini
            ApplyShopGridSizingRules();   // set lebar & block resize
            SetShopGridReadonly();
            if (dt.Rows.Count > 0) dgvShops.Rows[0].Selected = true;
        }

        // 1) Insert button column (safe)


        // 2) Format button text (safe bounds)


        // 3) Toggle on click (safe bounds)



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

        // ===== Shop list -> Item list (Shop.dat) =====
        private void DgvShops_SelectionChanged(object sender, EventArgs e)
        {
            if (dgvShops.SelectedRows.Count == 0) return;
            var val = dgvShops.SelectedRows[0].Cells["ShopID"].Value;
            if (val == null) return;
            if (!UInt32.TryParse(val.ToString(), out uint shopId)) return;

            RebindItemsForShop_Safe(shopId);
        }


        private void BtnSave_Click(object sender, EventArgs e)
        {
            if (conn == null || conn.State != ConnectionState.Open)
            {
                MessageBox.Show("Please connect to MySQL first before saving!", "Connection Required");
                return;
            }
            if (string.IsNullOrEmpty(shopDatPath) || allShops == null || allShops.Count == 0)
            {
                MessageBox.Show("No data available to save!");
                return;
            }

            // commit grid
            dgvItems.EndEdit();
            dgvNewShopMxItems.EndEdit();

            try
            {
                // =============== helpers ===============
                static bool IsItemSection(string sec)
                    => sec.StartsWith("Item", StringComparison.OrdinalIgnoreCase) && int.TryParse(sec.Substring(4), out _);

                static bool HasValue(string? v, bool keepNull)
                {
                    if (v is null) return false;
                    var t = v.Trim();
                    if (t.Length == 0) return false;
                    if (!keepNull && t.Equals("null", StringComparison.OrdinalIgnoreCase)) return false;
                    return true; // "0" ok; "null" ok bila keepNull=true
                }

                static int SuffixIndex(string key, string prefix)
                {
                    if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return -1;
                    return int.TryParse(key.Substring(prefix.Length), out var n) ? n : -1;
                }

                static void CopyIfPresent(Dictionary<string, string> src, Dictionary<string, string> dst, params string[] keys)
                {
                    foreach (var k in keys)
                        if (src.TryGetValue(k, out var v) && HasValue(v, false))
                            dst[k] = v.Trim();
                }

                static void MergeFieldIndexed(Dictionary<string, string> src, Dictionary<string, string> dst, string baseKey, int idx)
                {
                    var k = baseKey + idx.ToString();
                    if (src.TryGetValue(k, out var v) && HasValue(v, false)) dst[baseKey] = v.Trim();
                }
                static void MergeFieldIndexedRaw(Dictionary<string, string> src, Dictionary<string, string> dst, string baseKey, int idx, bool keepNull)
                {
                    var k = baseKey + idx.ToString();
                    if (src.TryGetValue(k, out var v) && HasValue(v, keepNull)) dst[baseKey] = v.Trim();
                }

                static void WriteIf(List<string> dst, string key, Dictionary<string, string> map, bool keepNull)
                {
                    if (map.TryGetValue(key, out var v) && HasValue(v, keepNull))
                        dst.Add($"{key}={v.Trim()}");
                }

                // Parse newshop.ini -> LAST-WINS map untuk [ItemX]
                Dictionary<string, Dictionary<string, string>> BuildLastWinsItemSectionsFromLines(List<string> lines)
                {
                    var last = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);
                    string? curHeader = null;
                    Dictionary<string, string>? cur = null;

                    void Flush()
                    {
                        if (curHeader != null && IsItemSection(curHeader) && cur != null)
                            last[curHeader] = new Dictionary<string, string>(cur, StringComparer.OrdinalIgnoreCase);
                        curHeader = null; cur = null;
                    }

                    foreach (var raw in lines)
                    {
                        var line = (raw ?? "").Trim();
                        if (line.StartsWith("[") && line.EndsWith("]"))
                        {
                            Flush();
                            curHeader = line.Substring(1, line.Length - 2);
                            if (IsItemSection(curHeader))
                                cur = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                            continue;
                        }
                        if (cur != null && curHeader != null)
                        {
                            int p = line.IndexOf('=');
                            if (p > 0)
                            {
                                var k = line.Substring(0, p).Trim();
                                var v = line.Substring(p + 1).Trim();
                                // last-wins per key inside section
                                cur[k] = v;
                            }
                        }
                    }
                    Flush();
                    return last;
                }

                // =============== 1) Normalize newshopmx & kutip ItemX utk newshop.ini ===============
                var mxSanitized = new Dictionary<string, Dictionary<string, string>>();
                var newItemSections = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

                // LAST-WINS snapshot dari newshop.ini asal
                var lastWinsIniItems = BuildLastWinsItemSectionsFromLines(newShopRawLines);

                foreach (var sec in newShopMxCategories)
                {
                    var src = sec.Value ?? new Dictionary<string, string>();
                    var dst = new Dictionary<string, string>();

                    CopyIfPresent(src, dst, "Amount", "Catalog", "Name", "Pic", "ItemSource");

                    var ids = src
                        .Where(p => p.Key.StartsWith("ItemID", StringComparison.OrdinalIgnoreCase))
                        .Select(p => (Idx: SuffixIndex(p.Key, "ItemID"), Val: (p.Value ?? "").Trim()))
                        .Where(x => x.Idx > 0 && x.Val.Length > 0)
                        .OrderBy(x => x.Idx)
                        .Select(x => x.Val)
                        .ToList();

                    dst["Amount"] = ids.Count.ToString();
                    for (int i = 0; i < ids.Count; i++)
                        dst["ItemID" + (i + 1)] = ids[i];

                    for (int i = 0; i < ids.Count; i++)
                    {
                        var itemId = ids[i];
                        var itemKey = "Item" + itemId;
                        if (!newItemSections.TryGetValue(itemKey, out var map))
                            newItemSections[itemKey] = map = new(StringComparer.OrdinalIgnoreCase);

                        // ambil nilai dari MX dulu
                        MergeFieldIndexed(src, map, "ItemType", i + 1);
                        MergeFieldIndexed(src, map, "CostType", i + 1);
                        MergeFieldIndexed(src, map, "Version", i + 1);
                        MergeFieldIndexed(src, map, "New", i + 1);
                        MergeFieldIndexed(src, map, "Commend", i + 1);
                        MergeFieldIndexed(src, map, "OEM", i + 1);
                        MergeFieldIndexedRaw(src, map, "Describe", i + 1, keepNull: true);
                        MergeFieldIndexed(src, map, "ReturnEmoney", i + 1);
                        MergeFieldIndexed(src, map, "BeginTime", i + 1);
                        MergeFieldIndexed(src, map, "EndTime", i + 1);
                        MergeFieldIndexedRaw(src, map, "Tip", i + 1, keepNull: true);

                        // OVERLAY dengan versi PALING BAWAH dari newshop.ini asal (last-wins)
                        if (lastWinsIniItems.TryGetValue(itemKey, out var oldLast))
                        {
                            foreach (var p in oldLast)
                                map[p.Key] = p.Value?.Trim() ?? "";
                        }
                    }

                    mxSanitized[sec.Key] = dst;
                }

                // Rebuild newshop.ini & set ShopInfo.ItemAmount = MAX([ItemX] index at bottom), not count
                List<string> RebuildNewShopIni_OnlyItemX_KeepOthers(
                    List<string> original,
                    Dictionary<string, Dictionary<string, string>> itemMap)
                {
                    // collect [ItemX] indices
                    var itemIndices = itemMap.Keys
                        .Where(IsItemSection)
                        .Select(k => { int n; return int.TryParse(k.Substring(4), out n) ? n : -1; })
                        .Where(n => n >= 0)
                        .ToList();

                    int itemCount = itemIndices.Count;                 // not used for ItemAmount anymore
                    int maxItemIdx = itemIndices.Count > 0 ? itemIndices.Max() : 0; // <-- use this

                    // split original into blocks
                    var blocks = new List<(string? Header, List<string> Lines)>();
                    string? curHeader = null;
                    var curLines = new List<string>();

                    void Flush()
                    {
                        blocks.Add((curHeader, curLines));
                        curHeader = null; curLines = new();
                    }

                    foreach (var raw in original)
                    {
                        var line = raw ?? "";
                        var t = line.Trim();
                        if (t.StartsWith("[") && t.EndsWith("]"))
                        {
                            Flush();
                            curHeader = t.Substring(1, t.Length - 2);
                        }
                        else
                        {
                            curLines.Add(line);
                        }
                    }
                    Flush();

                    var outLines = new List<string>(original.Count + itemMap.Count * 10);
                    bool wroteShopInfo = false;

                    foreach (var b in blocks)
                    {
                        // drop old [ItemX]
                        if (b.Header != null && IsItemSection(b.Header)) continue;

                        if (b.Header == null)
                        {
                            outLines.AddRange(b.Lines);
                            continue;
                        }

                        if (b.Header.Equals("ShopInfo", StringComparison.OrdinalIgnoreCase))
                        {
                            outLines.Add("[" + b.Header + "]");
                            foreach (var l in b.Lines)
                            {
                                var tt = (l ?? "").Trim();
                                if (tt.StartsWith("ItemAmount", StringComparison.OrdinalIgnoreCase))
                                    continue; // remove old ItemAmount
                                outLines.Add(l);
                            }
                            // write new ItemAmount = MAX index (paling bawah)
                            outLines.Add($"ItemAmount={maxItemIdx}");
                            wroteShopInfo = true;
                        }
                        else
                        {
                            outLines.Add("[" + b.Header + "]");
                            outLines.AddRange(b.Lines);
                        }
                    }

                    if (!wroteShopInfo)
                    {
                        int insertAt = 0;
                        if (outLines.Count > 0 && !outLines[0].TrimStart().StartsWith("["))
                            insertAt = outLines.FindIndex(s => s.TrimStart().StartsWith("["));
                        if (insertAt < 0) insertAt = outLines.Count;

                        outLines.Insert(insertAt, $"ItemAmount={maxItemIdx}");
                        outLines.Insert(insertAt, "[ShopInfo]");
                    }

                    // append new [ItemX] in ascending index
                    var ordered = itemMap.Keys
                        .Where(IsItemSection)
                        .Select(k => (Id: int.Parse(k.Substring(4)), Key: k))
                        .OrderBy(x => x.Id)
                        .ToList();

                    foreach (var (id, key) in ordered)
                    {
                        var map = itemMap[key];
                        outLines.Add("[" + key + "]");
                        WriteIf(outLines, "ItemType", map, keepNull: false);
                        WriteIf(outLines, "CostType", map, keepNull: false);
                        WriteIf(outLines, "Version", map, keepNull: false);
                        WriteIf(outLines, "New", map, keepNull: false);
                        WriteIf(outLines, "Commend", map, keepNull: false);
                        WriteIf(outLines, "OEM", map, keepNull: false);
                        WriteIf(outLines, "Describe", map, keepNull: true);
                        WriteIf(outLines, "ReturnEmoney", map, keepNull: false);
                        WriteIf(outLines, "BeginTime", map, keepNull: false);
                        WriteIf(outLines, "EndTime", map, keepNull: false);
                        WriteIf(outLines, "Tip", map, keepNull: true);
                    }

                    return outLines;
                }


                // =============== 3) Render newshopmx.ini (normalized) ===============
                List<string> RenderNewShopMxIni(Dictionary<string, Dictionary<string, string>> cats)
                {
                    var numeric = cats.Keys
                        .Select(k => (Key: k, Num: int.TryParse(k, out var n) ? n : int.MinValue))
                        .Where(x => x.Num >= 0)
                        .OrderBy(x => x.Num)
                        .Select(x => x.Key)
                        .ToList();
                    var others = cats.Keys.Where(k => !numeric.Contains(k))
                        .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                        .ToList();

                    var order = new List<string>(); order.AddRange(numeric); order.AddRange(others);

                    var lines = new List<string>(cats.Count * 64);
                    foreach (var sec in order)
                    {
                        lines.Add("[" + sec + "]");
                        var d = cats[sec];

                        if (d.TryGetValue("Amount", out var amt)) lines.Add("Amount=" + amt);
                        if (d.TryGetValue("Catalog", out var cat)) lines.Add("Catalog=" + cat);
                        if (d.TryGetValue("ItemSource", out var src)) lines.Add("ItemSource=" + src);
                        if (d.TryGetValue("Name", out var nm)) lines.Add("Name=" + nm);
                        if (d.TryGetValue("Pic", out var pic)) lines.Add("Pic=" + pic);

                        var ids = d.Where(p => p.Key.StartsWith("ItemID", StringComparison.OrdinalIgnoreCase))
                                   .Select(p => (Idx: SuffixIndex(p.Key, "ItemID"), Val: p.Value))
                                   .Where(x => x.Idx > 0)
                                   .OrderBy(x => x.Idx)
                                   .ToList();
                        foreach (var (Idx, Val) in ids) lines.Add($"ItemID{Idx}={Val}");
                    }
                    return lines;
                }


                try { SaveWardrobeToDressroomIni(); } catch { } // yang sedia ada
                try { SaveFollowPetInfo(); }
                catch (Exception ex) { MessageBox.Show("Failed to save FollowPetInfo: " + ex.Message); }

                try { SaveEudSkinIni(); }
                catch (Exception ex) { MessageBox.Show("Failed to save EudLookInfo.ini: " + ex.Message); }

                try { SaveServantComposeIniSplit(); }
                catch (Exception ex) { MessageBox.Show("Failed to save composeconfig.ini: " + ex.Message); }

                // =============== RUN FLOW ===============
                var newshopIniNew = RebuildNewShopIni_OnlyItemX_KeepOthers(newShopRawLines, newItemSections);
                //var newshopmxIniNew = RenderNewShopMxIni(mxSanitized);
                var newshopmxIniNew = RenderNewShopMxIni_KeepCatalogTop(newShopMxRawLines, mxSanitized);

                var iniFolder = Path.Combine(txtClientPath.Text, "ini");
                var newshopIniPath = Path.Combine(iniFolder, "newshop.ini");
                var newshopmxIniPath = Path.Combine(iniFolder, "newshopmx.ini");

                File.WriteAllLines(newshopIniPath, newshopIniNew, Encoding.UTF8);
                File.WriteAllLines(newshopmxIniPath, newshopmxIniNew, Encoding.UTF8);

                // refresh in-memory
                newShopRawLines = newshopIniNew;
                newShopMxRawLines = newshopmxIniNew;
                newShopMxCategories = mxSanitized;
                newShopItems = newItemSections;

                // === shop.dat ===
                ShopDatHandler.Save(shopDatPath, allShops);

                // === convert .dat ===
                if (!string.IsNullOrEmpty(newShopDatPath))
                    NewShopDatHandler.Write(newShopDatPath, newshopIniNew);
                if (!string.IsNullOrEmpty(newShopMxDatPath))
                    NewShopMxDatHandler.Write(newShopMxDatPath, newShopMxCategories);

                SaveEventShopIni();
                using (var tx = conn.BeginTransaction())
                {
                    UpsertEventShopToDb(conn, tx);
                    tx.Commit();
                }

                // === patch itemtype.fdb (harga) ===
                try
                {
                    var fdbPath = Path.Combine(txtClientPath.Text, "ini", "itemtype.fdb");
                    if (File.Exists(fdbPath) && _it != null)
                    {
                        var updates = new Dictionary<uint, (int Gold, int Emoney)>();
                        if (_it.GridTable != null && _it.GridTable.Rows.Count > 0)
                        {
                            foreach (DataRow row in _it.GridTable.Rows)
                            {
                                if (!UInt32.TryParse(Convert.ToString(row["ID"]), out var id)) continue;
                                int g = 0, ep = 0;
                                int.TryParse(Convert.ToString(row["Gold"]), out g);
                                int.TryParse(Convert.ToString(row["Emoney"]), out ep);
                                updates[id] = (g, ep);
                            }
                        }
                        else if (_it.ById != null)
                        {
                            foreach (var kv in _it.ById)
                                updates[kv.Key] = (kv.Value.Gold, kv.Value.Emoney);
                        }
                        if (updates.Count > 0)
                            FdbItemtypePatcher.UpdatePrices(fdbPath, updates);
                    }
                }
                catch (Exception ex2)
                {
                    MessageBox.Show("Failed to update itemtype.fdb: " + ex2.Message, "FDB Update");
                }
                // --- SAVE Wardrobe (dressroomitem.ini) ---
                try { SaveWardrobeToDressroomIni(); }
                catch (Exception ex) { MessageBox.Show("Failed to save dressroomitem.ini: " + ex.Message); }

                // === NEW: refresh DB tables (cq_goods & cq_collectiongoods) ===
                try
                {
                    RefreshGoodsTables();
                }
                catch (Exception dbex)
                {
                    MessageBox.Show("Failed to refresh cq_goods/cq_collectiongoods: " + dbex.Message, "DB Update");
                }

                MessageBox.Show("Save completed & Database updated.");
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to save: " + ex.Message);
            }

        }


        // === Wardrobe helpers ===
        private IEnumerable<(uint id, int buy, int price, string desc)> ReadWardrobeGrid(DataGridView gv)
        {
            if (gv == null || gv.DataSource == null) yield break;
            if (!gv.Columns.Contains("ItemID") || !gv.Columns.Contains("Buyable")
                || !gv.Columns.Contains("Price") || !gv.Columns.Contains("Describe")) yield break;

            foreach (DataGridViewRow r in gv.Rows)
            {
                if (r.IsNewRow) continue;
                uint id = ToUInt(r.Cells["ItemID"]?.Value);
                if (id == 0) continue;
                int buy = (r.Cells["Buyable"]?.Value is bool b && b) ? 1 : 0;
                int price = ToInt(r.Cells["Price"]?.Value);
                string desc = (r.Cells["Describe"]?.Value?.ToString() ?? "").Trim().Replace(" ", "~");
                yield return (id, buy, price, desc);
            }
        }

        // Himpun item Buyable utk cq_goods (ownerid=1207) – hanya Casual/Weapon/Decor/Toy
        private HashSet<uint> CollectWardrobeBuyables()
        {
            var res = new HashSet<uint>();
            void addFrom(DataGridView gv)
            {
                foreach (var x in ReadWardrobeGrid(gv))
                    if (x.buy == 1) res.Add(x.id);
            }
            addFrom(dgvWBCasual);
            addFrom(dgvWBWeapon);
            addFrom(dgvWBDecor);
            addFrom(dgvWBToy);
            return res;
        }
        // map label -> code in dressroom (1,2,64,128)
        private static int GenderCodeFromLabel(string? s)
        {
            s = (s ?? "").Trim();
            if (s.Equals("Male(SS)", StringComparison.OrdinalIgnoreCase)) return 64;
            if (s.Equals("Female(SS)", StringComparison.OrdinalIgnoreCase)) return 128;
            if (s.StartsWith("Male", StringComparison.OrdinalIgnoreCase)) return 1;
            if (s.StartsWith("Female", StringComparison.OrdinalIgnoreCase)) return 2;
            return 0;
        }

        private IEnumerable<(uint id, int g, int buy, int price, string desc)>
        ReadWardrobeRows(DataGridView gv)
        {
            if (gv?.DataSource == null) yield break;
            bool hasGender = gv.Columns.Contains("Gender");
            foreach (DataGridViewRow r in gv.Rows)
            {
                if (r.IsNewRow) continue;
                uint id = ToUInt(r.Cells["ItemID"]?.Value);
                if (id == 0) continue;

                int g = 0;
                if (hasGender) g = GenderCodeFromLabel(r.Cells["Gender"]?.Value?.ToString());

                int buy = (r.Cells["Buyable"]?.Value is bool b && b) ? 1 : 0;
                int price = ToInt(r.Cells["Price"]?.Value);
                string desc = (r.Cells["Describe"]?.Value?.ToString() ?? "").Trim().Replace(" ", "~");

                yield return (id, g, buy, price, desc);
            }
        }

        // === REPLACE: simpan ikut (ItemID, GenderCode) supaya Male/Female tak bercampur ===
        private void SaveWardrobeToDressroomIni()
        {
            string clientPath = txtClientPath.Text.Trim();
            var p1 = Path.Combine(clientPath, "ini", "dressroomitem.ini");
            var p2 = Path.Combine(clientPath, "dressroomitem.ini");
            string iniPath = File.Exists(p1) ? p1 : (File.Exists(p2) ? p2 : "");
            if (string.IsNullOrEmpty(iniPath)) return;

            // build map: key=(ItemID,GenderCode)
            var map = new Dictionary<(uint id, int g), (int buy, int price, string desc)>();

            void collect(DataGridView gv)
            {
                foreach (var x in ReadWardrobeRows(gv))
                    map[(x.id, x.g)] = (x.buy, x.price, x.desc);
            }
            collect(dgvWBCasual);
            collect(dgvWBWeapon);   // tiada Gender → g=0
            collect(dgvWBDecor);    // g=0
            collect(dgvWBToy);      // g=0
            collect(dgvWBHair);
            collect(dgvWBAvatar);

            var enc = Encoding.GetEncoding(936);
            var lines = File.ReadAllLines(iniPath, enc).ToList();

            for (int i = 0; i < lines.Count; i++)
            {
                string raw = lines[i];
                string line = raw.Trim();
                if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("//")) continue;

                var parts = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries).ToList();
                if (parts.Count < 7) continue;

                if (!uint.TryParse(parts[1], out var itemId)) continue;
                int genderCode = SafeToInt(parts[2]); // 1/2/64/128 or 0

                if (map.TryGetValue((itemId, genderCode), out var v))
                {
                    // parts[3]=Buyable, [4]=Price, [6]=Describe
                    parts[3] = v.buy == 1 ? "1" : "0";
                    parts[4] = v.price.ToString();
                    parts[6] = string.IsNullOrWhiteSpace(v.desc) ? " " : v.desc;

                    lines[i] = string.Join(" ", parts);
                }
                else if (map.TryGetValue((itemId, 0), out v))
                {
                    // untuk kategori tanpa Gender (weapon/decor/toy)
                    parts[3] = v.buy == 1 ? "1" : "0";
                    parts[4] = v.price.ToString();
                    parts[6] = string.IsNullOrWhiteSpace(v.desc) ? " " : v.desc;
                    lines[i] = string.Join(" ", parts);
                }
            }

            try { File.Copy(iniPath, iniPath + ".bak", true); } catch { }
            File.WriteAllLines(iniPath, lines, enc);
        }

        // Insert Wardrobe Buyables -> cq_goods(ownerid=1207) tanpa duplicate
        private void UpsertWardrobeBuyablesIntoGoods(MySqlConnection c, MySqlTransaction tx)
        {
            var ids = CollectWardrobeBuyables();
            if (ids.Count == 0) return;

            using var cmd = new MySqlCommand(
                @"INSERT INTO cq_goods (ownerid,itemtype,notemoney2)
          SELECT 1207,@itemtype,0 FROM DUAL
          WHERE NOT EXISTS (SELECT 1 FROM cq_goods WHERE ownerid=1207 AND itemtype=@itemtype)", c, tx);
            var pItem = cmd.Parameters.Add("@itemtype", MySqlDbType.UInt32);

            foreach (var id in ids)
            {
                pItem.Value = id;
                cmd.ExecuteNonQuery();
            }
        }

        // Helper
        static bool IsCatalogSection(string sec)
            => sec.StartsWith("Catalog", StringComparison.OrdinalIgnoreCase)
               && int.TryParse(sec.Substring(7), out _);

        // NEW: render newshopmx.ini while keeping [CatalogX] blocks at top untouched
        List<string> RenderNewShopMxIni_KeepCatalogTop(
            List<string> original,
            Dictionary<string, Dictionary<string, string>> cats)
        {
            // split original into [blocks]
            var blocks = new List<(string? Header, List<string> Lines)>();
            string? curHeader = null;
            var curLines = new List<string>();
            void Flush()
            {
                blocks.Add((curHeader, curLines));
                curHeader = null; curLines = new();
            }
            foreach (var raw in original)
            {
                var line = raw ?? "";
                var t = line.Trim();
                if (t.StartsWith("[") && t.EndsWith("]"))
                {
                    Flush();
                    curHeader = t.Substring(1, t.Length - 2);
                }
                else curLines.Add(line);
            }
            Flush();

            // collect prelude (only the very first pre-section block)
            var prelude = blocks.FirstOrDefault(b => b.Header == null).Lines ?? new List<string>();

            // collect Catalog blocks (keep order as in original)
            var catalogs = blocks.Where(b => b.Header != null && IsCatalogSection(b.Header)).ToList();

            // now render
            var lines = new List<string>(original.Count + cats.Count * 64);

            // keep prelude
            lines.AddRange(prelude);

            // keep Catalog blocks EXACTLY as-is, at the top
            foreach (var b in catalogs)
            {
                lines.Add("[" + b.Header + "]");
                lines.AddRange(b.Lines);
            }

            // render sanitized sections for NON-Catalog only
            // order: numeric ("0","1",...) then others (alpha)
            var nonCatalogKeys = cats.Keys
                .Where(k => !IsCatalogSection(k))
                .ToList();

            var numeric = nonCatalogKeys
                .Select(k => (Key: k, Num: int.TryParse(k, out var n) ? n : int.MinValue))
                .Where(x => x.Num >= 0)
                .OrderBy(x => x.Num)
                .Select(x => x.Key)
                .ToList();

            var others = nonCatalogKeys
                .Where(k => !numeric.Contains(k))
                .OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                .ToList();

            var order = new List<string>(); order.AddRange(numeric); order.AddRange(others);

            foreach (var sec in order)
            {
                lines.Add("[" + sec + "]");
                var d = cats[sec];

                if (d.TryGetValue("Amount", out var amt)) lines.Add("Amount=" + amt);
                if (d.TryGetValue("Catalog", out var cat)) lines.Add("Catalog=" + cat);
                if (d.TryGetValue("ItemSource", out var src)) lines.Add("ItemSource=" + src);
                if (d.TryGetValue("Name", out var nm)) lines.Add("Name=" + nm);
                if (d.TryGetValue("Pic", out var pic)) lines.Add("Pic=" + pic);

                var ids = d.Where(p => p.Key.StartsWith("ItemID", StringComparison.OrdinalIgnoreCase))
                           .Select(p => (Idx: SuffixIndex(p.Key, "ItemID"), Val: p.Value))
                           .Where(x => x.Idx > 0)
                           .OrderBy(x => x.Idx)
                           .ToList();
                foreach (var (Idx, Val) in ids) lines.Add($"ItemID{Idx}={Val}");
            }

            return lines;
        }


        // Get price from itemtype.fdb cache; return (Gold, Emoney)
        private (int Gold, int Emoney) GetPriceFor(uint itemType)
        {
            int g = 0, ep = 0;
            if (_it != null)
            {
                if (_it.ByType != null && _it.ByType.TryGetValue(itemType, out var t))
                { g = t.Gold; ep = t.Emoney; }
                else if (_it.ById != null && _it.ById.TryGetValue(itemType, out var i))
                { g = i.Gold; ep = i.Emoney; }
            }
            return (g, ep);
        }



        private static int SuffixIndex(string key, string prefix)
        {
            if (!key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) return -1;
            var s = key.Substring(prefix.Length);
            return int.TryParse(s, out var n) ? n : -1;
        }
        private static bool HasValue(string? v, bool keepNull)
        {
            if (v == null) return false;
            var t = v.Trim();
            if (t.Length == 0) return false;
            if (!keepNull && t.Equals("null", StringComparison.OrdinalIgnoreCase)) return false;
            return true; // "0" is valid; "null" valid only if keepNull=true
        }




        private void TabControl_Selected(object sender, TabControlEventArgs e)
        {
            // handle NewShopMx sekali sahaja
            if (e.TabPage == tabNewShopMx && (newShopMxCategories == null || newShopMxCategories.Count == 0))
            {
                EnsureItemtypeIndex(); // untuk Name/Emoney

                var mxIniPath = Path.Combine(txtClientPath.Text, "ini", "newshopmx.ini");
                var mxDatPath = Path.Combine(txtClientPath.Text, "ini", "newshopmx.dat");

                if (!File.Exists(mxIniPath))
                {
                    if (File.Exists(mxDatPath))
                    {
                        newShopMxRawLines = NewShopMxDatHandler.Read(mxDatPath);
                        SaveNewShopMxIni(mxIniPath);
                    }
                    else
                    {
                        MessageBox.Show("newshopmx.ini & newshopmx.dat not found!", "Error");
                        return;
                    }
                }
                else
                {
                    var gbk = Encoding.GetEncoding("GBK");
                    newShopMxRawLines = new List<string>(File.ReadAllLines(mxIniPath, gbk));
                }

                newShopMxCategories = ParseIniFile(mxIniPath);
                LoadMxIniCategoriesToTreeView();
            }
        }

        // ===== Itemtype cache loader =====
        private void EnsureItemtypeIndex(bool force = false)
        {
            if (!force && _it != null) return;
            var fdbPath = Path.Combine(txtClientPath.Text, "ini", "itemtype.fdb");
            _it = ItemtypeCache.Load(fdbPath); // one shared cache
        }

        // ===== NewShopMx Tree =====
        private void LoadMxIniCategoriesToTreeView()
        {
            tvMxCategories.Nodes.Clear();
            var catalogNodes = new Dictionary<int, TreeNode>();

            foreach (var entry in newShopMxCategories)
            {
                if (!int.TryParse(entry.Key, out _)) continue;
                if (hiddenMxShops.Contains(entry.Key)) continue;

                var data = entry.Value;

                int catalog = (data.TryGetValue("Catalog", out var catStr) && int.TryParse(catStr, out var c)) ? c : -1;
                string catalogName = MxCatalogMap.TryGetValue(catalog, out var mappedName)
                    ? mappedName
                    : $"Catalog {catalog}";

                if (!catalogNodes.TryGetValue(catalog, out var parent))
                {
                    parent = tvMxCategories.Nodes.Add($"cat_{catalog}", catalogName);
                    catalogNodes[catalog] = parent;
                }

                // --- label handling ---
                string label;
                if (data.TryGetValue("Name", out var nm) && !string.IsNullOrWhiteSpace(nm))
                    label = nm;
                else
                    label = (entry.Key == "0") ? "Home" : $"[{entry.Key}]";

                if (data.TryGetValue("Amount", out var amtStr) && !string.IsNullOrEmpty(amtStr))
                    label += $" ({amtStr})";

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

        // ===== NewShop.dat helpers =====
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
                        newShopItems[currentSection] = currentItem;

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
                newShopItems[currentSection] = currentItem;
        }

        private void SaveNewShopIni(string savePath)
        {
            try { File.WriteAllLines(savePath, newShopRawLines, Encoding.UTF8); }
            catch (Exception ex) { MessageBox.Show("Failed to save NewShop.ini: " + ex.Message); }
        }

        // ===== NewShopMx handlers =====
        private void tvMxCategories_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node == null || e.Node.Parent == null) return;
            _currentMxSectionKey = e.Node.Name ?? "";
            if (!newShopMxCategories.TryGetValue(_currentMxSectionKey, out var category)) return;

            EnsureItemtypeIndex();
            var itemIds = new List<string>();
            foreach (var kv in category)
                if (kv.Key.StartsWith("ItemID", StringComparison.OrdinalIgnoreCase))
                    itemIds.Add(kv.Value.Trim());

            dgvNewShopMxItems.DataSource = CreateItemGrid(itemIds, category, _currentMxSectionKey);
            // DataBindingComplete will call ApplyMxGridUi()
        }

        private void ApplyMxGridUi()
        {
            if (dgvNewShopMxItems.DataSource == null) return;

            // hide columns seperti biasa
            HideMxCol("ItemID", true);
            HideMxCol("OEM", true);
            HideMxCol("Describe", true);
            HideMxCol("ReturnEmoney", true);
            HideMxCol("BeginTime", true);
            HideMxCol("EndTime", true);

            // pastikan CostType button – JANGAN kongsi flag
            if (!_mxCostBtnEnsuring)
            {
                _mxCostBtnEnsuring = true;
                BeginInvoke(new MethodInvoker(() =>
                {
                    try { EnsureMxCostTypeButtonColumn(); }
                    finally { _mxCostBtnEnsuring = false; }
                }));
            }

            // pastikan checkbox & RO rules – flag berasingan
            if (!_mxChkEnsuring)
            {
                _mxChkEnsuring = true;
                BeginInvoke(new MethodInvoker(() =>
                {
                    try
                    {
                        EnsureMxCheckBoxColumns();
                        ApplyMxReadOnlyRules();   // pastikan CostTypeBtn clickable
                    }
                    finally { _mxChkEnsuring = false; }
                }));
            }
        }

        // Create checkbox columns bound to hidden numeric cols "New"/"Commend"
        private void EnsureMxCheckBoxColumns()
        {
            if (!dgvNewShopMxItems.Columns.Contains("New") || !dgvNewShopMxItems.Columns.Contains("Commend")) return;

            try { dgvNewShopMxItems.CurrentCell = null; } catch { }

            EnsureOneCheck("New", "NewChk", "New");
            EnsureOneCheck("Commend", "CommendChk", "Commend");
        }
        private void EnsureOneCheck(string dataCol, string chkName, string header)
        {
            var baseCol = dgvNewShopMxItems.Columns[dataCol];
            baseCol.Visible = false; // keep numeric "1/0" hidden

            if (!dgvNewShopMxItems.Columns.Contains(chkName))
            {
                var c = new DataGridViewCheckBoxColumn
                {
                    Name = chkName,
                    HeaderText = header,
                    ThreeState = false
                };
                int insertAt = Math.Min(baseCol.DisplayIndex, dgvNewShopMxItems.Columns.Count);
                dgvNewShopMxItems.Columns.Insert(insertAt, c);
            }
        }

        // Lock RO/Editable cols per requirement
        private void ApplyMxReadOnlyRules()
        {
            if (dgvNewShopMxItems.Columns.Contains("No")) dgvNewShopMxItems.Columns["No"].ReadOnly = true;
            if (dgvNewShopMxItems.Columns.Contains("Name")) dgvNewShopMxItems.Columns["Name"].ReadOnly = true;
            if (dgvNewShopMxItems.Columns.Contains("ItemType")) dgvNewShopMxItems.Columns["ItemType"].ReadOnly = true;

            if (dgvNewShopMxItems.Columns.Contains("Version")) dgvNewShopMxItems.Columns["Version"].ReadOnly = false;
            if (dgvNewShopMxItems.Columns.Contains("Tip")) dgvNewShopMxItems.Columns["Tip"].ReadOnly = false;

            if (dgvNewShopMxItems.Columns.Contains("NewChk")) dgvNewShopMxItems.Columns["NewChk"].ReadOnly = false;
            if (dgvNewShopMxItems.Columns.Contains("CommendChk")) dgvNewShopMxItems.Columns["CommendChk"].ReadOnly = false;

            // penting: pastikan boleh klik butang CostType
            if (dgvNewShopMxItems.Columns.Contains("CostTypeBtn"))
                dgvNewShopMxItems.Columns["CostTypeBtn"].ReadOnly = false;
        }


        // Ensure CostType button column exists & positioned; no column removal
        // SELAMAT: tak remove kolum, tak tukar CurrentCell masa edit
        private void EnsureMxCostTypeButtonColumn()
        {
            if (dgvNewShopMxItems.IsDisposed) return;
            if (!dgvNewShopMxItems.Columns.Contains("CostType")) return;

            var costCol = dgvNewShopMxItems.Columns["CostType"];
            costCol.Visible = false; // simpan data numeric 1/3/19

            // create jika belum ada
            if (!dgvNewShopMxItems.Columns.Contains("CostTypeBtn"))
            {
                var btn = new DataGridViewButtonColumn
                {
                    Name = "CostTypeBtn",
                    HeaderText = "CostType",
                    FlatStyle = FlatStyle.Standard,
                    UseColumnTextForButtonValue = false,
                    ReadOnly = false    // penting: jangan read-only
                };
                int insertAt = Math.Min(costCol.DisplayIndex, dgvNewShopMxItems.Columns.Count);
                if (insertAt < 0) insertAt = dgvNewShopMxItems.Columns.Count;
                dgvNewShopMxItems.Columns.Insert(insertAt, btn);
            }

            // letak sebelah hidden CostType & pastikan nampak
            var btnCol = dgvNewShopMxItems.Columns["CostTypeBtn"];
            btnCol.Visible = true;
            btnCol.ReadOnly = false;
            btnCol.Resizable = DataGridViewTriState.False;
            btnCol.DisplayIndex = costCol.DisplayIndex;
        }



        private void HideMxCol(string name, bool hide)
        {
            if (dgvNewShopMxItems.Columns.Contains(name))
                dgvNewShopMxItems.Columns[name].Visible = !hide ? true : false;
        }

        private void ApplyMxCostTypeButtonColumn()
        {
            if (!dgvNewShopMxItems.Columns.Contains("CostType")) return;

            if (dgvNewShopMxItems.Columns.Contains("CostTypeBtn"))
                dgvNewShopMxItems.Columns.Remove("CostTypeBtn");

            var col = dgvNewShopMxItems.Columns["CostType"];
            int insertAt = col.Index;
            col.Visible = false; // keep numeric

            var btn = new DataGridViewButtonColumn
            {
                Name = "CostTypeBtn",
                HeaderText = "CostType",
                FlatStyle = FlatStyle.Standard,
                UseColumnTextForButtonValue = false
            };
            if (insertAt < 0 || insertAt > dgvNewShopMxItems.Columns.Count) insertAt = dgvNewShopMxItems.Columns.Count;
            dgvNewShopMxItems.Columns.Insert(insertAt, btn);
        }


        // Build grid for NewShopMx selection (adds MaxCount/Supplement for section [25])
        private DataTable CreateItemGrid(List<string> itemIds, Dictionary<string, string> category, string sectionKey)
        {
            var dt = new DataTable();
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ItemID", typeof(string));
            dt.Columns.Add("Name", typeof(string));      // from itemtype.fdb
            dt.Columns.Add("Emoney", typeof(int));       // from itemtype.fdb
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

            // Only for shop [25]
            bool isShop25 = string.Equals(sectionKey, "25", StringComparison.OrdinalIgnoreCase);
            if (isShop25)
            {
                dt.Columns.Add("MaxCount", typeof(int));
                dt.Columns.Add("Supplement", typeof(int));
            }

            // Keep the resolved ItemType list to query DB afterwards
            var typeKeys = new List<uint>();

            for (int i = 0; i < itemIds.Count; i++)
            {
                string itemIdStr = itemIds[i]?.Trim() ?? "";
                int idx = i + 1;

                // Resolve ItemType: prefer MX ItemType{i}, fallback newshop.ini [Item{ID}].ItemType
                string itemTypeStr = GetMx(category, "ItemType", idx);
                if (string.IsNullOrWhiteSpace(itemTypeStr))
                    itemTypeStr = GetNewShop(itemIdStr, "ItemType");

                uint typeKey = 0; UInt32.TryParse(itemTypeStr, out typeKey);
                typeKeys.Add(typeKey);

                // Pull Name/Emoney from itemtype cache if available
                string name = "";
                int ep = 0;
                if (typeKey > 0 && _it != null && _it.ByType.TryGetValue(typeKey, out var tinfo))
                {
                    name = tinfo.Name;
                    ep = tinfo.Emoney;
                }

                // Other fields: MX value first → fallback to newshop.ini (0 is valid)
                string costType = FirstPresent(GetMx(category, "CostType", idx), GetNewShop(itemIdStr, "CostType"));
                string version = FirstPresent(GetMx(category, "Version", idx), GetNewShop(itemIdStr, "Version"));
                string isNew = FirstPresent(GetMx(category, "New", idx), GetNewShop(itemIdStr, "New"));
                string commend = FirstPresent(GetMx(category, "Commend", idx), GetNewShop(itemIdStr, "Commend"));
                string oem = FirstPresent(GetMx(category, "OEM", idx), GetNewShop(itemIdStr, "OEM"));
                string retEp = FirstPresent(GetMx(category, "ReturnEmoney", idx), GetNewShop(itemIdStr, "ReturnEmoney"));
                string beginT = FirstPresent(GetMx(category, "BeginTime", idx), GetNewShop(itemIdStr, "BeginTime"));
                string endT = FirstPresent(GetMx(category, "EndTime", idx), GetNewShop(itemIdStr, "EndTime"));

                // Raw fields: keep literal "null" if present
                string desc; if (!TryGetMxRaw(category, "Describe", idx, out desc)) TryGetNewShopRaw(itemIdStr, "Describe", out desc);
                string tip; if (!TryGetMxRaw(category, "Tip", idx, out tip)) TryGetNewShopRaw(itemIdStr, "Tip", out tip);

                var row = dt.Rows.Add(
                    idx, itemIdStr, name, ep,
                    itemTypeStr, costType, version, isNew, commend, oem, desc,
                    retEp, beginT, endT, tip
                );

                if (isShop25)
                {
                    // Temporary placeholders; will be filled from DB/cache below
                    row["MaxCount"] = 0;
                    row["Supplement"] = 0;
                }
            }

            // After rows: fill MaxCount/Supplement for [25]
            if (isShop25)
            {
                var dbMap = FetchCollectionGoods(typeKeys);
                foreach (var kv in dbMap)
                    if (!_mxShop25Limits.ContainsKey(kv.Key))
                        _mxShop25Limits[kv.Key] = kv.Value;


                // apply into grid (prefer cache -> DB)
                foreach (DataRow r in dt.Rows)
                {
                    uint tk = ToUInt(r["ItemType"]);
                    int mc = 0, sup = 0;

                    if (_mxShop25Limits.TryGetValue(tk, out var v)) { mc = v.MaxCount; sup = v.Supplement; }
                    else if (dbMap.TryGetValue(tk, out var v2)) { mc = v2.MaxCount; sup = v2.Supplement; }


                    r["MaxCount"] = mc;
                    r["Supplement"] = sup;
                }
            }

            return dt;
        }
        // Read maxcount/supplement from cq_collectiongoods for given itemtypes
        // Prefer rows with ownerid=1207; else fallback any owner.
        private Dictionary<uint, (int MaxCount, int Supplement)> FetchCollectionGoods(IEnumerable<uint>? itemTypes)
        {
            var result = new Dictionary<uint, (int, int)>();
            if (conn == null || conn.State != ConnectionState.Open) return result;

            var ids = (itemTypes ?? Enumerable.Empty<uint>()).Where(x => x > 0).Distinct().ToList();
            if (ids.Count == 0) return result;

            string inList = string.Join(",", ids);
            string sql = @"SELECT itemtype, ownerid,
                          IFNULL(maxcount,0)   AS maxcount,
                          IFNULL(supplement,0) AS supplement
                   FROM cq_collectiongoods
                   WHERE itemtype IN (" + inList + ")";

            using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                uint it = Convert.ToUInt32(rd["itemtype"]);
                uint own = Convert.ToUInt32(rd["ownerid"]);
                int mc = Convert.ToInt32(rd["maxcount"]);
                int sup = Convert.ToInt32(rd["supplement"]);

                // first seen -> take; ownerid=1207 -> override (preferred)
                if (!result.ContainsKey(it)) result[it] = (mc, sup);
                if (own == 1207) result[it] = (mc, sup);
            }
            return result;
        }

        // prefer ownerid=1207; else fallback any owner
        private Dictionary<uint, (int MaxCount, int Supplement)>
        SnapshotCollectionGoodsPrefer1207(IEnumerable<uint> typeKeys)
        {
            var res = new Dictionary<uint, (int, int)>();
            if (conn == null || conn.State != ConnectionState.Open) return res;

            var ids = typeKeys?.Where(x => x > 0).Distinct().ToList() ?? new List<uint>();
            if (ids.Count == 0) return res;

            string inList = string.Join(",", ids);
            string sql = @"SELECT itemtype, ownerid,
                          IFNULL(maxcount,0) AS maxcount,
                          IFNULL(supplement,0) AS supplement
                   FROM cq_collectiongoods
                   WHERE itemtype IN (" + inList + ")";

            using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                uint it = Convert.ToUInt32(rd["itemtype"]);
                int mc = Convert.ToInt32(rd["maxcount"]);
                int sp = Convert.ToInt32(rd["supplement"]);
                uint owner = Convert.ToUInt32(rd["ownerid"]);

                if (!res.ContainsKey(it)) res[it] = (mc, sp);        // first seen (any owner)
                if (owner == 1207) res[it] = (mc, sp);               // prefer owner 1207
            }
            return res;
        }


        // helpers untuk MX/NewShop fallback
        private string GetMx(Dictionary<string, string> cat, string baseKey, int idx)
        {
            if (cat != null)
            {
                if (cat.TryGetValue(baseKey + idx.ToString(), out var v) && !string.IsNullOrWhiteSpace(v)) return v;
                if (cat.TryGetValue(baseKey, out var v2) && !string.IsNullOrWhiteSpace(v2)) return v2;
            }
            return "";
        }
        private string GetNewShop(string itemId, string key)
        {
            if (newShopItems != null &&
                newShopItems.TryGetValue("Item" + itemId, out var d) &&
                d.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v))
                return v;
            return "";
        }
        private static string FirstPresent(params string[] vals)
        {
            foreach (var s in vals)
            {
                if (s == null) continue;
                var t = s.Trim();
                if (t.Length == 0) continue;
                if (t.Equals("null", StringComparison.OrdinalIgnoreCase)) continue;
                return t; // keep "0"
            }
            return "";
        }
        private bool TryGetMxRaw(Dictionary<string, string> cat, string baseKey, int idx, out string value)
        {
            value = "";
            if (cat == null) return false;
            if (cat.TryGetValue(baseKey + idx.ToString(), out var v1)) { value = v1; return true; }
            if (cat.TryGetValue(baseKey, out var v2)) { value = v2; return true; }
            return false;
        }
        private bool TryGetNewShopRaw(string itemId, string key, out string value)
        {
            value = "";
            if (newShopItems != null &&
                newShopItems.TryGetValue("Item" + itemId, out var d) &&
                d.TryGetValue(key, out var v))
            { value = v; return true; }
            return false;
        }

        private void SaveNewShopMxIni(string savePath)
        {
            try { File.WriteAllLines(savePath, newShopMxRawLines, Encoding.UTF8); }
            catch (Exception ex) { MessageBox.Show("Failed to save NewShopMx.ini: " + ex.Message); }
        }

        private void BtnConnect_Click(object? sender, EventArgs e)
        {
            if (conn != null && conn.State == ConnectionState.Open)
            {
                try
                {
                    conn.Close();
                    conn.Dispose();
                    conn = null;
                    btnConnect.Text = "Connect";
                    UpdateConnectUi(); // disable tabs back
                    MessageBox.Show("Disconnected from MySQL.");
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Failed to disconnect: " + ex.Message);
                }
                return;
            }

            // Check path
            string clientPath = txtClientPath.Text.Trim();
            if (string.IsNullOrEmpty(clientPath) || !Directory.Exists(clientPath))
            {
                MessageBox.Show("Please select the client folder first.");
                return;
            }

            string shopPath = Path.Combine(clientPath, "ini", "shop.dat");
            string newshopPath = Path.Combine(clientPath, "ini", "newshop.dat");
            if (!File.Exists(shopPath) || !File.Exists(newshopPath))
            {
                MessageBox.Show("shop.dat or newshop.dat not found in \\ini folder!");
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
                MessageBox.Show("Please fill in all MySQL connection details.");
                return;
            }

            string connStr = $"server={host};port={port};uid={user};pwd={pass};database={db};";

            try
            {
                conn = new MySql.Data.MySqlClient.MySqlConnection(connStr);
                conn.Open();

                if (conn.State == ConnectionState.Open)
                {
                    btnConnect.Text = "Disconnect";

                    // >>> ADD THIS: load item names cache from _it / itemtype.fdb
                    EnsureItemNamesLoaded();

                    // directly load all files including wardrobe (dressroomitem.ini)
                    LoadAllFiles();

                    UpdateConnectUi(); // enable tabs
                }
            }
            catch (Exception ex)
            {
                conn = null;
                MessageBox.Show("Failed to connect to MySQL:\n" + ex.Message);
            }
        }

        private void LoadAllFiles()
        {
            if (string.IsNullOrEmpty(shopDatPath) || !File.Exists(shopDatPath))
            {
                MessageBox.Show("Invalid shop.dat path!", "Error");
                return;
            }

            // SHOP.DAT
            allShops = ShopDatHandler.Read(shopDatPath);
            LoadShopsToGrid();
            LoadFollowPetInfo();
            LoadEudSkinInfo();
            LoadServantComposeSplit();
            LoadEventShopIniSplit();

            //if (ShopDatHandler.hiddenShopIDs.Count > 0)
            //{
            //    MessageBox.Show(
            //       "The following shops are hidden (untouched):\n" +
            //        string.Join(", ", ShopDatHandler.hiddenShopIDs),
            //        "Hidden ShopID Information"
            //    );
            //}

            // ITEMTYPE.FDB (cache)
            try { EnsureItemtypeIndex(true); } catch { _it = null; }

            // NEWSHOP.DAT
            var newshopPath = Path.Combine(txtClientPath.Text, "ini", "newshop.dat");
            if (File.Exists(newshopPath))
            {
                newShopDatPath = newshopPath;
                newShopRawLines = NewShopDatHandler.Read(newShopDatPath);
                ParseNewShopItems();
                SaveNewShopIni(Path.Combine(txtClientPath.Text, "ini", "newshop.ini"));
            }

            // NEWSHOPMX.DAT
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

            // Wardrobe Buy (dressroomitem.ini)
            try
            {
                var dressPath = Path.Combine(txtClientPath.Text.Trim(), "ini", "dressroomitem.ini");
                if (File.Exists(dressPath))
                    LoadWardrobeBuyFromIni(dressPath);
                else
                {
                    // clear if missing
                    BindWardrobeGrid(dgvWBCasual, CreateWardrobeTable());
                    BindWardrobeGrid(dgvWBWeapon, CreateWardrobeTable());
                    BindWardrobeGrid(dgvWBHair, CreateWardrobeTable());
                    BindWardrobeGrid(dgvWBAvatar, CreateWardrobeTable());
                    BindWardrobeGrid(dgvWBDecor, CreateWardrobeTable());
                    BindWardrobeGrid(dgvWBToy, CreateWardrobeTable());
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to load Wardrobe: " + ex.Message);
            }

            if (File.Exists(mxIniPath))
            {
                newShopMxCategories = ParseIniFile(mxIniPath);
                LoadMxIniCategoriesToTreeView();
            }
        }

        private void btnNewShop_Click(object sender, EventArgs e)
        {
            using var f = new NewShopForm();
            if (f.ShowDialog(this) != DialogResult.OK) return;

            if (allShops.Exists(s => s.ShopID == f.ShopId) || ShopDatHandler.hiddenShopIDs.Contains(f.ShopId))
            {
                MessageBox.Show("ShopID already exists or is in the hidden/VIP list.", "Error");
                return;
            }

            if (!string.IsNullOrEmpty(f.ShopName) && Encoding.GetEncoding("GB2312").GetByteCount(f.ShopName) > 16)
            {
                var res = MessageBox.Show(
                    "Name exceeds 16 bytes (it will be truncated when saving). Continue?",
                    "Warning", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (res != DialogResult.Yes) return;
            }

            var shop = new ShopInfo
            {
                ShopID = f.ShopId,
                Name = f.ShopName,
                Type = f.ShopType, // 2=Gold, 5=EP
                ItemIDs = new List<uint>(),
                Reserved1 = new List<uint>(),
                Reserved2 = new List<uint>()
            };

            allShops.Add(shop);
            allShops.Sort((a, b) => a.ShopID.CompareTo(b.ShopID));

            LoadShopsToGrid();
            foreach (DataGridViewRow row in dgvShops.Rows)
            {
                if (row.Cells["ShopID"].Value != null &&
                    Convert.ToUInt32(row.Cells["ShopID"].Value) == f.ShopId)
                {
                    row.Selected = true;
                    dgvShops.CurrentCell = row.Cells["ShopID"];
                    dgvShops.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }

            dgvItems.DataSource = null;
        }

        private void btnNewItem_Click(object sender, EventArgs e)
        {
            EnsureItemtypeIndex(); // make sure _it is ready

            using (var dlg = new NewShopItemForm(_it))
            {
                if (dlg.ShowDialog(this) != DialogResult.OK) return;
                if (dgvShops.SelectedRows.Count == 0) return;

                uint sid = Convert.ToUInt32(dgvShops.SelectedRows[0].Cells["ShopID"].Value);
                var shop = allShops.Find(s => s.ShopID == sid);
                if (shop == null) return;

                // 1) add item to shop
                shop.ItemIDs.Add(dlg.ItemId);
                shop.Reserved1.Add(dlg.Reserved1);
                shop.Reserved2.Add(dlg.Reserved2);

                // 2) sync new prices into the shared Itemtype cache so grids show updated values
                if (_it != null)
                {
                    // Update ById (ID -> Name/Gold/Emoney)
                    if (_it.ById.TryGetValue(dlg.ItemId, out var oldById))
                        _it.ById[dlg.ItemId] = (oldById.Name, dlg.Gold, dlg.Emoney);
                    else
                        _it.ById[dlg.ItemId] = (dlg.ItemName, dlg.Gold, dlg.Emoney);

                    // Update ByType too if you key it by uID (safe to try)
                    if (_it.ByType.TryGetValue(dlg.ItemId, out var oldByType))
                        _it.ByType[dlg.ItemId] = (oldByType.Name, dlg.Gold, dlg.Emoney);

                    // Update the shared DataTable used by dialogs/search
                    var rows = _it.GridTable.Select("ID = " + dlg.ItemId);
                    foreach (var r in rows)
                    {
                        r["Gold"] = dlg.Gold;
                        r["Emoney"] = dlg.Emoney;
                        if (string.IsNullOrEmpty(Convert.ToString(r["Name"])) && !string.IsNullOrEmpty(dlg.ItemName))
                            r["Name"] = dlg.ItemName;
                    }
                    _it.GridTable.AcceptChanges();
                }

                BeginInvoke(new Action(() =>
                {
                    // push EP ke Wardrobe jika item wujud dalam dressroom (casual/weapon/decor/toy)
                    int epNow = dlg.Emoney;
                    if (_it != null && _it.ById.TryGetValue(dlg.ItemId, out var info))
                        epNow = info.Emoney; // ambil dari cache kalau berbeza

                    SyncWardrobePrice(dlg.ItemId, epNow);   // <-- inilah kunci
                }));
                // 3) refresh the items list for this shop
                DgvShops_SelectionChanged(null, EventArgs.Empty);
            }
        }


        // handler butang tambah NewShopMx item
        private void btnNewMallItem_Click(object sender, EventArgs e)
        {
            EnsureItemtypeIndex();
            if (tvMxCategories.SelectedNode == null || tvMxCategories.SelectedNode.Parent == null)
            { MessageBox.Show("Please select a NewShopMx category first."); return; }

            var sectionKey = tvMxCategories.SelectedNode.Name;

            using (var f = new NewShopMxItem(_it, txtClientPath.Text))
            {
                if (f.ShowDialog(this) != DialogResult.OK) return;

                // === simpan ke struktur INI category (ringkas; ikut pattern sedia ada anda) ===
                if (!newShopMxCategories.TryGetValue(sectionKey, out var cat))
                    newShopMxCategories[sectionKey] = cat = new Dictionary<string, string>();

                // tambah di hujung
                int nextIdx = 1;
                while (cat.ContainsKey("ItemID" + nextIdx)) nextIdx++;

                cat["ItemID" + nextIdx] = f.ItemID;
                cat["ItemType" + nextIdx] = f.ItemType;
                cat["CostType" + nextIdx] = f.CostType;
                cat["Version" + nextIdx] = f.Version;
                cat["New" + nextIdx] = f.NewFlag;
                cat["Commend" + nextIdx] = f.CommendFlag;
                cat["OEM" + nextIdx] = f.OEM;
                cat["Describe" + nextIdx] = f.Describe;        // kekal "null"
                cat["ReturnEmoney" + nextIdx] = f.ReturnEmoney;
                cat["BeginTime" + nextIdx] = f.BeginTime;
                cat["EndTime" + nextIdx] = f.EndTime;
                if (!string.IsNullOrEmpty(f.Tip)) cat["Tip" + nextIdx] = f.Tip;

                // === UPDATE semua paparan Emoney kepada nilai baru ===
                uint uType = Convert.ToUInt32(f.ItemType);
                int epVal = f.EmoneyOut;

                // update cache pusat itemtype supaya semua grid ikut
                if (_it != null)
                {
                    if (_it.ByType.TryGetValue(uType, out var a))
                        _it.ByType[uType] = (a.Name, a.Gold, epVal);
                    if (_it.ById.TryGetValue(uType, out var b))
                        _it.ById[uType] = (b.Name, b.Gold, epVal);

                    // grid table (untuk dialog lain / carian)
                    var rows = _it.GridTable.Select("ID = " + uType);
                    foreach (var r in rows) r["Emoney"] = epVal;
                }

                // refresh grids: Shop item list & NewShopMx list
                DgvShops_SelectionChanged(null, EventArgs.Empty);
                tvMxCategories_AfterSelect(tvMxCategories, new TreeViewEventArgs(tvMxCategories.SelectedNode));
                BeginInvoke(new Action(() =>
                {
                    // uType = ItemType; epVal = EmoneyOut
                    SyncWardrobePrice(uType, epVal);        // <-- kunci Wardrobe ikut harga baru
                }));

            }
        }


        private void Dgv_CommitOnDirty(object? sender, EventArgs e)
        {
            var g = sender as DataGridView;
            if (g != null && g.IsCurrentCellDirty)
                g.CommitEdit(DataGridViewDataErrorContexts.Commit);
        }

        // guard rebind elak reentrant


        // === SINGLE COPY ONLY ===
        // Guard to avoid re-entrant rebinds from CellEndEdit -> DataSource changes
        private bool _rebindGuard = false;

        private void RebindItemsForShop_Safe(uint shopId)
        {
            if (_rebindGuard) return;
            _rebindGuard = true;
            try
            {
                var shop = allShops.FirstOrDefault(s => s.ShopID == shopId);
                if (shop == null) { dgvItems.DataSource = null; return; }

                EnsureItemtypeIndex();

                var dt = new DataTable();
                dt.Columns.Add("No", typeof(int));
                dt.Columns.Add("ItemID", typeof(uint));
                dt.Columns.Add("Name", typeof(string));
                dt.Columns.Add("Gold", typeof(int));
                dt.Columns.Add("Emoney", typeof(int));
                dt.Columns.Add("Reserved1", typeof(uint));
                dt.Columns.Add("Reserved2", typeof(uint));

                int n = shop.ItemIDs?.Count ?? 0;
                for (int i = 0; i < n; i++)
                {
                    uint itemId = shop.ItemIDs[i];
                    uint r1 = (i < (shop.Reserved1?.Count ?? 0)) ? shop.Reserved1[i] : 0;
                    uint r2 = (i < (shop.Reserved2?.Count ?? 0)) ? shop.Reserved2[i] : 0;

                    string name = ""; int gold = 0, ep = 0;
                    if (_it != null && _it.ById.TryGetValue(itemId, out var info))
                    { name = info.Name; gold = info.Gold; ep = info.Emoney; }

                    dt.Rows.Add(i + 1, itemId, name, gold, ep, r1, r2);
                }

                // keep selection
                int keepRow = dgvItems.CurrentCell?.RowIndex ?? -1;
                string keepCol = dgvItems.CurrentCell?.OwningColumn?.Name;

                dgvItems.AllowUserToAddRows = false;
                dgvItems.AutoGenerateColumns = true;
                dgvItems.DataSource = dt;

                // hide Reserved1/2 after binding
                HideItemReservedCols();
                ApplyItemGridEditRules();
                ApplyItemGridSizingRules(); // ensure widths right after bind

                // restore selection
                if (keepRow >= 0 && keepRow < dgvItems.Rows.Count)
                {
                    int colIndex = 0;
                    if (!string.IsNullOrEmpty(keepCol) && dgvItems.Columns.Contains(keepCol))
                        colIndex = dgvItems.Columns[keepCol].Index;
                    try { dgvItems.CurrentCell = dgvItems.Rows[keepRow].Cells[colIndex]; } catch { }
                }
            }
            finally { _rebindGuard = false; }
        }

        // 1) SHOP: when Gold/Emoney edited
        // FIX: gunakan Columns.Contains(...) bukan Cells.Contains(...)
        private void DgvItems_CellEndEdit_Safe(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || dgvShops.SelectedRows.Count == 0) return;

            uint shopId = ToUInt(dgvShops.SelectedRows[0].Cells["ShopID"]?.Value);
            var shop = allShops.Find(s => s.ShopID == shopId);
            if (shop == null) return;

            var row = dgvItems.Rows[e.RowIndex];
            string col = dgvItems.Columns[e.ColumnIndex].Name;

            void EnsureLen(int i)
            {
                while (shop.Reserved1.Count <= i) shop.Reserved1.Add(0);
                while (shop.Reserved2.Count <= i) shop.Reserved2.Add(61);
            }
            EnsureLen(e.RowIndex);

            uint itemId = ToUInt(row.Cells["ItemID"]?.Value);
            int gold = dgvItems.Columns.Contains("Gold") ? ToInt(row.Cells["Gold"]?.Value) : 0;

            // EP from cell if exists, else from itemtype cache
            int epNow = GetRowEmoneyFlexible(dgvItems, row, itemId);

            uint r1 = ToUInt(row.Cells["Reserved1"]?.Value);
            uint r2 = ToUInt(row.Cells["Reserved2"]?.Value);

            switch (col)
            {
                case "ItemID":
                    if (itemId == 0) { MessageBox.Show("Invalid ItemID."); return; }
                    shop.ItemIDs[e.RowIndex] = itemId;
                    break;
                case "Reserved1":
                    shop.Reserved1[e.RowIndex] = r1; break;
                case "Reserved2":
                    shop.Reserved2[e.RowIndex] = r2; break;
                case "Gold":
                case "Emoney":
                case "EP":
                case "SellPrice":
                case "Price":
                    if (_it != null)
                    {
                        if (_it.ById.TryGetValue(itemId, out var infoId))
                            _it.ById[itemId] = (infoId.Name, gold, epNow);
                        else
                            _it.ById[itemId] = ("", gold, epNow);

                        if (_it.ByType.TryGetValue(itemId, out var infoTy))
                            _it.ByType[itemId] = (infoTy.Name, gold, epNow);

                        var rs = _it.GridTable.Select("ID = " + itemId);
                        foreach (var r in rs) { r["Gold"] = gold; r["Emoney"] = epNow; }
                        _it.GridTable.AcceptChanges();
                    }
                    break;
            }

            // rebind shop view (as existing code)
            BeginInvoke(new Action(() => RebindItemsForShop_Safe(shopId)));

            // ❗ ALWAYS sync Wardrobe after ANY edit (covers adding new ItemID too)
            BeginInvoke(new Action(() => SyncMxGrid(itemId, epNow)));
            BeginInvoke(new Action(() => SyncWardrobePrice(itemId, epNow)));
        }


        // === REPLACE keseluruhan method ini ===
        // REPLACE whole method
        private void DgvNewShopMxItems_CellEndEdit(object? sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;
            var row = dgvNewShopMxItems.Rows[e.RowIndex];
            string colName = dgvNewShopMxItems.Columns[e.ColumnIndex].Name;

            if (!dgvNewShopMxItems.Columns.Contains("ItemType")) return;
            uint typeKey = ToUInt(row.Cells["ItemType"]?.Value);
            if (typeKey == 0) return;

            // EP flex from row, fallback to cache
            int ep = GetRowEmoneyFlexible(dgvNewShopMxItems, row, typeKey);

            // if user changed EP-like/ID columns, update itemtype cache
            if (colName.Equals("Emoney", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("EP", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("SellPrice", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("Price", StringComparison.OrdinalIgnoreCase) ||
                colName.Equals("ItemType", StringComparison.OrdinalIgnoreCase))
            {
                if (_it != null)
                {
                    int curGold = 0;
                    if (_it.ByType != null && _it.ByType.TryGetValue(typeKey, out var tInfo))
                        curGold = tInfo.Gold;
                    else if (_it.ById != null && _it.ById.TryGetValue(typeKey, out var iInfo))
                        curGold = iInfo.Gold;

                    if (_it.ByType != null)
                    {
                        if (_it.ByType.TryGetValue(typeKey, out var infoT))
                            _it.ByType[typeKey] = (infoT.Name, curGold, ep);
                        else
                            _it.ByType[typeKey] = ("", curGold, ep);
                    }
                    if (_it.ById != null)
                    {
                        if (_it.ById.TryGetValue(typeKey, out var infoI))
                            _it.ById[typeKey] = (infoI.Name, curGold, ep);
                        else
                            _it.ById[typeKey] = ("", curGold, ep);
                    }
                    if (_it.GridTable != null)
                    {
                        var rs = _it.GridTable.Select("ID = " + typeKey);
                        foreach (var r in rs) r["Emoney"] = ep;
                        _it.GridTable.AcceptChanges();
                    }
                }

                if (dgvNewShopMxItems.Columns.Contains("ReturnEmoney"))
                    row.Cells["ReturnEmoney"].Value = (ep < 10) ? "" : (ep / 10).ToString();

                if (dgvShops.SelectedRows.Count > 0)
                {
                    var val = dgvShops.SelectedRows[0].Cells["ShopID"].Value;
                    if (val != null && UInt32.TryParse(val.ToString(), out uint currentShopId))
                        BeginInvoke(new Action(() => RebindItemsForShop_Safe(currentShopId)));
                }
            }

            // ❗ always sync Wardrobe after any edit/add
            BeginInvoke(new Action(() => SyncWardrobePrice(typeKey, ep)));
        }

        private static uint ToUInt(object v)
        {
            if (v == null) return 0u;
            try { return Convert.ToUInt32(v); }
            catch { UInt32.TryParse(Convert.ToString(v), out var o); return o; }
        }
        private static int ToInt(object v)
        {
            if (v == null) return 0;
            try { return Convert.ToInt32(v); }
            catch { Int32.TryParse(Convert.ToString(v), out var o); return o; }
        }


        // ===== MX normalizer =====
        private sealed class MxRow
        {
            public string ItemID = "";
            public string ItemType = "";
            public string CostType = "";
            public string Version = "";
            public string New = "";
            public string Commend = "";
            public string OEM = "";
            public string Describe = "";
            public string ReturnEmoney = "";
            public string BeginTime = "";
            public string EndTime = "";
            public string Tip = "";
        }

        private static readonly string[] _mxKeysPerIndex = new[]
        {
    "ItemID","ItemType","CostType","Version","New","Commend","OEM",
    "Describe","ReturnEmoney","BeginTime","EndTime","Tip"
};



        private static readonly string[] _mxPerItemKeys = new[]{
    "ItemID","ItemType","CostType","Version","New","Commend","OEM",
    "Describe","ReturnEmoney","BeginTime","EndTime","Tip"
};


        // Snapshot semua mc/sup ownerid=1207 sebelum TRUNCATE
        private Dictionary<uint, (int MaxCount, int Supplement)> SnapshotCollectionGoods1207()
        {
            var res = new Dictionary<uint, (int, int)>();
            if (conn == null || conn.State != ConnectionState.Open) return res;

            using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                "SELECT itemtype, IFNULL(maxcount,0) AS maxcount, IFNULL(supplement,0) AS supplement " +
                "FROM cq_collectiongoods WHERE ownerid = 1207", conn);
            using var rd = cmd.ExecuteReader();
            while (rd.Read())
            {
                uint it = Convert.ToUInt32(rd["itemtype"]);
                int mc = Convert.ToInt32(rd["maxcount"]);
                int sp = Convert.ToInt32(rd["supplement"]);
                res[it] = (mc, sp);
            }
            return res;
        }

        // call inside class ShopEditor
        private void RefreshGoodsTables()
        {
            if (conn == null || conn.State != ConnectionState.Open) return;

            // --- Kira siap-siap item [25] & harga EP (sebelum TRUNCATE) ---
            var items25 = new List<(uint TypeKey, int Price)>();
            if (newShopMxCategories.TryGetValue("25", out var sec25))
            {
                int amount = 0; int.TryParse(sec25.TryGetValue("Amount", out var a) ? a : "0", out amount);
                var seen25 = new HashSet<uint>();
                for (int i = 1; i <= amount; i++)
                {
                    string itemTypeStr = sec25.TryGetValue("ItemType" + i, out var t) ? t : "";
                    if (string.IsNullOrWhiteSpace(itemTypeStr))
                    {
                        var itemIdStr = sec25.TryGetValue("ItemID" + i, out var v) ? v?.Trim() ?? "" : "";
                        if (newShopItems.TryGetValue("Item" + itemIdStr, out var d) && d.TryGetValue("ItemType", out var t2))
                            itemTypeStr = t2;
                    }
                    if (!UInt32.TryParse(itemTypeStr, out var typeKey) || typeKey == 0) continue;
                    if (!seen25.Add(typeKey)) continue;

                    var (_, ep) = GetPriceFor(typeKey); // EP price (0 allowed)
                    items25.Add((typeKey, ep));
                }
            }

            // --- Snapshot nilai lama (prefer 1207; else any owner) untuk item2 [25] ---
            var prev25 = SnapshotCollectionGoodsPrefer1207(items25.Select(x => x.TypeKey));

            using var tx = conn.BeginTransaction();
            try
            {
                // TRUNCATE
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("TRUNCATE TABLE cq_goods", conn, tx))
                    cmd.ExecuteNonQuery();
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand("TRUNCATE TABLE cq_collectiongoods", conn, tx))
                    cmd.ExecuteNonQuery();

                RebuildPackPetInfoType(conn, tx);

                // ---------- (1) Shop.dat -> cq_goods (skip if Gold&Emoney == 0) ----------
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "INSERT INTO cq_goods (ownerid, itemtype, notemoney2) VALUES (@ownerid,@itemtype,@note2)", conn, tx))
                {
                    var pOwner = cmd.Parameters.Add("@ownerid", MySql.Data.MySqlClient.MySqlDbType.UInt32);
                    var pItem = cmd.Parameters.Add("@itemtype", MySql.Data.MySqlClient.MySqlDbType.UInt32);
                    var pNote2 = cmd.Parameters.Add("@note2", MySql.Data.MySqlClient.MySqlDbType.Int32);

                    foreach (var shop in allShops)
                    {
                        if (ShopDatHandler.hiddenShopIDs.Contains(shop.ShopID)) continue;
                        if (shop.ItemIDs == null) continue;

                        int note2 = (shop.Type == 5u) ? 1 : 0;
                        foreach (var itemType in shop.ItemIDs)
                        {
                            if (itemType == 0) continue;
                            var (g, ep) = GetPriceFor(itemType);
                            if (g == 0 && ep == 0) continue; // skip kosong

                            pOwner.Value = shop.ShopID;
                            pItem.Value = itemType;   // Shop.dat is itemtype
                            pNote2.Value = note2;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                // ---------- (2) NewShopMx (kecuali [25]) -> cq_goods (ownerid=1207, dedup, price filter) ----------
                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "INSERT INTO cq_goods (ownerid, itemtype, notemoney2) VALUES (1207,@itemtype,0)", conn, tx))
                {
                    var pItem = cmd.Parameters.Add("@itemtype", MySql.Data.MySqlClient.MySqlDbType.UInt32);
                    var seen = new HashSet<uint>();

                    foreach (var kv in newShopMxCategories)
                    {
                        var sectionKey = kv.Key?.Trim() ?? "";
                        if (string.Equals(sectionKey, "25", StringComparison.OrdinalIgnoreCase)) continue;
                        var map = kv.Value ?? new Dictionary<string, string>();

                        int amount = 0; int.TryParse(map.TryGetValue("Amount", out var a) ? a : "0", out amount);
                        for (int i = 1; i <= amount; i++)
                        {
                            string itemTypeStr = map.TryGetValue("ItemType" + i, out var t) ? t : "";
                            if (string.IsNullOrWhiteSpace(itemTypeStr))
                            {
                                var itemIdStr = map.TryGetValue("ItemID" + i, out var v) ? v?.Trim() ?? "" : "";
                                if (newShopItems.TryGetValue("Item" + itemIdStr, out var d) && d.TryGetValue("ItemType", out var t2))
                                    itemTypeStr = t2;
                            }
                            if (!UInt32.TryParse(itemTypeStr, out var typeKey) || typeKey == 0) continue;
                            if (!seen.Add(typeKey)) continue;

                            var (g, ep) = GetPriceFor(typeKey);
                            if (g == 0 && ep == 0) continue; // skip kosong

                            pItem.Value = typeKey;
                            cmd.ExecuteNonQuery();
                        }
                    }
                }

                UpsertWardrobeBuyablesIntoGoods(conn, tx);

                UpsertWardrobeFaceHairPerRow(dgvWBAvatar, "cq_faceinfotype", conn, tx);
                UpsertWardrobeFaceHairPerRow(dgvWBHair, "cq_hairinfotype", conn, tx);

                UpsertEudSkinToDb(conn, tx);

                UpsertServantToDbSplit(conn, tx);

                using (var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "TRUNCATE TABLE cq_goddesscomposeconfig;", conn, tx))
                    cmd.ExecuteNonQuery();

                UpsertServantToDbSplit(conn, tx);
                // ---------- (3) Shop [25] -> cq_collectiongoods (ownerid=1207) ----------
                if (items25.Count > 0)
                {
                    // ambil edit grid kalau ada
                    var gridEdits = new Dictionary<uint, (int Max, int Sup)>();
                    if (dgvNewShopMxItems != null &&
                        dgvNewShopMxItems.Columns.Contains("ItemType") &&
                        dgvNewShopMxItems.Columns.Contains("MaxCount") &&
                        dgvNewShopMxItems.Columns.Contains("Supplement"))
                    {
                        foreach (DataGridViewRow r in dgvNewShopMxItems.Rows)
                        {
                            if (r.IsNewRow) continue;
                            uint tk = ToUInt(r.Cells["ItemType"]?.Value);
                            if (tk == 0) continue;
                            gridEdits[tk] = (ToInt(r.Cells["MaxCount"]?.Value), ToInt(r.Cells["Supplement"]?.Value));
                        }
                    }

                    using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                        @"INSERT INTO cq_collectiongoods
                  (ownerid,itemtype,maxcount,price,pricetype,`index`,supplement,viewtype,`_datetime`,Retainfield)
                  VALUES (1207,@itemtype,@maxcount,@price,1,1,@supplement,0,0,1)", conn, tx);
                    var pItem = cmd.Parameters.Add("@itemtype", MySql.Data.MySqlClient.MySqlDbType.UInt32);
                    var pMax = cmd.Parameters.Add("@maxcount", MySql.Data.MySqlClient.MySqlDbType.Int32);
                    var pSup = cmd.Parameters.Add("@supplement", MySql.Data.MySqlClient.MySqlDbType.Int32);
                    var pPrice = cmd.Parameters.Add("@price", MySql.Data.MySqlClient.MySqlDbType.Int32);

                    foreach (var it in items25)
                    {
                        int mc = 0, sup = 0;

                        // Priority: _mxShop25Limits (cache UI) > gridEdits > prev snapshot (prefer 1207, else any)
                        if (_mxShop25Limits.TryGetValue(it.TypeKey, out var v1)) { mc = v1.MaxCount; sup = v1.Supplement; }
                        else if (gridEdits.TryGetValue(it.TypeKey, out var v2)) { mc = v2.Max; sup = v2.Sup; }
                        else if (prev25.TryGetValue(it.TypeKey, out var v3)) { mc = v3.MaxCount; sup = v3.Supplement; }

                        pItem.Value = it.TypeKey;
                        pMax.Value = mc;
                        pSup.Value = sup;
                        pPrice.Value = it.Price;
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
            catch
            {
                try { tx.Rollback(); } catch { }
                throw;
            }
        }

        // ===== Viewport helpers =====
        private static int SafeFirstRow(DataGridView gv)
        {
            try { return gv.FirstDisplayedScrollingRowIndex; } catch { return -1; }
        }
        private static int SafeHOffset(DataGridView gv)
        {
            try { return gv.HorizontalScrollingOffset; } catch { return 0; }
        }
        private static void RestoreViewport(DataGridView gv, int firstRow, int hOff)
        {
            try
            {
                if (firstRow >= 0 && firstRow < gv.Rows.Count)
                    gv.FirstDisplayedScrollingRowIndex = firstRow;
                gv.HorizontalScrollingOffset = Math.Max(0, hOff);
            }
            catch { /* ignore */ }
        }


        // --- utils: traverse controls ---
        private IEnumerable<Control> AllControls(Control root)
        {
            foreach (Control c in root.Controls)
            {
                yield return c;
                foreach (var cc in AllControls(c)) yield return cc;
            }
        }


        // get Emoney for a row (prefer cell, fallback to itemtype cache)
        private int GetRowEmoneyFlexible(DataGridView gv, DataGridViewRow row, uint itemId)
        {
            // try common EP column names
            foreach (var name in new[] { "Emoney", "EP", "SellPrice", "Price" })
                if (gv.Columns.Contains(name))
                    return ToInt(row.Cells[name]?.Value);
            // fallback: pull from itemtype cache
            try { var p = GetPriceFor(itemId); return p.Item2; } catch { return 0; }
        }

        // ====== REPLACE: Upsert Wardrobe -> face/hair (NO sex updates) ======
        private sealed class SideInfo { public bool Set; public int Price; }

        // ===== per-row UPSERT utk Avatar/Hair (NO sex update, ikut baris) =====
        // Update satu baris ke table target (face/hair) ikut gender baris tu

        // === PER-ROW UPSERT utk Avatar/Hair (ikut baris; NO sex update) ===
        // tableName: "cq_faceinfotype" atau "cq_hairinfotype"
        // pastikan wujud 1 row utk id
        // pastikan wujud 1 row utk id
        // pastikan wujud 1 row utk id (supaya boleh update ke 0 juga)
        private void EnsureRowExists(string tableName, uint id, MySqlConnection c, MySqlTransaction tx)
        {
            using var ins = new MySql.Data.MySqlClient.MySqlCommand(
                $"INSERT IGNORE INTO {tableName} (id) VALUES (@id);", c, tx);
            ins.Parameters.AddWithValue("@id", id);
            ins.ExecuteNonQuery();
        }

        // === Per-row UPSERT (Avatar/Hair) — ikut baris; bila untick -> set 0 ===
        private void UpsertFaceHairRow(DataGridViewRow r, string tableName, MySqlConnection c, MySqlTransaction tx)
        {
            if (r == null || r.IsNewRow) return;
            uint id = ToUInt(r.Cells["ItemID"]?.Value);
            if (id == 0) return;

            string g = r.Cells["Gender"]?.Value?.ToString() ?? "";
            bool isMale = g.StartsWith("Male", StringComparison.OrdinalIgnoreCase);
            bool isFemale = g.StartsWith("Female", StringComparison.OrdinalIgnoreCase);

            bool buy = (r.Cells["Buyable"]?.Value is bool b && b);
            int price = ToInt(r.Cells["Price"]?.Value);

            // pastikan ada row
            EnsureRowExists(tableName, id, c, tx);

            if (isMale)
            {
                string sql = buy
                    ? $"UPDATE {tableName} SET PriceType1=1, Price1=@p, Job1=0, ChangePrice1=10000, item1=0 WHERE id=@id;"
                    : $"UPDATE {tableName} SET PriceType1=0, Price1=0, Job1=0, ChangePrice1=0,     item1=0 WHERE id=@id;";
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, c, tx);
                cmd.Parameters.AddWithValue("@id", id);
                if (buy) cmd.Parameters.AddWithValue("@p", price);
                cmd.ExecuteNonQuery();
            }

            if (isFemale)
            {
                string sql = buy
                    ? $"UPDATE {tableName} SET PriceType2=1, Price2=@p2, Job2=0, ChangePrice2=10000, item2=0 WHERE id=@id;"
                    : $"UPDATE {tableName} SET PriceType2=0, Price2=0, Job2=0, ChangePrice2=0,      item2=0 WHERE id=@id;";
                using var cmd2 = new MySql.Data.MySqlClient.MySqlCommand(sql, c, tx);
                cmd2.Parameters.AddWithValue("@id", id);
                if (buy) cmd2.Parameters.AddWithValue("@p2", price);
                cmd2.ExecuteNonQuery();
            }
        }



        // loop semua baris grid
        private void UpsertWardrobeFaceHairPerRow(DataGridView grid, string tableName, MySqlConnection c, MySqlTransaction tx)
        {
            if (grid?.DataSource == null) return;
            if (!grid.Columns.Contains("ItemID") || !grid.Columns.Contains("Gender")
                || !grid.Columns.Contains("Buyable") || !grid.Columns.Contains("Price")) return;

            foreach (DataGridViewRow r in grid.Rows)
                UpsertFaceHairRow(r, tableName, c, tx);
        }

        // xor cipher
        private static byte[] FP_Decrypt(byte[] cipher)
        {
            var plain = new byte[cipher.Length];
            for (int i = 0; i < cipher.Length; i++) plain[i] = (byte)(cipher[i] ^ FP_PASSWORD[i % 8]);
            return plain;
        }
        private static byte[] FP_Encrypt(byte[] plain)
        {
            var cipher = new byte[plain.Length];
            for (int i = 0; i < plain.Length; i++) cipher[i] = (byte)(plain[i] ^ FP_PASSWORD[i % 8]);
            return cipher;
        }

        // DAT -> INI
        private static void FP_DatToIni(string datPath, string iniPath)
        {
            var bytes = File.ReadAllBytes(datPath);
            int pos = 0, total = bytes.Length;
            using var fs = new FileStream(iniPath, FileMode.Create, FileAccess.Write);
            using var w = new StreamWriter(fs, Gbk);
            while (pos + 4 <= total)
            {
                int len = BitConverter.ToInt32(bytes, pos); pos += 4;
                if (pos + len > total) break;
                var cipher = new byte[len];
                Buffer.BlockCopy(bytes, pos, cipher, 0, len); pos += len;
                var line = Gbk.GetString(FP_Decrypt(cipher));
                w.WriteLine(line);
            }
        }

        // INI -> DAT
        private static void FP_IniToDat(string iniPath, string datPath)
        {
            var lines = File.ReadAllLines(iniPath, Gbk);
            using var fs = new FileStream(datPath, FileMode.Create, FileAccess.Write);
            foreach (var line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                var plain = Gbk.GetBytes(line);
                var cipher = FP_Encrypt(plain);
                var len = BitConverter.GetBytes(cipher.Length);
                fs.Write(len, 0, 4);
                fs.Write(cipher, 0, cipher.Length);
            }
        }

        // REPLACE
        private DataTable CreateFollowPetTable()
        {
            var dt = new DataTable("FollowPet");
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("Type", typeof(int));
            dt.Columns.Add("Name", typeof(string));
            dt.Columns.Add("MoveSpeed", typeof(int));
            dt.Columns.Add("PickCD", typeof(int));
            dt.Columns.Add("NeedMoney", typeof(int));
            dt.Columns.Add("PickRange", typeof(int));
            dt.Columns.Add("ForceCall", typeof(int));
            dt.Columns.Add("CallScale", typeof(int));
            dt.Columns.Add("Look", typeof(int));
            dt.Columns.Add("Title", typeof(string));
            dt.Columns.Add("FeatureDesc", typeof(string));
            dt.Columns.Add("Desc", typeof(string));
            dt.Columns.Add("OrigPrice", typeof(int));
            dt.Columns.Add("VersionFlag", typeof(int));   // <-- add
            dt.Columns.Add("SVIPLev", typeof(int));
            dt.Columns.Add("RareDegree", typeof(int));
            dt.Columns.Add("NewEndDate", typeof(string));
            dt.Columns.Add("Collection", typeof(int));
            return dt;
        }


        private void BindFollowPetGrid()
        {
            if (dgvWBFPet == null) return;
            dgvWBFPet.AutoGenerateColumns = true;
            dgvWBFPet.DataSource = dtWBFPet;

            // no resize
            dgvWBFPet.AllowUserToResizeColumns = false;
            dgvWBFPet.AllowUserToResizeRows = false;
            dgvWBFPet.RowHeadersVisible = false;
            dgvWBFPet.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvWBFPet.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // ❌ disable sorting on ALL columns
            foreach (DataGridViewColumn c in dgvWBFPet.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.Resizable = DataGridViewTriState.False;
            }

            // lock No & Type look
            if (dgvWBFPet.Columns.Contains("No"))
                dgvWBFPet.Columns["No"].ReadOnly = true;
            if (dgvWBFPet.Columns.Contains("Type"))
            {
                var c = dgvWBFPet.Columns["Type"];
                c.ReadOnly = true;
                c.DefaultCellStyle.BackColor = Color.Gainsboro;
                c.DefaultCellStyle.SelectionBackColor = Color.Gainsboro;
                c.DefaultCellStyle.SelectionForeColor = Color.Black;
            }

            // keep after rebind
            dgvWBFPet.DataBindingComplete -= DgvWBFPet_NoSort;
            dgvWBFPet.DataBindingComplete += DgvWBFPet_NoSort;
        }
        private void DgvWBFPet_NoSort(object? s, DataGridViewBindingCompleteEventArgs e)
        {
            var gv = s as DataGridView; if (gv == null) return;
            foreach (DataGridViewColumn c in gv.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.Resizable = DataGridViewTriState.False;
            }
        }

        // Pastikan selepas rebind pun tak boleh resize
        private void dgvWBFPet_DataBindingComplete(object sender, DataGridViewBindingCompleteEventArgs e)
        {
            if (sender is DataGridView gv)
            {
                foreach (DataGridViewColumn col in gv.Columns)
                    col.Resizable = DataGridViewTriState.False;
                foreach (DataGridViewRow row in gv.Rows)
                    row.Resizable = DataGridViewTriState.False;
            }
        }

        // REPLACE
        private void LoadFollowPetInfo()
        {
            string client = txtClientPath.Text.Trim();
            string ini = Path.Combine(client, "ini", "FollowPetInfo.ini");
            string dat = Path.Combine(client, "ini", "FollowPetInfo.dat");

            // convert DAT->INI jika ada
            if (File.Exists(dat)) { try { FP_DatToIni(dat, ini); } catch { } }

            dtWBFPet = CreateFollowPetTable();
            if (!File.Exists(ini)) { BindFollowPetGrid(); return; }

            var lines = File.ReadAllLines(ini, Gbk);
            var cur = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            void flush()
            {
                if (cur.Count == 0) return;
                var r = dtWBFPet.NewRow();
                int I(string k) => int.TryParse(cur.TryGetValue(k, out var v) ? v : null, out var x) ? x : 0;
                string S(string k) => cur.TryGetValue(k, out var v) ? v : "";

                r["Type"] = I("Type");
                r["Name"] = S("Name");
                r["MoveSpeed"] = I("MoveSpeed");
                r["PickCD"] = I("PickCD");
                r["NeedMoney"] = I("NeedMoney");
                r["PickRange"] = I("PickRange");
                r["ForceCall"] = I("ForceCall");
                r["CallScale"] = I("CallScale");
                r["Look"] = I("Look");
                r["Title"] = S("Title");
                r["FeatureDesc"] = S("FeatureDesc");
                r["Desc"] = S("Desc");
                r["OrigPrice"] = I("OrigPrice");
                r["VersionFlag"] = I("VersionFlag");   // <-- read
                r["SVIPLev"] = I("SVIPLev");
                r["RareDegree"] = I("RareDegree");
                r["NewEndDate"] = S("NewEndDate");
                r["Collection"] = I("Collection");
                dtWBFPet.Rows.Add(r);
                cur.Clear();
            }

            var reHeader = new System.Text.RegularExpressions.Regex(@"^\[.*\]\s*$");
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                if (reHeader.IsMatch(line)) { flush(); continue; }
                int p = line.IndexOf('=');
                if (p <= 0) continue;
                cur[line.Substring(0, p).Trim()] = (p + 1 < line.Length) ? line[(p + 1)..] : "";
            }
            flush();

            // sort by Type asc + renumber No
            var dv = new DataView(dtWBFPet) { Sort = "Type ASC" };
            dtWBFPet = dv.ToTable();
            for (int i = 0; i < dtWBFPet.Rows.Count; i++) dtWBFPet.Rows[i]["No"] = i + 1;

            BindFollowPetGrid();
        }

        // REPLACE
        private void SaveFollowPetInfo()
        {
            if (dtWBFPet == null) return;

            string client = txtClientPath.Text.Trim();
            string iniDir = Path.Combine(client, "ini");
            Directory.CreateDirectory(iniDir);
            string ini = Path.Combine(iniDir, "FollowPetInfo.ini");
            string dat = Path.Combine(iniDir, "FollowPetInfo.dat");

            // order by Type asc
            var dv = new DataView(dtWBFPet) { Sort = "Type ASC" };
            var dt = dv.ToTable();

            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                int type = Convert.ToInt32(r["Type"]);
                sb.AppendLine($"[{type}]"); // header = Type
                sb.AppendLine($"Type={type}");
                sb.AppendLine($"Name={r["Name"]?.ToString() ?? ""}");
                sb.AppendLine($"MoveSpeed={Convert.ToInt32(r["MoveSpeed"])}");
                sb.AppendLine($"PickCD={Convert.ToInt32(r["PickCD"])}");
                sb.AppendLine($"NeedMoney={Convert.ToInt32(r["NeedMoney"])}");
                sb.AppendLine($"PickRange={Convert.ToInt32(r["PickRange"])}");
                sb.AppendLine($"ForceCall={Convert.ToInt32(r["ForceCall"])}");
                sb.AppendLine($"Look={Convert.ToInt32(r["Look"])}");
                sb.AppendLine($"CallScale={Convert.ToInt32(r["CallScale"])}");
                sb.AppendLine($"Title={r["Title"]?.ToString() ?? ""}");
                sb.AppendLine($"FeatureDesc={r["FeatureDesc"]?.ToString() ?? ""}");
                sb.AppendLine($"Desc={r["Desc"]?.ToString() ?? ""}");
                sb.AppendLine($"OrigPrice={Convert.ToInt32(r["OrigPrice"])}");
                sb.AppendLine($"VersionFlag={Convert.ToInt32(r["VersionFlag"])}"); // <-- write
                sb.AppendLine($"SVIPLev={Convert.ToInt32(r["SVIPLev"])}");
                sb.AppendLine($"RareDegree={Convert.ToInt32(r["RareDegree"])}");
                sb.AppendLine($"NewEndDate={r["NewEndDate"]?.ToString() ?? ""}");
                sb.AppendLine($"Collection={Convert.ToInt32(r["Collection"])}");
                sb.AppendLine();
            }

            try { if (File.Exists(ini)) File.Copy(ini, ini + ".bak", true); } catch { }
            File.WriteAllText(ini, sb.ToString(), Gbk);

            // convert back to DAT
            FP_IniToDat(ini, dat);
        }

        // Rebuild cq_packpetinfotype dari grid Follow Pet
        private void RebuildPackPetInfoType(MySqlConnection c, MySqlTransaction tx)
        {
            // kosongkan table
            using (var t = new MySql.Data.MySqlClient.MySqlCommand("TRUNCATE TABLE cq_packpetinfotype;", c, tx))
                t.ExecuteNonQuery();

            if (dtWBFPet == null || dtWBFPet.Rows.Count == 0) return;

            // insert id(Type) + money(NeedMoney)
            using var ins = new MySql.Data.MySqlClient.MySqlCommand(
                "INSERT INTO cq_packpetinfotype (id, money) VALUES (@id, @money);", c, tx);
            var pId = ins.Parameters.Add("@id", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pMoney = ins.Parameters.Add("@money", MySql.Data.MySqlClient.MySqlDbType.Int32);

            // elak duplicate Type (ambil sekali je)
            var seen = new HashSet<int>();
            foreach (System.Data.DataRow r in dtWBFPet.Rows)
            {
                int type = ToInt(r["Type"]);
                if (type <= 0 || !seen.Add(type)) continue;
                pId.Value = type;
                pMoney.Value = ToInt(r["NeedMoney"]);
                ins.ExecuteNonQuery();
            }
        }

        private DataTable CreateEudSkinTable()
        {
            var dt = new DataTable("EudSkin");
            dt.Columns.Add("No", typeof(int));
            dt.Columns.Add("ID", typeof(int));                 // [section] id  (READ ONLY)
            dt.Columns.Add("LookName", typeof(string));
            dt.Columns.Add("LookType", typeof(int));
            dt.Columns.Add("SetHead", typeof(int));
            dt.Columns.Add("DLG", typeof(int));
            dt.Columns.Add("ShowScale", typeof(int));
            dt.Columns.Add("CallOutScale", typeof(int));
            dt.Columns.Add("NeedEmoney", typeof(int));
            dt.Columns.Add("NeedStarLev", typeof(int));
            dt.Columns.Add("UseEudType1", typeof(int));
            dt.Columns.Add("UseEudType2", typeof(int));
            dt.Columns.Add("UseEudType3", typeof(int));
            dt.Columns.Add("LookPic", typeof(string));
            dt.Columns.Add("LookDesc", typeof(string));
            dt.Columns.Add("LookQuality", typeof(int));
            dt.Columns.Add("GetBackLookCostNum", typeof(int));
            dt.Columns.Add("GetBackLookCostTypeID", typeof(int));
            dt.Columns.Add("GetBackLookTypeID", typeof(int));
            return dt;
        }

        private void BindEudSkinGrid()
        {
            if (dgvWBEudSkin == null) return;
            dgvWBEudSkin.AutoGenerateColumns = true;
            dgvWBEudSkin.DataSource = dtWBEudSkin;

            dgvWBEudSkin.AllowUserToAddRows = false;
            dgvWBEudSkin.AllowUserToResizeColumns = false;
            dgvWBEudSkin.AllowUserToResizeRows = false;
            dgvWBEudSkin.RowHeadersVisible = false;
            dgvWBEudSkin.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvWBEudSkin.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // ❌ disable sorting on ALL columns
            foreach (DataGridViewColumn c in dgvWBEudSkin.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.Resizable = DataGridViewTriState.False;
            }

            if (dgvWBEudSkin.Columns.Contains("No"))
                dgvWBEudSkin.Columns["No"].ReadOnly = true;

            if (dgvWBEudSkin.Columns.Contains("ID"))
            {
                var c = dgvWBEudSkin.Columns["ID"];
                c.ReadOnly = true;
                c.DefaultCellStyle.BackColor = Color.Gainsboro;
                c.DefaultCellStyle.SelectionBackColor = Color.Gainsboro;
                c.DefaultCellStyle.SelectionForeColor = Color.Black;
            }

            dgvWBEudSkin.DataBindingComplete -= DgvWBEudSkin_NoSort;
            dgvWBEudSkin.DataBindingComplete += DgvWBEudSkin_NoSort;
        }
        private void DgvWBEudSkin_NoSort(object? s, DataGridViewBindingCompleteEventArgs e)
        {
            var gv = s as DataGridView; if (gv == null) return;
            foreach (DataGridViewColumn c in gv.Columns)
            {
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.Resizable = DataGridViewTriState.False;
            }
        }


        private void LoadEudSkinInfo()
        {
            string ini = Path.Combine(txtClientPath.Text.Trim(), "ini", "EudLookInfo.ini");
            dtWBEudSkin = CreateEudSkinTable();

            if (!File.Exists(ini)) { BindEudSkinGrid(); return; }

            var lines = File.ReadAllLines(ini, Gbk);
            int curId = -1;
            var cur = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            void flush()
            {
                if (curId < 0) return;
                var r = dtWBEudSkin.NewRow();
                r["ID"] = curId;
                r["LookName"] = cur.TryGetValue("LookName", out var s) ? s : "";
                int I(string k) => int.TryParse(cur.TryGetValue(k, out var v) ? v : null, out var x) ? x : 0;
                r["LookType"] = I("LookType");
                r["SetHead"] = I("SetHead");
                r["DLG"] = I("DLG");
                r["ShowScale"] = I("ShowScale");
                r["CallOutScale"] = I("CallOutScale");
                r["NeedEmoney"] = I("NeedEmoney");
                r["NeedStarLev"] = I("NeedStarLev");
                r["UseEudType1"] = I("UseEudType1");
                r["UseEudType2"] = I("UseEudType2");
                r["UseEudType3"] = I("UseEudType3");
                r["LookPic"] = cur.TryGetValue("LookPic", out s) ? s : "";
                r["LookDesc"] = cur.TryGetValue("LookDesc", out s) ? s : "";
                r["LookQuality"] = I("LookQuality");
                r["GetBackLookCostNum"] = I("GetBackLookCostNum");
                r["GetBackLookCostTypeID"] = I("GetBackLookCostTypeID");
                r["GetBackLookTypeID"] = I("GetBackLookTypeID");
                dtWBEudSkin.Rows.Add(r);
                cur.Clear(); curId = -1;
            }

            var reHead = new Regex(@"^\[(\d+)\]\s*$");
            foreach (var raw in lines)
            {
                var line = raw.Trim();
                if (line.Length == 0) continue;
                var m = reHead.Match(line);
                if (m.Success) { flush(); curId = int.Parse(m.Groups[1].Value); continue; }
                int p = line.IndexOf('='); if (p <= 0) continue;
                cur[line[..p].Trim()] = (p + 1 < line.Length) ? line[(p + 1)..] : "";
            }
            flush();

            // sort by ID, renumber No
            var dv = new DataView(dtWBEudSkin) { Sort = "ID ASC" };
            dtWBEudSkin = dv.ToTable();
            for (int i = 0; i < dtWBEudSkin.Rows.Count; i++) dtWBEudSkin.Rows[i]["No"] = i + 1;

            BindEudSkinGrid();
        }

        private void SaveEudSkinIni()
        {
            if (dtWBEudSkin == null) return;
            string iniDir = Path.Combine(txtClientPath.Text.Trim(), "ini");
            Directory.CreateDirectory(iniDir);
            string ini = Path.Combine(iniDir, "EudLookInfo.ini");

            var dv = new DataView(dtWBEudSkin) { Sort = "ID ASC" };
            var dt = dv.ToTable();

            var sb = new StringBuilder();
            foreach (DataRow r in dt.Rows)
            {
                int id = Convert.ToInt32(r["ID"]);
                sb.AppendLine($"[{id}]");
                sb.AppendLine($"LookName={r["LookName"]}");
                sb.AppendLine($"LookType={Convert.ToInt32(r["LookType"])}");
                sb.AppendLine($"SetHead={Convert.ToInt32(r["SetHead"])}");
                sb.AppendLine($"DLG={Convert.ToInt32(r["DLG"])}");
                sb.AppendLine($"ShowScale={Convert.ToInt32(r["ShowScale"])}");
                sb.AppendLine($"CallOutScale={Convert.ToInt32(r["CallOutScale"])}");
                sb.AppendLine($"NeedEmoney={Convert.ToInt32(r["NeedEmoney"])}");
                sb.AppendLine($"NeedStarLev={Convert.ToInt32(r["NeedStarLev"])}");
                sb.AppendLine($"UseEudType1={Convert.ToInt32(r["UseEudType1"])}");
                sb.AppendLine($"UseEudType2={Convert.ToInt32(r["UseEudType2"])}");
                sb.AppendLine($"UseEudType3={Convert.ToInt32(r["UseEudType3"])}");
                sb.AppendLine($"LookPic={r["LookPic"]}");
                sb.AppendLine($"LookDesc={r["LookDesc"]}");
                sb.AppendLine($"LookQuality={Convert.ToInt32(r["LookQuality"])}");
                sb.AppendLine($"GetBackLookCostNum={Convert.ToInt32(r["GetBackLookCostNum"])}");
                sb.AppendLine($"GetBackLookCostTypeID={Convert.ToInt32(r["GetBackLookCostTypeID"])}");
                sb.AppendLine($"GetBackLookTypeID={Convert.ToInt32(r["GetBackLookTypeID"])}");
                sb.AppendLine();
            }

            try { if (File.Exists(ini)) File.Copy(ini, ini + ".bak", true); } catch { }
            File.WriteAllText(ini, sb.ToString(), Gbk);
        }

        // Upsert all rows to cq_eudlookinfotype
        private void UpsertEudSkinToDb(MySqlConnection c, MySqlTransaction tx)
        {
            if (dtWBEudSkin == null || dtWBEudSkin.Rows.Count == 0) return;

            string sql = @"
INSERT INTO cq_eudlookinfotype
(id, LookType, NeedEmoney, NeedStarLev, UseEudType1, UseEudType2, UseEudType3,
 GetBackLookCostNum, GetBackLookCostTypeID, GetBackLookTypeID)
VALUES (@id, @lt, @ep, @sl, @u1, @u2, @u3, @bn, @bt, @bl)
ON DUPLICATE KEY UPDATE
 LookType=VALUES(LookType),
 NeedEmoney=VALUES(NeedEmoney),
 NeedStarLev=VALUES(NeedStarLev),
 UseEudType1=VALUES(UseEudType1),
 UseEudType2=VALUES(UseEudType2),
 UseEudType3=VALUES(UseEudType3),
 GetBackLookCostNum=VALUES(GetBackLookCostNum),
 GetBackLookCostTypeID=VALUES(GetBackLookCostTypeID),
 GetBackLookTypeID=VALUES(GetBackLookTypeID);";

            using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, c, tx);
            var pId = cmd.Parameters.Add("@id", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pLt = cmd.Parameters.Add("@lt", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pEp = cmd.Parameters.Add("@ep", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pSl = cmd.Parameters.Add("@sl", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pU1 = cmd.Parameters.Add("@u1", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pU2 = cmd.Parameters.Add("@u2", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pU3 = cmd.Parameters.Add("@u3", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pBn = cmd.Parameters.Add("@bn", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pBt = cmd.Parameters.Add("@bt", MySql.Data.MySqlClient.MySqlDbType.Int32);
            var pBl = cmd.Parameters.Add("@bl", MySql.Data.MySqlClient.MySqlDbType.Int32);

            foreach (DataRow r in dtWBEudSkin.Rows)
            {
                pId.Value = Convert.ToInt32(r["ID"]);
                pLt.Value = Convert.ToInt32(r["LookType"]);
                pEp.Value = Convert.ToInt32(r["NeedEmoney"]);
                pSl.Value = Convert.ToInt32(r["NeedStarLev"]);
                pU1.Value = Convert.ToInt32(r["UseEudType1"]);
                pU2.Value = Convert.ToInt32(r["UseEudType2"]);
                pU3.Value = Convert.ToInt32(r["UseEudType3"]);
                pBn.Value = Convert.ToInt32(r["GetBackLookCostNum"]);
                pBt.Value = Convert.ToInt32(r["GetBackLookCostTypeID"]);
                pBl.Value = Convert.ToInt32(r["GetBackLookTypeID"]);
                cmd.ExecuteNonQuery();
            }
        }


        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////            
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private DataTable CreateServantTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("needtype", typeof(int));      // 1=Gift, 2=Spirit (read-only)
            dt.Columns.Add("needlevel", typeof(int));
            dt.Columns.Add("needmoney", typeof(int));
            dt.Columns.Add("needtime", typeof(int));

            dt.Columns.Add("item", typeof(int));
            dt.Columns.Add("ItemName", typeof(string));           // <-- new (read-only)

            dt.Columns.Add("max", typeof(int));
            dt.Columns.Add("proficiency", typeof(int));

            dt.Columns.Add("needitem1", typeof(int));
            dt.Columns.Add("NeedItem1Name", typeof(string));      // <-- new
            dt.Columns.Add("needitemcount1", typeof(int));

            dt.Columns.Add("needitem2", typeof(int));
            dt.Columns.Add("NeedItem2Name", typeof(string));      // <-- new
            dt.Columns.Add("needitemcount2", typeof(int));

            dt.Columns.Add("needitem3", typeof(int));
            dt.Columns.Add("NeedItem3Name", typeof(string));      // <-- new
            dt.Columns.Add("needitemcount3", typeof(int));

            dt.Columns.Add("needitem4", typeof(int));
            dt.Columns.Add("NeedItem4Name", typeof(string));      // <-- new
            dt.Columns.Add("needitemcount4", typeof(int));

            dt.Columns.Add("needitem5", typeof(int));
            dt.Columns.Add("NeedItem5Name", typeof(string));      // <-- new
            dt.Columns.Add("needitemcount5", typeof(int));
            return dt;
        }


        // Load composeconfig.ini and split by needtype (1=Gift, 2=Spirit)
        private void LoadServantComposeSplit()
        {
            string iniPath = Path.Combine(txtClientPath.Text.Trim(), "ini", "composeconfig.ini");
            dtGift = CreateServantTable();
            dtSpirit = CreateServantTable();

            try { EnsureItemNamesLoaded(); } catch { } // make sure itemtype cache ready

            if (File.Exists(iniPath))
            {
                var lines = File.ReadAllLines(iniPath, Gbk);
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;

                    var p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length < 17) continue;
                    int I(int idx) => int.TryParse(p[idx], out var v) ? v : 0;

                    int needtype = I(0);
                    var dt = needtype == 1 ? dtGift : needtype == 2 ? dtSpirit : null;
                    if (dt == null) continue;

                    var r = dt.NewRow();
                    int item = I(4);
                    int n1 = I(7), n2 = I(9), n3 = I(11), n4 = I(13), n5 = I(15);

                    r["needtype"] = needtype;
                    r["needlevel"] = I(1);
                    r["needmoney"] = I(2);
                    r["needtime"] = I(3);

                    r["item"] = item;
                    r["ItemName"] = GetItemNameSafe(item);

                    r["max"] = I(5);
                    r["proficiency"] = I(6);

                    r["needitem1"] = n1;
                    r["NeedItem1Name"] = GetItemNameSafe(n1);
                    r["needitemcount1"] = I(8);

                    r["needitem2"] = n2;
                    r["NeedItem2Name"] = GetItemNameSafe(n2);
                    r["needitemcount2"] = I(10);

                    r["needitem3"] = n3;
                    r["NeedItem3Name"] = GetItemNameSafe(n3);
                    r["needitemcount3"] = I(12);

                    r["needitem4"] = n4;
                    r["NeedItem4Name"] = GetItemNameSafe(n4);
                    r["needitemcount4"] = I(14);

                    r["needitem5"] = n5;
                    r["NeedItem5Name"] = GetItemNameSafe(n5);
                    r["needitemcount5"] = I(16);

                    dt.Rows.Add(r);
                }
            }

            // sort kekal (needlevel asc, proficiency asc)
            DataTable Sort2(DataTable dt)
            {
                var dv = new DataView(dt) { Sort = "needlevel ASC, proficiency ASC" };
                return dv.ToTable();
            }
            dtGift = Sort2(dtGift);
            dtSpirit = Sort2(dtSpirit);

            BindGiftGrid();
            BindSpiritGrid();
        }

        // Fill auto 'id' by (needtype,item)
        private void TryFillServantIdsFromDbSplit()
        {
            try
            {
                if (conn == null || conn.State != ConnectionState.Open) return;

                var map = new Dictionary<(int needtype, int item), int>();
                using var cmd = new MySql.Data.MySqlClient.MySqlCommand(
                    "SELECT id, needtype, item FROM cq_goddesscomposeconfig", conn);
                using var rd = cmd.ExecuteReader();
                while (rd.Read())
                    map[(rd.GetInt32(1), rd.GetInt32(2))] = rd.GetInt32(0);

                void apply(DataTable dt)
                {
                    foreach (DataRow r in dt.Rows)
                    {
                        var key = (Convert.ToInt32(r["needtype"]), Convert.ToInt32(r["item"]));
                        if (map.TryGetValue(key, out var id)) r["id"] = id;
                    }
                }
                apply(dtGift); apply(dtSpirit);
            }
            catch { }
        }

        // using System.ComponentModel; // for ListChangedType

        private void BindGiftGrid()
        {
            if (dgvGift == null) return;
            dgvGift.AutoGenerateColumns = true;

            // hook BEFORE datasource
            dgvGift.DataBindingComplete -= DgvServant_AfterBind;
            dgvGift.DataBindingComplete += DgvServant_AfterBind;

            // base props
            dgvGift.AllowUserToAddRows = false;
            dgvGift.AllowUserToResizeColumns = false;
            dgvGift.AllowUserToResizeRows = false;
            dgvGift.RowHeadersVisible = false;
            dgvGift.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvGift.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvGift.ScrollBars = ScrollBars.Both;

            // set datasource (event may have fired already in some cases)
            dgvGift.DataSource = dtGift;

            foreach (DataGridViewColumn c in dgvGift.Columns)
            { c.SortMode = DataGridViewColumnSortMode.NotSortable; c.Resizable = DataGridViewTriState.False; }

            LockServantFixedCols(dgvGift);
            AttachDeleteContext(dgvGift);

            // force once (in case event didn't fire)
            DgvServant_AfterBind(dgvGift, new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
        }

        private void BindSpiritGrid()
        {
            if (dgvSpirit == null) return;
            dgvSpirit.AutoGenerateColumns = true;

            // hook BEFORE datasource
            dgvSpirit.DataBindingComplete -= DgvServant_AfterBind;
            dgvSpirit.DataBindingComplete += DgvServant_AfterBind;

            dgvSpirit.AllowUserToAddRows = false;
            dgvSpirit.AllowUserToResizeColumns = false;
            dgvSpirit.AllowUserToResizeRows = false;
            dgvSpirit.RowHeadersVisible = false;
            dgvSpirit.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvSpirit.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvSpirit.ScrollBars = ScrollBars.Both;

            dgvSpirit.DataSource = dtSpirit;

            foreach (DataGridViewColumn c in dgvSpirit.Columns)
            { c.SortMode = DataGridViewColumnSortMode.NotSortable; c.Resizable = DataGridViewTriState.False; }

            LockServantFixedCols(dgvSpirit);
            AttachDeleteContext(dgvSpirit);

            // ✅ penting utk Spirit
            DgvServant_AfterBind(dgvSpirit, new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
        }


        // letak dlm class
        private void EnsureHorizontalScroll(DataGridView gv)
        {
            if (gv == null) return;

            gv.ScrollBars = ScrollBars.Both;
            gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            foreach (DataGridViewColumn col in gv.Columns)
            {
                if (col.AutoSizeMode == DataGridViewAutoSizeColumnMode.Fill)
                    col.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                col.Resizable = DataGridViewTriState.False;
                col.SortMode = DataGridViewColumnSortMode.NotSortable;
            }

            // paksa total width > client width supaya bar muncul
            int total = 0;
            foreach (DataGridViewColumn col in gv.Columns) total += col.Width;

            int need = gv.ClientSize.Width + 40; // buffer
            if (total <= need && gv.Columns.Count > 0)
            {
                var last = gv.Columns[gv.Columns.Count - 1];
                last.Width += (need - total) + 40;
            }
        }

        // call from DgvServant_AfterBind(gv, ...)
        private void ConfigureServantWidths(DataGridView gv)
        {
            // All columns must be fixed width (no Fill)
            gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;

            // === EDIT WIDTHS HERE ===
            var W = new Dictionary<string, int>
            {
                // locked cols
                ["needtype"] = 80,
                ["needlevel"] = 100,

                // main fields
                ["needmoney"] = 120,
                ["needtime"] = 100,
                ["max"] = 80,
                ["proficiency"] = 120,

                // item id + name
                ["item"] = 110,
                ["ItemName"] = 150,

                // needitem ids + names + counts
                ["needitem1"] = 110,
                ["NeedItem1Name"] = 150,
                ["needitemcount1"] = 90,
                ["needitem2"] = 110,
                ["NeedItem2Name"] = 150,
                ["needitemcount2"] = 90,
                ["needitem3"] = 110,
                ["NeedItem3Name"] = 150,
                ["needitemcount3"] = 90,
                ["needitem4"] = 110,
                ["NeedItem4Name"] = 150,
                ["needitemcount4"] = 90,
                ["needitem5"] = 110,
                ["NeedItem5Name"] = 150,
                ["needitemcount5"] = 90,
            };

            // apply
            foreach (var kv in W)
            {
                if (!gv.Columns.Contains(kv.Key)) continue;
                var c = gv.Columns[kv.Key];
                c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; // important
                c.Width = kv.Value;
            }
        }

        private void DgvServant_AfterBind(object? s, DataGridViewBindingCompleteEventArgs e)
        {
            var gv = s as DataGridView; if (gv == null) return;

            LockServantFixedCols(gv);     // keep read-only/grey
            ConfigureServantWidths(gv);   // <-- ubah saiz kat sini
            EnsureHorizontalScroll(gv);   // kekalkan H-scroll
        }




        // lock id & needtype
        private void LockServantFixedCols(DataGridView gv)
        {
            void Lock(string col)
            {
                if (!gv.Columns.Contains(col)) return;
                var c = gv.Columns[col];
                c.ReadOnly = true;
                c.DefaultCellStyle.BackColor = Color.Gainsboro;
                c.DefaultCellStyle.SelectionBackColor = Color.Gainsboro;
                c.DefaultCellStyle.SelectionForeColor = Color.Black;
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.Resizable = DataGridViewTriState.False;
            }

            Lock("needtype");
            Lock("needlevel"); // <-- tambah ini

            string[] ids = { "item", "needitem1", "needitem2", "needitem3", "needitem4", "needitem5" };
            string[] names = { "ItemName", "NeedItem1Name", "NeedItem2Name", "NeedItem3Name", "NeedItem4Name", "NeedItem5Name" };
            foreach (var col in ids) Lock(col);
            foreach (var col in names) Lock(col);
        }




        private void SaveServantComposeIniSplit()
        {
            string iniPath = Path.Combine(txtClientPath.Text.Trim(), "ini", "composeconfig.ini");
            var sb = new StringBuilder();

            void write(DataTable dt)
            {
                var dv = new DataView(dt) { Sort = "needlevel ASC, proficiency ASC" }; // keep same order on save
                foreach (DataRow r in dv.ToTable().Rows)
                {
                    int[] arr = new int[]
                    {
            ToInt(r["needtype"]),
            ToInt(r["needlevel"]),
            ToInt(r["needmoney"]),
            ToInt(r["needtime"]),
            ToInt(r["item"]),
            ToInt(r["max"]),
            ToInt(r["proficiency"]),
            ToInt(r["needitem1"]),
            ToInt(r["needitemcount1"]),
            ToInt(r["needitem2"]),
            ToInt(r["needitemcount2"]),
            ToInt(r["needitem3"]),
            ToInt(r["needitemcount3"]),
            ToInt(r["needitem4"]),
            ToInt(r["needitemcount4"]),
            ToInt(r["needitem5"]),
            ToInt(r["needitemcount5"])
                    };
                    sb.AppendLine(string.Join(" ", arr));
                }
            }


            write(dtGift);
            write(dtSpirit);

            try { if (File.Exists(iniPath)) File.Copy(iniPath, iniPath + ".bak", true); } catch { }
            File.WriteAllText(iniPath, sb.ToString(), Gbk);
        }

        private void UpsertServantToDbSplit(MySqlConnection c, MySqlTransaction tx)
        {
            string sql = @"
INSERT INTO cq_goddesscomposeconfig
(needtype, needlevel, needmoney, needtime, item, `max`, proficiency,
 needitem1, needitemcount1, needitem2, needitemcount2, needitem3, needitemcount3,
 needitem4, needitemcount4, needitem5, needitemcount5)
VALUES
(@needtype, @needlevel, @needmoney, @needtime, @item, @max, @prof,
 @ni1, @nc1, @ni2, @nc2, @ni3, @nc3, @ni4, @nc4, @ni5, @nc5)
ON DUPLICATE KEY UPDATE
 needlevel=VALUES(needlevel),
 needmoney=VALUES(needmoney),
 needtime=VALUES(needtime),
 `max`=VALUES(`max`),
 proficiency=VALUES(proficiency),
 needitem1=VALUES(needitem1),
 needitemcount1=VALUES(needitemcount1),
 needitem2=VALUES(needitem2),
 needitemcount2=VALUES(needitemcount2),
 needitem3=VALUES(needitem3),
 needitemcount3=VALUES(needitemcount3),
 needitem4=VALUES(needitem4),
 needitemcount4=VALUES(needitemcount4),
 needitem5=VALUES(needitem5),
 needitemcount5=VALUES(needitemcount5);";

            using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, c, tx);
            var P = new Func<string, MySql.Data.MySqlClient.MySqlParameter>(name =>
                cmd.Parameters.Add(name, MySql.Data.MySqlClient.MySqlDbType.Int32));
            var needtypeP = P("@needtype"); var needlevelP = P("@needlevel"); var needmoneyP = P("@needmoney");
            var needtimeP = P("@needtime"); var itemP = P("@item"); var maxP = P("@max"); var profP = P("@prof");
            var ni1 = P("@ni1"); var nc1 = P("@nc1"); var ni2 = P("@ni2"); var nc2 = P("@nc2");
            var ni3 = P("@ni3"); var nc3 = P("@nc3"); var ni4 = P("@ni4"); var nc4 = P("@nc4");
            var ni5 = P("@ni5"); var nc5 = P("@nc5");

            void upsert(DataTable dt, int needtype)
            {
                foreach (DataRow r in dt.Rows)
                {
                    needtypeP.Value = needtype;
                    needlevelP.Value = ToInt(r["needlevel"]);
                    needmoneyP.Value = ToInt(r["needmoney"]);
                    needtimeP.Value = ToInt(r["needtime"]);
                    itemP.Value = ToInt(r["item"]);
                    maxP.Value = ToInt(r["max"]);
                    profP.Value = ToInt(r["proficiency"]);
                    ni1.Value = ToInt(r["needitem1"]); nc1.Value = ToInt(r["needitemcount1"]);
                    ni2.Value = ToInt(r["needitem2"]); nc2.Value = ToInt(r["needitemcount2"]);
                    ni3.Value = ToInt(r["needitem3"]); nc3.Value = ToInt(r["needitemcount3"]);
                    ni4.Value = ToInt(r["needitem4"]); nc4.Value = ToInt(r["needitemcount4"]);
                    ni5.Value = ToInt(r["needitem5"]); nc5.Value = ToInt(r["needitemcount5"]);
                    cmd.ExecuteNonQuery();
                }
            }

            upsert(dtGift, 1);
            upsert(dtSpirit, 2);

            // refresh 'id' in both grids
            TryFillServantIdsFromDbSplit();
        }


        // Right-click context menu: Delete selected rows for any DataGridView
        // letak dlm class ShopEditor
        private void AttachDeleteContext(DataGridView gv)
        {
            if (gv == null) return;
            if (gv.Tag as string == "ctx-attached") return;   // elak attach dua kali

            var cms = new ContextMenuStrip();
            var miDel = new ToolStripMenuItem("Delete selected");
            miDel.Click += (s, e) => DeleteSelectedRows(gv);
            cms.Items.Add(miDel);
            gv.ContextMenuStrip = cms;

            // right-click: pilih row di posisi cursor
            gv.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                var hit = gv.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    gv.ClearSelection();
                    gv.CurrentCell = gv.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
                    gv.Rows[hit.RowIndex].Selected = true;
                }
            };

            gv.Tag = "ctx-attached";
        }

        private void DeleteSelectedRows(DataGridView gv)
        {
            if (gv?.DataSource == null) return;
            gv.EndEdit();

            var toRemove = new List<DataGridViewRow>();
            foreach (DataGridViewRow r in gv.SelectedRows)
                if (!r.IsNewRow) toRemove.Add(r);

            foreach (var r in toRemove)
            {
                if (r.DataBoundItem is DataRowView drv) drv.Row.Delete();
                else gv.Rows.Remove(r);
            }
        }


        // Get item name by ID from itemtype cache (safe)
        private string GetItemNameSafe(int id)
        {
            if (id <= 0) return "";
            try
            {
                if (_it?.ById != null && _it.ById.TryGetValue((uint)id, out var info))
                    return info.Name ?? "";
                if (_it?.GridTable != null)
                {
                    var rs = _it.GridTable.Select("ID = " + id);
                    if (rs.Length > 0) return rs[0]["Name"]?.ToString() ?? "";
                }
            }
            catch { }
            return "";
        }

        private void btnNewServerItem_Click(object sender, EventArgs e)
        {
            var frm = new NewServantItem
            {
                ItemSource = _it?.ById?.Select(kv => ((int)kv.Key, kv.Value.Name ?? "")).ToList() ?? new List<(int, string)>(),
                ResolveItemName = id => GetItemNameSafe(id) // reuse helper kau
            };
            if (frm.ShowDialog(this) == DialogResult.OK)
            {
                var x = frm.Result; // ServantComposeEntry
                if (x != null)
                {
                    // masukkan ke dtGift atau dtSpirit
                    var dt = (x.needtype == 1) ? dtGift : dtSpirit;
                    var r = dt.NewRow();
                    r["needtype"] = x.needtype;
                    r["needlevel"] = x.needlevel;
                    r["needmoney"] = x.needmoney;
                    r["needtime"] = x.needtime;
                    r["item"] = x.item;
                    r["ItemName"] = GetItemNameSafe(x.item);
                    r["max"] = x.max;
                    r["proficiency"] = x.proficiency;
                    r["needitem1"] = x.needitem1; r["NeedItem1Name"] = GetItemNameSafe(x.needitem1); r["needitemcount1"] = x.needitemcount1;
                    r["needitem2"] = x.needitem2; r["NeedItem2Name"] = GetItemNameSafe(x.needitem2); r["needitemcount2"] = x.needitemcount2;
                    r["needitem3"] = x.needitem3; r["NeedItem3Name"] = GetItemNameSafe(x.needitem3); r["needitemcount3"] = x.needitemcount3;
                    r["needitem4"] = x.needitem4; r["NeedItem4Name"] = GetItemNameSafe(x.needitem4); r["needitemcount4"] = x.needitemcount4;
                    r["needitem5"] = x.needitem5; r["NeedItem5Name"] = GetItemNameSafe(x.needitem5); r["needitemcount5"] = x.needitemcount5;
                    dt.Rows.Add(r);
                    ResortAndRebindServantGrids(x.needtype, x.item);

                }
            }
        }


        // sort helper
        private DataTable SortServantTable(DataTable dt)
        {
            var dv = new DataView(dt) { Sort = "needlevel ASC, proficiency ASC" };
            return dv.ToTable();
        }

        // pilih balik row yang baru dimasukkan
        private void SelectServantRow(DataGridView gv, string col, int itemId)
        {
            if (gv == null || !gv.Columns.Contains(col)) return;
            foreach (DataGridViewRow row in gv.Rows)
            {
                if (row.IsNewRow) continue;
                if (int.TryParse(Convert.ToString(row.Cells[col].Value), out int v) && v == itemId)
                {
                    gv.ClearSelection();
                    row.Selected = true;
                    gv.CurrentCell = row.Cells[0];
                    gv.FirstDisplayedScrollingRowIndex = row.Index;
                    break;
                }
            }
        }

        // resort & rebind kedua-dua grid
        private void ResortAndRebindServantGrids(int? selectNeedtype = null, int? selectItem = null)
        {
            // sort data
            dtGift = SortServantTable(dtGift);
            dtSpirit = SortServantTable(dtSpirit);

            // rebind Gift
            if (dgvGift != null)
            {
                dgvGift.DataBindingComplete -= DgvServant_AfterBind;
                dgvGift.DataSource = null;
                dgvGift.DataSource = dtGift;
                dgvGift.DataBindingComplete += DgvServant_AfterBind;
                DgvServant_AfterBind(dgvGift, new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
                if (selectNeedtype == 1 && selectItem.HasValue) SelectServantRow(dgvGift, "item", selectItem.Value);
            }

            // rebind Spirit
            if (dgvSpirit != null)
            {
                dgvSpirit.DataBindingComplete -= DgvServant_AfterBind;
                dgvSpirit.DataSource = null;
                dgvSpirit.DataSource = dtSpirit;
                dgvSpirit.DataBindingComplete += DgvServant_AfterBind;
                DgvServant_AfterBind(dgvSpirit, new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
                if (selectNeedtype == 2 && selectItem.HasValue) SelectServantRow(dgvSpirit, "item", selectItem.Value);
            }
        }



        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////            
        //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        /////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////




        // ====== CREATE TABLE SCHEMA ======
        private DataTable CreateEventShopTable()
        {
            var dt = new DataTable();
            dt.Columns.Add("shop_type", typeof(int));     // 1=Astra, 2=Honor, 4=Plane
            dt.Columns.Add("id", typeof(int));            // <-- 2nd token in INI (explicit PK)
            dt.Columns.Add("Itemtype", typeof(int));
            dt.Columns.Add("ItemName", typeof(string));   // read-only, from itemtype.fdb (optional)
            dt.Columns.Add("Priority", typeof(int));
            dt.Columns.Add("Level_type", typeof(int));
            dt.Columns.Add("Need_level", typeof(int));
            dt.Columns.Add("Monopoly", typeof(int));
            dt.Columns.Add("Talent_coin", typeof(int));
            dt.Columns.Add("Eudemon_currency", typeof(int));
            dt.Columns.Add("Item_currency", typeof(int));
            dt.Columns.Add("Amount_type", typeof(int));
            dt.Columns.Add("Amount_limit", typeof(int));
            dt.Columns.Add("Begin_time", typeof(int));
            dt.Columns.Add("End_time", typeof(int));
            dt.Columns.Add("New_time", typeof(int));
            dt.Columns.Add("Version", typeof(int));
            return dt;
        }

        private void LoadEventShopIniSplit()
        {
            string iniPath = Path.Combine(txtClientPath.Text.Trim(), "ini", "activitynewshop.ini");
            dtAstraES = CreateEventShopTable();
            dtHonorES = CreateEventShopTable();
            dtPlaneES = CreateEventShopTable();

            try { EnsureItemNamesLoaded(); } catch { }

            if (File.Exists(iniPath))
            {
                var lines = File.ReadAllLines(iniPath, Gbk);
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";") || line.StartsWith("#")) continue;

                    var p = line.Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
                    if (p.Length < 16) continue; // require 16 tokens

                    int I(int idx) => int.TryParse(p[idx], out var v) ? v : 0;

                    int shopType = I(0);
                    int id = I(1);
                    int item = I(2);

                    var r = (shopType == 1 ? dtAstraES :
                             shopType == 2 ? dtHonorES :
                             shopType == 4 ? dtPlaneES : null)?.NewRow();
                    if (r == null) continue;

                    r["shop_type"] = shopType;
                    r["id"] = id;
                    r["Itemtype"] = item;
                    r["ItemName"] = GetItemNameSafe(item); // optional name
                    r["Priority"] = I(3);
                    r["Level_type"] = I(4);
                    r["Need_level"] = I(5);
                    r["Monopoly"] = I(6);
                    r["Talent_coin"] = I(7);
                    r["Eudemon_currency"] = I(8);
                    r["Item_currency"] = I(9);
                    r["Amount_type"] = I(10);
                    r["Amount_limit"] = I(11);
                    r["Begin_time"] = I(12);
                    r["End_time"] = I(13);
                    r["New_time"] = I(14);
                    r["Version"] = I(15);

                    if (shopType == 1) dtAstraES.Rows.Add(r);
                    else if (shopType == 2) dtHonorES.Rows.Add(r);
                    else if (shopType == 4) dtPlaneES.Rows.Add(r);
                }
            }

            BindAstraShopGrid();
            BindHonorShopGrid();
            BindPlaneShopGrid();
        }

        // ====== BIND GRIDS ======
        private void BindAstraShopGrid()
        {
            if (dgvAstraShop == null) return;
            dgvAstraShop.AutoGenerateColumns = true;
            dgvAstraShop.DataBindingComplete -= DgvEventShop_AfterBind;
            dgvAstraShop.DataSource = dtAstraES;
            dgvAstraShop.DataBindingComplete += DgvEventShop_AfterBind;

            dgvAstraShop.AllowUserToAddRows = false;
            dgvAstraShop.AllowUserToResizeColumns = false;
            dgvAstraShop.AllowUserToResizeRows = false;
            dgvAstraShop.RowHeadersVisible = false;
            dgvAstraShop.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvAstraShop.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvAstraShop.ScrollBars = ScrollBars.Both;

            foreach (DataGridViewColumn c in dgvAstraShop.Columns)
            { c.SortMode = DataGridViewColumnSortMode.NotSortable; c.Resizable = DataGridViewTriState.False; }

            LockEventShopCols(dgvAstraShop);
            AttachDeleteContext(dgvAstraShop);
            AttachEventShopMoves(dgvAstraShop);
            EnsureDeleteAtBottom(dgvAstraShop);


            AttachRowDragDrop(dgvAstraShop);   // ⬅️ enable drag & drop

            DgvEventShop_AfterBind(dgvAstraShop, new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
        }

        private void BindHonorShopGrid()
        {
            if (dgvHonorShop == null) return;
            dgvHonorShop.AutoGenerateColumns = true;
            dgvHonorShop.DataBindingComplete -= DgvEventShop_AfterBind;
            dgvHonorShop.DataSource = dtHonorES;
            dgvHonorShop.DataBindingComplete += DgvEventShop_AfterBind;

            dgvHonorShop.AllowUserToAddRows = false;
            dgvHonorShop.AllowUserToResizeColumns = false;
            dgvHonorShop.AllowUserToResizeRows = false;
            dgvHonorShop.RowHeadersVisible = false;
            dgvHonorShop.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvHonorShop.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvHonorShop.ScrollBars = ScrollBars.Both;

            foreach (DataGridViewColumn c in dgvHonorShop.Columns)
            { c.SortMode = DataGridViewColumnSortMode.NotSortable; c.Resizable = DataGridViewTriState.False; }

            LockEventShopCols(dgvHonorShop);
            AttachDeleteContext(dgvHonorShop);
            AttachEventShopMoves(dgvHonorShop);
            EnsureDeleteAtBottom(dgvHonorShop);

            AttachRowDragDrop(dgvHonorShop);   // ⬅️ enable drag & drop

            DgvEventShop_AfterBind(dgvHonorShop, new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
        }

        private void BindPlaneShopGrid()
        {
            if (dgvPlaneShop == null) return;
            dgvPlaneShop.AutoGenerateColumns = true;
            dgvPlaneShop.DataBindingComplete -= DgvEventShop_AfterBind;
            dgvPlaneShop.DataSource = dtPlaneES;
            dgvPlaneShop.DataBindingComplete += DgvEventShop_AfterBind;

            dgvPlaneShop.AllowUserToAddRows = false;
            dgvPlaneShop.AllowUserToResizeColumns = false;
            dgvPlaneShop.AllowUserToResizeRows = false;
            dgvPlaneShop.RowHeadersVisible = false;
            dgvPlaneShop.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvPlaneShop.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            dgvPlaneShop.ScrollBars = ScrollBars.Both;

            foreach (DataGridViewColumn c in dgvPlaneShop.Columns)
            { c.SortMode = DataGridViewColumnSortMode.NotSortable; c.Resizable = DataGridViewTriState.False; }

            LockEventShopCols(dgvPlaneShop);
            AttachDeleteContext(dgvPlaneShop);
            AttachEventShopMoves(dgvPlaneShop);
            EnsureDeleteAtBottom(dgvPlaneShop);

            AttachRowDragDrop(dgvPlaneShop);   // ⬅️ enable drag & drop

            DgvEventShop_AfterBind(dgvPlaneShop, new DataGridViewBindingCompleteEventArgs(ListChangedType.Reset));
        }

        // ====== LOCK + WIDTHS + H-SCROLL ======
        private void DgvEventShop_AfterBind(object? s, DataGridViewBindingCompleteEventArgs e)
        {
            var gv = s as DataGridView; if (gv == null) return;

            HideEventShopCols(gv); // <-- hide here

            // widths (edit if needed, 'shop_type' & 'id' are hidden anyway)
            var W = new Dictionary<string, int>
            {
                ["Itemtype"] = 110,
                ["ItemName"] = 220,
                ["Priority"] = 80,
                ["Level_type"] = 90,
                ["Need_level"] = 100,
                ["Monopoly"] = 90,
                ["Talent_coin"] = 110,
                ["Eudemon_currency"] = 130,
                ["Item_currency"] = 110,
                ["Amount_type"] = 110,
                ["Amount_limit"] = 110,
                ["Begin_time"] = 110,
                ["End_time"] = 110,
                ["New_time"] = 100,
                ["Version"] = 100
            };
            foreach (var kv in W)
                if (gv.Columns.Contains(kv.Key)) { var c = gv.Columns[kv.Key]; c.AutoSizeMode = DataGridViewAutoSizeColumnMode.None; c.Width = kv.Value; }

            EnsureHorizontalScroll(gv);
        }


        private void SaveEventShopIni()
        {
            ReindexEventShopIds(); // <-- ensure contiguous IDs

            string iniPath = Path.Combine(txtClientPath.Text.Trim(), "ini", "activitynewshop.ini");
            var all = CreateEventShopTable();
            void addAll(DataTable src) { foreach (DataRow r in src.Rows) all.Rows.Add(r.ItemArray.Clone() as object[]); }
            addAll(dtAstraES); addAll(dtHonorES); addAll(dtPlaneES);

            var dv = new DataView(all) { Sort = "shop_type ASC, id ASC" };
            var sb = new StringBuilder();
            foreach (DataRow r in dv.ToTable().Rows)
            {
                int shop_type = ToInt(r["shop_type"]);
                int id = ToInt(r["id"]);
                int item = ToInt(r["Itemtype"]);
                int[] arr =
                {
            ToInt(r["Priority"]), ToInt(r["Level_type"]), ToInt(r["Need_level"]),
            ToInt(r["Monopoly"]), ToInt(r["Talent_coin"]), ToInt(r["Eudemon_currency"]),
            ToInt(r["Item_currency"]), ToInt(r["Amount_type"]), ToInt(r["Amount_limit"]),
            ToInt(r["Begin_time"]), ToInt(r["End_time"]), ToInt(r["New_time"]), ToInt(r["Version"])
        };
                sb.Append(shop_type).Append(' ').Append(id).Append(' ').Append(item);
                foreach (var v in arr) sb.Append(' ').Append(v);
                sb.AppendLine();
            }

            try { if (File.Exists(iniPath)) File.Copy(iniPath, iniPath + ".bak", true); } catch { }
            File.WriteAllText(iniPath, sb.ToString(), Gbk);
        }

        // ====== UPSERT TO DB ======
        private void UpsertEventShopToDb(MySql.Data.MySqlClient.MySqlConnection c, MySql.Data.MySqlClient.MySqlTransaction tx)
        {
            ReindexEventShopIds(); // <-- keep DB IDs in sync with INI

            string sql = @"
INSERT INTO cq_activitynewshop
(id, shop_type, Itemtype, Priority, Level_type, Need_level, Monopoly,
 Talent_coin, Eudemon_currency, Item_currency, Amount_type, Amount_limit,
 Begin_time, End_time, New_time, Version)
VALUES
(@id,@shop_type,@Itemtype,@Priority,@Level_type,@Need_level,@Monopoly,
 @Talent_coin,@Eudemon_currency,@Item_currency,@Amount_type,@Amount_limit,
 @Begin_time,@End_time,@New_time,@Version)
ON DUPLICATE KEY UPDATE
 shop_type=VALUES(shop_type),
 Itemtype=VALUES(Itemtype),
 Priority=VALUES(Priority),
 Level_type=VALUES(Level_type),
 Need_level=VALUES(Need_level),
 Monopoly=VALUES(Monopoly),
 Talent_coin=VALUES(Talent_coin),
 Eudemon_currency=VALUES(Eudemon_currency),
 Item_currency=VALUES(Item_currency),
 Amount_type=VALUES(Amount_type),
 Amount_limit=VALUES(Amount_limit),
 Begin_time=VALUES(Begin_time),
 End_time=VALUES(End_time),
 New_time=VALUES(New_time),
 Version=VALUES(Version);";

            using var cmd = new MySql.Data.MySqlClient.MySqlCommand(sql, c, tx);
            var p = new Func<string, MySql.Data.MySqlClient.MySqlParameter>(name =>
                cmd.Parameters.Add(name, MySql.Data.MySqlClient.MySqlDbType.Int32));

            var pid = p("@id"); var pshop = p("@shop_type"); var pitem = p("@Itemtype");
            var ppri = p("@Priority"); var plt = p("@Level_type"); var pnl = p("@Need_level");
            var pmono = p("@Monopoly"); var ptc = p("@Talent_coin"); var peu = p("@Eudemon_currency");
            var pic = p("@Item_currency"); var pat = p("@Amount_type"); var pal = p("@Amount_limit");
            var pbt = p("@Begin_time"); var pet = p("@End_time"); var pnt = p("@New_time"); var pver = p("@Version");

            void push(DataTable dt, int shopType)
            {
                foreach (DataRow r in dt.Rows)
                {
                    pid.Value = ToInt(r["id"]);
                    pshop.Value = shopType;
                    pitem.Value = ToInt(r["Itemtype"]);
                    ppri.Value = ToInt(r["Priority"]);
                    plt.Value = ToInt(r["Level_type"]);
                    pnl.Value = ToInt(r["Need_level"]);
                    pmono.Value = ToInt(r["Monopoly"]);
                    ptc.Value = ToInt(r["Talent_coin"]);
                    peu.Value = ToInt(r["Eudemon_currency"]);
                    pic.Value = ToInt(r["Item_currency"]);
                    pat.Value = ToInt(r["Amount_type"]);
                    pal.Value = ToInt(r["Amount_limit"]);
                    pbt.Value = ToInt(r["Begin_time"]);
                    pet.Value = ToInt(r["End_time"]);
                    pnt.Value = ToInt(r["New_time"]);
                    pver.Value = ToInt(r["Version"]);
                    cmd.ExecuteNonQuery();
                }
            }

            push(dtAstraES, 1);
            push(dtHonorES, 2);
            push(dtPlaneES, 4);
        }

        // Call this before saving INI/DB so IDs are contiguous across all shops
        private void ReindexEventShopIds()
        {
            int next = 1;

            void reindex(DataTable dt, int shopType)
            {
                if (dt == null) return;
                foreach (DataRow r in dt.Rows)
                {
                    r["shop_type"] = shopType; // keep correct type
                    r["id"] = next++;          // contiguous id
                }
            }

            reindex(dtAstraES, 1);
            reindex(dtHonorES, 2);
            reindex(dtPlaneES, 4);
        }

        private void HideEventShopCols(DataGridView gv)
        {
            void Hide(string name)
            {
                if (gv.Columns.Contains(name))
                    gv.Columns[name].Visible = false;
            }
            Hide("shop_type");
            Hide("id");
        }

        private void LockEventShopCols(DataGridView gv)
        {
            void Lock(string col, bool grey = true)
            {
                if (!gv.Columns.Contains(col)) return;
                var c = gv.Columns[col];
                c.ReadOnly = true;
                if (grey)
                {
                    c.DefaultCellStyle.BackColor = Color.Gainsboro;
                    c.DefaultCellStyle.SelectionBackColor = Color.Gainsboro;
                    c.DefaultCellStyle.SelectionForeColor = Color.Black;
                }
                c.SortMode = DataGridViewColumnSortMode.NotSortable;
                c.Resizable = DataGridViewTriState.False;
            }
            Lock("ItemName");
            Lock("Itemtype");          // ⬅️ itemtype read-only
                                       // kalau nak kekalkan hide:
                                       // Lock("shop_type"); Lock("id");
        }

        // --- drag & drop row reordering for EventShop grids ---
        private int _esDragIndex = -1;

        private void AttachRowDragDrop(DataGridView gv)
        {
            if (gv == null) return;
            if (gv.Tag as string == "drag-attach") return; // avoid double hook

            gv.AllowDrop = true;

            gv.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Left) return;
                var hit = gv.HitTest(e.X, e.Y);
                _esDragIndex = hit.RowIndex;
            };

            gv.MouseMove += (s, e) =>
            {
                if ((e.Button & MouseButtons.Left) == 0) return;
                if (_esDragIndex < 0 || _esDragIndex >= gv.Rows.Count) return;
                if (gv.Rows[_esDragIndex].IsNewRow) return;
                gv.DoDragDrop(gv.Rows[_esDragIndex], DragDropEffects.Move);
            };

            gv.DragOver += (s, e) =>
            {
                e.Effect = e.Data.GetDataPresent(typeof(DataGridViewRow))
                    ? DragDropEffects.Move : DragDropEffects.None;
            };

            gv.DragDrop += (s, e) =>
            {
                try
                {
                    var pt = gv.PointToClient(new Point(e.X, e.Y));
                    var hit = gv.HitTest(pt.X, pt.Y);
                    int toIndex = hit.RowIndex;
                    if (toIndex < 0) toIndex = gv.Rows.Count - 1;
                    if (_esDragIndex >= 0 && toIndex >= 0 && toIndex != _esDragIndex)
                        MoveEventShopRow(gv, _esDragIndex, toIndex);
                }
                finally { _esDragIndex = -1; }
            };

            gv.MouseUp += (s, e) => { _esDragIndex = -1; };

            gv.Tag = "drag-attach";
        }

        // kekalkan scroll position bila susun semula
        private void MoveEventShopRow(DataGridView gv, int from, int to)
        {
            if (gv?.DataSource is not DataTable dt) return;
            if (from < 0 || from >= dt.Rows.Count || to < 0 || to >= dt.Rows.Count || from == to) return;

            // snapshot scroll pos
            int oldFirstRow = -1, oldFirstCol = -1;
            try { oldFirstRow = gv.FirstDisplayedScrollingRowIndex; } catch { }
            try { oldFirstCol = gv.FirstDisplayedScrollingColumnIndex; } catch { }

            gv.SuspendLayout();
            try
            {
                // clone & move
                var data = (object[])dt.Rows[from].ItemArray.Clone();
                var newRow = dt.NewRow(); newRow.ItemArray = data;
                dt.Rows.RemoveAt(from);
                dt.Rows.InsertAt(newRow, to);

                // highlight row baru TANPA set CurrentCell (elak auto-scroll)
                foreach (DataGridViewRow rr in gv.Rows) rr.Selected = false;
                int vis = Math.Max(0, Math.Min(to, gv.Rows.Count - 1));
                if (gv.Rows.Count > 0) gv.Rows[vis].Selected = true;

                // restore scroll pos
                if (oldFirstRow >= 0 && oldFirstRow < gv.Rows.Count)
                    gv.FirstDisplayedScrollingRowIndex = oldFirstRow;
                if (oldFirstCol >= 0 && oldFirstCol < gv.Columns.Count)
                    gv.FirstDisplayedScrollingColumnIndex = oldFirstCol;
            }
            finally
            {
                gv.ResumeLayout();
            }
        }


        // ===== EventShop: Right-click "Move to top/bottom" =====
        private void AttachEventShopMoves(DataGridView gv)
        {
            if (gv == null) return;
            // elak duplicate
            if ((gv.Tag as string)?.Contains("ctx-es-move") == true) return;

            // pastikan ada context menu (merge dengan yang sedia ada)
            var cms = gv.ContextMenuStrip ?? new ContextMenuStrip();

            // separator kecil
            cms.Items.Add(new ToolStripSeparator());

            var miTop = new ToolStripMenuItem("Move to top");
            miTop.Click += (s, e) =>
            {
                if (gv.CurrentRow == null || gv.CurrentRow.IsNewRow) return;
                int from = gv.CurrentRow.Index;
                MoveEventShopRow(gv, from, 0);
            };
            cms.Items.Add(miTop);

            var miBottom = new ToolStripMenuItem("Move to bottom");
            miBottom.Click += (s, e) =>
            {
                if (gv.CurrentRow == null || gv.CurrentRow.IsNewRow) return;
                int from = gv.CurrentRow.Index;
                int to = Math.Max(0, gv.Rows.Count - 1);
                MoveEventShopRow(gv, from, to);
            };
            cms.Items.Add(miBottom);

            // right-click pilih row di cursor (kalau belum ada daripada AttachDeleteContext)
            gv.MouseDown += (s, e) =>
            {
                if (e.Button != MouseButtons.Right) return;
                var hit = gv.HitTest(e.X, e.Y);
                if (hit.RowIndex >= 0)
                {
                    gv.ClearSelection();
                    gv.CurrentCell = gv.Rows[hit.RowIndex].Cells[Math.Max(0, hit.ColumnIndex)];
                    gv.Rows[hit.RowIndex].Selected = true;
                }
            };

            gv.ContextMenuStrip = cms;
            gv.Tag = ((gv.Tag as string) ?? "") + "|ctx-es-move";
        }

        // === Pastikan "Delete selected" duduk di bahagian bawah menu ===
        private void EnsureDeleteAtBottom(DataGridView gv)
        {
            if (gv?.ContextMenuStrip == null) return;
            var cms = gv.ContextMenuStrip;

            ToolStripItem del = null;
            foreach (ToolStripItem it in cms.Items)
                if (it is ToolStripMenuItem mi && mi.Text == "Delete selected") { del = it; break; }

            if (del != null)
            {
                cms.Items.Remove(del);
                // tambah separator kalau item terakhir bukan separator
                if (cms.Items.Count > 0 && !(cms.Items[cms.Items.Count - 1] is ToolStripSeparator))
                    cms.Items.Add(new ToolStripSeparator());
                cms.Items.Add(del); // letak paling bawah
            }
        }

        // add new EventShop item via dialog
        // pakai helper nama item yang kau dah ada
        // private string GetItemNameSafe(int id) { ... }

        // ===== helper: select row by value tanpa ubah scroll =====
        private void SelectEventShopRow(DataGridView gv, string col, int value)
        {
            if (gv == null || !gv.Columns.Contains(col)) return;

            int oldFirstRow = -1, oldFirstCol = -1;
            try { oldFirstRow = gv.FirstDisplayedScrollingRowIndex; } catch { }
            try { oldFirstCol = gv.FirstDisplayedScrollingColumnIndex; } catch { }

            foreach (DataGridViewRow row in gv.Rows)
            {
                if (row.IsNewRow) continue;
                if (int.TryParse(Convert.ToString(row.Cells[col].Value), out int v) && v == value)
                {
                    gv.ClearSelection();
                    row.Selected = true;
                    // JANGAN set CurrentCell kalau tak nak auto-scroll; cukup highlight
                    break;
                }
            }

            if (oldFirstRow >= 0 && oldFirstRow < gv.Rows.Count)
                gv.FirstDisplayedScrollingRowIndex = oldFirstRow;
            if (oldFirstCol >= 0 && oldFirstCol < gv.Columns.Count)
                gv.FirstDisplayedScrollingColumnIndex = oldFirstCol;
        }

        // ===== click handler =====
        private void btnNewEventItem_Click(object sender, EventArgs e)
        {
            var frm = new NewShopEditor.NewEventItem
            {
                ItemSource = _it?.ById?.Select(kv => ((int)kv.Key, kv.Value.Name ?? "")).ToList()
                             ?? new List<(int, string)>(),
                ResolveItemName = id => GetItemNameSafe(id)
            };
            if (frm.ShowDialog(this) != DialogResult.OK) return;

            int shopType = frm.SelectedShopType;
            int itemId = frm.SelectedItemId;
            if (itemId <= 0) return;

            DataTable dt = shopType == 1 ? dtAstraES : shopType == 2 ? dtHonorES : dtPlaneES;
            DataGridView gv = shopType == 1 ? dgvAstraShop : shopType == 2 ? dgvHonorShop : dgvPlaneShop;

            if (dt.Select("Itemtype = " + itemId).Length > 0)
            { SelectEventShopRow(gv, "Itemtype", itemId); return; }

            var r = dt.NewRow();
            r["shop_type"] = shopType;
            r["id"] = 0;
            r["Itemtype"] = itemId;
            r["ItemName"] = GetItemNameSafe(itemId);
            r["Priority"] = 0;
            r["Level_type"] = 0;
            r["Need_level"] = 0;
            r["Monopoly"] = 1;
            r["Talent_coin"] = frm.EnteredTalentCoin;   // <-- dari txtPrice
            r["Eudemon_currency"] = 0;
            r["Item_currency"] = 0;
            r["Amount_type"] = 0;
            r["Amount_limit"] = 0;
            r["Begin_time"] = 0;
            r["End_time"] = 0;
            r["New_time"] = 0;
            r["Version"] = EventShopDefaultVersion; // <-- 49213
            dt.Rows.Add(r);

            SelectEventShopRow(gv, "Itemtype", itemId);
        }




    }


}
