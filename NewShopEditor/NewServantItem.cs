using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Media;
using System.Text;
using System.Windows.Forms;

namespace NewShopEditor
{
    public partial class NewServantItem : Form
    {
        // === Injected from parent ===
        public List<(int Id, string Name)> ItemSource { get; set; } = new();
        public Func<int, string> ResolveItemName { get; set; } = id => "";

        // === Result ===
        public ServantComposeEntry Result { get; private set; } = null;

        public NewServantItem()
        {
            InitializeComponent();
            InitUiBehavior();
        }

        // ---------- UI wiring ----------
        private void InitUiBehavior()
        {
            if (!rbGift.Checked && !rbSpirit.Checked) rbGift.Checked = true;

            // lock read-only boxes
            MakeReadOnly(txtSrvtItem);
            MakeReadOnly(txtSrvtItemName);
            MakeReadOnly(txtNeedItem1);
            MakeReadOnly(txtNeedItem2);
            MakeReadOnly(txtNeedItem3);
            MakeReadOnly(txtNeedItem4);
            MakeReadOnly(txtNeedItem5);
            MakeReadOnly(txtNeedItemName1);
            MakeReadOnly(txtNeedItemName2);
            MakeReadOnly(txtNeedItemName3);
            MakeReadOnly(txtNeedItemName4);
            MakeReadOnly(txtNeedItemName5);

            // numeric ranges
            WireNumericRange(txtSvrtLevel, 1, 6);   // level 1..6 only
            WireNumericRange(txtSrvtMaxItem, 1, 5); // max 1..5 only

            // numeric only (no range)
            WireNumeric(txtSrvtEXP);
            WireNumeric(txtSvrtTime);
            WireNumeric(txtSvrtPrice);
            WireNumeric(txtNeedItemAmount1);
            WireNumeric(txtNeedItemAmount2);
            WireNumeric(txtNeedItemAmount3);
            WireNumeric(txtNeedItemAmount4);
            WireNumeric(txtNeedItemAmount5);

            // buttons
            btnFNeedItem.Click += (s, e) => PickMainItem();
            btnFitem1.Click += (s, e) => PickNeedItem(txtNeedItem1, txtNeedItemName1);
            btnFitem2.Click += (s, e) => PickNeedItem(txtNeedItem2, txtNeedItemName2);
            btnFitem3.Click += (s, e) => PickNeedItem(txtNeedItem3, txtNeedItemName3);
            btnFitem4.Click += (s, e) => PickNeedItem(txtNeedItem4, txtNeedItemName4);
            btnFitem5.Click += (s, e) => PickNeedItem(txtNeedItem5, txtNeedItemName5);

            btnSrvtConfirm.Click += (s, e) => OnConfirm();
            btnSvrtCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
        }

        private void MakeReadOnly(TextBox t)
        {
            t.ReadOnly = true;
            t.BackColor = Color.Gainsboro;
            t.ForeColor = Color.Black;
        }

        // digits only
        private void WireNumeric(TextBox t)
        {
            t.KeyPress += (s, e) =>
            {
                bool ctrl = (ModifierKeys & Keys.Control) == Keys.Control;
                if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar) || ctrl) return;
                e.Handled = true;
            };
            t.Leave += (s, e) => { if (!int.TryParse((t.Text ?? "").Trim(), out _)) t.Text = "0"; };
        }

        // digits with min/max clamp
        private void WireNumericRange(TextBox t, int min, int max)
        {
            t.KeyPress += (s, e) =>
            {
                bool ctrl = (ModifierKeys & Keys.Control) == Keys.Control;
                if (char.IsControl(e.KeyChar) || char.IsDigit(e.KeyChar) || ctrl) return;
                e.Handled = true;
            };
            void Clamp()
            {
                if (!int.TryParse((t.Text ?? "").Trim(), out var v)) v = min;
                if (v < min) v = min; else if (v > max) v = max;
                t.Text = v.ToString();
            }
            t.Leave += (s, e) => Clamp();
        }

        // ---------- Item picking ----------
        private void PickMainItem()
        {
            if (ShowItemPicker("Select Item", out int id, out string name))
            {
                txtSrvtItem.Text = id.ToString();
                txtSrvtItemName.Text = name;
            }
        }

        private void PickNeedItem(TextBox idBox, TextBox nameBox)
        {
            if (ShowItemPicker("Select Required Item", out int id, out string name))
            {
                idBox.Text = id.ToString();
                nameBox.Text = name;
            }
        }

        private bool ShowItemPicker(string title, out int id, out string name)
        {
            id = 0; name = "";
            var data = ItemSource?.Count > 0 ? ItemSource : new List<(int, string)>();
            using var dlg = new ItemPickDialog(title, data, ResolveItemName);
            if (dlg.ShowDialog(this) == DialogResult.OK)
            {
                id = dlg.SelectedId;
                name = string.IsNullOrEmpty(dlg.SelectedName) ? ResolveItemName?.Invoke(id) ?? "" : dlg.SelectedName;
                return id > 0;
            }
            return false;
        }

        // ---------- Build result ----------
        private void OnConfirm()
        {
            int needtype = rbGift.Checked ? 1 : rbSpirit.Checked ? 2 : 0;
            if (needtype == 0) { MessageBox.Show("Please select Gift Master or Spirit Master."); return; }

            int item = ToInt(txtSrvtItem.Text);
            if (item <= 0) { MessageBox.Show("Please choose the Item ID (Find Item)."); return; }

            var entry = new ServantComposeEntry
            {
                needtype = needtype,
                needlevel = ToInt(txtSvrtLevel.Text),
                needmoney = ToInt(txtSvrtPrice.Text),
                needtime = ToInt(txtSvrtTime.Text),
                item = item,
                max = ToInt(txtSrvtMaxItem.Text),
                proficiency = ToInt(txtSrvtEXP.Text),

                needitem1 = ToInt(txtNeedItem1.Text),
                needitemcount1 = ToInt(txtNeedItemAmount1.Text),
                needitem2 = ToInt(txtNeedItem2.Text),
                needitemcount2 = ToInt(txtNeedItemAmount2.Text),
                needitem3 = ToInt(txtNeedItem3.Text),
                needitemcount3 = ToInt(txtNeedItemAmount3.Text),
                needitem4 = ToInt(txtNeedItem4.Text),
                needitemcount4 = ToInt(txtNeedItemAmount4.Text),
                needitem5 = ToInt(txtNeedItem5.Text),
                needitemcount5 = ToInt(txtNeedItemAmount5.Text),
            };

            Result = entry;
            DialogResult = DialogResult.OK;
            Close();
        }

        private int ToInt(string s) => int.TryParse((s ?? "").Trim(), out var v) ? v : 0;

        // ================== Helper dialog: Item picker (smooth) ==================
        private class ItemPickDialog : Form
        {
            private readonly DataGridView _grid = new DataGridView();
            private readonly TextBox _search = new TextBox();
            private readonly Button _btnOk = new Button();
            private readonly Button _btnCancel = new Button();
            private readonly BindingList<ItemRow> _rows = new BindingList<ItemRow>();
            private readonly Func<int, string> _resolver;

            private string _lastQuery = "";
            private int _lastIndex = -1;

            public int SelectedId { get; private set; }
            public string SelectedName { get; private set; } = "";

            public ItemPickDialog(string title, List<(int Id, string Name)> items, Func<int, string> resolver)
            {
                Text = title;
                StartPosition = FormStartPosition.CenterParent;
                Size = new Size(780, 640);
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MaximizeBox = false; MinimizeBox = false;

                _resolver = resolver ?? (id => "");

                // ===== Layout (2 rows) =====
                var root = new TableLayoutPanel { Dock = DockStyle.Fill, RowCount = 2, ColumnCount = 1 };
                root.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
                root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
                Controls.Add(root);

                // --- row 0: search + buttons ---
                var top = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 10, 12, 10) };
                root.Controls.Add(top, 0, 0);

                _search.Parent = top;
                _search.Location = new Point(12, 10);
                _search.Width = 460;
                _search.Anchor = AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Top;
                _search.PlaceholderText = "Type ID/Name – Enter: next, Ctrl+Enter: OK";
                _search.KeyDown += Search_KeyDown;
                _search.Enter += (s, e) => this.AcceptButton = null;
                _search.Leave += (s, e) => this.AcceptButton = _btnOk;

                _btnOk.Parent = top; _btnOk.Text = "OK";
                _btnOk.Size = new Size(80, 26);
                _btnOk.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                _btnOk.Location = new Point(top.Width - 12 - 80 - 8 - 80, 10);
                _btnOk.Click += (s, e) => ConfirmSelection();

                _btnCancel.Parent = top; _btnCancel.Text = "Cancel";
                _btnCancel.Size = new Size(80, 26);
                _btnCancel.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                _btnCancel.Location = new Point(top.Width - 12 - 80, 10);
                _btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

                top.Resize += (s, e) =>
                {
                    _btnCancel.Left = top.Width - 12 - _btnCancel.Width;
                    _btnOk.Left = _btnCancel.Left - 8 - _btnOk.Width;
                    _search.Width = _btnOk.Left - 12 - _search.Left;
                };

                this.AcceptButton = _btnOk;
                this.CancelButton = _btnCancel;

                // --- row 1: grid ---
                var gridHost = new Panel { Dock = DockStyle.Fill, Padding = new Padding(12, 0, 12, 12) };
                root.Controls.Add(gridHost, 0, 1);

                _grid.Parent = gridHost;
                _grid.Dock = DockStyle.Fill;
                _grid.ReadOnly = true;
                _grid.AllowUserToAddRows = false;
                _grid.AllowUserToDeleteRows = false;
                _grid.AllowUserToResizeColumns = false; // no resize
                _grid.AllowUserToResizeRows = false;    // no resize
                _grid.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
                _grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing;
                _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
                _grid.MultiSelect = false;
                _grid.RowHeadersVisible = false;
                _grid.AutoGenerateColumns = false;
                _grid.ScrollBars = ScrollBars.Both;
                _grid.Columns.Clear();

                var colId = new DataGridViewTextBoxColumn
                {
                    HeaderText = "ID",
                    DataPropertyName = "Id",
                    Width = 160,
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    Resizable = DataGridViewTriState.False,
                    Frozen = true
                };
                var colName = new DataGridViewTextBoxColumn
                {
                    HeaderText = "Name",
                    DataPropertyName = "Name",
                    AutoSizeMode = DataGridViewAutoSizeColumnMode.None,
                    Width = 520, // adjust if needed
                    SortMode = DataGridViewColumnSortMode.NotSortable,
                    Resizable = DataGridViewTriState.False
                };
                _grid.Columns.Add(colId);
                _grid.Columns.Add(colName);
                _grid.DataSource = _rows;

                if (items != null && items.Count > 0)
                    foreach (var it in items) _rows.Add(new ItemRow { Id = it.Id, Name = it.Name });

                _grid.CellDoubleClick += (s, e) => ConfirmSelection();
                _grid.KeyDown += (s, e) =>
                {
                    if (!_search.Focused && e.KeyCode == Keys.Enter)
                    { e.Handled = true; e.SuppressKeyPress = true; ConfirmSelection(); }
                };
            }

            private struct ItemRow { public int Id { get; set; } public string Name { get; set; } }

            // Enter = jump next (wrap). Ctrl+Enter = OK. No beep.
            private void Search_KeyDown(object sender, KeyEventArgs e)
            {
                if (e.Control && e.KeyCode == Keys.Enter)
                { e.Handled = true; e.SuppressKeyPress = true; ConfirmSelection(); return; }

                if (e.KeyCode != Keys.Enter) return;
                e.Handled = true; e.SuppressKeyPress = true;

                var q = (_search.Text ?? "").Trim();
                if (string.IsNullOrEmpty(q)) return;

                if (int.TryParse(q, out int qid))
                {
                    if (_rows.Count == 0)
                    {
                        var nm = _resolver?.Invoke(qid) ?? "";
                        if (qid > 0 && !string.IsNullOrEmpty(nm))
                            _rows.Add(new ItemRow { Id = qid, Name = nm });
                    }

                    int start = (_lastQuery == q) ? (_lastIndex + 1) : 0;
                    int idx = FindNextIndex(start, r => r.Id == qid);
                    if (idx < 0 && start > 0) idx = FindNextIndex(0, r => r.Id == qid); // wrap
                    if (idx >= 0) { _lastQuery = q; _lastIndex = idx; SelectRow(idx); }
                    return;
                }
                else
                {
                    string qlow = q.ToLowerInvariant();
                    int start = (_lastQuery.Equals(q, StringComparison.OrdinalIgnoreCase)) ? (_lastIndex + 1) : 0;
                    int idx = FindNextIndex(start, r => !string.IsNullOrEmpty(r.Name) && r.Name.ToLowerInvariant().Contains(qlow));
                    if (idx < 0 && start > 0)
                        idx = FindNextIndex(0, r => !string.IsNullOrEmpty(r.Name) && r.Name.ToLowerInvariant().Contains(qlow)); // wrap
                    if (idx >= 0) { _lastQuery = q; _lastIndex = idx; SelectRow(idx); }
                }
            }

            private int FindNextIndex(int start, Func<ItemRow, bool> pred)
            {
                for (int i = Math.Max(0, start); i < _rows.Count; i++)
                    if (pred(_rows[i])) return i;
                return -1;
            }

            private void SelectRow(int index)
            {
                if (index < 0 || index >= _grid.Rows.Count) return;
                _grid.ClearSelection();
                _grid.Rows[index].Selected = true;
                _grid.CurrentCell = _grid.Rows[index].Cells[0];
                _grid.FirstDisplayedScrollingRowIndex = index;
            }

            private void ConfirmSelection()
            {
                if (_grid.CurrentRow == null) return;
                if (int.TryParse(Convert.ToString(_grid.CurrentRow.Cells[0].Value), out int id))
                {
                    SelectedId = id;
                    SelectedName = Convert.ToString(_grid.CurrentRow.Cells[1].Value) ?? "";
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }
        }
    }

    // === DTO ===
    public class ServantComposeEntry
    {
        public int needtype { get; set; }        // 1=Gift, 2=Spirit
        public int needlevel { get; set; }
        public int needmoney { get; set; }
        public int needtime { get; set; }
        public int item { get; set; }
        public int max { get; set; }
        public int proficiency { get; set; }
        public int needitem1 { get; set; }
        public int needitemcount1 { get; set; }
        public int needitem2 { get; set; }
        public int needitemcount2 { get; set; }
        public int needitem3 { get; set; }
        public int needitemcount3 { get; set; }
        public int needitem4 { get; set; }
        public int needitemcount4 { get; set; }
        public int needitem5 { get; set; }
        public int needitemcount5 { get; set; }

        public int[] ToIniArray() => new int[]
        {
            needtype, needlevel, needmoney, needtime, item, max, proficiency,
            needitem1, needitemcount1, needitem2, needitemcount2, needitem3, needitemcount3,
            needitem4, needitemcount4, needitem5, needitemcount5
        };
    }
}
