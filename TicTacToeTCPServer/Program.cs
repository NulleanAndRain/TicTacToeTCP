using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using SimpleJSON;
//using CommonClasses;

namespace TicTacToeTCPServer
{
	class Program {
		static void Main(string[] args) {
			Server server = new Server("127.0.0.1", 8888);
			server.Start();
		}

	}

	#region Server alt
	public class Server {
		TcpListener listener;
		List<ClientObject> clients;

        List<Room> rooms;

		public Server(string IP, int port) {
			clients = new List<ClientObject>();
			listener = new TcpListener(IPAddress.Parse(IP), port);
            rooms = new List<Room>();

        }

		public void Start() {
			try {
				listener.Start();
				Console.WriteLine("Сервер запущен. Ожидание подключений...");

				while (true) {
					TcpClient tcpClient = listener.AcceptTcpClient();

                    ClientObject clientObject = new ClientObject(tcpClient, this);

                    clientObject.Init();
					clients.Add(clientObject);
					Thread.Sleep(5);
				}
			} catch (Exception ex) {
				Console.WriteLine(ex.Message);
				Disconnect();
			}
        }
        protected internal void AddConnection(ClientObject clientObject) {
            clients.Add(clientObject);
            findEmptyRoom().AddUser(clientObject);
        }
        protected internal void RemoveConnection(string id) {
            // получаем по id закрытое подключение
            ClientObject client = clients.FirstOrDefault(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null) {
                clients.Remove(client);
                var r = client.room;
                r.RemoveUser(client);
                if (r.isEmpty) {
                    rooms.Remove(r);
				}
            }
        }

        Room findEmptyRoom() {
            var r = rooms.Find(r => r.hasSpace);
            if (r == null) {
                r = new Room();
                rooms.Add(r);
			}
            return r;
		}

        protected internal void Disconnect() {
            listener.Stop(); //остановка сервера

            for (int i = 0; i < clients.Count; i++) {
                clients[i].Close(); //отключение клиента
            }
            Environment.Exit(0); //завершение процесса
        }

        protected internal void BroadcastMessage(string message, string id) {
            var sender = clients.Find(c => c.Id == id);
            sender.room.ProcessMessage(message, id);
        }
    }

    public class Room {
        public ClientObject client1;
        public ClientObject client2;
        public Server server1;

        public bool hasSpace => client1 == null || client2 == null;
        public bool isEmpty => client1 == null && client2 == null;

        bool inProcess;



        public void AddUser(ClientObject user) {
            if (client1 == null) {
                client1 = user;
                return;
            }
            if (client2 == null) {
                client2 = user;
            }
        }

        public void RemoveUser(ClientObject user) {
            if (client1 == user) {
                client1 = null;
			}
            if (client2 == user) {
                client2 = null;
			}
		}

        protected internal void ProcessMessage(string message, string id) {
            JSONNode json = JSON.Parse(message);
            if (!inProcess && json["type"] == "room_info") {
                //if (json["data"]["command"] == "start")

            }
            if (inProcess && json["type"] == "room_data") {

			}
        }
    }

	#endregion


    #region ClientObject

    public class ClientObject {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        string userName;
        TcpClient client;
        Server server; // объект сервера
        public Room room;

        public ClientObject(TcpClient tcpClient, Server serverObject) {
            Id = Guid.NewGuid().ToString();
            client = tcpClient;
            server = serverObject;
            serverObject.AddConnection(this);
        }

        public void Init() {
            Thread clientThread = new Thread(new ThreadStart(Process));
            clientThread.Start();
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
