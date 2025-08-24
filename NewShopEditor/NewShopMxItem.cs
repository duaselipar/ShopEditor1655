using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace NewShopEditor
{
    public partial class NewShopMxItem : Form
    {
        private readonly ItemtypeCache _cache;
        private readonly string _clientPath;

        private DataTable _mxTable;
        private bool _suppressToggle = false;

        // map newshop: ItemType(uID) -> ItemID (X dalam [ItemX])
        private readonly Dictionary<uint, string> _typeToItemId = new();
        private readonly List<string> _itemIdList = new(); // untuk kira max sedia ada

        // === GLOBAL (satu sesi) ===
        private static int s_SessionMaxItemId = 0;
        private static readonly Dictionary<uint, string> s_SessionTypeToItemId = new(); // uID -> ItemID

        public int EmoneyOut { get; private set; } = 0;

        // OUTPUTS
        public string ItemID { get; private set; } = "";     // X dari [ItemX]
        public string ItemType { get; private set; } = "";   // uID dari FDB (textbox disabled)
        public string CostType { get; private set; } = "1";
        public string Version { get; private set; } = "125";
        public string NewFlag { get; private set; } = "0";
        public string CommendFlag { get; private set; } = "0";
        public string OEM { get; private set; } = "0";
        public string Describe { get; private set; } = "null";
        public string ReturnEmoney { get; private set; } = "";
        public string BeginTime { get; private set; } = "1606230730";
        public string EndTime { get; private set; } = "1606300729";
        public string Tip { get; private set; } = "";

        private class TipItem { public string Text { get; set; } = ""; public int Value { get; set; } public override string ToString() => Text; }

        // 1) Add this field (top of class)
        // add fields (top of class)
        private int _lastFoundIndex = -1;
        private IButtonControl? _savedAcceptBtn = null; // keep original AcceptButton


        public NewShopMxItem(ItemtypeCache cache, string clientPath)
        {
            _cache = cache;
            _clientPath = clientPath;
            InitializeComponent();
            InitUi();


        }

        private void InitUi()
        {
            // --- lock fields ---
            txtItemType.Enabled = false;
            txtItemId.Enabled = false;
            txtName.ReadOnly = true;

            // build indices (existing)
            BuildNewShopIndexAndSessionMax();

            // in InitUi() — add handlers
            txtSearchMx.TextChanged += TxtSearchMx_TextChanged;
            txtSearchMx.KeyDown += TxtSearchMx_KeyDown;              // Enter => next
            txtSearchMx.PreviewKeyDown += TxtSearchMx_PreviewKeyDown; // treat Enter as input
            txtSearchMx.Enter += TxtSearchMx_Enter;                  // disable AcceptButton
            txtSearchMx.Leave += TxtSearchMx_Leave;                  // restore AcceptButton


            // --- defaults & combos (keep version, hide cost combo later) ---
            cboVersion.Items.AddRange(new object[] { 29, 32, 61, 63, 64, 95, 125, 126, 127 });
            cboVersion.SelectedItem = 125;

            // OEM default 0 + hide
            if (cboOEM.Items.Count == 0) cboOEM.Items.AddRange(new object[] { 0, 1, 7 });
            cboOEM.SelectedItem = 0;
            lblOEM.Visible = false;
            cboOEM.Visible = false;

            // Describe default "null" + hide
            txtDescribe.Text = "null";
            txtDescribe.ReadOnly = true;
            lblDescribe.Visible = false;
            txtDescribe.Visible = false;

            // ReturnEmoney auto (hidden)
            txtEmoney.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtEmoney.TextChanged += (s, e) =>
            {
                int ep = TryParseInt(txtEmoney.Text);
                txtReturnEmoney.Text = CalcReturnEmoneyText(ep);
            };
            lblReturnEmoney.Visible = false;
            txtReturnEmoney.Visible = false;

            // Begin/End default + hide
            txtBegin.Text = "1606230730";
            txtEnd.Text = "1606300729";
            lblBegin.Visible = false; txtBegin.Visible = false;
            lblEnd.Visible = false; txtEnd.Visible = false;

            // Hide ItemID (but still used internally)
            lblItemId.Visible = false;
            txtItemId.Visible = false;

            // --- CostType as RADIO (1 = EP, 3 = EP+PP), remove 19 option visually ---
            // keep label, hide combo (we'll read radios on OK)
            cboCostType.Visible = false;

            // create radios near former combo position
            var rbCostEP = new RadioButton
            {
                Name = "rbCostEP",
                Text = "EP",
                AutoSize = true,
                Left = cboCostType.Left,
                Top = cboCostType.Top + 2,
                Checked = true // default to 1
            };
            var rbCostEPPP = new RadioButton
            {
                Name = "rbCostEPPP",
                Text = "EP + PP",
                AutoSize = true,
                Left = rbCostEP.Left + rbCostEP.Width + 12,
                Top = rbCostEP.Top
            };
            this.Controls.Add(rbCostEP);
            this.Controls.Add(rbCostEPPP);

            // New/Commend mutual exclusive (both off allowed)
            chkNew.CheckedChanged += (s, e) =>
            {
                if (_suppressToggle) return;
                if (chkNew.Checked) { _suppressToggle = true; chkCommend.Checked = false; _suppressToggle = false; }
            };
            chkCommend.CheckedChanged += (s, e) =>
            {
                if (_suppressToggle) return;
                if (chkCommend.Checked) { _suppressToggle = true; chkNew.Checked = false; _suppressToggle = false; }
            };

            // buttons
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => this.DialogResult = DialogResult.Cancel;

            // left list events (keep existing)
            txtSearchMx.TextChanged += TxtSearchMx_TextChanged;
            dgvMx.SelectionChanged += DgvMx_SelectionChanged;
            dgvMx.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) btnOK.PerformClick(); };

            // Tip cascading (keep)
            cboTip1.SelectedIndexChanged += (s, e) =>
            {
                bool on = cboTip1.SelectedIndex >= 0;
                cboTip2.Enabled = on;
                if (!on) { cboTip2.SelectedIndex = -1; cboTip3.Enabled = false; cboTip3.SelectedIndex = -1; }
            };
            cboTip2.SelectedIndexChanged += (s, e) =>
            {
                bool on = cboTip2.SelectedIndex >= 0;
                cboTip3.Enabled = on;
                if (!on) { cboTip3.SelectedIndex = -1; }
            };

            // load data
            LoadTips();
            LoadMxGrid();
        }

        // baca newshop.ini/dat -> bina map uID→ItemID, dan set s_SessionMaxItemId
        private void BuildNewShopIndexAndSessionMax()
        {
            _typeToItemId.Clear();
            _itemIdList.Clear();

            var ini = Path.Combine(_clientPath, "ini", "newshop.ini");
            List<string> lines = new();

            if (File.Exists(ini)) lines.AddRange(File.ReadAllLines(ini, Encoding.UTF8));
            else
            {
                var dat = Path.Combine(_clientPath, "ini", "newshop.dat");
                if (File.Exists(dat)) lines = NewShopDatHandler.Read(dat);
            }
            if (lines.Count > 0)
            {
                string curId = "";
                foreach (var raw in lines)
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith(";")) continue;

                    var mSec = Regex.Match(line, @"^\[(?:Item)?(\d+)\]$", RegexOptions.IgnoreCase);
                    if (mSec.Success) { curId = mSec.Groups[1].Value; if (!_itemIdList.Contains(curId)) _itemIdList.Add(curId); continue; }
                    if (curId.Length == 0) continue;

                    var mKV = Regex.Match(line, @"^ItemType\s*=\s*(\d+)$", RegexOptions.IgnoreCase);
                    if (mKV.Success && uint.TryParse(mKV.Groups[1].Value, out uint uType))
                        if (!_typeToItemId.ContainsKey(uType)) _typeToItemId[uType] = curId;
                }
            }

            // sync max dari file
            int fileMax = 0;
            foreach (var s in _itemIdList) if (int.TryParse(s, out var n) && n > fileMax) fileMax = n;
            if (fileMax > s_SessionMaxItemId) s_SessionMaxItemId = fileMax;

            // === MERGE sesi → instance ===
            foreach (var kv in s_SessionTypeToItemId)
                if (!_typeToItemId.ContainsKey(kv.Key)) _typeToItemId[kv.Key] = kv.Value;

            foreach (var v in s_SessionTypeToItemId.Values)
                if (!_itemIdList.Contains(v)) _itemIdList.Add(v);
        }

        // use ItemtypeCache first so Emoney reflects latest edits from Shop/NewShopMx
        private void LoadMxGrid()
        {
            _mxTable = new DataTable();
            _mxTable.Columns.Add("ID", typeof(uint));
            _mxTable.Columns.Add("Name", typeof(string));
            _mxTable.Columns.Add("Emoney", typeof(int));

            bool filled = false;

            // 1) prefer cache (kept in sync when you edit/add in Shop/NewShopMx)
            if (_cache != null && _cache.GridTable != null && _cache.GridTable.Rows.Count > 0)
            {
                foreach (DataRow r in _cache.GridTable.Rows)
                {
                    uint id = ToUInt(r["ID"]);
                    if (id == 0) continue;
                    string nm = r["Name"]?.ToString() ?? "";
                    int ep = ToInt(r["Emoney"]);
                    _mxTable.Rows.Add(id, nm, ep);
                }
                filled = true;
            }

            // 2) fallback: read itemtype.fdb (only if cache not available)
            if (!filled)
            {
                try
                {
                    var fdbPath = Path.Combine(_clientPath, "ini", "itemtype.fdb");
                    List<FdbField> fields; List<List<object>> rows;
                    (fields, rows) = FdbLoaderEPLStyle.Load(fdbPath);

                    int idxId = FindExact(fields, "uID");
                    int idxName = FindExact(fields, "szName");
                    int idxE = FindExact(fields, "uEPrice");
                    if (idxId < 0 || idxName < 0 || idxE < 0)
                    {
                        MessageBox.Show("itemtype.fdb wajib ada uID/szName/uEPrice.");
                        return;
                    }

                    for (int r = 0; r < rows.Count; r++)
                    {
                        uint id = ToUInt(rows[r][idxId]);
                        if (id == 0) continue;
                        string nm = rows[r][idxName]?.ToString() ?? "";
                        int ep = ToInt(rows[r][idxE]);
                        _mxTable.Rows.Add(id, nm, ep);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Gagal baca itemtype.fdb: " + ex.Message);
                }
            }

            dgvMx.DataSource = _mxTable;
            dgvMx.AllowUserToAddRows = false;
            dgvMx.Columns["ID"].Width = 90;
            dgvMx.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvMx.Columns["Emoney"].Width = 90;
        }

        private static int FindExact(List<FdbField> f, string name)
        {
            for (int i = 0; i < f.Count; i++)
                if (string.Equals(f[i].Name, name, StringComparison.OrdinalIgnoreCase)) return i;
            return -1;
        }

        // 3) REPLACE the old TxtSearchMx_TextChanged with this (no filter; jump to first match)
        // search-as-you-type: jump to first match (no filtering)
        private void TxtSearchMx_TextChanged(object? sender, EventArgs e)
        {
            GoToFirstMatch(txtSearchMx.Text);
        }


        // 4) Add: Enter key = jump to next match (wrap-around)
        // make Enter go to next match ONLY (and not click OK)
        private void TxtSearchMx_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;          // consume
                e.SuppressKeyPress = true; // no ding, no AcceptButton
                GoToNextMatch(txtSearchMx.Text);
            }
        }

        // helpers: find & scroll without filtering
        private void GoToFirstMatch(string query)
        {
            _lastFoundIndex = -1;
            SelectAndScroll(FindRowIndex(query, 0));
        }

        private void GoToNextMatch(string query)
        {
            if (dgvMx.Rows.Count == 0) return;
            int start = (_lastFoundIndex + 1) % dgvMx.Rows.Count;
            int idx = FindRowIndex(query, start);
            if (idx == -1 && start > 0) idx = FindRowIndex(query, 0); // wrap
            SelectAndScroll(idx);
        }

        private int FindRowIndex(string query, int startIndex)
        {
            if (string.IsNullOrWhiteSpace(query) || dgvMx.Rows.Count == 0) return -1;
            string q = query.Trim();

            for (int i = startIndex; i < dgvMx.Rows.Count; i++)
            {
                if (dgvMx.Rows[i].DataBoundItem is not DataRowView drv) continue;
                string name = drv["Name"]?.ToString() ?? string.Empty;
                string id = drv["ID"]?.ToString() ?? string.Empty;

                // Match: ID startswith OR Name contains (case-insensitive)
                if (id.StartsWith(q, StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
            return -1;
        }

        private void SelectAndScroll(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvMx.Rows.Count) return;

            dgvMx.ClearSelection();
            int col = Math.Min(0, dgvMx.Columns.Count - 1);
            dgvMx.CurrentCell = dgvMx[col, rowIndex];
            dgvMx.Rows[rowIndex].Selected = true;

            int target = Math.Max(0, rowIndex - 3); // offset a bit
            if (target < dgvMx.Rows.Count)
                dgvMx.FirstDisplayedScrollingRowIndex = target;

            _lastFoundIndex = rowIndex;
        }


        private void DgvMx_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvMx.CurrentRow?.DataBoundItem is DataRowView drv)
            {
                string idStr = drv["ID"]?.ToString() ?? "";
                string nm = drv["Name"]?.ToString() ?? "";
                int ep = TryParseInt(drv["Emoney"]?.ToString());

                txtItemType.Text = idStr;

                if (uint.TryParse(idStr, out uint uType) && TryResolveItemId(uType, out var itemX))
                    txtItemId.Text = itemX;           // reuse sedia ada
                else
                    txtItemId.Text = string.Empty;    // tiada → akan auto-gen semasa OK

                txtName.Text = nm;
                txtEmoney.Text = ep.ToString();
                txtReturnEmoney.Text = CalcReturnEmoneyText(ep);
            }
        }


        private void LoadTips()
        {
            var items = new List<(int code, string name)>();
            string stress = Path.Combine(_clientPath, "ini", "stress.ini");
            if (File.Exists(stress))
            {
                var rx = new Regex(@"^(\d{6})\s*=\s*(.+)$");
                foreach (var line in File.ReadAllLines(stress))
                {
                    var m = rx.Match(line.Trim());
                    if (!m.Success) continue;
                    if (!int.TryParse(m.Groups[1].Value, out int key)) continue;
                    int code = key - 704400;
                    if (code < 0 || code > 200) continue;
                    items.Add((code, m.Groups[2].Value.Trim()));
                }
            }
            if (items.Count == 0)
            {
                string[] names = {
                    "Archer","Knight","Warrior","Mage","Paladin","Vampire","Necromancer",
                    "Main Attribute","Divine Main","Quality","Compose","Upgrade","Embed",
                    "Instrument","Blessed Bonus","Socket","+ BP","Divine Fire Raising",
                    "Summon 3 Eudemons","Race","Essential to Composing","Eudemon Awakening",
                    "Sacred Servent","Inventory","Incubation Slot","Child Pet","Eudemon Skin",
                    "Soul Composing","Divine EXP","Weapon","Helmet","Necklace","Armor",
                    "Bracelet","Boots","Agate","MinorATT","Eat Soul","Luck","Compose","Harp",
                    "Flute","3 Sockets","Thunder","Upgrade","Open Slot","Minor Attribute",
                    "Initial Pet","Gain Vitality","Initial Attribute","Orb","Violin",
                    "Dragon Gene","Vigor","Soul Point","SP","Eudemon Race","Bahamut",
                    "Damage Reduction","Damage Increase","Eudemon EXP","Increase Soul",
                    "Gain Mana","Three Water","Add Sacred Point","Talisman Increase",
                    "ThousandFace","SwordMaster"
                };
                for (int i = 0; i < names.Length; i++) items.Add((i, names[i]));
            }

            var combos = new[] { cboTip1, cboTip2, cboTip3 };
            foreach (var cb in combos)
            {
                cb.Items.Clear();
                cb.DisplayMember = "Text";
                cb.DropDownStyle = ComboBoxStyle.DropDownList;
                foreach (var (code, name) in items)
                    cb.Items.Add(new TipItem { Text = name, Value = code });
                cb.SelectedIndex = -1;
            }

            cboTip1.Enabled = true;
            cboTip2.Enabled = false;
            cboTip3.Enabled = false;
        }

        // ReturnEmoney: 0–9 => "", >=10 => drop last digit
        private static string CalcReturnEmoneyText(int ep)
        {
            if (ep < 10) return "";
            return (ep / 10).ToString();
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // --- ItemType required ---
            ItemType = (txtItemType.Text ?? "").Trim();
            if (string.IsNullOrWhiteSpace(ItemType) || !UInt32.TryParse(ItemType, out var uType))
            {
                MessageBox.Show("Sila pilih item dari senarai kiri.");
                return;
            }

            // --- ItemID: reuse if present; else auto from index/session ---
            string picked = (txtItemId.Text ?? "").Trim();
            if (string.IsNullOrEmpty(picked))
            {
                if (!TryResolveItemId(uType, out picked) || string.IsNullOrEmpty(picked))
                    picked = NextItemId().ToString();
                txtItemId.Text = picked;
            }
            ItemID = picked;

            // --- Emoney must > 0 ---
            int ep = TryParseInt(txtEmoney.Text);
            if (ep <= 0)
            {
                MessageBox.Show("Emoney tak boleh 0. Sila isi nilai > 0.");
                return;
            }
            EmoneyOut = ep;
            txtReturnEmoney.Text = CalcReturnEmoneyText(ep);
            ReturnEmoney = (txtReturnEmoney.Text ?? "").Trim();

            // --- CostType from radios (1 or 3 only) ---
            var rbEPPP = this.Controls.Find("rbCostEPPP", true).FirstOrDefault() as RadioButton;
            CostType = (rbEPPP != null && rbEPPP.Checked) ? "3" : "1";

            // --- Other fields with required defaults/values ---
            Version = (cboVersion.SelectedItem ?? 125).ToString();
            OEM = "0";            // forced default (field hidden)
            NewFlag = chkNew.Checked ? "1" : "0";
            CommendFlag = chkCommend.Checked ? "1" : "0";
            Describe = "null";         // forced default (field hidden)
            BeginTime = "1606230730";   // forced default (field hidden)
            EndTime = "1606300729";   // forced default (field hidden)

            // --- Tip (max 3, unique) ---
            int? ReadTip(ComboBox cb) => cb.SelectedItem is TipItem ti ? ti.Value : (int?)null;
            var codes = new List<int>();
            void add(int? v) { if (v.HasValue && !codes.Contains(v.Value)) codes.Add(v.Value); }
            add(ReadTip(cboTip1)); add(ReadTip(cboTip2)); add(ReadTip(cboTip3));
            Tip = codes.Count > 0 ? string.Join("|", codes) : "";

            // --- cache mapping for session reuse ---
            _typeToItemId[uType] = ItemID;
            s_SessionTypeToItemId[uType] = ItemID;
            if (int.TryParse(ItemID, out var n) && n > s_SessionMaxItemId) s_SessionMaxItemId = n;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool TryResolveItemId(uint uType, out string itemX)
        {
            if (_typeToItemId.TryGetValue(uType, out itemX)) return true;          // dari file
            if (s_SessionTypeToItemId.TryGetValue(uType, out itemX)) return true;  // dari sesi
            itemX = ""; return false;
        }

        // ===== helpers =====
        private static int NextItemId() => ++s_SessionMaxItemId;

        private static uint ToUInt(object v)
        {
            if (v == null) return 0u;
            try { return Convert.ToUInt32(v); }
            catch { uint.TryParse(v.ToString(), out var o); return o; }
        }
        private static int ToInt(object v)
        {
            if (v == null) return 0;
            try { return Convert.ToInt32(v); }
            catch { int.TryParse(v.ToString(), out var o); return o; }
        }
        private static int TryParseInt(string? s) => int.TryParse((s ?? "").Trim(), out var n) ? n : 0;


        // ensure Enter is treated as input key inside TextBox
        private void TxtSearchMx_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.IsInputKey = true;
        }
        // disable AcceptButton while typing in search box
        private void TxtSearchMx_Enter(object? sender, EventArgs e)
        {
            _savedAcceptBtn = this.AcceptButton;
            this.AcceptButton = null; // prevent OK on Enter
        }

        private void TxtSearchMx_Leave(object? sender, EventArgs e)
        {
            this.AcceptButton = _savedAcceptBtn; // restore OK
        }

    }
}
