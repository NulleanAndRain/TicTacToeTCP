using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TicTacToeTCPServer
{
	class Program {
		static void Main(string[] args) {
			Server server = new Server("127.0.0.1", 8888);
			server.Start();
		}

	}

	class Server {
		TcpListener listener;
		List<Client> clients;

		public Server(string IP, int port) {
			clients = new List<Client>();
			listener = new TcpListener(IPAddress.Parse(IP), port);
		}

		public void Start() {
			try {
				listener.Start();
				Console.WriteLine("Сервер запущен. Ожидание подключений...");

				while (true) {
					//TcpClient tcpClient = listener.AcceptTcpClient();

					//Client clientObject = new Client(tcpClient, this);
					//Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
					//clientThread.Start();
					Thread.Sleep(5);
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Disconnect();
			}
		}

		void Disconnect() {
			listener.Stop(); //остановка сервера
			foreach (var client in clients) {
				client.Close();
			}
			Environment.Exit(0); //завершение процесса
		}

		/*
		public void Start() {
			listener.Start();
			listeningThread = new Thread(ReadData);
			listeningThread.Start();
			while (true) {
				if (recievedData.Count > 0) {
					Console.WriteLine(recievedData.Dequeue());
				}
				Thread.Sleep(16);
			}
		}

		/*
		void ReadData() {
			StringBuilder builder = new StringBuilder();
			while (true) {
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
					//Disconnect();
				}

				Thread.Sleep(16);
			}
		}*/
	}

	class Client {
		public Client(TcpClient client, Server server)
        {

        }
		public void Process()
        {

        }
		public void Close()
        {

        }
	}
}
