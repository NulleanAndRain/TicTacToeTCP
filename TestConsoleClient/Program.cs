using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestConsoleClient {
	#region Client
	//class Program {
	//	static void Main(string[] args) {
	//		Console.WriteLine("Hello World!");
	//	}
	//}

	class Client {
		TcpClient client;
		NetworkStream stream;
		Thread listeningThread;

		Queue<string> recievedData;

		private void Connect(string IP, string portStr, string name) {
			if (!string.IsNullOrWhiteSpace(IP) &&
				int.TryParse(portStr, out int port) &&
				string.IsNullOrWhiteSpace(name)) {
				try {

					//todo: connect to server

					client = new TcpClient(IP, port);
					stream = client.GetStream();

					listeningThread = new Thread(ReadData);
					listeningThread.Start();

					WriteData(name);

					//_ConnectionStatus.Content = "Connected";
					Console.WriteLine("Connected");

					while (true) {
						var msg = recievedData.Dequeue();
						if (msg != null) {
							//test_area.Content += Environment.NewLine + msg;
							Console.WriteLine(msg);
						}
						Thread.Sleep(16);
					}
				} catch (SocketException e) {
					Console.WriteLine($"SocketException: {e}");
				} catch (Exception e) {
					Console.WriteLine($"Exception: {e.Message}");
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
			//_Connect_Btn.Content = "Connect";
		}

	}
	#endregion

	class Program {
		static string userName;
		private const string host = "127.0.0.1";
		private const int port = 8888;
		static TcpClient client;
		static NetworkStream stream;

		static void Main(string[] args) {
			Console.Write("Введите свое имя: ");
			userName = Console.ReadLine();
			client = new TcpClient();
			try {
				client.Connect(host, port); //подключение клиента
				stream = client.GetStream(); // получаем поток

				string message = userName;
				byte[] data = Encoding.Unicode.GetBytes(message);
				stream.Write(data, 0, data.Length);

				// запускаем новый поток для получения данных
				Thread receiveThread = new Thread(new ThreadStart(ReceiveMessage));
				receiveThread.Start(); //старт потока
				Console.WriteLine("Добро пожаловать, {0}", userName);
				SendMessage();
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
			} finally {
				Disconnect();
			}
		}
		// отправка сообщений
		static void SendMessage() {
			Console.WriteLine("Введите сообщение: ");

			while (true) {
				string message = Console.ReadLine();
				byte[] data = Encoding.Unicode.GetBytes(message);
				stream.Write(data, 0, data.Length);
			}
		}
		// получение сообщений
		static void ReceiveMessage() {
			while (true) {
				try {
					byte[] data = new byte[64]; // буфер для получаемых данных
					StringBuilder builder = new StringBuilder();
					int bytes = 0;
					do {
						bytes = stream.Read(data, 0, data.Length);
						builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
					}
					while (stream.DataAvailable);

					string message = builder.ToString();
					Console.WriteLine(message);//вывод сообщения
				} catch {
					Console.WriteLine("Подключение прервано!"); //соединение было прервано
					Console.ReadLine();
					Disconnect();
				}
			}
		}

		static void Disconnect() {
			if (stream != null)
				stream.Close();//отключение потока
			if (client != null)
				client.Close();//отключение клиента
			Environment.Exit(0); //завершение процесса
		}
	}
}