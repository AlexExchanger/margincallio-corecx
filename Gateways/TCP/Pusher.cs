using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;

namespace CoreCX.Gateways.TCP
{
    static class Pusher
    {
        #region CLIENTS BLOCKING PUSHES

        internal static void FCRejected(TcpClient client, int rej_code)
        {
            //эта функция вызывается из потока клиентского коннекта, поэтому она пишет синхронно
            switch (rej_code)
            {
                case (int)FCRejCodes.InvalidFuncArgs: //невалидные параметры
                    {
                        SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.ErrorInvalidFunctionArguments));
                        Console.WriteLine("[Invalid arguments passed to core function]");
                        break;
                    }

                case (int)FCRejCodes.FuncNotFound: //отсутствует функция с данным ID
                    {
                        SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.ErrorFunctionNotFound));
                        Console.WriteLine("[Core function not found]");
                        break;
                    }

                case (int)FCRejCodes.MarketClosed: //торги закрыты
                    {
                        SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.ErrorMarketClosed));
                        Console.WriteLine("[Market closed]");
                        break;
                    }

                case (int)FCRejCodes.BackupRestoreInProc: //выполняется резервирование или восстановление снэпшота
                    {
                        SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.ErrorBackupRestoreInProc));
                        Console.WriteLine("[Backup restore in proc]");
                        break;
                    }
            }
        }

        internal static void FCAccepted(TcpClient client, long func_call_id)
        {
            //эта функция вызывается из потока клиентского коннекта, поэтому она пишет синхронно            
            SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.Success, func_call_id));
            Console.WriteLine("Accepted call #" + func_call_id);
        }

        #endregion

        #region PHP CLIENTS NON-BLOCKING PUSHES

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code) //func call id + status code => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, string api_key, string secret) //generate api key / fix acc => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, api_key, secret));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, string password) //generate new fix password => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, password));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, Dictionary<string, FixAccount> fix_accounts) //get FIX accounts => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, fix_accounts));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, Dictionary<string, ApiKey> api_keys) //get api keys => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, api_keys));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, Account account) //get account info => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, account));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, List<Order> open_buy, List<Order> open_sell) //get open orders => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, open_buy, open_sell));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, List<Order> long_sls, List<Order> short_sls, List<Order> long_tps, List<Order> short_tps, List<TSOrder> long_tss, List<TSOrder> short_tss) //get open SL/TP/TS => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, long_sls, short_sls, long_tps, short_tps, long_tss, short_tss));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, decimal bid_price, decimal ask_price) //get ticker => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, bid_price, ask_price));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, List<OrderBuf> bids, List<OrderBuf> asks, decimal bids_vol, decimal asks_vol, int bids_num, int asks_num) //get depth => PHP
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, bids, asks, bids_vol, asks_vol, bids_num, asks_num));
            });
        }

        #endregion

        #region HTTP API CLIENTS NON-BLOCKING PUSHES

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, decimal bid_price, decimal ask_price, DateTime dt_made) //ticker => HTTP API
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, bid_price, ask_price, dt_made));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, List<OrderBuf> bids, List<OrderBuf> asks, decimal bids_vol, decimal asks_vol, int bids_num, int asks_num, DateTime dt_made) //depth => HTTP API
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, bids, asks, bids_vol, asks_vol, bids_num, asks_num, dt_made));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, Account account, DateTime dt_made) //account info => HTTP API
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, account, dt_made));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, List<Order> open_buy, List<Order> open_sell, DateTime dt_made) //open orders => HTTP API
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, open_buy, open_sell, dt_made));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, List<Order> long_sls, List<Order> short_sls, List<Order> long_tps, List<Order> short_tps, List<TSOrder> long_tss, List<TSOrder> short_tss, DateTime dt_made) //open conditional orders => HTTP API
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, long_sls, short_sls, long_tps, short_tps, long_tss, short_tss, dt_made));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, bool order_kind, Order order, DateTime dt_made) //place (add, cancel) order => HTTP API
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, order_kind, order, dt_made));
            });
        }

        internal static void FuncExecuted(TcpClient client, long func_call_id, int status_code, bool order_kind, TSOrder ts_order, DateTime dt_made) //add (remove) ts_order => HTTP API
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, order_kind, ts_order, dt_made));
            });
        }

        #endregion

        #region DAEMON QUEUE BLOCKING PUSHES
        
        internal static void NewBalance(int user_id, Account account, DateTime dt_made) //new balance => DAEMON
        {
            Queues.daemon_queue.Enqueue(new BalanceMsg(user_id, account, dt_made));
        }

        internal static void NewTicker(decimal bid, decimal ask, DateTime dt_made) //new ticker => DAEMON
        {
            Queues.daemon_queue.Enqueue(new TickerMsg(bid, ask, dt_made));
        }

        internal static void NewTrade(Trade trade) //new trade => DAEMON
        {
            Queues.daemon_queue.Enqueue(new TradeMsg(trade));
        }

        internal static void NewOrder(int msg_type, long func_call_id, int fc_source, bool order_kind, Order order) //new order (msg_type дифференциирует тип заявки) => DAEMON
        {
            Queues.daemon_queue.Enqueue(new OrderMsg(msg_type, func_call_id, fc_source, order_kind, order));
        }

        internal static void NewOrder(int msg_type, bool order_kind, Order order) //new order (msg_type дифференциирует тип заявки) => DAEMON
        {
            Queues.daemon_queue.Enqueue(new OrderMsg(msg_type, order_kind, order));
        }

        internal static void NewOrderStatus(long order_id, int user_id, int order_status, DateTime dt_made) //new order status => DAEMON
        {
            Queues.daemon_queue.Enqueue(new OrderStatusMsg(order_id, user_id, order_status, dt_made)); 
        }

        internal static void NewAccountFee(long func_call_id, int user_id, decimal fee_in_perc, DateTime dt_made) //new account fee => DAEMON
        {
            Queues.daemon_queue.Enqueue(new AccountFeeMsg(func_call_id, user_id, fee_in_perc, dt_made));
        }

        internal static void NewMarginInfo(int user_id, decimal equity, decimal ml_in_perc, DateTime dt_made) //new margin info => DAEMON
        {
            Queues.daemon_queue.Enqueue(new MarginInfoMsg(user_id, equity, ml_in_perc, dt_made));
        }

        internal static void NewMarginCall(int user_id, DateTime dt_made) //new margin call => DAEMON
        {
            Queues.daemon_queue.Enqueue(new MarginCallMsg(user_id, dt_made));
        }

        internal static void NewActiveBuyTop(List<OrderBuf> act_buy_top, DateTime dt_made) //new active buy top => DAEMON
        {
            Queues.daemon_queue.Enqueue(new ActiveTopMsg(false, act_buy_top, dt_made));
        }

        internal static void NewActiveSellTop(List<OrderBuf> act_sell_top, DateTime dt_made) //new active sell top => DAEMON
        {
            Queues.daemon_queue.Enqueue(new ActiveTopMsg(true, act_sell_top, dt_made));
        }

        internal static void NewFixRestart(int status_code, DateTime dt_made) //new FIX application restart => DAEMON
        {
            Queues.daemon_queue.Enqueue(new FixRestartMsg(status_code, dt_made));
        }

        internal static void NewMarketStatus(int market_status, DateTime dt_made) //new market status => DAEMON
        {
            Queues.daemon_queue.Enqueue(new MarketStatusMsg(market_status, dt_made));
        }

        internal static void NewSnapshotOperation(int op_code, int status_code, DateTime dt_made) //new snapshot operation => DAEMON
        {
            Queues.daemon_queue.Enqueue(new SnapshotOperationMsg(op_code, status_code, dt_made));
        }

        #endregion

        #region SLAVE QUEUE BLOCKING PUSHES

        internal static void ReplicateFC(int func_id, string[] str_args) //replicate function call => SLAVE CORE
        {
            Queues.slave_queue.Enqueue(new FuncCallReplica(func_id, str_args));
        }

        #endregion

    }
}
