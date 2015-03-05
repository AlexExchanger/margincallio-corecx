using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CoreCX.Trading;
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

        internal static void NewMarginCall(int user_id) //new margin call => DAEMON
        {
            Queues.daemon_queue.Enqueue(new MarginCallMsg(user_id));
        }

        internal static void NewTicker(string derived_currency, decimal bid, decimal ask) //new ticker => DAEMON
        {
            Queues.daemon_queue.Enqueue(new TickerMsg(derived_currency, bid, ask));
        }

        internal static void NewOrderBookTop(string derived_currency, bool side, List<OrderBuf> act_top) //new active top => DAEMON
        {
            Queues.daemon_queue.Enqueue(new ActiveTopMsg(derived_currency, side, act_top));
        }

        internal static void NewOrder(int msg_type, long func_call_id, FCSources fc_source, bool order_kind, Order order) //new order (msg_type дифференциирует тип заявки) => DAEMON
        {
            Queues.daemon_queue.Enqueue(new OrderMsg(msg_type, func_call_id, (int)fc_source, order_kind, order));
        }

        internal static void NewOrderStatus(long order_id, int user_id, OrderStatuses order_status) //new order status => DAEMON
        {
            Queues.daemon_queue.Enqueue(new OrderStatusMsg(order_id, user_id, (int)order_status));
        }

        internal static void NewTrade(Trade trade) //new trade => DAEMON
        {
            Queues.daemon_queue.Enqueue(new TradeMsg(trade));
        }

    }
}
