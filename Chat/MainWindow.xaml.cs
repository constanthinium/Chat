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
            InputDialog inputBox = new InputDialog(Properties.Resources.EnterIP);
            if (inputBox.ShowDialog() == true)
            {
                IPAddress address;
                if (string.IsNullOrWhiteSpace(inputBox.input))
                    address = IPAddress.Loopback;
                else if (IPAddress.TryParse(inputBox.input, out IPAddress parsedAddress))
                    address = parsedAddress;
                else
                {
                    MessageBox.Show(Properties.Resources.IncorrectIP, Properties.Resources.IncorrectInput,
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                client = new TcpClient();
                client.BeginConnect(address, 80, ConnectCallback, address);
                menu.IsEnabled = false;
                statusBarItem.Content = Properties.Resources.IncorrectIP;
            }
        }

        void ConnectCallback(IAsyncResult ar)
        {
            IPAddress address = (IPAddress)ar.AsyncState;
            Dispatcher.Invoke(() => statusBarItem.Content = string.Empty);

            try
            {
                client.EndConnect(ar);
                Log(string.Format(Properties.Resources.ConnectedTo, address));
                SocketAsyncState state = new SocketAsyncState(client.Client);
                client.Client.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize,
                    SocketFlags.None, ClientReceiveCallback, state);
                Dispatcher.Invoke(() =>
                {
                    gridMessaging.IsEnabled = true;
                    textBoxMessage.Focus();
                });
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    MessageBox.Show(string.Format(Properties.Resources.CannotConnectTo, address),
                        Properties.Resources.ServerUnavailable, MessageBoxButton.OK, MessageBoxImage.Error);
                    Dispatcher.Invoke(() => menu.IsEnabled = true);
                    client = null;
                }
            }
        }

        private void ClientReceiveCallback(IAsyncResult ar)
        {
            try
            {
                SocketAsyncState state = (SocketAsyncState)ar.AsyncState;
                Log(Encoding.UTF8.GetString(state.buffer, 0, client.Client.EndReceive(ar)));
                client.Client.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize,
                    SocketFlags.None, ClientReceiveCallback, state);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset)
                    Log(string.Format(Properties.Resources.ServerDisconnected,
                        ((IPEndPoint)client.Client.RemoteEndPoint).Address));
            }
        }

        private void MenuItemServer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, 80);
                listener.Start();
                Log(Properties.Resources.ServerStarted);
                listener.BeginAcceptTcpClient(AcceptCallback, listener);

                menu.IsEnabled = false;
                gridMessaging.IsEnabled = true;
                textBoxMessage.Focus();
            }
            catch (SocketException ex)
            {
                if (ex.SocketErrorCode == SocketError.AddressAlreadyInUse)
                    MessageBox.Show(Properties.Resources.AddressAlreadyInUse,
                        Properties.Resources.CannotStartServer,
                        MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        void AcceptCallback(IAsyncResult ar)
        {
            TcpListener listener = (TcpListener)ar.AsyncState;
            TcpClient acceptedClient = listener.EndAcceptTcpClient(ar);
            clients.Add(acceptedClient);
            IPAddress address = ((IPEndPoint)acceptedClient.Client.RemoteEndPoint).Address;
            Log(string.Format(Properties.Resources.ClientAccepted, address));

            SocketAsyncState state = new SocketAsyncState(acceptedClient.Client);
            acceptedClient.Client.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize,
                SocketFlags.None, ServerReceiveCallback, state);
            listener.BeginAcceptTcpClient(AcceptCallback, listener);
        }

        void ServerReceiveCallback(IAsyncResult ar)
        {
            SocketAsyncState state = (SocketAsyncState)ar.AsyncState;
            IPAddress address = ((IPEndPoint)state.socket.RemoteEndPoint).Address;

            try
            {
                string receivedMessage = address + ": " +
                    Encoding.UTF8.GetString(state.buffer, 0, state.socket.EndReceive(ar));
                Log(receivedMessage);
                foreach (TcpClient client in clients)
                    client.Client.Send(Encoding.UTF8.GetBytes(receivedMessage));
                state.socket.BeginReceive(state.buffer, 0, SocketAsyncState.bufferSize,
                    SocketFlags.None, ServerReceiveCallback, state);
            }
            catch (SocketException e)
            {
                if (e.SocketErrorCode == SocketError.ConnectionReset)
                {
                    Log(string.Format(Properties.Resources.ClientDisconnected, address));
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
            if (!string.IsNullOrWhiteSpace(textBoxMessage.Text))
            {
                if (client != null)
                    client.Client.Send(Encoding.UTF8.GetBytes(textBoxMessage.Text));
                else
                {
                    string message = Properties.Resources.Server + ": " + textBoxMessage.Text;
                    foreach (TcpClient client in clients)
                        client.Client.Send(Encoding.UTF8.GetBytes(message));
                    Log(message);
                }
                textBoxMessage.Clear();
            }
        }

        private void TextBoxMessage_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
                ButtonSend_Click(sender, e);
        }
    }
}
