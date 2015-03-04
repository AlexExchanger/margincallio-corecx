﻿using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using CoreCX.Gateways.TCP.Messages;

namespace CoreCX.Gateways.TCP
{
    class TcpServer
    {        
        //прослушиваемые порты (константы с момента старта инстанса ядра)
        private int WebAppPort;
        private int HttpApiPort;
        private int DaemonPort;

        //адреса для IP-рестрикта TODO функция управления рестриктом)
        private string WebAppIP;
        private string HttpApiIP;
        private string DaemonIP;

        internal TcpServer(int web_app_port, int http_api_port, int daemon_port)
        {
            //инициализация прослушиваемых портов
            WebAppPort = web_app_port;
            HttpApiPort = http_api_port;
            DaemonPort = daemon_port;

            //инициализация IP-рестрикта
            WebAppIP = "199.0.0.1"; //TODO IP-рестрикт
            HttpApiIP = "127.0.0.1";
            DaemonIP = "199.0.0.1"; //TODO IP-рестрикт

            //создание и пуск потоков акцепторов
            Thread web_app_thread = new Thread(new ThreadStart(ListenWebAppThread));
            web_app_thread.Start();
            Console.WriteLine("WEB APP: listening thread started");

            //Thread http_api_thread = new Thread(new ThreadStart(ListenHttpApiThread));
            //http_api_thread.Start();
            //Console.WriteLine("HTTP API: listening thread started");

            Thread daemon_thread = new Thread(new ThreadStart(ListenHandleDaemonThread));
            daemon_thread.Start();
            Console.WriteLine("DAEMON: listening/handling thread started");
        }

        #region WEB APP LISTENING LOGIC

        private void ListenWebAppThread() //TODO add IP restriction
        {
            //запуск TCP-акцептора
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, WebAppPort);
                listener.Start();

                while (true)
                {
                    Console.WriteLine("WEB APP: waiting for connections");

                    //ожидаем подключения сервисных клиентов
                    TcpClient client = listener.AcceptTcpClient();

                    //проверка по IP клиента
                    string remote_ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(); //получаем IP подключившегося клиента

                    Console.WriteLine("WEB APP: connection request from " + remote_ip);

                    //  <<<< DELETE
                    Console.WriteLine("WEB APP: client connected [test mode]"); //  <<<< DELETE
                    ThreadPool.QueueUserWorkItem(new WaitCallback(HandleWebAppThread), client); //  <<<< DELETE                    

                    //  <<<< UNCOMMENT 
                    //if (remote_ip == WebAppIP) //WEB APP
                    //{
                    //    Console.WriteLine("WEB APP: client connected");
                    //    ThreadPool.QueueUserWorkItem(new WaitCallback(HandleWebAppThread), client);   
                    //}
                    //else //неизвестный клиент
                    //{
                    //    client.Close();
                    //    Console.WriteLine("WEB APP: client rejected by IP restriction");
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("==TCP ERROR==");
                Console.WriteLine(e.ToString());
            }
        }

        private void HandleWebAppThread(object obj)
        {
            TcpClient client = obj as TcpClient;

            while (true)
            {
                //получаем команду, парсим и ставим в очередь
                string received = SocketIO.Read(client);

                Console.WriteLine("WEB APP: received " + received);

                if (received == "dc") break;

                string[] json_fcs = received.Split(new char[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);

                for (int i = 0; i < json_fcs.Length; i++)
                {
                    //JSON-парсинг вызова функции ядра
                    int func_id;
                    string[] str_args;
                    bool _parsed = JsonManager.ParseTechJson(json_fcs[i], out func_id, out str_args);

                    if (_parsed)
                    {
                        Console.WriteLine("WEB APP: to queue function #" + func_id);                        
                        WebAppRequest.QueueFC(client, func_id, str_args); //попытка парсинга аргументов и постановки в очередь соответствующей функции
                    }
                    else //ошибка JSON-парсинга
                    {
                        WebAppResponse.RejectInvalidJson(client);                        
                    }
                }
            }

            client.Close();
            Console.WriteLine("WEB APP: connection closed");
        }

        #endregion

        #region DAEMON LISTENING LOGIC

        private void ListenHandleDaemonThread() //TODO add IP restriction
        {
            //запуск TCP-акцептора
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, DaemonPort);
                listener.Start();

                while (true)
                {
                    Console.WriteLine("DAEMON: waiting for connections");

                    //ожидаем подключения сервисных клиентов
                    TcpClient client = listener.AcceptTcpClient();

                    //проверка по IP клиента: DAEMON
                    string remote_ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(); //получаем IP подключившегося клиента

                    Console.WriteLine("DAEMON: connection request from " + remote_ip);

                    //  <<<< MOVE INSIDE THE COMMENTED SNIPPET BELOW (TO ADD IP RECTRICTION)
                    Console.WriteLine("DAEMON: client connected [test mode]");
                    while (true)
                    {
                        //попытка преобразовать сообщение в JSON и отправить его демону
                        IJsonSerializable msg;
                        if (Queues.daemon_queue.TryPeek(out msg))
                        {
                            bool _sent = SocketIO.Write(client, msg.Serialize());

                            if (_sent)
                            {
                                Console.WriteLine("DAEMON: message sent");
                                Queues.daemon_queue.TryDequeue(out msg);
                            }
                            else
                            {
                                Console.WriteLine("DAEMON: failed to send a message (dc)");
                                break;
                            }
                        }
                    }

                    client.Close();
                    Console.WriteLine("DAEMON: connection closed");

                    //if (remote_ip == DaemonIP) //DAEMON
                    //{
                    //    Console.WriteLine("DAEMON: client connected");
                    //
                    //    //логика выгребания из очереди в сокет Slave-ядра
                    //    //MOVE LOGIC HERE
                    //
                    //}
                    //else //неизвестный клиент
                    //{
                    //    client.Close();
                    //    Console.WriteLine("DAEMON: client rejected by IP restriction");
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("==TCP ERROR==");
                Console.WriteLine(e.ToString());
            }
        }
        
        #endregion

    }
}
