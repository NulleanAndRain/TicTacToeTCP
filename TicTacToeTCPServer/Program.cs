using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace TicTacToeTCPServer
{
	class Program {
		static void Main(string[] args) {
			ServerObject server = new ServerObject();
			server.Listen("127.0.0.1", 8888);
		}

	}

	#region Server alt
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
					TcpClient tcpClient = listener.AcceptTcpClient();

					Client clientObject = new Client(tcpClient, this);
					Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
					clientThread.Start();

					clients.Add(clientObject);
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

		void Listen() {
			while (true) {

				Thread.Sleep(5);
			}
		}

		//public void Start() {
		//	listener.Start();
		//	listeningThread = new Thread(ReadData);
		//	listeningThread.Start();
		//	while (true) {
		//		if (recievedData.Count > 0) {
		//			Console.WriteLine(recievedData.Dequeue());
		//		}
		//		Thread.Sleep(16);
		//	}
		//}

		//void ReadData() {
		//	StringBuilder builder = new StringBuilder();
		//	while (true) {
		//		// todo: data reading
		//		try {
		//			byte[] buffer = new byte[256];
		//			int bytes;
		//			do {
		//				bytes = stream.Read(buffer, 0, buffer.Length);
		//				builder.Append(Encoding.Unicode.GetString(buffer, 0, bytes));
		//			} while (stream.DataAvailable);

		//			recievedData.Enqueue(builder.ToString());
		//			builder.Clear();
		//		} catch {
		//			//Disconnect();
		//		}

		//		Thread.Sleep(16);
		//	}
		//}
	}

    class Client {
        public Client(TcpClient client, Server server) { }
        public void Process() { }
        public void Close() { }
    }

	#endregion

	#region ServerObject

	public class ServerObject {
        static TcpListener tcpListener; // сервер для прослушивания
        List<ClientObject> clients = new List<ClientObject>(); // все подключения

        protected internal void AddConnection(ClientObject clientObject) {
            clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id) {
            // получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                clients.Remove(client);
        }
        // прослушивание входящих подключений
        protected internal void Listen(string IP, int port) {
            try {
                tcpListener = new TcpListener(IPAddress.Parse(IP), port);
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true) {
                    TcpClient tcpClient = tcpListener.AcceptTcpClient();

                    ClientObject clientObject = new ClientObject(tcpClient, this);
                    Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                    clientThread.Start();
                }
            } catch (Exception ex) {
                Console.WriteLine(ex.Message);
                Disconnect();
            }
        }

        // трансляция сообщения подключенным клиентам
        protected internal void BroadcastMessage(string message, string id) {
            byte[] data = Encoding.Unicode.GetBytes(message);
            for (int i = 0; i < clients.Count; i++) {
                if (clients[i].Id != id) // если id клиента не равно id отправляющего
                {
                    clients[i].Stream.Write(data, 0, data.Length); //передача данных
                }
            }
        }
        // отключение всех клиентов
        protected internal void Disconnect() {
            tcpListener.Stop(); //остановка сервера

            for (int i = 0; i < clients.Count; i++) {
                clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }
    }
    #endregion

    #region ClientObject

    public class ClientObject {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        string userName;
        TcpClient client;
        ServerObject server; // объект сервера

        public ClientObject(TcpClient tcpClient, ServerObject serverObject) {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }

        public void Process() {
            try {
                Stream = client.GetStream();
                // получаем имя пользователя
                string message = GetMessage();
                userName = message;

                message = userName + " вошел в чат";
                // посылаем сообщение о входе в чат всем подключенным пользователям
                server.BroadcastMessage(message, this.Id);
                Console.WriteLine(message);
                // в бесконечном цикле получаем сообщения от клиента
                while (true) {
                    try {
                        message = GetMessage();
                        message = String.Format("{0}: {1}", userName, message);
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                    } catch {
                        message = String.Format("{0}: покинул чат", userName);
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                }
            } catch (Exception e) {
                Console.WriteLine(e.Message);
            } finally {
                // в случае выхода из цикла закрываем ресурсы
                server.RemoveConnection(this.Id);
                Close();
            }
        }

        // чтение входящего сообщения и преобразование в строку
        private string GetMessage() {
            byte[] data = new byte[64]; // буфер для получаемых данных
            StringBuilder builder = new StringBuilder();
            int bytes = 0;
            do {
                bytes = Stream.Read(data, 0, data.Length);
                builder.Append(Encoding.Unicode.GetString(data, 0, bytes));
            }
            while (Stream.DataAvailable);

            return builder.ToString();
        }

        // закрытие подключения
        protected internal void Close() {
            if (Stream != null)
                Stream.Close();
            if (client != null)
                client.Close();
        }
    }
	#endregion
}
