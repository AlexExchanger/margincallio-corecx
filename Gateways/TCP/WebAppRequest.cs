﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using CoreCX.Trading;

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

                case (int)FuncIds.GetAccountBalance: //получить баланс торгового счёта для заданной пары
                    {
                        int user_id;
                        decimal base_af, base_bf, derived_af, derived_bf, fee;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetAccountBalance(user_id, str_args[1], out base_af, out base_bf, out derived_af, out derived_bf, out fee);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, base_af, base_bf, derived_af, derived_bf, fee);
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

                case (int)FuncIds.GetAccountParameters: //получить значения параметров торгового счёта
                    {
                        int user_id;
                        decimal max_leverage, level_mc, level_fl, equity, margin, free_margin, margin_level;
                        bool margin_call, suspended;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetAccountParameters(user_id, out max_leverage, out level_mc, out level_fl, out equity, out margin, out free_margin, out margin_level, out margin_call, out suspended);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, max_leverage, level_mc, level_fl, equity, margin, free_margin, margin_level, margin_call, suspended);
                            };
                            Console.WriteLine("To queue core.GetAccountParameters(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

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

                case (int)FuncIds.GetOpenOrders: //получить открытые заявки
                    {
                        int user_id;
                        List<Order> buy_limit, sell_limit, buy_sl, sell_sl, buy_tp, sell_tp;
                        List<TSOrder> buy_ts, sell_ts;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetOpenOrders(user_id, str_args[1], out buy_limit, out sell_limit, out buy_sl, out sell_sl, out buy_tp, out sell_tp, out buy_ts, out sell_ts);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, buy_limit, sell_limit, buy_sl, sell_sl, buy_tp, sell_tp, buy_ts, sell_ts);
                            };
                            Console.WriteLine("To queue core.GetOpenOrders(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                    //TODO GetOrderInfo


                case (int)FuncIds.CreateCurrencyPair:
                    {
                        if (!String.IsNullOrEmpty(str_args[0]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CreateCurrencyPair(str_args[0]);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.CreateCurrencyPair(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetCurrencyPairs:
                    {
                        List<string> currency_pairs;
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = App.core.GetCurrencyPairs(out currency_pairs);
                            WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, currency_pairs);
                        };
                        Console.WriteLine("To queue core.GetCurrencyPairs()");
                        break;                    
                    }

                case (int)FuncIds.GetDerivedCurrencies:
                    {
                        List<string> derived_currencies;
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = App.core.GetCurrencyPairs(out derived_currencies);
                            WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, derived_currencies);
                        };
                        Console.WriteLine("To queue core.GetDerivedCurrencies()");
                        break;
                    }

                    //TODO DeleteCurrencyPair

                case (int)FuncIds.GetTicker:
                    {
                        decimal bid, ask;
                        if (!String.IsNullOrEmpty(str_args[0]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetTicker(str_args[0], out bid, out ask);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, bid, ask);
                            };
                            Console.WriteLine("To queue core.GetTicker(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            WebAppResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetDepth:
                    {
                        int limit, bids_num, asks_num;                        
                        decimal bids_vol, asks_vol;
                        List<OrderBuf> bids, asks;
                        if (!String.IsNullOrEmpty(str_args[0]) && int.TryParse(str_args[1], out limit))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetDepth(str_args[0], limit, out bids, out asks, out bids_vol, out asks_vol, out bids_num, out asks_num);
                                WebAppResponse.ReportExecRes(client, call.FuncCallId, (int)status, bids, asks, bids_vol, asks_vol, bids_num, asks_num);
                            };
                            Console.WriteLine("To queue core.GetDepth(" + str_args[0] + ", " + str_args[1] + ")");
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
