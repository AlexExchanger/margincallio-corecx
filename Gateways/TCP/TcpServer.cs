using System;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using CoreCX.Gateways.TCP.Messages;
using CoreCX.Recovery;

namespace CoreCX.Gateways.TCP
{
    class TcpServer
    {        
        //прослушиваемые порты (константы с момента старта инстанса ядра)
        private int WebAppPort;
        private int MarketmakerPort;
        private int DaemonPort;
        private int ReservePort;

        //адреса для IP-рестрикта TODO функция управления рестриктом)
        private string WebAppIP;
        private string MarketmakerIP;
        private string DaemonIP;
        private string ReserveIP;

        internal TcpServer(int web_app_port, int marketmaker_port, int daemon_port, int reserve_port)
        {
            //инициализация прослушиваемых портов
            WebAppPort = web_app_port;
            MarketmakerPort = marketmaker_port;
            DaemonPort = daemon_port;
            ReservePort = reserve_port;

            //инициализация IP-рестрикта
            WebAppIP = "199.0.0.1"; //TODO IP-рестрикт
            MarketmakerIP = "199.0.0.1"; //TODO IP-рестрикт
            DaemonIP = "199.0.0.1"; //TODO IP-рестрикт
            ReserveIP = "127.0.0.1";

            //создание и пуск потоков акцепторов
            Thread web_app_thread = new Thread(new ThreadStart(ListenWebAppThread));
            web_app_thread.Start();
            Console.WriteLine(DateTime.Now + " WEB APP: listening thread started");

            Thread marketmaker_thread = new Thread(new ThreadStart(ListenMarketmakerThread));
            marketmaker_thread.Start();
            Console.WriteLine(DateTime.Now + " MARKETMAKER: listening thread started");

            Thread daemon_thread = new Thread(new ThreadStart(ListenHandleDaemonThread));
            daemon_thread.Start();
            Console.WriteLine(DateTime.Now + " DAEMON: listening/handling thread started");

            Thread reserve_thread = new Thread(new ThreadStart(ListenHandleReserveThread));
            reserve_thread.Start();
            Console.WriteLine(DateTime.Now + " RESERVE: listening/handling thread started");

            //Thread http_api_thread = new Thread(new ThreadStart(ListenHttpApiThread));
            //http_api_thread.Start();
            //Console.WriteLine(DateTime.Now + " HTTP API: listening thread started");
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
                    Console.WriteLine(DateTime.Now + " WEB APP: waiting for connections");

                    //ожидаем подключения сервисных клиентов
                    TcpClient client = listener.AcceptTcpClient();

                    //проверка по IP клиента
                    string remote_ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(); //получаем IP подключившегося клиента

                    Console.WriteLine(DateTime.Now + " WEB APP: connection request from " + remote_ip);

                    //  <<<< DELETE
                    Console.WriteLine(DateTime.Now + " WEB APP: client connected [test mode]"); //  <<<< DELETE
                    ThreadPool.QueueUserWorkItem(new WaitCallback(HandleWebAppThread), client); //  <<<< DELETE                    

                    //  <<<< UNCOMMENT 
                    //if (remote_ip == WebAppIP) //WEB APP
                    //{
                    //    Console.WriteLine(DateTime.Now + " WEB APP: client connected");
                    //    ThreadPool.QueueUserWorkItem(new WaitCallback(HandleWebAppThread), client);   
                    //}
                    //else //неизвестный клиент
                    //{
                    //    client.Close();
                    //    Console.WriteLine(DateTime.Now + " WEB APP: client rejected by IP restriction");
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " ==TCP ERROR==");
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

                Console.WriteLine(DateTime.Now + " WEB APP: received " + received);

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
                        Console.WriteLine(DateTime.Now + " WEB APP: to queue function #" + func_id);                        
                        WebAppRequest.QueueFC(client, func_id, str_args); //попытка парсинга аргументов и постановки в очередь соответствующей функции
                    }
                    else //ошибка JSON-парсинга
                    {
                        CoreResponse.RejectInvalidJson(client);                        
                    }
                }
            }

            client.Close();
            Console.WriteLine(DateTime.Now + " WEB APP: connection closed");
        }

        #endregion

        #region MARKETMAKER LISTENING LOGIC

        private void ListenMarketmakerThread() //TODO add IP restriction
        {
            //запуск TCP-акцептора
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, MarketmakerPort);
                listener.Start();

                while (true)
                {
                    Console.WriteLine(DateTime.Now + " MARKETMAKER: waiting for connections");

                    //ожидаем подключения сервисных клиентов
                    TcpClient client = listener.AcceptTcpClient();

                    //проверка по IP клиента
                    string remote_ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(); //получаем IP подключившегося клиента

                    Console.WriteLine(DateTime.Now + " MARKETMAKER: connection request from " + remote_ip);

                    //  <<<< DELETE
                    Console.WriteLine(DateTime.Now + " MARKETMAKER: client connected [test mode]"); //  <<<< DELETE
                    Thread mm_thread = new Thread(new ParameterizedThreadStart(HandleMarketmakerThread)); //  <<<< DELETE   
                    mm_thread.Start(client); //  <<<< DELETE 

                    //  <<<< UNCOMMENT 
                    //if (remote_ip == MarketmakerIP) //MARKETMAKER
                    //{
                    //    Console.WriteLine(DateTime.Now + " MARKETMAKER: client connected");
                    //    Thread mm_thread = new Thread(new ParameterizedThreadStart(HandleMarketmakerThread)); //  <<<< DELETE   
                    //    mm_thread.Start();
                    //}
                    //else //неизвестный клиент
                    //{
                    //    client.Close();
                    //    Console.WriteLine(DateTime.Now + " MARKETMAKER: client rejected by IP restriction");
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " ==TCP ERROR==");
                Console.WriteLine(e.ToString());
            }
        }

        private void HandleMarketmakerThread(object obj)
        {
            TcpClient client = obj as TcpClient;

            while (true)
            {
                //получаем команду, парсим и ставим в очередь
                string received = SocketIO.Read(client);

                Console.WriteLine(DateTime.Now + " MARKETMAKER: received " + received);

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
                        Console.WriteLine(DateTime.Now + " MARKETMAKER: to queue function #" + func_id);
                        MarketmakerRequest.QueueFC(client, func_id, str_args); //попытка парсинга аргументов и постановки в очередь соответствующей функции
                    }
                    else //ошибка JSON-парсинга
                    {
                        CoreResponse.RejectInvalidJson(client);
                    }
                }
            }

            client.Close();
            Console.WriteLine(DateTime.Now + " MARKETMAKER: connection closed");
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
                    Console.WriteLine(DateTime.Now + " DAEMON: waiting for a connection");

                    //ожидаем подключения сервисных клиентов
                    TcpClient client = listener.AcceptTcpClient();

                    //проверка по IP клиента: DAEMON
                    string remote_ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(); //получаем IP подключившегося клиента

                    Console.WriteLine(DateTime.Now + " DAEMON: connection request from " + remote_ip);

                    //  <<<< MOVE INSIDE THE COMMENTED SNIPPET BELOW (TO ADD IP RECTRICTION)
                    Console.WriteLine(DateTime.Now + " DAEMON: client connected [test mode]");
                    while (true)
                    {
                        //попытка преобразовать сообщение в JSON и отправить его демону
                        IJsonSerializable msg;
                        if (Queues.daemon_queue.TryPeek(out msg))
                        {
                            bool _sent = SocketIO.Write(client, msg.Serialize());

                            if (_sent)
                            {
                                Console.WriteLine(DateTime.Now + " DAEMON: message sent");
                                Queues.daemon_queue.TryDequeue(out msg);
                            }
                            else
                            {
                                Console.WriteLine(DateTime.Now + " DAEMON: failed to send a message (dc)");
                                break;
                            }
                        }
                    }

                    client.Close();
                    Console.WriteLine(DateTime.Now + " DAEMON: connection closed");

                    //if (remote_ip == DaemonIP) //DAEMON
                    //{
                    //    Console.WriteLine(DateTime.Now + " DAEMON: client connected");
                    //
                    //    //логика выгребания из очереди в сокет
                    //    //MOVE LOGIC HERE
                    //
                    //}
                    //else //неизвестный клиент
                    //{
                    //    client.Close();
                    //    Console.WriteLine(DateTime.Now + " DAEMON: client rejected by IP restriction");
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " ==TCP ERROR==");
                Console.WriteLine(e.ToString());
            }
        }
        
        #endregion
        
        #region RESERVE LISTENING LOGIC

        private void ListenHandleReserveThread() //TODO add IP restriction
        {
            //запуск TCP-акцептора
            try
            {
                TcpListener listener = new TcpListener(IPAddress.Any, ReservePort);
                listener.Start();

                while (true)
                {
                    Console.WriteLine(DateTime.Now + " RESERVE: waiting for a connection");

                    //ожидаем подключения сервисных клиентов
                    TcpClient client = listener.AcceptTcpClient();

                    //проверка по IP клиента: RESERVE
                    string remote_ip = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString(); //получаем IP подключившегося клиента

                    Console.WriteLine(DateTime.Now + " RESERVE: connection request from " + remote_ip);

                    //  <<<< MOVE INSIDE THE COMMENTED SNIPPET BELOW (TO ADD IP RECTRICTION)
                    Console.WriteLine(DateTime.Now + " RESERVE: client connected [test mode]");
                    //while (true)
                    //{
                    //    //попытка преобразовать сообщение в JSON и отправить его демону
                    //    FuncCallReplica fc_replica;
                    //    if (Queues.recovery_queue.TryPeek(out fc_replica))
                    //    {
                    //        bool _sent = SocketIO.Write(client, fc_replica.Serialize());

                    //        if (_sent)
                    //        {
                    //            Console.WriteLine(DateTime.Now + " RESERVE: replica sent");
                    //            Queues.recovery_queue.TryDequeue(out fc_replica);
                    //        }
                    //        else
                    //        {
                    //            Console.WriteLine(DateTime.Now + " RESERVE: failed to send a replica (dc)");
                    //            break;
                    //        }
                    //    }
                    //}

                    client.Close();
                    Console.WriteLine(DateTime.Now + " RESERVE: connection closed");

                    //if (remote_ip == ReserveIP) //RESERVE
                    //{
                    //    Console.WriteLine(DateTime.Now + " RESERVE: client connected");
                    //
                    //    //логика выгребания из очереди в сокет
                    //    //MOVE LOGIC HERE
                    //
                    //}
                    //else //неизвестный клиент
                    //{
                    //    client.Close();
                    //    Console.WriteLine(DateTime.Now + " RESERVE: client rejected by IP restriction");
                    //}
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " ==TCP ERROR==");
                Console.WriteLine(e.ToString());
            }
        }

        #endregion

    }
}
