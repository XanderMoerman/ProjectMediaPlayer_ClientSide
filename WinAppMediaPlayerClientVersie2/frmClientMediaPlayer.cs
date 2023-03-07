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
using System.Net.Http;

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
                while (!client.Connected) ; // wachten tot de client verbonden is

                // als de client verbonden is
                writer = new StreamWriter(client.GetStream());
                reader = new StreamReader(client.GetStream());
                writer.AutoFlush = true;
                // start ontvangen van data
                bgWorkerOntvang.WorkerSupportsCancellation = true;
                bgWorkerOntvang.RunWorkerAsync();
                btnZoekServer.Enabled = false;
                btnVerbreek.Enabled = true;
                tssClient.Text = "Client Verbonden";
                tssClient.ForeColor = Color.Green;
                splitContainer1.Panel2.Enabled = true;
                
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
                    if (bericht == "Disconnect") break; // verbinding verbreken
                    if (bericht.StartsWith("SONGLISTADD")) // add song
                    {
                        string Song = bericht.Remove(0, 12);
                        if (lstSong.Items.Contains(Song)) return; // als de song al bestaat
                        lstSong.Invoke(new MethodInvoker(delegate ()
                        {
                            lstSong.Items.Add(Song); // voeg toe aan de list
                        }));
                        return;
                    }
                    if (bericht.StartsWith("PLAYLISTADD")) // add song
                    {
                        string Song = bericht.Remove(0, 12);
                        if (lstSongPlayList.Items.Contains(Song)) return; // als de song al bestaat
                        lstSongPlayList.Invoke(new MethodInvoker(delegate ()
                        {
                            lstSongPlayList.Items.Add(Song); // voeg toe aan de list
                        }));
                        return;
                    }
                    if (bericht.StartsWith("PLAYLISTREMOVE"))
                    {
                        string Song = bericht.Remove(0, 15);
                        lstSongPlayList.Invoke(new MethodInvoker(delegate ()
                        {
                            // als de song bestaat, verwijder
                            if (lstSongPlayList.Items.Contains(Song)) lstSongPlayList.Items.Remove(Song);
                        }));
                        return;
                    }
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
            txtMelding.AppendText("Verbinding verbroken | Background Worker Complete");
            btnVerbreek.Enabled = false;
            btnZoekServer.Enabled = true;
            splitContainer1.Panel2.Enabled = false;
            tssClient.Text = "Client niet verbonden";
            tssClient.ForeColor = Color.Red;
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
                txtMelding.AppendText("Verbinding verbroken | Verbreek button clicked");
                tssClient.Text = "Client niet verbonden";
                tssClient.ForeColor = Color.Red;
            }
            catch
            {
                txtMelding.AppendText("Verbinding verbreken mislukt.");
            }
        }

        private void btnVoegToePlayList_Click(object sender, EventArgs e)
        {
            if (lstSong.SelectedIndex == -1) return; // als er niets geselecteerd is
            string Song = lstSong.SelectedItem.ToString();
            if (lstSongPlayList.Items.Contains(Song)) return; // als de song al bestaat
            lstSongPlayList.Items.Add(Song); // voeg toe aan de list

            // doorsturen naar server
            writer.WriteLine("PLAYLISTADD " + Song);
        }

        private void btnVerwijderPlayList_Click(object sender, EventArgs e)
        {
            if (lstSongPlayList.SelectedIndex == -1) return; // als er niets geselecteerd is
            string Song = lstSongPlayList.SelectedItem.ToString();
            if (lstSongPlayList.Items.Contains(Song)) lstSongPlayList.Items.Remove(Song); // als de song bestaat, verwijder

            // doorsturen naar client
            writer.WriteLine("PLAYLISTREMOVE " + Song);
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            writer.WriteLine("START-PLAYER");
        }

        private void btnStopPlay_Click(object sender, EventArgs e)
        {
            writer.WriteLine("STOP-PLAYER");
        }
    }
}
