using System;
using System.Threading;

namespace CoreCX.Trading
{
    static class CondOrdManager
    {
        private static volatile bool delayed = new bool();
        private static decimal crit_market_rate_deviation = 0.05m;
        private static int delay = 1500;

        internal static void QueueExecution(decimal deviation)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                if (deviation < crit_market_rate_deviation)
                {
                    if (!delayed)
                    {
                        delayed = true;
                        Thread.Sleep(delay);
                        delayed = false;
                    }
                    else return;
                }
                Queues.prdf_queue.Enqueue(() => { App.core.ManageConditionalOrders(); });
            });
        }

        internal static StatusCodes SetCriticalMarketRateDeviation(decimal deviation_in_perc)
        {
            if (deviation_in_perc >= 0 && deviation_in_perc <= 100) //проверка на корректность процентного значения
            {
                crit_market_rate_deviation = deviation_in_perc / 100m;
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorIncorrectPercValue;
        }

        internal static StatusCodes SetDelay(int new_delay)
        {
            if (new_delay >= 1 && new_delay <= 60000) //от 1 миллисекунды до 1 минуты
            {
                delay = new_delay;
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorIncorrectDelayValue;
        }
    }
}
