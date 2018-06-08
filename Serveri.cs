using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Security.Cryptography;
using System.Windows.Forms;
using System.IO;

namespace Projekti2___Serveri
{
    public partial class Form1 : Form
    {
        private IPAddress ipAddress;
        private TcpListener objTcpListener;
        private Thread threadAccept;
        private Thread threadRespond;
        private Socket clientSocket;
        private bool keysImported = false;
        private bool serverStarted = false;
        private RSACryptoServiceProvider objRSAServer;
        private RSACryptoServiceProvider objRSAClients;


        public string RollTheDice()
        {
            int min = 1;
            int max = 6;
            Random rand = new Random();
            int kubi1 = rand.Next(1, 6);
            int kubi2 = rand.Next(1, 6);
            string pergjigja = "Rolling the dice...   Results:  " + kubi1.ToString() + " and " + kubi2.ToString();
            return pergjigja;
        }

        public string PickACard()
        {
            Random rand = new Random();
            String[] cards = {"Ace", "Two", "Three","Four","Five","Six","Seven","Eight","Nine","Ten","Jack","Queen","King","THE JOKER"};
            String[] suits = {"Spades", "Hearts", "Diamonds", "Clubs"};
            int cardSelection = rand.Next(0, 14);
            int suitSelection = rand.Next(0, 4);
            string pergjigja = "Your card: " + cards[cardSelection];
            if(cardSelection!=13)
            {
                pergjigja += " of " + suits[suitSelection];
            }
            return pergjigja;
        }
        
        public Form1()
        {
            InitializeComponent();
            btnStartServer.BackColor = Color.Transparent;
            btnStopServer.BackColor = Color.Transparent;
            btnImportKey.BackColor = Color.Transparent;
            connectionPng.BackColor = Color.Transparent;
            serverPng.BackColor = Color.Transparent;
            playPng.BackColor = Color.Transparent;
            stopPng.BackColor = Color.Transparent;
            importPng.BackColor = Color.Transparent;
            lblLog.BackColor = Color.Transparent;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // krijo objeketet me ane te konstruktorit
            objRSAServer = new RSACryptoServiceProvider();
            objRSAClients = new RSACryptoServiceProvider();
        }

        // 
        private void btnStartServer_Click(object sender, EventArgs e)
        {
            try
            {
                // nese nuk ka celesa te importuar ose nese serveri eshte i startuar 
                // mos vazhdo me funksionin
                if (!keysImported || serverStarted)
                {
                    MessageBox.Show("Please import your keys!");
                    return;
                }
                // ia caktojme nje IP adrese ketij serveri
                ipAddress = IPAddress.Parse("127.0.0.1");
                // ia ndajme ate IP adrese dhe nje port te caktuar
                objTcpListener = new TcpListener(ipAddress, 9999);
                // startojme serverin
                objTcpListener.Start();
                serverStarted = true;

                txtOutput.Text += "The local end point is: " + objTcpListener.LocalEndpoint;
                txtOutput.Text += "\r\nWaiting for a connection...";
                // fillo pranimin e klienteve
                threadAccept = new Thread(acceptClients);
                threadAccept.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error..... " + ex.StackTrace);
            }
        }

        private void acceptClients()
        {
            // vazhdimisht kerko nese ka klient te ri qe duan te qasen
            while(true)
            {
                try
                {
                    // prano kerkesen e klientit per tu lidhur 
                    clientSocket = objTcpListener.AcceptSocket();
                    Invoke((MethodInvoker)delegate
                    {
                        txtOutput.AppendText("\r\n*********************CONNECTION**********************");
                        txtOutput.AppendText("\r\nConnection accepted from: " + clientSocket.RemoteEndPoint);
                    });
                    // krijo nje thread qe te mirret me kerkesat e klientit
                    threadRespond = new Thread(new ParameterizedThreadStart(respondClient));
                    // startojme ate thread dhe si parameter ia japim soketin qe caktohet per ate klient
                    threadRespond.Start(clientSocket);
                }
                catch(Exception ex)
                {
                    clientSocket.Close();
                    break;
                }
            }
        }

        private void respondClient(Object clientS)
        {
            // kastojme ne menyre eksplicite soketin e klientit nga tipi Object ne Socket
            Socket clientSocketC = (Socket)clientS;
            // ==============================SHKEMBIMI I CELESAVE==============================
            // ====================================FILLIMI=====================================
            byte[] byteClientKeysReceived = new byte[1024];
            // ruajme sa eshte e gjate pergjigja dhe marrim celesin publik te klientit e e ruajme
            // ne byteClientKeysReceived
            int msgKeysReceiveSize = clientSocket.Receive(byteClientKeysReceived);
            String clientPublicKey = "";
            for (int i = 0; i < msgKeysReceiveSize; i++)
            {
                clientPublicKey += Convert.ToChar(byteClientKeysReceived[i]);
            }
            // split-im eksponentin dhe modulin ne nje string array (pasi jane te ndare me #)
            String[] clientArrayPublicKey = clientPublicKey.Split('#');
            // dergojme celesin tone publik tek klienti
            RSAParameters objParameters = objRSAServer.ExportParameters(false);
            string publicKeyServer = Convert.ToBase64String(objParameters.Exponent);
            publicKeyServer += "#" + Convert.ToBase64String(objParameters.Modulus);
            clientSocketC.Send(Encoding.ASCII.GetBytes(publicKeyServer));
            // =====================================FUNDI======================================
            // ==============================SHKEMBIMI I CELESAVE==============================
            try
            {          
                // perderisa klienti te jete i lidhur merr kerkesa nga ai the ktheji nje pergjigje
                while (clientSocketC.Connected)
                {
                    byte[] byteReceived = new byte[1024];
                    // ruajme sa eshte e gjate pergjigja
                    int msgReceiveSize = clientSocketC.Receive(byteReceived);
                    if (msgReceiveSize == 0)
                    {
                        break;
                    }

                    String clientRequest = "";
                    // krijojme nje byte array te ri me gjatesi aq sa eshte kerkesa e klientit
                    byte[] byteRequestReceived = new byte[msgReceiveSize];
                    for(int i=0; i<msgReceiveSize; i++)
                    {
                        byteRequestReceived[i] = byteReceived[i];
                    }
                    // dekriptojme kerkesen e klientit
                    byte[] byteTekstiDekriptuar = objRSAServer.Decrypt(byteRequestReceived, true);
                    // kthejme kerkesen e klientit nga Bytes ne String)
                    clientRequest = Encoding.ASCII.GetString(byteTekstiDekriptuar);
                    string toSend = "";
                    // perpunojme pergjigjen
                    switch (clientRequest.ToLower())
                    {
                        case "ipaddress":
                            toSend = "Your IP Address is: " + clientSocketC.RemoteEndPoint;
                            break;
                        case "protocol":
                            toSend = "The protocol you are using is: " + clientSocket.ProtocolType;
                            break;
                        case "localendpoint":
                            toSend = "Your local end point is: " + clientSocket.LocalEndPoint.ToString();
                            break;
                        case "time":
                            toSend = "Current time is: " + DateTime.Now.ToString("HH:mm:ss tt");
                            break;
                        case "rollthedice":
                            toSend = RollTheDice();
                            break;
                        case "pickacard":
                            toSend = PickACard();
                            break;
                        case "help":
                            toSend = "Requests: IPAddress, Protocol, LocalEndPoint, Time, RollTheDices and PickACard";
                            break;
                        
                        default:
                            toSend = "Your request is not valid!";
                            break;
                    }

                    // ia japim celesin publik te klientit (qe e morem me heret me split) objektit
                    // RSA qe eshte krijuar si i perbashket per te gjithe klientet, pra per cdo kerkese
                    // modifikojme ate objekt varesisht nga cili klient po i qasemi
                    RSAParameters objRSAClientParameters = objRSAClients.ExportParameters(true);
                    objRSAClientParameters.Exponent = Convert.FromBase64String(clientArrayPublicKey[0]);
                    objRSAClientParameters.Modulus = Convert.FromBase64String(clientArrayPublicKey[1]);
                    objRSAClients.ImportParameters(objRSAClientParameters);

                    // konvertojme nga string ne bytes
                    byte[] byteToSend = Encoding.ASCII.GetBytes(toSend);
                    // enkriptojme mesazhin qe do ti kthejme klientit
                    byte[] toSendEncrypted = objRSAClients.Encrypt(byteToSend, true);
                    // dergojme mesazhin
                    clientSocketC.Send(toSendEncrypted);
                    Invoke((MethodInvoker)delegate
                    {
                        txtOutput.AppendText("\r\n----------------------MESSAGE------------------------");
                        txtOutput.AppendText("\r\n" + clientSocketC.RemoteEndPoint + ": " + clientRequest);
                    });
                }
            }
            // nese ndodh ndonje gabim shkepute lidhjen me ate klient (por pranimi i klienteve te rinj vazhdon ende)
            catch (Exception ex)
            {
                try
                {
                    Invoke((MethodInvoker)delegate
                    {
                        txtOutput.AppendText("\r\nxxxxxxxxxxxxxxxxxxxxxxxERRORxxxxxxxxxxxxxxxxxxxxxxxxx");
                        txtOutput.AppendText("\r\nClient \"" + clientSocketC.RemoteEndPoint + "\" has ended the connection!");
                    });
                    clientSocketC.Close();
                }
                catch(Exception ex2)
                {

                }
            }
        }

        private string Quadratic(double v1, double v2, double v3)
        {
            throw new NotImplementedException();
        }

        // Shkeputi lidhjet 
        private void btnStopServer_Click(object sender, EventArgs e)
        {
            try
            {
                if (!serverStarted)
                    return;
                // ndaloje serverin
                clientSocket.Close();
                objTcpListener.Stop();
                serverStarted = false;
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error..... " + ex.StackTrace);
            }
        }

        // Kur te preket 'X' per te mbyllur formen shkeputi te gjitha lidhjet se pari dhe pastaj mbylle
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                // ndaloje serverin
                clientSocket.Close();
                objTcpListener.Stop();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error..... " + ex.StackTrace);
            }
            Application.Exit();
        }





private void btnImportKey_Click(object sender, EventArgs e)
        {
            OpenFileDialog opf = new OpenFileDialog();
            if(opf.ShowDialog() == DialogResult.OK)
            {
                string path = opf.FileName;
                String strPrivateParameters = "";
                StreamReader sr = new StreamReader(path);
                // lexojme filen e hapur deri ne fund
                strPrivateParameters = sr.ReadToEnd();
                sr.Close();
                try
                {
                    // tentojme te ia japim filen e lexuar objektit RSA (nese nuk eshte file i mire,
                    // kapet gabimi nga Catch, pra nese nuk eshte file xml qe permban celesa RSA)
                    objRSAServer.FromXmlString(strPrivateParameters);
                    // meqe ka mundesi qe te importohet vetem celesei publik, atehere pyesim a permban
                    // ai xml file tagun <D> (celesin privat), meqe ky gabim nuk kapet nga kodi i siperm
                    if (!strPrivateParameters.Contains("<D>"))
                    {
                        throw new Exception("Imported Public Key");
                    }
                    keysImported = true;
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Please import valid RSA keys!","Error");
                }
            }
        }
    }


}
