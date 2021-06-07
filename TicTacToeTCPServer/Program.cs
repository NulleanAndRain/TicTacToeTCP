﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
//using CommonClasses;

namespace TicTacToeTCPServer
{
	class Program {
		static void Main(string[] args) {
            Console.Write("Enter IP: ");
            var ip = Console.ReadLine();
            if (string.IsNullOrEmpty(ip)) ip = "127.0.0.1";
            Server server = new Server(ip, 8888);
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
                    if (listener.Pending()) {
                        TcpClient tcpClient = listener.AcceptTcpClient();

                        ClientObject clientObject = new ClientObject(tcpClient, this);
                        Thread clientThread = new Thread(new ThreadStart(clientObject.Process));
                        clientThread.Start();
                    }
                    try {
                        Thread.Sleep(5);
                    } catch { }
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
            ClientObject client = clients.Find(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null) {
                clients.Remove(client);
                var r = client.room;
                if (r == null) return;
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
            if (sender.room != null)
                sender.room.ProcessMessage(message, id);
        }
    }

	#endregion

	#region Room

	public class Room {
        public ClientObject client1;
        public ClientObject client2;
        public Server server1;

        public bool hasSpace => client1 == null || client2 == null;
        public bool isEmpty => client1 == null && client2 == null;

        void sendMessage(string message, string exceptId = null) {
            if (client1 != null && client1.Id != exceptId) client1.SendMessage(message);
            if (client2 != null && client2.Id != exceptId) client2.SendMessage(message);
        }

        public void AddUser(ClientObject user) {
            if (client1 == null) {
                client1 = user;
                user.room = this;
            } else if (client2 == null) {
                client2 = user;
                user.room = this;
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

        ClientObject getById(string id) {
            if (client1 != null && client1.Id == id) return client1;
            if (client2 != null && client2.Id == id) return client2;
            return null;
        }

        void swapUsers() {
            var tmp = client1;
            client1 = client2;
            client2 = tmp;
		}

        string usrData(string id) {
            StringBuilder b = new StringBuilder("//usr ");

            if (client1 != null) {
                string usr1 = client1.userName;
                int isUsr1 = client1.Id == id ? 1 : 0;
                b.Append(usr1).Append(" ").Append(isUsr1).Append(" ");
            } else {
                b.Append(" 0 ");
			}
            if (client2 != null) {
                string usr2 = client2.userName;
                int isUsr2 = client2.Id == id ? 1 : 0;
                b.Append(usr2).Append(" ").Append(isUsr2);
            } else {
                b.Append(" 0");
            }
            return b.ToString();
        }

        void sendUsrData() {
            if (client1 != null) {
                client1.SendMessage(usrData(client1.Id));
            }
            if (client2 != null) {
                client2.SendMessage(usrData(client2.Id));
            }
        }

        //game data
        int size = 3;
        int rowSize = 3;
        bool started = false;
        string[,] field;
        string currId;

        string sendRoomData() {
            return $"//rd {size}";
		}
        void start() {
            if (client1 != null && client2 != null) {
                sendMessage("//start");
                started = true;
                field = new string[size, size];
                currId = client1.Id;
            }

        }

        string otherId(string id) {
            if (client1.Id == id) return client2.Id;
            return client1.Id;
		}

        void process(string id, string n1, string n2) {
            if(currId == id) {
                var i1 = int.Parse(n1);
                var i2 = int.Parse(n2);
                if (string.IsNullOrEmpty(field[i1, i2])) {
                    field[i1, i2] = currId;
                    sendFieldData();
                    check();
                    currId = otherId(id);
                }
			}
		}

        void sendFieldData() {
            StringBuilder b = new StringBuilder("//field ");

            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size; j++) {
                    var str = "";
                    if (!string.IsNullOrEmpty(field[i, j])) {
                        var usr = getById(field[i, j]);
                        if (usr == client1) str = "X";
                        else str = "O";
                    }
                    b.Append(str).Append(",");
                }
                b.Remove(b.Length - 1, 1);
                b.Append("|");
            }
            b.Remove(b.Length - 1, 1);

            //todo: get field data
            sendMessage(b.ToString());
        }

        void check() {
            //rows
            int inRow;
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size - rowSize; j++) {
                    string id = field[i, j];
                    inRow = 1;
                    for (int d = 1; d < rowSize; d++) {
                        if (field[i, j + d] != id) {
                            break;
						}
                        inRow++;
                        if (inRow == rowSize) {
                            showWinner(id);
                            return;
                        }
                    }
				}
            }

            //columns
            for (int i = 0; i < size; i++) {
                for (int j = 0; j < size - rowSize; j++) {
                    string id = field[j, i];
                    inRow = 1;
                    for (int d = 1; d < rowSize; d++) {
                        if (field[j + d, i] != id) {
                            break;
                        }
                        inRow++;
                        if (inRow == rowSize) {
                            showWinner(id);
                            return;
                        }
                    }
                }
            }

            //diagonal
            for (int i = 0; i < size - rowSize; i++) {
                for (int j = 0; j < size - rowSize; j++) {
                    string id = field[i, j];
                    inRow = 1;
                    for (int d = 1; d < rowSize; d++) {
                        if (field[i + d, j + d] != id) {
                            break;
                        }
                        inRow++;
                        if (inRow == rowSize) {
                            showWinner(id);
                            return;
                        }
                    }
                }
            }
        }

        void showWinner(string id) {
            sendMessage($"//wnr {getById(id).userName}");
            started = false;
		}

        void remUserWithId(string id) {
            if (client1 != null && client1.Id == id) client1 = null;
            if (client2 != null && client2.Id == id) client2 = null;
        }

        protected internal void ProcessMessage(string message, string id) {
            var args = message.Split(' ', 3);
            var cmd = args[0];
            if (cmd == "//add") {
                // on connection
                sendMessage($"//msg {args[1]} connected", id);
                sendUsrData();
                sendRoomData();
                return;
            }
            if (cmd == "//msg") {
                var _args = args[2].Split(' ');
                var _cmd = _args[0];
                sendMessage($"cmd: {_cmd}");
                if (_cmd == "//gm") {
                    if (!started) start();
                    process(id, _args[1], _args[2]);
                } else if (_cmd == "//start") {
                    start();
                } else if (cmd == "//sz") {
                    if (started) return;
                    size = int.Parse(_args[1]);
                } else if (_cmd == "//swp") {
                    if (started) return;
                    swapUsers();
                    sendUsrData();
                } else {
                    sendMessage($"//msg {args[1]}: {args[2]}", id);
                }
                return;
            }
            if (cmd == "//rem") {
                // on disconnect
                remUserWithId(id);
                if (client1 == null && client2 != null) swapUsers();
                sendUsrData();
                if (started) {
                    showWinner(otherId(id));
                }

                sendMessage($"//msg {args[1]} disconected", id);
                return;
			}
		}
    }

	#endregion


    #region ClientObject

    public class ClientObject {
        protected internal string Id { get; private set; }
        protected internal NetworkStream Stream { get; private set; }
        TcpClient client;
        Server server; // объект сервера
        public Room room;

        public string userName { get; private set; }

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

                message = $"//add {userName}";
                // посылаем сообщение о входе в чат всем подключенным пользователям
                server.BroadcastMessage(message, this.Id);
                Console.WriteLine(message);
                // в бесконечном цикле получаем сообщения от клиента
                while (true) {
                    try {
                        message = GetMessage();
                        if (string.IsNullOrEmpty(message)) continue;
                        if (message == "\\disconnect") throw new SocketException();
                        message = $"//msg {userName} {message}";
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                    } catch {
                        message = $"//rem {userName}";
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                }
            } catch (Exception e) {
                //Console.WriteLine(e.Message);
            } finally {
                // в случае выхода из цикла закрываем ресурсы
                server.RemoveConnection(this.Id);
                Close();
            }
        }

        public void SendMessage(string msg) {
            byte[] data = Encoding.Unicode.GetBytes(msg);
            Stream.Write(data, 0, data.Length);

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
            room.RemoveUser(this);
        }
    }
	#endregion
}
