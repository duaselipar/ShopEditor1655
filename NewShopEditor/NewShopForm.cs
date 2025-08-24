using System;
using System.Windows.Forms;

namespace NewShopEditor
{
    public partial class NewShopForm : Form
    {
        // Exposed values after OK
        public uint ShopId { get; private set; }
        public string ShopName { get; private set; } = "";
        public uint ShopType { get; private set; } = 2; // 2 = Gold, 5 = EP

        public NewShopForm()
        {
            InitializeComponent();
            rbTypeGold.Checked = true; // default: Gold (2)
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            // numeric only for ShopID
            txtShopId.KeyPress += (s, ev) =>
            {
                if (!char.IsControl(ev.KeyChar) && !char.IsDigit(ev.KeyChar)) ev.Handled = true;
            };
        }

        private void btnOK_Click(object? sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtShopId.Text))
            {
                MessageBox.Show("Sila isi ShopID."); txtShopId.Focus(); return;
            }
            if (!UInt32.TryParse(txtShopId.Text.Trim(), out var id) || id == 0)
            {
                MessageBox.Show("ShopID tidak sah."); txtShopId.Focus(); return;
            }
            if (string.IsNullOrWhiteSpace(txtShopName.Text))
            {
                MessageBox.Show("Sila isi nama shop."); txtShopName.Focus(); return;
            }

            ShopId = id;
            ShopName = txtShopName.Text.Trim();
            // 2 = Gold, 5 = EP
            ShopType = rbTypeEp.Checked ? 5u : 2u;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
