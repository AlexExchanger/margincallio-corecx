using System;
using System.Threading;

namespace CoreCX.Trading
{
    static class MarginManager
    {
        private static volatile bool manage = new bool();
        private static int mm_interval = 100;
        private static Timer MM_Timer = new Timer(MM_Tick, null, mm_interval, Timeout.Infinite);

        internal static void QueueManageMarginExecution()
        {
            manage = true;
        }

        private static void MM_Tick(object data)
        {
            //if (Flags.backup_restore_in_proc) return; //проверка на резервирование или восстановление снэпшота

            if (manage)
            {
                //ставим в очередь
                Queues.prdf_queue.Enqueue(() => { App.core.ManageMargin(); });

                manage = false;
            }
            
            MM_Timer.Change(mm_interval, Timeout.Infinite);
        }
    }
}
