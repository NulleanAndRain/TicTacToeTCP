using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace TicTacToeTCPClient {
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		TcpClient client;
		NetworkStream stream;
		Thread listeningThread;

		Queue<string> recievedData;

		public MainWindow() {
			recievedData = new Queue<string>();
			InitializeComponent();
            
        }

		private void Button_Click(object sender, RoutedEventArgs e) {
			//var _name = _Name_text.Text;
			//var _IP = _IP_text.Text;
   //         int _port = int.Parse(_Port_text.Text);
            //Connect(_IP, _port ,_name);
            if (client == null) {
				Connect();
			} else {
				Disconnect();
			}
		}

		private void Connect() {
			if (!string.IsNullOrWhiteSpace(_IP_text.Text) &&
				int.TryParse(_Port_text.Text, out int port) &&
				string.IsNullOrWhiteSpace(_Name_text.Text)) {
				try {
					_Connect_Btn.Content = "Disconnect";

					//todo: connect to server

					client = new TcpClient(_IP_text.Text, port);
					stream = client.GetStream();

					listeningThread = new Thread(ReadData);
					listeningThread.Start();

					WriteData(_Name_text.Text);

					_ConnectionStatus.Content = "Connected";

					while (true) {
						var msg = recievedData.Dequeue();
						if (msg != null) {
							test_area.Content += Environment.NewLine + msg;
						}
						Thread.Sleep(16);
					}
				} catch (SocketException e) {
					_ConnectionStatus.Content = $"SocketException: {e}";
				} catch (Exception e) {
					_ConnectionStatus.Content = $"Exception: {e.Message}";
				} finally {
					Disconnect();
				}
			}
		}

		void ReadData() {
			StringBuilder builder = new StringBuilder();
			while (client.Connected) {
				// todo: data reading
				try {
					byte[] buffer = new byte[256];
					int bytes;
					do {
						bytes = stream.Read(buffer, 0, buffer.Length);
						builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
					} while (stream.DataAvailable);

					recievedData.Enqueue(builder.ToString());
					builder.Clear();
				} catch {
					Disconnect();
				}

				Thread.Sleep(16);
			}
		}

		void WriteData(string data) {
			if (stream != null && stream.CanWrite) {
				byte[] bytes = Encoding.Unicode.GetBytes(data);
				stream.Write(bytes, 0, bytes.Length);
			}
		}

		private void Disconnect() {
			if (client != null) {
				// todo: disconect from server
				stream?.Close();
				client.Close();
				client = null;
				stream = null;

				listeningThread?.Interrupt();
				listeningThread = null;
			}
			_Connect_Btn.Content = "Connect";
		}

		private void Button_Click_1(object sender, RoutedEventArgs e) {
			//var IP = _IP_text.Text;
			//var Port = _Port_text.Text;
			////Application.Run(new TestConsole);
			//System.Diagnostics.Process.Start("TestConsoleClient.csproj");
			//this.Close();
		}

        private void _Port_text_TextChanged(object sender, TextChangedEventArgs e)
        {
			///удалить этот метод
        }
    }

	public class Client
	{
		TcpClient client;
		NetworkStream stream;
		Thread listeningThread;
		Thread consoleWritingThread;

		Queue<string> recievedData;

		public Client()
		{
			recievedData = new Queue<string>();
		}

		public bool Connect(string IP, int port, string name)
		{
			try
			{

				//todo: connect to server

				client = new TcpClient(IP, port);
				stream = client.GetStream();

				listeningThread = new Thread(ReadData);
				listeningThread.Start();

				consoleWritingThread = new Thread(WriteReceivedData);
				consoleWritingThread.Start();

				WriteData(name);

				//_ConnectionStatus.Content = "Connected";
				Console.WriteLine("Connected");

				while (client.Connected)
				{
					var msg = Console.ReadLine();
					WriteData(msg);
				}

				return true;
			}
			catch (SocketException e)
			{
				Console.WriteLine($"SocketException: {e}");
			}
			catch (Exception e)
			{
				Console.WriteLine($"Exception: {e.Message}");
			}
			finally
			{
				Disconnect();
			}
			return false;
		}

		void WriteReceivedData()
		{
			while (true)
			{
				if (recievedData.Count > 0)
				{
					var msg = recievedData.Dequeue();
					//test_area.Content += Environment.NewLine + msg;
					Console.WriteLine(msg);
				}
				Thread.Sleep(16);
			}
		}

		void ReadData()
		{
			StringBuilder builder = new StringBuilder();
			while (client.Connected)
			{
				// todo: data reading
				try
				{
					byte[] buffer = new byte[256];
					int bytes;
					do
					{
						bytes = stream.Read(buffer, 0, buffer.Length);
						builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
					} while (stream.DataAvailable);

					recievedData.Enqueue(builder.ToString());
					builder.Clear();
				}
				catch
				{
					Disconnect();
				}

				Thread.Sleep(16);
			}
		}

		public void WriteData(string data)
		{
			if (stream != null && stream.CanWrite)
			{
				byte[] bytes = Encoding.Unicode.GetBytes(data);
				stream.Write(bytes, 0, bytes.Length);
			}
		}

		public void Disconnect()
		{
			if (client != null)
			{
				// todo: disconect from server
				stream?.Close();
				client.Close();
				client = null;
				stream = null;

				listeningThread?.Interrupt();
				listeningThread = null;
			}
			//_Connect_Btn.Content = "Connect";
		}

	}
}
