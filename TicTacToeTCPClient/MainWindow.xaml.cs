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
using System.Windows.Threading;

namespace TicTacToeTCPClient {
	/// <summary>
	/// Логика взаимодействия для MainWindow.xaml
	/// </summary>
	public partial class MainWindow : Window {
		TcpClient client;
		NetworkStream stream;
		Thread listeningThread;

		[DllImport("Kernel32")]
		public static extern void AllocConsole();
		[DllImport("Kernel32")]
		public static extern void FreeConsole();

		Brush transp;
		Brush green;

		public MainWindow() {
			Color _tr = Color.FromArgb(0, 0, 0, 0);
			transp = new SolidColorBrush(_tr);
			Color _gr = Color.FromRgb(142, 234, 42);
			green = new SolidColorBrush(_gr);
			InitializeComponent();

			void close(object s, EventArgs e) {
				Disconnect();
			}
			Closed += close;
			_usr1.Background = transp;
			_usr2.Background = transp;

			_WinnerText.Content = "";
			_WinnerText.Background = transp;

			SetFieldSize(3);
		}


		void onDisconnect() {
			void updateUI() {
				_ConnectionStatus.Content = "Disconnected";
				_Connect_Btn.Content = "Connect";
				_usr1.Content = "";
				_usr2.Content = "";
				_usr1.Background = transp;
				_usr2.Background = transp;
				test_area.Content = "";

				_WinnerText.Content = "";
			}
			_ConnectionStatus.Dispatcher.Invoke(updateUI);
		}

		void addText(string msg) {
			void update() {
				test_area.Content += Environment.NewLine + msg;
			}
			test_area.Dispatcher.Invoke(update);
		}
		void winnerText(string txt, bool isGreen) {
			void updTxt() {
				_WinnerText.Content = txt;
			}
			_WinnerText.Dispatcher.Invoke(updTxt);

			void updCol() {
				if (isGreen) {
					_WinnerText.Background = green;
				} else {
					_WinnerText.Background = transp;
				}
			}
			_WinnerText.Background.Dispatcher.Invoke(updCol);
		}

		void processCmd(string command) {
			var args = command.Split(' ');
			var cmd = args[0];
			if (cmd == "//msg") {
				addText(command.Remove(0, 6));
				return;
			}
			if (cmd == "//usr") {

				void updateUser1() {
					_usr1.Content = args[1];
					void updBG() {
						if (args[2] == "1") {
							_usr1.Background = green;
						} else {
							_usr1.Background = transp;
						}
					}
					_usr1.Background.Dispatcher.Invoke(updBG);
				}
				_usr1.Dispatcher.Invoke(updateUser1);

				void updateUser2() {
					_usr2.Content = args[3];
					void updBG() {
						if (args[4] == "1") {
							_usr2.Background = green;
						} else {
							_usr2.Background = transp;
						}
					}
					_usr2.Background.Dispatcher.Invoke(updBG);
				}
				_usr2.Dispatcher.Invoke(updateUser2);
				return;
			}
			if (cmd == " //start") {
				addText("---- game started ----");
			}

			if (cmd == "//field") {
				addText("game data:");
				addText(args[1] + Environment.NewLine);
				updateField(args[1]);
			}
			if (cmd == "//wnr") {
				winnerText("Winner: " + args[1], args[2] == "1");
			}
			if (cmd == "//rd") {
				void update() {
					SetFieldSize(Int32.Parse(args[1]));
				}
				_Field.Dispatcher.Invoke(update);
			}
			if (cmd == "//cur") {
				winnerText("Player turn: " + args[1], args[2] == "1");
			}
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

				var port = int.Parse(_Port_text.Text);

				client = new TcpClient(_IP_text.Text, port);
				stream = client.GetStream();

				listeningThread = new Thread(ReadData);
				listeningThread.Start();

				WriteData(_Name_text.Text);

				_ConnectionStatus.Content = "Connected";
			} catch (SocketException e) {
				_ConnectionStatus.Content = $"SocketException: {e}";
			} catch (Exception e) {
				_ConnectionStatus.Content = $"Exception: {e.Message}";
			}
		}

		void ReadData() {
			StringBuilder builder = new StringBuilder();
			while (client!= null && client.Connected) {
				try {
					byte[] buffer = new byte[256];
					int bytes;
					while (stream.DataAvailable) {
						bytes = stream.Read(buffer, 0, buffer.Length);
						builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
					}
					var str = builder.ToString();
					builder.Clear();
					if (string.IsNullOrEmpty(str)) continue;

					var cmds = str.Split(new string[] {"//"}, 16, StringSplitOptions.RemoveEmptyEntries);
					foreach (var cmd in cmds) {
						processCmd("//" + cmd);
					}
				} catch (Exception e) {
					Console.WriteLine("---- ex:" + e.ToString());
					Disconnect();
					Thread.CurrentThread.Interrupt();
				}

				try {
					Thread.Sleep(16);
				} catch { }
			}
		}

		void WriteData(string data) {
			if (string.IsNullOrEmpty(data)) return;
			try {
				if (stream == null) {
					if (data != "\\disconnect")
						Disconnect();
					return;
				}
				if (stream.CanWrite) {
					byte[] bytes = Encoding.Unicode.GetBytes(data);
					stream.Write(bytes, 0, bytes.Length);
				}
			} catch {
				if (data != "\\disconnect")
					Disconnect();
			}
		}

		private void Disconnect() {
			if (client != null) {
				WriteData("//msg disconnceting...");
				WriteData("\\disconnect");
				stream.Close();
				client.Close();
				client = null;
				stream = null;

				listeningThread.Interrupt();
				listeningThread = null;
				onDisconnect();
			}
		}

		private void SendMsgBtn(object sender, RoutedEventArgs e) {
			var txt = testInput.Text;
			testInput.Text = "";
			if (string.IsNullOrEmpty(txt)) return;
			addText(txt);
			WriteData(txt);
		}

		private void BtnSwap(object sender, RoutedEventArgs e) {
			WriteData("//swp");
		}

		private void BtnStart(object sender, RoutedEventArgs e) {
			WriteData("//start");
		}

		List<Button> btns = new List<Button>();
		private void SetFieldSize(int size) {
			_Field.Children.Clear();
			btns.Clear();
			for (int i = 0; i < size; i++) {
				var col = new StackPanel();
				col.Width = 50;
				col.VerticalAlignment = VerticalAlignment.Center;
				col.HorizontalAlignment = HorizontalAlignment.Center;
				for (int j = 0; j < size; j++) {
					var btn = new Button();
					var cmd = $"//gm {j} {i}";
					void onClick(object sender, RoutedEventArgs e) {
						WriteData(cmd);
					}
					btn.Content = "";
					btn.Name = $"_{j}_{i}";
					btn.Click += onClick;
					btn.Width = 40;
					btn.Height = 40;
					btn.Margin = new Thickness(5);
					btn.HorizontalAlignment = HorizontalAlignment.Center;
					btn.VerticalAlignment = VerticalAlignment.Center;
					col.Children.Add(btn);
					btns.Add(btn);
				}
				_Field.Children.Add(col);
			}
		}

		void updateField(string data) {
			var rows = data.Split('|');
			for (int i = 0; i < rows.Length; i++) {
				var cells = rows[i].Split(',');
				for (int j = 0; j < cells.Length; j++) {
					var btn = btns.Find(b => b.Name == $"_{j}_{i}");
					void upd() {
						btn.Content = cells[j];
					}
					btn.Dispatcher.Invoke(upd);
				}
				addText(rows[i]);
			}
		}


		private void Btn_size3(object sender, RoutedEventArgs e) {
			WriteData("//sz 3");
		}

		private void Btn_size4(object sender, RoutedEventArgs e) {
			WriteData("//sz 4");
		}

		private void Btn_size5(object sender, RoutedEventArgs e) {
			WriteData("//sz 5");
		}
	}
}
