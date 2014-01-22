using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MapleShop
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "MapleShark Binary (.msb)|*.msb";
                ofd.Title = "Select a MapleShark Binary...";

                DialogResult result = ofd.ShowDialog();

                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    txtPath.Text = ofd.FileName;

                    btnParseShops.Enabled = true;
                    //btnParseCashShop.Enabled = true;
                }
            }
        }

        private void txtPath_MouseClick(object sender, MouseEventArgs e)
        {
            btnBrowse.PerformClick();
        }

        private void btnParseShops_Click(object sender, EventArgs e)
        {
            ParseResult result = Parser.ParseShops(txtPath.Text);

            if (result == ParseResult.Success)
            {
                MessageBox.Show(string.Format("Successfully parsed the data ({0} Entires).\r\nYou may now dump it to SQL format.", Parser.ShopQueries.Count), "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
                btnParseShops.Enabled = false;
                btnDump.Enabled = true;
            }
        }

        private void btnDump_Click(object sender, EventArgs e)
        {
            string finalQuery = "";

            progressBar1.Maximum = Parser.ShopQueries.Count;

            foreach (string query in Parser.ShopQueries)
            {
                finalQuery += query + Environment.NewLine;
                progressBar1.Increment(1);
            }

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "Shop Items Data";
            sfd.Filter = "SQL Script File UTF-8 | *.sql";

            if (sfd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                StreamWriter writer = new StreamWriter(sfd.OpenFile());
                writer.Write(finalQuery);

                writer.Dispose();
                writer.Close();
            }

        }
    }
}
