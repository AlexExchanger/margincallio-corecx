using System;

namespace CoreCX
{
    class Processor
    {
        private bool _proc_flag;

        internal Processor()
        {
            _proc_flag = new bool();
        }

        internal void Start() //запуск обработки очереди
        {
            _proc_flag = true;

            while (_proc_flag)
            {
                FuncCall stdf_call;
                if (Queues.stdf_queue.TryDequeue(out stdf_call))
                {
                    Console.WriteLine(DateTime.Now + " MAIN THREAD: EXECUTING STDF ITEM");
                    try
                    {
                        stdf_call.Action();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
                Action prdf_action;
                if (Queues.prdf_queue.TryDequeue(out prdf_action))
                {
                    //Console.WriteLine(DateTime.Now + " MAIN THREAD: EXECUTING PRDF ITEM");                    
                    try
                    {
                        prdf_action();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                }
            }
        }

        internal void Stop() //приостановление обработки очереди
        {
            _proc_flag = false;
        }
        


    }
}
