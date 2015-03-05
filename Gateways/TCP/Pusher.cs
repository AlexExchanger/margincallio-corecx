using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreCX.Gateways.TCP.Messages;

namespace CoreCX.Gateways.TCP
{
    static class Pusher
    {
        internal static void NewBalance(int user_id, string currency, decimal available_funds, decimal blocked_funds) //new balance => DAEMON
        {
            Queues.daemon_queue.Enqueue(new BalanceMsg(user_id, currency, available_funds, blocked_funds));
        }

        internal static void NewMarginInfo(int user_id, decimal equity, decimal margin, decimal free_margin, decimal margin_level) //new margin info => DAEMON
        {
            Queues.daemon_queue.Enqueue(new MarginInfoMsg(user_id, equity, margin, free_margin, margin_level));
        }

        internal static void NewTicker(string derived_currency, decimal bid, decimal ask) //new ticker => DAEMON
        {
            Queues.daemon_queue.Enqueue(new TickerMsg(derived_currency, bid, ask));
        }




        internal static void NewMarginCall(int user_id) //new margin call => DAEMON
        {
            Queues.daemon_queue.Enqueue(new MarginCallMsg(user_id));
        }


             
    }
}
