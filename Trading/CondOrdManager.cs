using System;
using System.Threading;

namespace CoreCX.Trading
{
    static class CondOrdManager
    {
        private static volatile bool manage = new bool();
        private static int mco_interval = 100;
        private static Timer MCO_Timer = new Timer(MCO_Tick, null, mco_interval, Timeout.Infinite);
        private static int guaranteed_manage_interval = 1250;
        private static DateTime last_dt_managed = new DateTime();

        internal static void QueueManageConditionalOrdersExecution()
        {
            manage = true;
        }

        private static void MCO_Tick(object data)
        {
            //if (Flags.backup_restore_in_proc) return; //проверка на резервирование или восстановление снэпшота

            if (manage) //если ядро требует поставить ManageConditionalOrders в очередь, то выполняем
            {
                //ставим в очередь
                Queues.prdf_queue.Enqueue(() => { App.core.ManageConditionalOrders(); });

                manage = false;
                last_dt_managed = DateTime.Now;
            }
            else if (last_dt_managed.AddMilliseconds(guaranteed_manage_interval) <= DateTime.Now) //если прошёл заданный интервал, то выполняем
            {
                //ставим в очередь
                Queues.prdf_queue.Enqueue(() => { App.core.ManageConditionalOrders(); });

                last_dt_managed = DateTime.Now;
            }

            MCO_Timer.Change(mco_interval, Timeout.Infinite);
        }
    }
}
