using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Chat
{
    public partial class MainWindow : Window
    {
        TcpClient client;
        readonly List<TcpClient> clients = new List<TcpClient>();

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

                SocketAsyncState state = new SocketAsyncState(client.Client);
                client.Client.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize, SocketFlags.None, ClientReceiveCallback, state);
            }
        }

        private void ClientReceiveCallback(IAsyncResult ar)
        {
            try
            {
                SocketAsyncState state = (SocketAsyncState)ar.AsyncState;
                Log(Encoding.UTF8.GetString(state.buffer, 0, client.Client.EndReceive(ar)));
                client.Client.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize, SocketFlags.None, ClientReceiveCallback, state);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset)
                    Log($"Server {((IPEndPoint)client.Client.RemoteEndPoint).Address} disconnected");
            }
        }

        private void MenuItemServer_Click(object sender, RoutedEventArgs e)
        {
            TcpListener listener = new TcpListener(IPAddress.Any, 80);
            listener.Start();
            Log("Listener started");
            listener.BeginAcceptTcpClient(AcceptCallback, listener);
        }

        void AcceptCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient acceptedClient = listener.EndAcceptTcpClient(ar);
            clients.Add(acceptedClient);
            IPAddress address = ((IPEndPoint)acceptedClient.Client.RemoteEndPoint).Address;
            Log($"Client {address} accepted");

            SocketAsyncState state = new SocketAsyncState(acceptedClient.Client);
            acceptedClient.Client.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize, SocketFlags.None, ServerReceiveCallback, state);
            listener.BeginAcceptTcpClient(AcceptCallback, listener);
        }

        void ServerReceiveCallback(IAsyncResult ar)
        {
            SocketAsyncState state = (SocketAsyncState)ar.AsyncState;
            IPAddress address = ((IPEndPoint)state.socket.RemoteEndPoint).Address;

            try
            {
                string receivedMessage = address + ": " + Encoding.UTF8.GetString(state.buffer, 0, state.socket.EndReceive(ar));
                Log(receivedMessage);
                foreach (TcpClient client in clients)
                    client.Client.Send(Encoding.UTF8.GetBytes(receivedMessage));
                state.socket.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize, SocketFlags.None, ServerReceiveCallback, state);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Log($"Client {address} disconnected");

                    for (int i = clients.Count - 1; i >= 0; i--)
                        if (((IPEndPoint)clients[i].Client.RemoteEndPoint).Address == address)
                            clients.Remove(clients[i]);
                }
            }
        }

        private void Log(string message)
        {
            Dispatcher.Invoke(() =>
            {
                textBoxLogs.AppendText(message + '\n');
                textBoxLogs.ScrollToEnd();
            });
        }

        private void ButtonSend_Click(object sender, RoutedEventArgs e)
        {
            if (client != null)
                client.Client.Send(Encoding.UTF8.GetBytes(textBoxMessage.Text));
            else
            {
                string message = "server: " + textBoxMessage.Text;
                foreach (TcpClient client in clients)
                    client.Client.Send(Encoding.UTF8.GetBytes(message));
                Log(message);
            }
            textBoxMessage.Clear();
        }

        private void TextBoxMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ButtonSend_Click(sender, e);
        }
    }
}
