using System;
using System.Drawing;
using System.ComponentModel;
using System.Windows.Forms;
using System.IO;

namespace Icedream.Binsembler
{
    public partial class Main : Form
    {
        OpenFileDialog ofd = new OpenFileDialog();
        public Main()
        {
            InitializeComponent();
        }

        private void browse_Click(object sender, EventArgs e)
        {
            ofd.Filter = "Alle Dateien|*";
            ofd.Multiselect = false;
            ofd.RestoreDirectory = true;
            ofd.ReadOnlyChecked = true;
            ofd.SupportMultiDottedExtensions = true;
            ofd.Title = "Binärdatei öffnen";
            ofd.AutoUpgradeEnabled = true;
            ofd.CheckFileExists = true;
            ofd.CheckPathExists = true;
            if (System.Threading.Thread.CurrentThread.GetApartmentState() == System.Threading.ApartmentState.MTA)
                MessageBox.Show("Die Dateiauswahl ist nur in der Release-Version verfügbar.");
            else if (ofd.ShowDialog(this) == System.Windows.Forms.DialogResult.OK)
            {
                this.file.Text = ofd.FileName;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Threading.Thread _t = new System.Threading.Thread(new System.Threading.ThreadStart(Verarbeiten));
                _t.IsBackground = true;
                _t.Start();
            }
            catch (Exception) { MessageBox.Show("Fehler beim Threadstart"); }
        }

        private delegate void NormalVoid();
        private void TransferStatus()
        {
            this.label2.Text = (this.status.Text + "\r\n" + this.label2.Text).Substring(0, this.label2.Text.Length + this.status.Text.Length < 2048 ? this.label2.Text.Length + this.status.Text.Length : 2048);
        }

        private void Verarbeiten()
        {
            try
            {
                if (this.InvokeRequired)
                {
                    this.Invoke(new NormalVoid(Verarbeiten));
                    return;
                }
                label2.Text = "";


                status.Text = "Starte Converter...";
                
                Binsembler b2db = new Binsembler();
                b2db.Status += new Binsembler.StatusHandler(b2db_Status);
                b2db.Compile(this.file.Text, this.file.Text + ".txt");
                TransferStatus();
                status.Text = "Konvertierung erfolgreich!";
                b2db = null;
            }
            catch (Exception e) {
                TransferStatus();
                MessageBox.Show("Fehler bei der Umwandlung:\n" + e.Message);
                status.Text = "Fehler bei der Umwandlung: " + e.Message;
            }
        }

        void b2db_Status(LogEventArgs e)
        {
            if (!e.Text.StartsWith("\tSTATUS\t"))
                TransferStatus();
            this.status.Text = "[" + e.Module + "]: ";
            string[] spl = e.Text.Split(new char[] { '\t' }, StringSplitOptions.RemoveEmptyEntries);
            switch (spl[0].ToLower())
            {
                case "error":
                    this.status.Text += "Fehler: ";
                    this.status.BackColor = Color.Red;
                    this.status.ForeColor = Color.White;
                    break;
                case "status":
                    this.status.BackColor = Color.Transparent;
                    this.status.ForeColor = SystemColors.ControlText;
                    break;
                case "opening":
                    this.status.Text += "Öffne Datei: ";
                    this.status.BackColor = Color.Transparent;
                    this.status.ForeColor = SystemColors.GradientActiveCaption;
                    break;
                case "closing":
                    this.status.Text += "Schließe Datei: ";
                    this.status.BackColor = Color.Transparent;
                    this.status.ForeColor = SystemColors.GradientActiveCaption;
                    break;
                default:
                    this.status.Text += spl[0].ToUpper().Substring(0, 1) + spl[0].ToLower().Substring(1) + ": ";
                    this.status.BackColor = Color.Transparent;
                    this.status.ForeColor = SystemColors.GradientActiveCaption;
                    break;
            }
            this.status.Text += spl[1];
            this.status.Refresh();
        }

        private void status_Click(object sender, EventArgs e)
        {

        }

        private void Main_Load(object sender, EventArgs e)
        {

        }
    }
}
