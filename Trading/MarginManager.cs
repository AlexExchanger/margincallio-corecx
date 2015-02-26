using System;
using System.Threading;

namespace CoreCX.Trading
{
    static class MarginManager
    {
        private static volatile bool manage = new bool();
        private static int mm_interval = 100;
        private static Timer MM_Timer = new Timer(MM_Tick, null, mm_interval, Timeout.Infinite);
        private static int guaranteed_manage_interval = 2000;
        private static DateTime last_dt_managed = new DateTime();

        internal static void QueueManageMarginExecution()
        {
            manage = true;
        }

        private static void MM_Tick(object data)
        {
            //if (Flags.backup_restore_in_proc) return; //проверка на резервирование или восстановление снэпшота

            if (manage) //если ядро требует поставить ManageMargin в очередь, то выполняем
            {
                //ставим в очередь
                Queues.prdf_queue.Enqueue(() => { App.core.ManageMargin(); });

                manage = false;
                last_dt_managed = DateTime.Now;
            }
            else if (last_dt_managed.AddMilliseconds(guaranteed_manage_interval) <= DateTime.Now) //если прошёл заданный интервал, то выполняем
            {
                //ставим в очередь
                Queues.prdf_queue.Enqueue(() => { App.core.ManageMargin(); });

                last_dt_managed = DateTime.Now;
            }
            
            MM_Timer.Change(mm_interval, Timeout.Infinite);
        }
    }
}
