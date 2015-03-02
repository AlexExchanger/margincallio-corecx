using System;
using System.Net.Sockets;

namespace CoreCX.Gateways.TCP
{
    static class WebAppRequest
    {
        internal static void QueueFC(TcpClient client, int func_id, string[] str_args) //выбор функции по ID, попытка парсинга аргументов и постановки в очередь
        {
            FuncCall call;

            switch (func_id)
            {

                case (int)FuncIds.CreateAccount: //создать торговый счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CreateAccount(user_id);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.CreateAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.SuspendAccount: //заблокировать торговый счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.SuspendAccount(user_id);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.SuspendAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.UnsuspendAccount: //разблокировать торговый счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.UnsuspendAccount(user_id);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.UnsuspendAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.DeleteAccount: //удалить торговый счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.DeleteAccount(user_id);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.DeleteAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.DepositFunds: //пополнить торговый счёт
                    {
                        int user_id;
                        decimal amount;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]) && decimal.TryParse(str_args[2], out amount))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.DepositFunds(user_id, str_args[1], amount);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.DepositFunds(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.WithdrawFunds: //снять с торгового счёта
                    {
                        int user_id;
                        decimal amount;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]) && decimal.TryParse(str_args[2], out amount))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.WithdrawFunds(user_id, str_args[1], amount);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.WithdrawFunds(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

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
                                StatusCodes status = App.core.PlaceLimit(user_id, str_args[1], side, amount, rate, sl_rate, tp_rate, ts_offset, call.FuncCallId, FCSources.WebApp);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.PlaceLimit(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ", " + str_args[4] + ", " + str_args[5] + ", " + str_args[6] + ", " + str_args[7] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
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
                                StatusCodes status = App.core.PlaceMarket(user_id, str_args[1], side, base_amount, amount, sl_rate, tp_rate, ts_offset, call.FuncCallId, FCSources.WebApp);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.PlaceMarket(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ", " + str_args[4] + ", " + str_args[5] + ", " + str_args[6] + ", " + str_args[7] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
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
                                StatusCodes status = App.core.CancelOrder(user_id, order_id, call.FuncCallId, FCSources.WebApp);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.CancelOrder(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetAccountBalance: //получить баланс торгового счёта в заданной валюте
                    {
                        int user_id;
                        decimal available_funds, blocked_funds;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetAccountBalance(user_id, str_args[1], out available_funds, out blocked_funds);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, available_funds, blocked_funds);
                            };
                            Console.WriteLine("To queue core.GetAccountBalance(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }


                    //TODO GetAccountParameters



                case (int)FuncIds.GetWithdrawalLimit: //получить лимит средств, доступных для вывода
                    {
                        int user_id;
                        decimal amount;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetWithdrawalLimit(user_id, str_args[1], out amount);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, amount);
                            };
                            Console.WriteLine("To queue core.GetWithdrawalLimit(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }









                default:
                    {
                        WebAppResponse.RejectFuncNotFound(client);
                        return;
                    }
            }

            //сообщаем об успешной регистрации FC
            WebAppResponse.AcceptFC(client, call.FuncCallId);

            //ставим в очередь вызов функции
            Queues.stdf_queue.Enqueue(call);
        }
               
    }
}
