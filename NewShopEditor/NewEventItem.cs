using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace NewShopEditor
{
    public partial class NewEventItem : Form
    {
        // Inject dari parent (itemtype.fdb)
        public List<(int Id, string Name)> ItemSource { get; set; } = new();
        public Func<int, string> ResolveItemName { get; set; } = id => "";

        // Output
        public int SelectedShopType { get; private set; } = 1; // 1=Astra,2=Honor,4=Plane
        public int SelectedItemId { get; private set; }
        public string SelectedItemName { get; private set; } = "";

        // Data dalaman (pantas)
        private struct ItemRow { public int Id { get; set; } public string Name { get; set; } }
        private List<ItemRow> _data = new();
        private readonly BindingSource _bs = new BindingSource();

        // State cari
        private string _lastQuery = "";
        private int _lastIndex = -1;

        public int EnteredTalentCoin { get; private set; } = 0;

        public NewEventItem()
        {
            InitializeComponent();
            WireUp();
            FastGrid(dgvSearch);

            // numeric-only + MIN = 1 for Talent_coin
            txtPrice.Text = "1";
            txtPrice.KeyPress += (s, e) =>
            {
                bool ctrl = (ModifierKeys & Keys.Control) == Keys.Control;
                if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar) || ctrl) return;
                e.Handled = true;
            };
            txtPrice.Leave += (s, e) =>
            {
                // clamp to >= 1
                if (!int.TryParse(txtPrice.Text.Trim(), out var v) || v < 1) txtPrice.Text = "1";
            };
        }


        // helper
        private static int ToInt(string s) => int.TryParse((s ?? "").Trim(), out var v) ? v : 0;

        // dalam ConfirmPick() – sebelum set DialogResult


        private void WireUp()
        {
            // bind grid -> 2 kolum: ID, Name
            dgvSearch.AutoGenerateColumns = false;
            if (dgvSearch.Columns.Count >= 2)
            {
                dgvSearch.Columns[0].DataPropertyName = "Id";
                dgvSearch.Columns[1].DataPropertyName = "Name";
            }
            dgvSearch.DataSource = _bs;

            // event grid
            dgvSearch.CellDoubleClick += (s, e) => ConfirmPick();
            dgvSearch.KeyDown += (s, e) =>
            {
                if (e.KeyCode == Keys.Enter)
                { e.SuppressKeyPress = true; e.Handled = true; ConfirmPick(); }
            };

            // search
            txtSearch.KeyDown += TxtSearch_KeyDown;
            txtSearch.Enter += (s, e) => this.AcceptButton = null;
            txtSearch.Leave += (s, e) => this.AcceptButton = btnAdd;

            // butang
            btnAdd.Click += (s, e) => ConfirmPick();
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            // OK/Cancel
            this.AcceptButton = btnAdd;
            this.CancelButton = btnCancel;

            // radio shop type
            rbAstra.CheckedChanged += ShopTypeChanged;
            rbHonor.CheckedChanged += ShopTypeChanged;
            rbPlane.CheckedChanged += ShopTypeChanged;
            if (!rbAstra.Checked && !rbHonor.Checked && !rbPlane.Checked) rbAstra.Checked = true;
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            LoadRowsFromSource();
        }

        private void LoadRowsFromSource()
        {
            dgvSearch.SuspendLayout();
            try
            {
                _data = (ItemSource ?? new())
                    .OrderBy(x => x.Id)
                    .Select(x => new ItemRow { Id = x.Id, Name = x.Name ?? "" })
                    .ToList();

                _bs.DataSource = _data;
                _bs.ResetBindings(false);
            }
            finally { dgvSearch.ResumeLayout(); }

            if (dgvSearch.Rows.Count > 0)
            {
                dgvSearch.ClearSelection();
                dgvSearch.Rows[0].Selected = true;
                dgvSearch.CurrentCell = dgvSearch.Rows[0].Cells[0];
            }
        }

        private void ShopTypeChanged(object? sender, EventArgs e)
        {
            if (rbAstra.Checked) SelectedShopType = 1;
            else if (rbHonor.Checked) SelectedShopType = 2;
            else if (rbPlane.Checked) SelectedShopType = 4;
        }

        // Enter = next (wrap). Ctrl+Enter = Add. Tak filter grid.
        // Enter = lompat next (wrap). Ctrl+Enter = Add.
        // Cari ID dan Name dua-dua guna 'contains'.
        private void TxtSearch_KeyDown(object? sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.Enter)
            { e.SuppressKeyPress = true; e.Handled = true; ConfirmPick(); return; }
            if (e.KeyCode != Keys.Enter) return;

            e.SuppressKeyPress = true; e.Handled = true;

            var q = (txtSearch.Text ?? "").Trim();
            if (string.IsNullOrEmpty(q) || _data.Count == 0) return;

            string qlow = q.ToLowerInvariant();
            int start = (_lastQuery.Equals(q, StringComparison.OrdinalIgnoreCase)) ? (_lastIndex + 1) : 0;

            bool Pred(NewEventItem.ItemRow r)
                => r.Id.ToString().Contains(q) ||
                   (!string.IsNullOrEmpty(r.Name) && r.Name.ToLowerInvariant().Contains(qlow));

            int idx = FindNextIndex(start, Pred);
            if (idx < 0 && start > 0) idx = FindNextIndex(0, Pred); // wrap

            if (idx >= 0) { _lastQuery = q; _lastIndex = idx; SelectRow(idx); }
        }

        private int FindNextIndex(int start, Func<ItemRow, bool> pred)
        {
            for (int i = Math.Max(0, start); i < _data.Count; i++)
                if (pred(_data[i])) return i;
            return -1;
        }

        private void SelectRow(int index)
        {
            if (index < 0 || index >= dgvSearch.Rows.Count) return;
            dgvSearch.ClearSelection();
            dgvSearch.Rows[index].Selected = true;
            dgvSearch.CurrentCell = dgvSearch.Rows[index].Cells[0];
            dgvSearch.FirstDisplayedScrollingRowIndex = index;
        }

        private void ConfirmPick()
        {
            // sync shop type
            ShopTypeChanged(null, EventArgs.Empty);

            if (dgvSearch.CurrentRow == null)
            { DialogResult = DialogResult.Cancel; Close(); return; }

            if (int.TryParse(Convert.ToString(dgvSearch.CurrentRow.Cells[0].Value), out int id))
            {
                SelectedItemId = id;
                SelectedItemName = Convert.ToString(dgvSearch.CurrentRow.Cells[1].Value) ?? "";
                if (string.IsNullOrEmpty(SelectedItemName))
                    SelectedItemName = ResolveItemName?.Invoke(id) ?? "";
                EnteredTalentCoin = ToInt(txtPrice.Text);
                int tc = ToInt(txtPrice.Text);
                if (tc < 1)
                {
                    MessageBox.Show("Price (Talent coin) must ≥ 1.");
                    txtPrice.Focus();
                    txtPrice.SelectAll();
                    return;
                }
                EnteredTalentCoin = tc;   // pass back to caller
                DialogResult = DialogResult.OK;
                Close();
            }
        }

        // speed tweaks (double buffer, fixed sizes)
        private void FastGrid(DataGridView gv)
        {
            gv.ReadOnly = true;
            gv.RowHeadersVisible = false;
            gv.AllowUserToAddRows = gv.AllowUserToDeleteRows = false;
            gv.AllowUserToResizeColumns = gv.AllowUserToResizeRows = false;
            gv.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.None;
            gv.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            gv.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            gv.ScrollBars = ScrollBars.Both;
            foreach (DataGridViewColumn c in gv.Columns)
            { c.SortMode = DataGridViewColumnSortMode.NotSortable; c.Resizable = DataGridViewTriState.False; }

            // enable double-buffer
            var pi = typeof(DataGridView).GetProperty("DoubleBuffered",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            pi?.SetValue(gv, true, null);
        }
    }
}
