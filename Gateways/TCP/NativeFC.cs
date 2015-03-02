using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace CoreCX.Gateways.TCP
{
    static class NativeFC
    {
        internal static void QueueWebAppFuncCall(TcpClient client, int func_id, string[] str_args) //выбор функции ядра по ID, попытка парсинга аргументов и постановки в очередь
        {
            FuncCall call;

            //if (Flags.backup_restore_in_proc) //проверка на резервирование или восстановление снэпшота
            //{
            //    Pusher.FCRejected(client, (int)FCRejCodes.BackupRestoreInProc);
            //    return;
            //}

            //if (Flags.market_closed) //проверка на закрытие торгов
            //{
            //    if (Enum.IsDefined(typeof(MarketClosedForbiddenFuncIds), func_id))
            //    {
            //        Pusher.FCRejected(client, (int)FCRejCodes.MarketClosed);
            //        return;
            //    }
            //}

            switch (func_id)
            {

                case (int)FuncIds.CreateAccount: //создать торговый счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () => {
                                StatusCodes status = App.core.CreateAccount(user_id);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };                            
                            Console.WriteLine("To queue core.CreateAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);      
                            return;
                        }
                    }

                case (int)FuncIds.SuspendAccount: //заблокировать торговый счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () => {
                                StatusCodes status = App.core.SuspendAccount(user_id);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.SuspendAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
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
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.UnsuspendAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
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
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.DeleteAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.DepositFunds: //пополнить торговый счёт
                    {
                        int user_id;
                        bool currency;
                        decimal sum;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out currency) && decimal.TryParse(str_args[2], out sum))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.DepositFunds(user_id, currency, sum);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.DepositFunds(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.WithdrawFunds: //снять с торгового счёта
                    {
                        int user_id;
                        bool currency;
                        decimal sum;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out currency) && decimal.TryParse(str_args[2], out sum))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.WithdrawFunds(user_id, currency, sum);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.WithdrawFunds(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.PlaceLimit: //подать лимитную заявку
                    {
                        int user_id;
                        bool order_kind;
                        decimal amount, rate;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out order_kind) && decimal.TryParse(str_args[2], out amount) && decimal.TryParse(str_args[3], out rate))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.PlaceLimit(user_id, order_kind, amount, rate, call.FuncCallId, (int)FCSources.WebApp, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.PlaceLimit(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.PlaceMarket: //подать рыночную заявку
                    {
                        int user_id;
                        bool order_kind;
                        decimal amount;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out order_kind) && decimal.TryParse(str_args[2], out amount))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.PlaceMarket(user_id, order_kind, amount, call.FuncCallId, (int)FCSources.WebApp, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.PlaceMarket(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.PlaceInstant: //подать рыночную заявку (на входе валюта 2)
                    {
                        int user_id;
                        bool order_kind;
                        decimal total;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out order_kind) && decimal.TryParse(str_args[2], out total))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.PlaceInstant(user_id, order_kind, total, call.FuncCallId, (int)FCSources.WebApp, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.PlaceInstant(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.CancelOrder: //отменить заявку
                    {
                        int user_id;
                        long order_id;
                        bool order_kind;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && long.TryParse(str_args[1], out order_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CancelOrder(user_id, order_id, call.FuncCallId, (int)FCSources.WebApp, out order_kind, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.CancelOrder(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.AddSL: //добавить стоп-лосс
                    {
                        int user_id;
                        bool pos_type;
                        decimal amount, rate;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out pos_type) && decimal.TryParse(str_args[2], out amount) && decimal.TryParse(str_args[3], out rate))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.AddSL(user_id, pos_type, amount, rate, call.FuncCallId, (int)FCSources.WebApp, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.AddSL(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.AddTP: //добавить тейк-профит
                    {
                        int user_id;
                        bool pos_type;
                        decimal amount, rate;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out pos_type) && decimal.TryParse(str_args[2], out amount) && decimal.TryParse(str_args[3], out rate))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.AddTP(user_id, pos_type, amount, rate, call.FuncCallId, (int)FCSources.WebApp, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.AddTP(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.AddTS: //добавить трейлинг-стоп
                    {
                        int user_id;
                        bool pos_type;
                        decimal amount, offset;
                        TSOrder ts_order;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out pos_type) && decimal.TryParse(str_args[2], out amount) && decimal.TryParse(str_args[3], out offset))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.AddTS(user_id, pos_type, amount, offset, call.FuncCallId, (int)FCSources.WebApp, out ts_order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.AddTS(" + str_args[0] + ", " + str_args[1] + ", " + str_args[2] + ", " + str_args[3] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.RemoveSL: //отменить стоп-лосс
                    {
                        int user_id;
                        long sl_id;
                        bool pos_type;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && long.TryParse(str_args[1], out sl_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.RemoveSL(user_id, sl_id, call.FuncCallId, (int)FCSources.WebApp, out pos_type, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.RemoveSL(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.RemoveTP: //отменить тейк-профит
                    {
                        int user_id;
                        long tp_id;
                        bool pos_type;
                        Order order;
                        if (int.TryParse(str_args[0], out user_id) && long.TryParse(str_args[1], out tp_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.RemoveTP(user_id, tp_id, call.FuncCallId, (int)FCSources.WebApp, out pos_type, out order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.RemoveTP(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.RemoveTS: //отменить трейлинг-стоп
                    {
                        int user_id;
                        long ts_id;
                        bool pos_type;
                        TSOrder ts_order;
                        if (int.TryParse(str_args[0], out user_id) && long.TryParse(str_args[1], out ts_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.RemoveTS(user_id, ts_id, call.FuncCallId, (int)FCSources.WebApp, out pos_type, out ts_order);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.RemoveTS(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.CreateFixAccount: //создать FIX-аккаунт
                    {
                        int user_id;
                        string sender_comp_id, password;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CreateFixAccount(user_id, out sender_comp_id, out password);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, sender_comp_id, password);
                            };
                            Console.WriteLine("To queue core.CreateFixAccount(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.GenerateNewFixPassword: //сгенерировать новый FIX-пароль
                    {
                        int user_id;
                        string password;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GenerateNewFixPassword(user_id, str_args[1], out password);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, password);
                            };
                            Console.WriteLine("To queue core.GenerateNewFixPassword(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.GetFixAccounts: //получить FIX-счета
                    {
                        int user_id;
                        Dictionary<string, FixAccount> fix_accounts;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetFixAccounts(user_id, out fix_accounts);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, fix_accounts);
                            };
                            Console.WriteLine("To queue core.GetFixAccounts(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.CancelFixAccount: //отменить FIX-счёт
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CancelFixAccount(user_id, str_args[1]);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.CancelFixAccount(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.GenerateApiKey: //сгенерировать API-ключ
                    {
                        int user_id;
                        bool rights;
                        string key, secret;
                        if (int.TryParse(str_args[0], out user_id) && str_args[1].TryParseToBool(out rights))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GenerateApiKey(user_id, rights, out key, out secret);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, key, secret);
                            };
                            Console.WriteLine("To queue core.GenerateApiKey(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.GetApiKeys: //получить API-ключи
                    {
                        int user_id;
                        Dictionary<string, ApiKey> api_keys;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetApiKeys(user_id, out api_keys);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, api_keys);
                            };
                            Console.WriteLine("To queue core.GetApiKeys(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.CancelApiKey: //отменить API-ключ
                    {
                        int user_id;
                        if (int.TryParse(str_args[0], out user_id) && !String.IsNullOrEmpty(str_args[1]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.CancelApiKey(user_id, str_args[1]);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.CancelApiKey(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.GetAccountInfo: //получить данные аккаунта
                    {
                        int user_id;
                        Account account;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetAccountInfo(user_id, out account);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, account);
                            };
                            Console.WriteLine("To queue core.GetAccountInfo(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                    //TODO call 4 GET-functions (info-only)

                case (int)FuncIds.GetOpenOrders: //получить открытые заявки
                    {
                        int user_id;
                        List<Order> open_buy;
                        List<Order> open_sell;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetOpenOrders(user_id, out open_buy, out open_sell);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, open_buy, open_sell);
                            };
                            Console.WriteLine("To queue core.GetOpenOrders(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.GetOpenConditionalOrders: //получить открытые SL/TP/TS
                    {
                        int user_id;
                        List<Order> long_sls;
                        List<Order> short_sls;
                        List<Order> long_tps;
                        List<Order> short_tps;
                        List<TSOrder> long_tss;
                        List<TSOrder> short_tss;
                        if (int.TryParse(str_args[0], out user_id))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetOpenConditionalOrders(user_id, out long_sls, out short_sls, out long_tps, out short_tps, out long_tss, out short_tss);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, long_sls, short_sls, long_tps, short_tps, long_tss, short_tss);
                            };
                            Console.WriteLine("To queue core.GetOpenOrders(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.SetAccountFee: //задать индивидуальную комиссию
                    {
                        int user_id;
                        decimal fee_in_perc;
                        if (int.TryParse(str_args[0], out user_id) && decimal.TryParse(str_args[1], out fee_in_perc))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.SetAccountFee(user_id, fee_in_perc, call.FuncCallId);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue core.SetAccountFee(" + str_args[0] + ", " + str_args[1] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.GetTicker: //получить тикер
                    {
                        decimal bid_price;
                        decimal ask_price;
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = App.core.GetTicker(out bid_price, out ask_price);
                            Pusher.FuncExecuted(client, call.FuncCallId, (int)status, bid_price, ask_price);
                        };
                        Console.WriteLine("To queue core.GetTicker()");
                        break;
                    }

                case (int)FuncIds.GetDepth: //получить стаканы
                    {
                        int limit;
                        List<OrderBuf> bids, asks;
                        decimal bids_vol;
                        decimal asks_vol;
                        int bids_num;
                        int asks_num;
                        if (int.TryParse(str_args[0], out limit))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = App.core.GetDepth(limit, out bids, out asks, out bids_vol, out asks_vol, out bids_num, out asks_num);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status, bids, asks, bids_vol, asks_vol, bids_num, asks_num);
                            };
                            Console.WriteLine("To queue core.GetDepth()");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                    //TODO call 4 GLOBAL functions (GET/SET)

                case (int)FuncIds.CloseMarket: //закрытие рынка (запрет на вызов SET-функций юзера)
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Flags.CloseMarket();
                            Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine("To queue Flags.CloseMarket()");
                        break;
                    }

                case (int)FuncIds.OpenMarket: //открытие рынка (снятие ограничений на вызов функций)
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Flags.OpenMarket();
                            Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine("To queue Flags.OpenMarket()");
                        break;
                    }

                case (int)FuncIds.RestartFix: //перезапуск FIX с новыми аккаунтами
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = App.fixman.RestartApp(App.core.GetSenderCompIds());
                            Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine("To queue App.fixman.RestartApp(App.core.GetSenderCompIds())");
                        break;
                    }

                case (int)FuncIds.BackupMasterSnapshot: //блокирующее резервирование снэпшота Master-ядра
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Snapshot.BackupMasterSnapshot();
                            Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine("To queue Snapshot.BackupMasterSnapshot()");
                        break;
                    }

                case (int)FuncIds.RestoreMasterSnapshot: //блокирующее восстановление последнего снэпшота Master-ядра
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Snapshot.RestoreMasterSnapshot();
                            Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine("To queue Snapshot.RestoreMasterSnapshot()");
                        break;
                    }

                case (int)FuncIds.RestoreSlaveSnapshot: //блокирующее восстановление текущего снэпшота Slave-ядра
                    {
                        call = new FuncCall();
                        call.Action = () =>
                        {
                            StatusCodes status = Snapshot.RestoreSlaveSnapshot();
                            Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                        };
                        Console.WriteLine("To queue Snapshot.RestoreSlaveSnapshot()");
                        break;
                    }

                case (int)FuncIds.RestrictWebAppIP: //IP-рестрикт для веб-приложения
                    {
                        if (!String.IsNullOrEmpty(str_args[0]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = IPRestrict.RestrictWebApp(str_args[0]);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue IPRestrict.RestrictWebApp(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.RestrictHttpApiIP: //IP-рестрикт для HTTP API
                    {
                        if (!String.IsNullOrEmpty(str_args[0]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = IPRestrict.RestrictHttpApi(str_args[0]);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue IPRestrict.RestrictHttpApi(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                case (int)FuncIds.RestrictDaemonIP: //IP-рестрикт для демона
                    {
                        if (!String.IsNullOrEmpty(str_args[0]))
                        {
                            call = new FuncCall();
                            call.Action = () =>
                            {
                                StatusCodes status = IPRestrict.RestrictDaemon(str_args[0]);
                                Pusher.FuncExecuted(client, call.FuncCallId, (int)status);
                            };
                            Console.WriteLine("To queue IPRestrict.RestrictDaemon(" + str_args[0] + ")");
                            break;
                        }
                        else
                        {
                            Pusher.FCRejected(client, (int)FCRejCodes.InvalidFuncArgs);
                            return;
                        }
                    }

                default:
                    {
                        Pusher.FCRejected(client, (int)FCRejCodes.FuncNotFound);
                        return;
                    }                
            }

            //сообщаем об успешной регистрации FuncCall
            Pusher.FCAccepted(client, call.FuncCallId);

            //ставим в очередь вызов функции
            Queues.stdf_queue.Enqueue(call);   
        }

    }
}
