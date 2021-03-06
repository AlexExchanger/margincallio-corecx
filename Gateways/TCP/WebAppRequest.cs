﻿using System;
using System.Collections.Generic;
using System.Net.Sockets;
using CoreCX.Trading;
using CoreCX.Recovery;

namespace CoreCX.Gateways.TCP
{
    static class WebAppRequest
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

                case (int)FuncIds.CreateAccount: //создать торговый счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CreateAccount(user_id);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.CreateAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
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
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.SuspendAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
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
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.UnsuspendAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
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
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.DeleteAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
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
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.DepositFunds(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
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
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.WithdrawFunds(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
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
                                StatusCodes status = App.core.PlaceMarket(user_id, str_args[1], side, base_amount, amount, sl_rate, tp_rate, ts_offset, call.FuncCallId, FCSources.WebApp);
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
                                StatusCodes status = App.core.CancelOrder(user_id, order_id, call.FuncCallId, FCSources.WebApp);
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

                case (int)FuncIds.SetAccountFee: //установить размер комиссии для торгового счёта
                    {
                        int user_id;
                        decimal fee_in_perc;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]) && decimal.TryParse(str_args[2], out fee_in_perc))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.SetAccountFee(user_id, str_args[1], fee_in_perc, call.FuncCallId);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.SetAccountFee(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetAccountBalance: //получить баланс торгового счёта
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            if (!String.IsNullOrEmpty(str_args[1])) //задана валюта (получаем баланс по 1-ой валюте)
                            {
                                BaseFunds funds;
                                call.Action = () =>
                                {
                                    StatusCodes status = App.core.GetAccountBalance(user_id, str_args[1], out funds);
                                    CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, funds);
                                };
                                Console.WriteLine(DateTime.Now + " To queue core.GetAccountBalance(" + str_args[0] + ", " + str_args[1] + ")");
                            }
                            else //валюта не задана (получаем баланс по всем валютам)
                            {
                                Dictionary<string, BaseFunds> funds;
                                call.Action = () =>
                                {
                                    StatusCodes status = App.core.GetAccountBalance(user_id, out funds);
                                    CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, funds);
                                };
                                Console.WriteLine(DateTime.Now + " To queue core.GetAccountBalance(" + str_args[0] + ")");
                            }                            
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetAccountParameters: //получить значения параметров торгового счёта
                    {
                        int user_id;
                        Account acc_pars;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetAccountParameters(user_id, out acc_pars);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, acc_pars);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.GetAccountParameters(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetAccountFee: //получить размер комиссии для торгового счёта
                    {
                        int user_id;
                        decimal fee_in_perc;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetAccountFee(user_id, str_args[1], out fee_in_perc);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, fee_in_perc);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.GetAccountFee(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
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
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, buy_limit, sell_limit, buy_sl, sell_sl, buy_tp, sell_tp, buy_ts, sell_ts);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.GetOpenOrders(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetOrderInfo: //получить параметры заявки
                    {
                        int user_id;
                        long order_id;
                        string derived_currency;
                        bool side;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && long.TryParse(str_args[1], out order_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetOrderInfo(user_id, order_id, out derived_currency, out side, out order);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, derived_currency, side, order);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.GetOrderInfo(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }


                case (int)FuncIds.CreateCurrencyPair: //создать новую валютную пару
                    {
                        if (!String.IsNullOrEmpty(str_args[0]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CreateCurrencyPair(str_args[0]);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.CreateCurrencyPair(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetCurrencyPairs: //получить список валютных пар
                    {
                        List<string> currency_pairs;
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = App.core.GetCurrencyPairs(out currency_pairs);
                            CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, currency_pairs);
                        };
                        Console.WriteLine(DateTime.Now + " To queue core.GetCurrencyPairs()");
                        break;                    
                    }

                case (int)FuncIds.GetDerivedCurrencies: //получить список производных валют
                    {
                        List<string> derived_currencies;
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = App.core.GetDerivedCurrencies(out derived_currencies);
                            CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, derived_currencies);
                        };
                        Console.WriteLine(DateTime.Now + " To queue core.GetDerivedCurrencies()");
                        break;
                    }

                    //TODO DeleteCurrencyPair

                case (int)FuncIds.GetTicker: //получить тикер
                    {
                        decimal bid, ask;
                        if (!String.IsNullOrEmpty(str_args[0]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetTicker(str_args[0], out bid, out ask);
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, bid, ask);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.GetTicker(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }

                case (int)FuncIds.GetDepth: //получить глубину
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
                                CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status, bids, asks, bids_vol, asks_vol, bids_num, asks_num);
                            };
                            Console.WriteLine(DateTime.Now + " To queue core.GetDepth(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            CoreResponse.RejectInvalidFuncArgs(client);
                            return;
                        }
                    }








                case (int)FuncIds.CloseMarket: //приостановить торги
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Flags.CloseMarket();
                            CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine(DateTime.Now + " To queue Flags.CloseMarket()");
                        break;
                    }

                case (int)FuncIds.OpenMarket: //возобновить торги
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Flags.OpenMarket();
                            CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine(DateTime.Now + " To queue Flags.OpenMarket()");
                        break;
                    }

                case (int)FuncIds.BackupCore: //сделать снэпшот ядра
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Snapshot.BackupCore(true);
                            CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine(DateTime.Now + " To queue Snapshot.BackupCore()");
                        break;
                    }

                case (int)FuncIds.RestoreCore: //восстановить снэпшот ядра
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Snapshot.RestoreCore(true);
                            CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine(DateTime.Now + " To queue Snapshot.RestoreCore()");
                        break;
                    }

                case (int)FuncIds.ResetFuncCallId: //сбросить func_call_id
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = FuncCall.ResetFuncCallId();
                            CoreResponse.ReportExecRes(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine(DateTime.Now + " To queue FuncCall.ResetFuncCallId()");
                        break;
                    }

                default: //функция не найдена
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
