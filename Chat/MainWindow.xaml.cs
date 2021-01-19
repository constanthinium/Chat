using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Input;

namespace Chat
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        List<TcpClient> clients = new List<TcpClient>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void MenuItemClient_Click(object sender, RoutedEventArgs e)
        {
            InputDialog inputBox = new InputDialog("Enter IP (leave empty for loopback)");
            if (inputBox.ShowDialog() == true)
            {
                client = new TcpClient();
                IPAddress address = inputBox.input != "" ? IPAddress.Parse(inputBox.input) : IPAddress.Loopback;
                client.Connect(address, 80);
                Log("Connected to " + address);
                new Thread(() =>
                {
                    byte[] buffer = new byte[64];
                    while (true)
                    {
                        int bytesCount = client.Client.Receive(buffer);
                        Log(Encoding.UTF8.GetString(buffer, 0, bytesCount));
                    }
                }).Start();
            }
        }

        private void MenuItemServer_Click(object sender, RoutedEventArgs e)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 80);
            listener.Start();
            Log("Listener started");
            new Thread(() =>
            {
                while (true)
                {
                    TcpClient acceptedClient = listener.AcceptTcpClient();
                    clients.Add(acceptedClient);
                    IPAddress address = ((IPEndPoint)acceptedClient.Client.RemoteEndPoint).Address;
                    Log("Client accepted " + address);

                    new Thread(() =>
                    {
                        byte[] buffer = new byte[64];
                        while (true)
                        {
                            int bytesCount = acceptedClient.Client.Receive(buffer);
                            string receivedMessage = address + ": " + Encoding.UTF8.GetString(buffer, 0, bytesCount);
                            Log(receivedMessage);
                            foreach (TcpClient client in clients)
                                client.Client.Send(Encoding.UTF8.GetBytes(receivedMessage));
                        }
                    }).Start();
                }
            }).Start();
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                textBoxLogs.AppendText(message + '\n');
                textBoxLogs.ScrollToEnd();
            });
        }

        string GetLocalIPAddress()
        {
            using (UdpClient client = new UdpClient())
            {
                client.Connect("8.8.8.8", 80);
                return (client.Client.LocalEndPoint as IPEndPoint).Address.ToString();
            }
        }

        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
                client.Client.Send(Encoding.UTF8.GetBytes(textBoxMessage.Text));
            else
                foreach (TcpClient client in clients)
                    client.Client.Send(Encoding.UTF8.GetBytes("admin: " + textBoxMessage.Text));
            textBoxMessage.Clear();
        }

        private void TextBoxMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ButtonSend_Click(sender, e);
        }
    }
}
