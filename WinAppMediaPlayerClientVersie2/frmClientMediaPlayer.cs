using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Sockets;
using System.Net;
using System.IO;

namespace WinAppMediaPlayerClientVersie2
{
    public partial class frmClientMediaPlayer : Form
    {
        public frmClientMediaPlayer()
        {
            InitializeComponent();
        }

        TcpClient client;
        StreamReader reader;
        StreamWriter writer;

        private void btnZoekServer_Click(object sender, EventArgs e)
        {
            // controle IP adres
            IPAddress address;
            int poort;
            if(!IPAddress.TryParse(mtxtIPadres.Text.Replace(" ", ""), out address))
            {
                txtMelding.AppendText("Ongeldig IP adres");
                mtxtIPadres.Focus();
                return;
            }
            if(!int.TryParse(mtxtPoortnr.Text, out poort))
            {
                txtMelding.AppendText("Ongeldig poort nummer");
                mtxtIPadres.Focus();
                return;
            }
            // verbinding tussen client en server maken
            try
            {
                client = new TcpClient();
                client.Connect(address, poort);
                if (client.Connected)
                {
                    writer = new StreamWriter(client.GetStream());
                    reader = new StreamReader(client.GetStream());
                    writer.AutoFlush = true;
                    // start ontvangen van data
                    bgWorkerOntvang.WorkerSupportsCancellation = true;
                    bgWorkerOntvang.RunWorkerAsync();
                    btnZoekServer.Enabled = false;
                    btnVerbreek.Enabled = true;
                    splitContainer1.Panel1.Enabled = true;
                }
            }
            catch(Exception ex)
            {
                txtMelding.AppendText(ex.Message);
            }
        }

        private void bgWorkerOntvang_DoWork(object sender, DoWorkEventArgs e)
        {
            while(client.Connected)
            {
                string bericht;
                try
                {
                    bericht = reader.ReadLine();
                    if (bericht == "Disconnect") break;
                    txtCommunicatie.Invoke(new MethodInvoker(delegate ()
                    {
                        txtCommunicatie.AppendText(bericht + "\r\n");
                    }));
                }
                catch(Exception ex)
                {
                    txtMelding.Invoke(new MethodInvoker(delegate ()
                    {
                        txtMelding.AppendText("Kan bericht niet inlezen.\r\n" + ex.Message.ToString() + "\r\n");
                    }));
                }
            }
        }

        private void bgWorkerOntvang_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            txtMelding.AppendText("Verbinding verbroken");
            btnVerbreek.Enabled = false;
            btnZoekServer.Enabled = true;
            splitContainer1.Panel1.Enabled = false;
        }

        private void btnZend_Click(object sender, EventArgs e)
        {
            try
            {
                writer.WriteLine("Client >> " + txtBericht.Text);
                txtCommunicatie.AppendText("Client >> " + txtBericht.Text + "\r\n");
            }
            catch
            {
                txtMelding.AppendText("berciht verzenden mislukt" + "\r\n");
            }
        }

        private void btnVerbreek_Click(object sender, EventArgs e)
        {
            try
            {
                writer.WriteLine("Disconnect");
                bgWorkerOntvang.CancelAsync();
                client.Close();
                txtMelding.AppendText("Verbinding verbroken");
            }
            catch
            {
                txtMelding.AppendText("Verbinding verbreken mislukt.");
            }
        }
    }
}
