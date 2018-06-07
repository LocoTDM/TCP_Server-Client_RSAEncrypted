using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Projekti2___Klienti
{
    public partial class Form1 : Form
    {
        private TcpClient client;
        RSACryptoServiceProvider objRSAClient;
        RSACryptoServiceProvider objRSAServer;
        private bool keysImported = false;


        public Form1()
        {
            InitializeComponent();
            importPng.BackColor = Color.Transparent;
            btnImportKeys.BackColor = Color.Transparent;
            btnDergo.BackColor = Color.Transparent;
            sendPng.BackColor = Color.Transparent;
            lblResponse.BackColor = Color.Transparent;
            lblText.BackColor = Color.Transparent;
            connectionPng.BackColor = Color.Transparent;
            clientPng.BackColor = Color.Transparent;

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            objRSAClient = new RSACryptoServiceProvider();
            objRSAServer = new RSACryptoServiceProvider();
        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void btnDergo_Click(object sender, EventArgs e)
        {
            
            try
            {
                // nese ende nuk jane importuar celesat mos e le te vazhdohet
                if(!keysImported)
                {
                    MessageBox.Show("Please import your keys!");
                    return;
                }
                String request = "";
                // nese ka dicka te shenuar vecse ne textBox proceso, perndryshe mos beje asgje
                if (txtTeksti.Text != "")
                {
                    // marrim ate se cka ka shenuar klienti ne kerkese
                    request = txtTeksti.Text;
                    Stream stream = client.GetStream();

                    // kthejme kerkesen nga String ne Byte per ta derguar tek serveri
                    byte[] byteRequest = Encoding.ASCII.GetBytes(request);
                    // e enkriptojme me celesin publik te serverit
                    byte[] ciphertexti = objRSAServer.Encrypt(byteRequest, true);

                    // dergojme tek serveri
                    stream.Write(ciphertexti, 0, ciphertexti.Length);

                    byte[] byteResponse = new byte[1024];
                    // gjatesia e stringut qe kthehet nga serveri
                    int responseSize = stream.Read(byteResponse, 0, 1024);
                    string response = "";
                    // krijojme nje byte array me gjatesi aq sa ka pergjigja e serverit
                    byte[] byteServerResponse = new byte[responseSize];
                    for (int i = 0; i < responseSize; i++)
                    {
                        byteServerResponse[i] = byteResponse[i];
                    }
                    // dekriptojme ate se cka na ka shenuar serveri duke perdorur celesin tone privat
                    byte[] byteDecryptedResponse = objRSAClient.Decrypt(byteServerResponse, true);
                    // kthejme nga bytes ne string
                    response = Encoding.ASCII.GetString(byteDecryptedResponse);
                    txtPergjigja.AppendText("» " + response + "\r\n———————————————————————————————————————————————————\r\n");
                    txtTeksti.Focus();
                    txtTeksti.Text = "";
                }
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void txtTeksti_KeyDown(object sender, KeyEventArgs e)
        {
            // nese shtypim ENTER dhe nuk shtypim SHIFT dergoje kerkesen
            if (e.KeyCode == Keys.Enter && Control.ModifierKeys != Keys.Shift)
            {
                btnDergo.PerformClick();
            }
        }

        private void txtTeksti_KeyUp(object sender, KeyEventArgs e)
        {
            // nese shtypim ENTER dhe nuk shtypim SHIFT pastroje textBoxin (pasi eshte derguar kerkesa)
            if (e.KeyCode == Keys.Enter && Control.ModifierKeys != Keys.Shift)
            {
                txtTeksti.Text = "";
            }
        }

     
