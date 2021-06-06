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

		private void Button_Click(object sender, RoutedEventArgs e) {
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

		private void SendMsgBtn(object sender, RoutedEventArgs e) {

		}

		private void OpenConsoleBtn(object sender, RoutedEventArgs e) {
			AllocConsole();
		}

		private void CloseConsoleBtn(object sender, RoutedEventArgs e) {
			FreeConsole();
		}
	}
}
