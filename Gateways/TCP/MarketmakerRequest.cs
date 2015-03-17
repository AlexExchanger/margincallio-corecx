using System;
using System.Collections.Generic;
using System.Net.Sockets;

namespace CoreCX.Gateways.TCP
{
    static class MarketmakerRequest
    {
        internal static void QueueFC(TcpClient client, int func_id, string[] str_args) //выбор функции по ID, попытка парсинга аргументов и постановки в очередь
        {
            FuncCall call;

            if (Flags.market_closed) //проверка на приостановку торгов
            {
                if (Enum.IsDefined(typeof(MarketClosedForbiddenFuncIds), func_id))
                {
                    CoreResponse.RejectMarketClosed(client);
                    return;
                }
            }

            switch (func_id)
            {

                case (int)FuncIds.PlaceLimit: //подать лимитную заявку
                    {
                        int user_id;
                        bool side;
                        decimal amount, rate, sl_rate, tp_rate, ts_offset;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]) && str_args[2].TryParseToBool(out side) && decimal.TryParse(str_args[3], out amount) && decimal.TryParse(str_args[4], out rate) && decimal.TryParse(str_args[5], out sl_rate) && decimal.TryParse(str_args[6], out tp_rate) && decimal.TryParse(str_args[7], out ts_offset))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.PlaceLimit(user_id, str_args[1], side, amount, rate, sl_rate, tp_rate, ts_offset, call.FuncCallId, FCSources.Marketmaker);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.PlaceLimit(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ", " + str_args[4] + ", " + str_args[5] + ", " + str_args[6] + ", " + str_args[7] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.PlaceMarket: //подать рыночную заявку
                    {
                        int user_id;
                        bool side, base_amount;
                        decimal amount, sl_rate, tp_rate, ts_offset;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]) && str_args[2].TryParseToBool(out side) && str_args[3].TryParseToBool(out base_amount) && decimal.TryParse(str_args[4], out amount) && decimal.TryParse(str_args[5], out sl_rate) && decimal.TryParse(str_args[6], out tp_rate) && decimal.TryParse(str_args[7], out ts_offset))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.PlaceMarket(user_id, str_args[1], side, base_amount, amount, sl_rate, tp_rate, ts_offset, call.FuncCallId, FCSources.Marketmaker);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.PlaceMarket(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ", " + str_args[4] + ", " + str_args[5] + ", " + str_args[6] + ", " + str_args[7] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.CancelOrder: //отменить заявку
                    {
                        int user_id;
                        long order_id;
                        if (int.TryParse(str_args[0], out user_id) && long.TryParse(str_args[1], out order_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CancelOrder(user_id, order_id, call.FuncCallId, FCSources.Marketmaker);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.CancelOrder(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }
                    
                default:
                    {
                        CoreResponse.RejectFuncNotFound(client);
                        return;
                    }
            }

            //сообщаем об успешной регистрации FC
            CoreResponse.AcceptFC(client, call.FuncCallId);

            //ставим в очередь вызов функции
            Queues.stdf_queue.Enqueue(call);
        }
    }
}
