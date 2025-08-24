using System;
using System.Data;
using System.Windows.Forms;

namespace NewShopEditor
{
    public partial class NewShopItemForm : Form
    {
        private DataTable? _table;
        private readonly ItemtypeCache _cache;

        // selected item id from grid
        private uint _selectedId = 0;

        public uint ItemId => _selectedId;
        public string ItemName { get; private set; } = "";
        public int Gold { get; private set; } = 0;
        public int Emoney { get; private set; } = 0;
        public uint Reserved1 { get; private set; } = 4;
        public uint Reserved2 { get; private set; } = 127;
        // [1] Fields – add at class top
        private int _lastFoundIndex = -1;            // track last matched row
        private IButtonControl? _savedAcceptBtn = null; // preserve AcceptButton (OK)

        public NewShopItemForm(ItemtypeCache cache)
        {
            _cache = cache;
            InitializeComponent();
            InitUi();
        }

        private void InitUi()
        {
            // numeric-only inputs
            txtGold.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };
            txtEmoney.KeyPress += (s, e) => { if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar)) e.Handled = true; };

            // dropdown presets (keep but we will hide)
            cboReserved1.Items.AddRange(new object[] { 0, 1, 4 });
            cboReserved2.Items.AddRange(new object[] { 61, 29, 127, 95, 63, 125, 47, 16445 });

            // set default selections (even though hidden)
            cboReserved1.SelectedItem = 4;
            cboReserved2.SelectedItem = 127;

            // HIDE Reserved1/Reserved2 UI
            lblReserved1.Visible = false;
            cboReserved1.Visible = false;
            lblReserved2.Visible = false;
            cboReserved2.Visible = false;

            Load += NewShopItemForm_Load;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            dgvFdb.SelectionChanged += DgvFdb_SelectionChanged;
            dgvFdb.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) btnOK.PerformClick(); };
            btnOK.Click += BtnOK_Click;

            // [2] InitUi() – KEEP existing, then ADD these handlers
            txtSearch.KeyDown += TxtSearch_KeyDown;                // Enter => next
            txtSearch.PreviewKeyDown += TxtSearch_PreviewKeyDown;  // treat Enter as input
            txtSearch.Enter += TxtSearch_Enter;                    // disable AcceptButton
            txtSearch.Leave += TxtSearch_Leave;                    // restore AcceptButton

        }

        // [4] Enter key => go next (no OK click)
        private void TxtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
            {
                e.Handled = true;
                e.SuppressKeyPress = true;
                GoToNextMatch(txtSearch.Text);
            }
        }

        private void TxtSearch_PreviewKeyDown(object? sender, PreviewKeyDownEventArgs e)
        {
            if (e.KeyCode == Keys.Enter) e.IsInputKey = true;
        }

        // Temporarily disable AcceptButton (OK) while typing in search
        private void TxtSearch_Enter(object? sender, EventArgs e)
        {
            _savedAcceptBtn = this.AcceptButton;
            this.AcceptButton = null;
        }
        private void TxtSearch_Leave(object? sender, EventArgs e)
        {
            this.AcceptButton = _savedAcceptBtn;
        }

        private void NewShopItemForm_Load(object? sender, EventArgs e)
        {
            LoadFdbToGrid();
        }

        private void LoadFdbToGrid()
        {
            // use shared cached table (ID/Name/Gold/Emoney)
            _table = _cache.GridTable.Copy();
            dgvFdb.DataSource = _table;
            dgvFdb.Columns["ID"].Width = 90;
            dgvFdb.Columns["Name"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            dgvFdb.Columns["Gold"].Width = 90;
            dgvFdb.Columns["Emoney"].Width = 90;
        }

        // [3] REPLACE TxtSearch_TextChanged (no RowFilter; jump to first match)
        private void TxtSearch_TextChanged(object? sender, EventArgs e)
        {
            GoToFirstMatch(txtSearch.Text);
        }

        // [5] Helpers – find without filtering, select & scroll
        private void GoToFirstMatch(string? query)
        {
            _lastFoundIndex = -1;
            SelectAndScroll(FindRowIndex(query, 0));
        }

        private void GoToNextMatch(string? query)
        {
            if (dgvFdb.Rows.Count == 0) return;
            int start = (_lastFoundIndex + 1) % dgvFdb.Rows.Count;
            int idx = FindRowIndex(query, start);
            if (idx == -1 && start > 0) idx = FindRowIndex(query, 0); // wrap
            SelectAndScroll(idx);
        }

        private int FindRowIndex(string? query, int startIndex)
        {
            if (string.IsNullOrWhiteSpace(query) || dgvFdb.Rows.Count == 0) return -1;
            string q = query.Trim();

            for (int i = startIndex; i < dgvFdb.Rows.Count; i++)
            {
                if (dgvFdb.Rows[i].DataBoundItem is not DataRowView drv) continue;
                string name = drv["Name"]?.ToString() ?? string.Empty;
                string id = drv["ID"]?.ToString() ?? string.Empty;

                // Match rule: ID startswith OR Name contains (case-insensitive)
                if (id.StartsWith(q, StringComparison.OrdinalIgnoreCase) ||
                    name.IndexOf(q, StringComparison.OrdinalIgnoreCase) >= 0)
                    return i;
            }
            return -1;
        }

        private void SelectAndScroll(int rowIndex)
        {
            if (rowIndex < 0 || rowIndex >= dgvFdb.Rows.Count) return;

            dgvFdb.ClearSelection();
            int col = Math.Min(0, dgvFdb.Columns.Count - 1);
            dgvFdb.CurrentCell = dgvFdb[col, rowIndex];
            dgvFdb.Rows[rowIndex].Selected = true;

            int target = Math.Max(0, rowIndex - 3); // show a bit above
            if (target < dgvFdb.Rows.Count)
                dgvFdb.FirstDisplayedScrollingRowIndex = target;

            _lastFoundIndex = rowIndex;
        }

        private void DgvFdb_SelectionChanged(object? sender, EventArgs e)
        {
            if (dgvFdb.CurrentRow?.DataBoundItem is DataRowView drv)
            {
                _selectedId = ToUInt(drv["ID"]);
                txtSelName.Text = drv["Name"]?.ToString();
                txtGold.Text = drv["Gold"]?.ToString();
                txtEmoney.Text = drv["Emoney"]?.ToString();
            }
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            if (_selectedId == 0)
            {
                MessageBox.Show("Sila pilih item (ID mesti sah).");
                return;
            }

            ItemName = txtSelName.Text?.Trim() ?? "";
            Gold = TryParseInt(txtGold.Text);
            Emoney = TryParseInt(txtEmoney.Text);

            // Force defaults (hidden fields)
            Reserved1 = 4;
            Reserved2 = 127;

            DialogResult = DialogResult.OK;
            Close();
        }


        // helpers
        private static uint ToUInt(object v)
        {
            if (v == null) return 0;
            try { return Convert.ToUInt32(v); }
            catch { uint.TryParse(v.ToString(), out var o); return o; }
        }
        private static int TryParseInt(string? s) =>
            int.TryParse((s ?? "").Trim(), out var n) ? n : 0;
    }
}
