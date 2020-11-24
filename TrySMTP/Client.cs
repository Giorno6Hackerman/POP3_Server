using System;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using System.IO;

namespace TryPOP3
{
    class Client
    {
        string mailbox = "D:/prog/4 sem/KSIS/Labs/Lab_4/mails";

        TcpClient client;
        string userName = "mefistofel666";
        string userPassword = "666mefistofel";
        string[] mails;
        bool[] flags;

        public Client(TcpClient tcpClient)
        {
            client = tcpClient;

        }

        public void Process()
        {
            bool start = true;
            try
            {
                // Запрос клиента
                string Request = "";
                // Буфер для хранения принятых от клиента данных
                byte[] Buffer = new byte[1024];
                // Переменная для хранения количества байт, принятых от клиента
                int Count;
                while (true)
                {
                    Request = "";
                    if (start)
                    {
                        start = false;
                        SendResponse("+OK POP3 server ready\r\n");
                    }
                    else
                    {
                        Thread.Sleep(200);
                        while (client.GetStream().DataAvailable)
                        {
                            Count = client.GetStream().Read(Buffer, 0, Buffer.Length);
                            Request += Encoding.ASCII.GetString(Buffer, 0, Count);
                            /*if (Request.IndexOf("\r\n\r\n") >= 0)
                            {
                                break;
                            }*/
                        }
                        Console.WriteLine(Request);
                        DefineCommand(Request);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
            finally
            {
                if (client != null)
                    client.Close();
            }

        }

        private void DefineCommand(string Request)
        {
            string command;
            if (Request.Length <= 0)
            {
                SendResponse("+OK\r\n");
                return;
            }
            if (Request.IndexOf(" ") != -1)
            {
                command = Request.Substring(0, Request.IndexOf(" "));
            }
            else
            {
                command = Request.Substring(0, Request.IndexOf("\r"));
            }
            switch (command)
            {
                case "USER":
                    string name = Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r\n") - Request.IndexOf(" ") - 1);
                    if (Request.Contains("PASS"))
                    {
                        string password = Request.Substring(Request.IndexOf("PASS") + 5);
                        password = password.Substring(0, password.IndexOf("\r\n"));
                        if ((name == userName) && (password == userPassword))
                        {
                            SendResponse("+OK " + userName + " exists\r\n");
                        }
                        else
                        {
                            SendResponse("-ERR sorry :(, no such user here\r\n");
                        }
                    }
                    else
                    {
                        if (name == userName)
                        {
                            SendResponse("+OK " + userName + " exists\r\n");
                        }
                        else
                        {
                            SendResponse("-ERR sorry :(, no such user here\r\n");
                        }
                    }
                    break;
                case "PASS":
                    if (Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r\n") - Request.IndexOf(" ") - 1) == userPassword)
                    {
                        SendResponse("+OK startuem\r\n");
                    }
                    else
                    {
                        SendResponse("-ERR wrong password\r\n");
                    }
                    break;
                case "STAT"://считать сообщения и их суммарный размер
                    SendResponse("+OK " + CalculateMessages() + "\r\n");
                    break;
                case "LIST"://номер и размер сообщения/всех сообщений построчно
                    if (Request.IndexOf(" ") != -1)
                    {
                        int mesNum = Int32.Parse(Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1));
                        if ((mesNum > mails.Length) || (mesNum <= 0))
                        {
                            SendResponse("-ERR no such message\r\n");
                        }
                        else
                        {
                            FileInfo file = new FileInfo(mails[mesNum - 1]);
                            SendResponse("+OK " + mesNum.ToString() + " " + file.Length.ToString() + "\r\n");
                        }
                    }
                    else 
                    {
                        string Response = "+OK " + CalculateMessages() + "\r\n";
                        int index = 1;
                        foreach (string mail in mails)
                        {
                            FileInfo file = new FileInfo(mail);
                            Response += index.ToString() + " " + file.Length.ToString() + "\r\n";
                            index++;
                        }
                        Response += ".\r\n";
                        SendResponse(Response);
                    }
                    break;
                case "DELE"://удалить сообщение
                    int mes = Int32.Parse(Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1));
                    if ((mes <= 0) || (mes > mails.Length) || (!flags[mes - 1]))
                    {
                        SendResponse("-ERR no such message\r\n");
                    }
                    else
                    {
                        flags[mes - 1] = false;
                        string Response = "+OK message " + mes.ToString() + " deleted\r\n";
                        SendResponse(Response);
                    }
                    break;
                case "NOOP":
                    SendResponse("+OK\r\n");
                    break;
                case "RETR"://содержимое сообщения по номеру
                    int Number = Int32.Parse(Request.Substring(Request.IndexOf(" ") + 1, Request.IndexOf("\r") - Request.IndexOf(" ") - 1));
                    if (!flags[Number - 1])
                    {
                        SendResponse("-ERR no such message\r\n");
                    }
                    else
                    {
                        FileInfo file = new FileInfo(mails[Number - 1]);
                        StreamReader reader = new StreamReader(mails[Number - 1]);
                        string Response = "+OK " + file.Length.ToString() + "\r\n";
                        Response += reader.ReadToEnd();
                        SendResponse(Response);
                        reader.Close();
                    }
                    break;
                case "QUIT":
                    SendResponse("+OK bye\r\n");
                    DeleteMessages();
                    client.Close();
                    break;
                default:
                    SendResponse("-ERR\r\n");
                    break;
            }
        }

        private void SendResponse(string Response)
        {
            byte[] buf = Encoding.ASCII.GetBytes(Response);
            client.GetStream().Write(buf, 0, buf.Length);
            Console.WriteLine(Response);
        }

        private string CalculateMessages()
        {
            mails = Directory.GetFiles(mailbox);
            flags = new bool[mails.Length];

            int index = 0;
            long size = 0;
            foreach (string mail in mails)
            {
                FileInfo file = new FileInfo(mail);
                size += file.Length;
                flags[index] = true;
                index++;
            }

            
            return mails.Length.ToString() + " " + size.ToString();
        }

        private void DeleteMessages()
        {
            int index = 0;
            foreach (string mail in mails)
            {
                if (!flags[index])
                {
                    FileInfo file = new FileInfo(mail);
                    file.Delete();
                }
                index++;
            }
        }
    }
}
