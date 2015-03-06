using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using CoreCX.Trading;

namespace CoreCX.Gateways.TCP
{
    static class WebAppResponse
    {
        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code));
            });
        }

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, decimal amount)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, amount));
            });
        }

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, decimal bid, decimal ask)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, bid, ask));
            });
        }
        
        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, BaseFunds funds)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, funds));
            });
        }

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, Dictionary<string, BaseFunds> funds)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, funds));
            });
        }

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, Account acc_pars)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, acc_pars));
            });
        }
        
        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, List<string> strings)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, strings));
            });
        }

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, List<Order> buy_limit, List<Order> sell_limit, List<Order> buy_sl, List<Order> sell_sl, List<Order> buy_tp, List<Order> sell_tp, List<TSOrder> buy_ts, List<TSOrder> sell_ts)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, buy_limit, sell_limit, buy_sl, sell_sl, buy_tp, sell_tp, buy_ts, sell_ts));
            });
        }

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, string derived_currency, bool side, Order order)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, derived_currency, side, order));
            });
        }

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, List<OrderBuf> bids, List<OrderBuf> asks, decimal bids_vol, decimal asks_vol, int bids_num, int asks_num)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, bids, asks, bids_vol, asks_vol, bids_num, asks_num));
            });
        }

        internal static void AcceptFC(TcpClient client, long func_call_id) //запрос успешно обработан и будет поставлен в очередь
        {
            SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.Success, func_call_id));
            Console.WriteLine("WEB APP: accepted call #" + func_call_id);
        }

        internal static void RejectInvalidFuncArgs(TcpClient client)
        {
            SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.ErrorInvalidFunctionArguments));
            Console.WriteLine("WEB APP: [invalid arguments provided]");
        }

        internal static void RejectFuncNotFound(TcpClient client) //отклонение запроса из-за отсутствия функции в ядре
        {
            SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.ErrorFunctionNotFound));
            Console.WriteLine("WEB APP: [function not found]");
        }

        internal static void RejectInvalidJson(TcpClient client) //отклонение запроса из-за невалидного JSON
        {
            SocketIO.Write(client, JsonManager.FormTechJson((int)StatusCodes.ErrorInvalidJsonInput));
            Console.WriteLine("WEB APP: [tech JSON parse failed]");
        }
    }
}
