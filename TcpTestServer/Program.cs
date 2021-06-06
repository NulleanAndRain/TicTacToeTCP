using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Linq;

namespace TcpTestServer {
    class Program {
        static ServerObject server; // сервер
        static Thread listenThread; // потока для прослушивания
        static void Main(string[] args) {
            try {
                server = new ServerObject();
                listenThread = new Thread(new ThreadStart(server.Listen));
                listenThread.Start(); //старт потока
            } catch (Exception ex) {
                server.Disconnect();
                Console.WriteLine(ex.Message);
            }
        }
    }
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
						if (string.IsNullOrEmpty(message)) continue;
                        if (message == "\\disconnect") throw new SocketException();
						message = String.Format("{0}: {1}", userName, message);
                        Console.WriteLine(message);
                        server.BroadcastMessage(message, this.Id);
                    } catch (Exception e){
                        message = String.Format("{0} покинул чат", userName);
                        Console.WriteLine(message);
                        //Console.WriteLine("exeption: " + e.ToString());
                        server.BroadcastMessage(message, this.Id);
                        break;
                    }
                }
            } catch (Exception e) {
                //Console.WriteLine($"error with client ({Id}){userName}: {e.Message}");
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
            } while (Stream.DataAvailable);
            return builder.ToString();
        }

        public void SendMessage(string msg) {
            byte[] data = Encoding.Unicode.GetBytes(msg);
            Stream.Write(data, 0, data.Length);
        }

        // закрытие подключения
        protected internal void Close() {
            if (Stream != null)
                Stream.Close();
            if (client != null)
                client.Close();
            Stream = null;
            client = null;
        }
    }
    public class ServerObject {
        static TcpListener tcpListener; // сервер для прослушивания
        List<ClientObject> clients = new List<ClientObject>(); // все подключения

        protected internal void AddConnection(ClientObject clientObject) {
            clients.Add(clientObject);
        }
        protected internal void RemoveConnection(string id) {
            // получаем по id закрытое подключение
            ClientObject client = clients.Find(c => c.Id == id);
            // и удаляем его из списка подключений
            if (client != null)
                clients.Remove(client);
        }
        // прослушивание входящих подключений
        protected internal void Listen() {
            try {
                tcpListener = new TcpListener(IPAddress.Any, 8888);
                tcpListener.Start();
                Console.WriteLine("Сервер запущен. Ожидание подключений...");

                while (true) {
                    if (tcpListener.Pending()) {
                        TcpClient tcpClient = tcpListener.AcceptTcpClient();

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

        // трансляция сообщения подключенным клиентам
        protected internal void BroadcastMessage(string message, string id) {
            for (int i = 0; i < clients.Count; i++) {
                if (clients[i].Id != id) // если id клиента не равно id отправляющего
                {
                    clients[i].SendMessage(message); //передача данных
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
}
