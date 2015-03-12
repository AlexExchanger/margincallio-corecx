using System;
using System.IO;
using System.Threading;
using CoreCX.Gateways.TCP;

namespace CoreCX.Recovery
{
    static class FuncCallLogger
    {
        private static volatile bool is_logging = new bool();

        internal static void Start()
        {
            if (!is_logging)
            {
                Thread thread = new Thread(new ThreadStart(FuncCallLoggingThread));
                thread.Start();

                Console.WriteLine("CORE: FuncCall logging thread started");
            }
        }

        private static void FuncCallLoggingThread()
        {
            is_logging = true;

            //reset recovery queue
            Queues.recovery_queue.Clear();

            try
            {
                //reset log-file (FuncCalls)
                using (StreamWriter sw = new StreamWriter(new FileStream(@"recovery\log.dat", FileMode.Create, FileAccess.Write, FileShare.None)))
                { }

                //append FuncCalls
                using (StreamWriter sw = new StreamWriter(new FileStream(@"recovery\log.dat", FileMode.Append, FileAccess.Write, FileShare.None)))
                {
                    while (is_logging)
                    {
                        //попытка записи FC на диск
                        FuncCallReplica fc_replica;
                        if (Queues.recovery_queue.TryPeek(out fc_replica))
                        {
                            sw.WriteLine(fc_replica.Serialize());

                            Console.WriteLine("CORE: FuncCall appended to log file");
                            Queues.recovery_queue.TryDequeue(out fc_replica);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(DateTime.Now + " ==FUNC CALL LOGGING ERROR==");
                Console.WriteLine(e.ToString());
            }
        }

        internal static void Stop()
        {
            is_logging = false;
            Console.WriteLine("CORE: FuncCall logging thread stopped");
        }

        internal static void RestoreLogFile()
        {
            //string[] json_fcs = File.ReadAllLines(@"recovery\log.dat");
            //for (int i = 0; i < json_fcs.Length; i++)
            //{
            //    //JSON-парсинг вызова функции ядра
            //    int func_id;
            //    string[] str_args;
            //    bool _parsed = JsonManager.ParseTechJson(json_fcs[i], out func_id, out str_args);

            //    if (_parsed)
            //    {
            //        Console.WriteLine(DateTime.Now + " MARKETMAKER: to queue function #" + func_id);
            //        MarketmakerRequest.QueueFC(client, func_id, str_args); //попытка парсинга аргументов и постановки в очередь соответствующей функции
            //    }
            //    else //ошибка JSON-парсинга
            //    {
            //        CoreResponse.RejectInvalidJson(client);
            //    }
            //}
        }
    }
}
