using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TestConsoleClient {
	class Program {
		static void Main(string[] args) {
			Client client = new Client();
			Console.Write("Enter name: ");
			var name = Console.ReadLine();
			Console.WriteLine("Connecting...");
			client.Connect("127.0.0.1", 8888, name);
		}
	}

	class Client {
		TcpClient client;
		NetworkStream stream;
		Thread listeningThread;
		Thread consoleWritingThread;

		Queue<string> recievedData;

		public Client() {
			recievedData = new Queue<string>();
		}

		public void Connect(string IP, int port, string name) {
			try {

				//todo: connect to server

				using (client = new TcpClient(IP, port)) {
					stream = client.GetStream();

					listeningThread = new Thread(ReadData);
					listeningThread.IsBackground = true;
					listeningThread.Start();

					consoleWritingThread = new Thread(WriteReceivedData);
					consoleWritingThread.IsBackground = true;
					consoleWritingThread.Start();

					WriteData(name);

					//_ConnectionStatus.Content = "Connected";
					Console.WriteLine("Connected");

					while (client != null && client.Connected) {
						var msg = Console.ReadLine();
						WriteData(msg);
						try {
							Thread.Sleep(16);
						} catch { }
					}
				}
				Console.WriteLine("end reading data");
			} catch (SocketException e) {
				Console.WriteLine($"SocketException: {e}");
			} catch (Exception e) {
				Console.WriteLine($"Exception: {e.Message}");
			} finally {
				Disconnect();
			}
		}

		void WriteReceivedData() {
			while (true) {
				if (recievedData.Count > 0) {
					var msg = recievedData.Dequeue();
					//test_area.Content += Environment.NewLine + msg;
					Console.WriteLine(msg);
				}
				try {
					Thread.Sleep(16);
				} catch { }
			}
		}

		void ReadData() {
			StringBuilder builder = new StringBuilder();
			while (client != null && client.Connected) {
				try {
					byte[] buffer = new byte[256];
					int bytes;
					do {
						bytes = stream.Read(buffer, 0, buffer.Length);
						builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
					} while (stream.DataAvailable);

					var str = builder.ToString();
					if (string.IsNullOrEmpty(str)) continue;

					recievedData.Enqueue(str);
					builder.Clear();
				} catch {
					//Console.WriteLine("disconnect at reading");
					Disconnect();
					break;
				}
				try {
					Thread.Sleep(16);
				} catch { }
			}
		}

		public void WriteData(string data) {
			if (data == "dis") {
				Disconnect();
				return;
			}
			try {
				if (stream.CanWrite) {
					byte[] bytes = Encoding.Unicode.GetBytes(data);
					stream.Write(bytes, 0, bytes.Length);
				}
			} catch {
				//Console.WriteLine("disconnect at writing");
				Disconnect();
			}
		}

		public void Disconnect() {
			if (client != null) {
				WriteData("\\disconnect");
				stream.Close();
				client.Close();

				client = null;
				stream = null;
				consoleWritingThread.Interrupt();
				consoleWritingThread = null;

				listeningThread.Interrupt();
				listeningThread = null;
				Console.WriteLine("Disconnected");

				Console.WriteLine("Press any key to exit");
				Console.ReadKey();
				Environment.Exit(0);
			}
		}

	}
}