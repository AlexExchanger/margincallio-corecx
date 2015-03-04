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


    }
}
