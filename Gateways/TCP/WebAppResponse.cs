using System;
using System.Net.Sockets;
using System.Threading;

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

        internal static void ReportExecRes(TcpClient client, long func_call_id, int status_code, decimal available_funds, decimal blocked_funds)
        {
            ThreadPool.QueueUserWorkItem(delegate
            {
                SocketIO.Write(client, JsonManager.FormTechJson(func_call_id, status_code, available_funds, blocked_funds));
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
