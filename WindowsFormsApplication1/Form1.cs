using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;
using System.Net.Sockets;
using System.Net;
using System.Diagnostics;

namespace WindowsFormsApplication1
{
    public partial class Form1 : Form
    {
        bool isClientMode = true;
        Thread tClient;
        Thread tServer;

        const int portClient = 21701;
        const int portServer = 21703;

        public Form1()
        {
            InitializeComponent();
            
            this.radioButton1.Checked = true;
        }

        private void backgoundWork()
        {
            if (isClientMode)
            {
                if (tClient != null)
                    return;

                tClient = new Thread(() =>
                {
                    try
                    {
                        using (UdpClient udp = new UdpClient(portClient))
                        {
                            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            while (true)
                            {
                                Byte[] bytes = udp.Receive(ref RemoteIpEndPoint);
                                string msg = Encoding.UTF8.GetString(bytes);

                                Invoke((MethodInvoker)(() =>
                                {
                                    switch (msg)
                                    {
                                        case "Give me five!":
                                            if (this.txtProgram.Text == "")
                                            {
                                                this.txtProgram.Text = "Got it! ";
                                                this.txtProgram.Text += msg;
                                            }

                                            IPHostEntry myEntry = Dns.GetHostEntry(Dns.GetHostName());
                                            for (int i = 0; i < myEntry.AddressList.Length; i++)
                                            {
                                                IPAddress ip;
                                                if (IPAddress.TryParse(myEntry.AddressList[i].ToString(), out ip))
                                                {
                                                    if (ip.AddressFamily == AddressFamily.InterNetwork)
                                                    {
                                                        string hostname = Dns.GetHostEntry(ip).HostName;
                                                        udpSend(IPAddress.Broadcast, ip.ToString() + " - " + hostname);
                                                    }
                                                }
                                            }
                                            break;

                                        case "START":
                                            this.txtProgram.Text = "Starting...";

                                            Process[] myProcesses1 = System.Diagnostics.Process.GetProcesses();
                                            bool isAlreadyRunning = false;
                                            foreach (System.Diagnostics.Process myProcess in myProcesses1)
                                            {
                                                if ("game" == myProcess.ProcessName)
                                                {
                                                    isAlreadyRunning = true;
                                                }
                                            }

                                            if (isAlreadyRunning == false)
                                            {
                                                ProcessStartInfo info = new ProcessStartInfo();
                                                info.FileName = "game.exe";
                                                info.WorkingDirectory = @".\";
                                                info.WindowStyle = ProcessWindowStyle.Hidden;
                                                try
                                                {
                                                    Process.Start(info);
                                                }
                                                catch
                                                {

                                                }
                                            }
                                            break;

                                        case "STOP":
                                            this.txtProgram.Text = "Stoping...";
                                            Process[] myProcesses = System.Diagnostics.Process.GetProcesses();
                                            foreach (System.Diagnostics.Process myProcess in myProcesses)
                                            {
                                                if ("game" == myProcess.ProcessName)
                                                {
                                                    myProcess.Kill();
                                                }
                                            }
                                            break;

                                        case "SHUTDOWN":
                                            this.txtProgram.Text = "Shutdown...";
                                            Process.Start("shutdown.exe", "-s -t 10");
                                            break;

                                        default:
                                            break;
                                    }
                                }));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (this.txtProgram.Text == "")
                        {
                            this.txtProgram.Text = ex.ToString();
                        }
                    }
                });

                tClient.IsBackground = true;
                tClient.Start();
            }
            else
            {
                if (tServer != null)
                {
                    return;
                }

                tServer = new Thread(() =>
                {
                    try
                    {
                        using (UdpClient udp = new UdpClient(portServer))
                        {
                            IPEndPoint RemoteIpEndPoint = new IPEndPoint(IPAddress.Any, 0);
                            while (true)
                            {
                                Byte[] bytes = udp.Receive(ref RemoteIpEndPoint);
                                string msg = Encoding.UTF8.GetString(bytes);
                                
                                Invoke((MethodInvoker)(() =>
                                {
                                    IPAddress ip;
                                    //if (IPAddress.TryParse(msg, out ip))
                                    {
                                        //if (ip.AddressFamily == AddressFamily.InterNetwork)
                                        {
                                            if (checkedListClients.Items.Contains(msg) == false)
                                            {
                                                // got IP address, now
                                                this.checkedListClients.Items.Add(msg);
                                            }
                                        }
                                    }
                                }));
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (this.txtProgram.Text == "")
                        {
                            this.txtProgram.Text = ex.ToString();
                        }
                    }
                });

                tServer.IsBackground = true;
                tServer.Start();
            }
        }

        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            isClientMode = this.radioButton1.Checked;

            this.btnBrowser.Enabled = isClientMode;
            this.btnTest.Enabled = isClientMode;

            this.btnScan.Enabled = !isClientMode;
            this.checkedListClients.Enabled = !isClientMode;
            this.btnStart.Enabled = !isClientMode;
            this.btnStop.Enabled = !isClientMode;
            this.btnShutdown.Enabled = !isClientMode;
            
            backgoundWork();
        }

        static int counter = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            if (counter > 100)    // 100s
            {
                this.progressBarScan.Visible = false;
                this.progressBarScan.Value = 0;
                this.timer1.Enabled = false;
                this.btnScan.Enabled = true;
                
                return;
            }
            if (counter % 10 == 0)
            {
                udpSend(IPAddress.Broadcast, "Give me five!");
            }
            this.progressBarScan.Value = counter;
            counter += 1;
        }

        private void btnScan_Click(object sender, EventArgs e)
        {
            counter = 0;

            this.btnScan.Enabled = false;
            this.timer1.Enabled = true;
            this.progressBarScan.Visible = true;
        }

        private void udpSend(IPAddress ip, String msg)
        {
            Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            IPEndPoint iep;
            if (isClientMode)
            {
                iep = new IPEndPoint(ip, portServer);
            }
            else
            {
                iep = new IPEndPoint(ip, portClient);
            }

            socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.Broadcast, 1);
            byte[] buffer = Encoding.UTF8.GetBytes(msg);
            socket.SendTo(buffer, iep);
            socket.Close();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            if (!isClientMode)
            {
                for (int i = 0; i < checkedListClients.Items.Count; i++)
                {
                    if (checkedListClients.GetItemChecked(i))
                    {
                        String ipString = checkedListClients.Items[i].ToString();
                        int endIndex = ipString.IndexOf(" - ");
                        ipString = ipString.Substring(0, endIndex);
                        IPAddress ip;
                        IPAddress.TryParse(ipString, out ip);
                        udpSend(ip, "START");
                    }
                }
            }
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            if (!isClientMode)
            {
                for (int i = 0; i < checkedListClients.Items.Count; i++)
                {
                    if (checkedListClients.GetItemChecked(i))
                    {
                        String ipString = checkedListClients.Items[i].ToString();
                        int endIndex = ipString.IndexOf(" - ");
                        ipString = ipString.Substring(0, endIndex);
                        IPAddress ip;
                        IPAddress.TryParse(ipString, out ip);
                        udpSend(ip, "STOP");
                    }
                }
            }
        }

        private void btnShutdown_Click(object sender, EventArgs e)
        {
            if (!isClientMode)
            {
                for (int i = 0; i < checkedListClients.Items.Count; i++)
                {
                    if (checkedListClients.GetItemChecked(i))
                    {
                        String ipString = checkedListClients.Items[i].ToString();
                        int endIndex = ipString.IndexOf(" - ");
                        ipString = ipString.Substring(0, endIndex);
                        IPAddress ip;
                        IPAddress.TryParse(ipString, out ip);
                        udpSend(ip, "SHUTDOWN");
                    }
                }
            }
        }

        private void checkedListClients_MouseMove(object sender, MouseEventArgs e)
        {
           // this.checkedListClients.SelectedIndex = 
        }

        private void chkAll_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < this.checkedListClients.Items.Count; i++)
            {
                this.checkedListClients.SetItemChecked(i, this.chkAll.Checked);
            }
        }

        private void chkSwitch_CheckedChanged(object sender, EventArgs e)
        {
            for (int i = 0; i < this.checkedListClients.Items.Count; i++)
            {
                bool check = this.checkedListClients.GetItemChecked(i);
                this.checkedListClients.SetItemChecked(i, !check);
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
