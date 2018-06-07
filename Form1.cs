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


       private void btnImportKeys_Click(object sender, EventArgs e)
        {
            OpenFileDialog opf = new OpenFileDialog();
            if (opf.ShowDialog() == DialogResult.OK)
            {
                string path = opf.FileName;
                String strPrivateParameters = "";
                StreamReader sr = new StreamReader(path);
                strPrivateParameters = sr.ReadToEnd();
                sr.Close();
                try
                {
                    // tentojme te ia japim filen e lexuar objektit RSA (nese nuk eshte file i mire,
                    // kapet gabimi nga Catch, pra nese nuk eshte file xml qe permban celesa RSA)
                    objRSAClient.FromXmlString(strPrivateParameters);
                    // meqe ka mundesi qe te importohet vetem celesei publik, atehere pyesim a 
                    // permban ai xml file tagun <D> (celesin privat), meqe ky gabim nuk kapet 
                    // nga kodi i siperm
                    if (!strPrivateParameters.Contains("<D>"))
                    {
                        throw new Exception("Imported Public Key");
                    }
                    keysImported = true;
                    try
                    {
                        // krijojme lidhjen me serverin
                        client = new TcpClient();
                        client.Connect("127.0.0.1", 9999);
                        // ===========================SHKEMBIMI I CELESAVE===========================
                        // =================================FILLIMI==================================
                        // i bejme eksport tek objParameters vetem tagjet per celes publik
                        RSAParameters objParameters = objRSAClient.ExportParameters(false);
                        string publicKey = Convert.ToBase64String(objParameters.Exponent);
                        publicKey += "#" + Convert.ToBase64String(objParameters.Modulus);

                        Stream stream = client.GetStream();
                        // konvertojme celesin publik ne byte per ta derguar tek serveri
                        byte[] byteRequest = Encoding.ASCII.GetBytes(publicKey);
                        // i dergojme serverit celesin tone publik
                        stream.Write(byteRequest, 0, byteRequest.Length);

                        byte[] byteKeysResponse = new byte[1024];
                        // gjatesia e stringut qe kthehet nga serveri
                        int keysResponseSize = stream.Read(byteKeysResponse, 0, 1024);
                        string serversKeys = "";
                        for (int i = 0; i < keysResponseSize; i++)
                        {
                            serversKeys += Convert.ToChar(byteKeysResponse[i]);
                        }
                        // ndajme modulusin dhe eksponentin ne nje string array
                        String[] serversKeysArray = serversKeys.Split('#');
                        // ia shoqerojme objektit RSA (per mesazhet qe do tia dergojme serverit) 
                        // celesin publik te serverit
                        RSAParameters objParametersServer = objRSAServer.ExportParameters(true);
                        objParametersServer.Exponent = Convert.FromBase64String(serversKeysArray[0]);
                        objParametersServer.Modulus = Convert.FromBase64String(serversKeysArray[1]);
                        objRSAServer.ImportParameters(objParametersServer);
                        // ==================================FUNDI===================================
                        // ===========================SHKEMBIMI I CELESAVE===========================
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error..... " + ex.StackTrace);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Please import valid RSA keys!", "Error");
                }
            }
        }

        private void importPng_Click(object sender, EventArgs e)
        {
            btnImportKeys.PerformClick();
        }
    }
}
     
