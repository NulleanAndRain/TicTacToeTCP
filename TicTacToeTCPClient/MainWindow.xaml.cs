using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
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

		[DllImport("Kernel32")]
		public static extern void AllocConsole();
		[DllImport("Kernel32")]
		public static extern void FreeConsole();

		public MainWindow() {
			recievedData = new Queue<string>();
			InitializeComponent();
		}

		private void ButtonConnect(object sender, RoutedEventArgs e) {
			if (client == null) {
				Connect();
			} else {
				Disconnect();
			}
		}

		private void Connect() {
			try {
				_Connect_Btn.Content = "Disconnect";

				//todo: connect to server
				var port = int.Parse(_Port_text.Text);

				client = new TcpClient(_IP_text.Text, port);
				stream = client.GetStream();

				listeningThread = new Thread(ReadData);
				listeningThread.Start();

				WriteData(_Name_text.Text);

				_ConnectionStatus.Content = "Connected";

				while (true) {
					if (recievedData.Count > 0) {
						var msg = recievedData.Dequeue();
						test_area.Content += Environment.NewLine + msg;
						Console.WriteLine(msg);
					}
					Thread.Sleep(16);
				}
			} catch (SocketException e) {
				_ConnectionStatus.Content = $"SocketException: {e}";
				Console.WriteLine($"SocketException: {e}");
			} catch (Exception e) {
				Console.WriteLine($"Exception: {e.Message}");
				_ConnectionStatus.Content = $"Exception: {e.Message}";
			} finally {
				_Connect_Btn.Content = "Connect";
				Disconnect();
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
				} catch (Exception e) {
					Console.WriteLine("---- ex:" + e.ToString());
					Disconnect();
					Thread.CurrentThread.Interrupt();
				}

				Thread.Sleep(16);
			}
		}

		void WriteData(string data) {
			if (string.IsNullOrEmpty(data)) return;
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
			Console.WriteLine("disconnect");
			//_Connect_Btn.Content = "Connect";
		}

		private void SendMsgBtn(object sender, RoutedEventArgs e) {

		}
	}
}
