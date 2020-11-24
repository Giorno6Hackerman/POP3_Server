using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.Net.Mail;

namespace TryPOP3
{
    class Server
    {
        TcpListener Listener;

        // Запуск сервера
        public Server(int Port)
        {
            try
            {
                Listener = new TcpListener(IPAddress.Any, Port);
                Listener.Start();

                while (true)
                {
                    // Новый клиент
                    Client client = new Client(Listener.AcceptTcpClient());
                    // Поток для нового клиента
                    Thread ClientThread = new Thread(new ThreadStart(client.Process));
                    ClientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                Disconnect();
            }
        }


        // Остановка сервера
        public void Disconnect()
        {
            if (Listener != null)
                Listener.Stop();
            Environment.Exit(0);
        }

        static void Main(string[] args)
        {
            Server server = new Server(110);
        }
    }
}
