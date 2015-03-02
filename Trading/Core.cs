using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class Core
    {
        #region CORE VARIABLES

        #region DATA COLLECTIONS

        private Dictionary<int, Account> Accounts; //торговые счета "ID юзера -> торговый счёт"
        private Dictionary<int, Account> Debitors; //счета дебиторов (использующих заёмные средства)
        private Dictionary<string, OrderBook> OrderBooks; //словарь "производная валюта -> стакан"	
        private Dictionary<long, CancOrdData> CancelOrderDict; //словарь "ID заявки -> параметры и стакан"
        private Dictionary<string, FixAccount> FixAccounts; //FIX-аккаунты
        private Dictionary<string, ApiKey> ApiKeys; //API-ключи

        #endregion

        #region CURRENCY EXCHANGE PARAMETERS

        private string base_currency;
        private char currency_pair_separator;

        #endregion

        #region BUFFER VARIABLES

        private decimal bid_buf;
        private decimal ask_buf;
        private int act_buy_buf_max_size; //TODO change size on-the-fly
        private int act_sell_buf_max_size; //TODO change size on-the-fly
        private List<OrderBuf> act_buy_buf;
        private List<OrderBuf> act_sell_buf;

        #endregion

        #endregion

        #region CORE CONSTRUCTORS

        internal Core(string base_currency, char currency_pair_separator)
        {
            Accounts = new Dictionary<int, Account>(3000);
            Debitors = new Dictionary<int, Account>(1000);
            OrderBooks = new Dictionary<string, OrderBook>(10);
            CancelOrderDict = new Dictionary<long, CancOrdData>(2000);
            FixAccounts = new Dictionary<string, FixAccount>(500);
            ApiKeys = new Dictionary<string, ApiKey>(500);

            this.base_currency = base_currency;
            this.currency_pair_separator = currency_pair_separator;

            bid_buf = 0m;
            ask_buf = 0m;
            act_buy_buf_max_size = 30;
            act_sell_buf_max_size = 30;
            act_buy_buf = new List<OrderBuf>(act_buy_buf_max_size);
            act_sell_buf = new List<OrderBuf>(act_sell_buf_max_size);
        }

        #endregion

        #region CALLABLE CORE FUNCTIONS

        #region USER FUNCTIONS

        internal StatusCodes CreateAccount(int user_id) //открыть торговый счёт
        {
            //реплицировать

            if (!Accounts.ContainsKey(user_id)) //если счёт ещё не открыт, то открываем
            {
                Account acc = new Account(); //открываем счёт в базовой валюте

                foreach (string derived_currency in OrderBooks.Keys) //открываем счета в производных валютах
                {
                    acc.DerivedCFunds.Add(derived_currency, new DerivedFunds());
                }

                Accounts.Add(user_id, acc);
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorAccountAlreadyExists;
        }
        
        internal StatusCodes SuspendAccount(int user_id) //заблокировать торговый счёт
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то блокируем
            {
                if (!acc.Suspended)
                {
                    acc.Suspended = true;
                    return StatusCodes.Success;
                }
                else return StatusCodes.ErrorAccountAlreadySuspended;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes UnsuspendAccount(int user_id) //разблокировать торговый счёт
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то разблокируем
            {
                if (acc.Suspended)
                {
                    acc.Suspended = false;
                    return StatusCodes.Success;
                }
                else return StatusCodes.ErrorAccountAlreadyUnsuspended;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes DeleteAccount(int user_id) //удалить торговый счёт //TODO отменять заявки, возвращать массив средств на выход
        {
            //реплицировать

            if (Accounts.ContainsKey(user_id)) //если счёт существует, то удаляем
            {
                //отменяем заявки

                //удаляем все зависимости
                foreach (OrderBook book in OrderBooks.Values)
                {
                    book.ActiveBuyOrders.RemoveAll(i => i.UserId == user_id);
                    book.ActiveSellOrders.RemoveAll(i => i.UserId == user_id);
                    book.BuySLs.RemoveAll(i => i.UserId == user_id);
                    book.SellSLs.RemoveAll(i => i.UserId == user_id);
                }
                
                //RemoveUserFixAccounts(user_id);
                //RemoveUserApiKeys(user_id);

                //удаляем юзера
                Accounts.Remove(user_id);
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes DepositFunds(int user_id, string currency, decimal amount) //пополнить торговый счёт
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то пополняем
            {
                if (amount > 0m) //проверка на положительность суммы пополнения
                {
                    if (currency == base_currency) //пополнение базовой валюты
                    {
                        acc.BaseCFunds.AvailableFunds += amount;
                        //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                        return StatusCodes.Success;
                    }
                    else //пополнение производной валюты
                    {
                        DerivedFunds funds;
                        if (acc.DerivedCFunds.TryGetValue(currency, out funds))
                        {
                            funds.AvailableFunds += amount;
                            //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                            return StatusCodes.Success;
                        }
                        else return StatusCodes.ErrorCurrencyNotFound;
                    }                    
                }
                else return StatusCodes.ErrorNegativeOrZeroValue;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }
        
        internal StatusCodes WithdrawFunds(int user_id, string currency, decimal amount) //снять с торгового счёта
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то снимаем
            {
                if (acc.Suspended) return StatusCodes.ErrorAccountSuspended; //проверка на блокировку счёта

                if (amount > 0m) //проверка на положительность суммы вывода
                {
                    decimal[] margin_pars = CalcAccMarginPars(acc); //расчёт маржинальных параметров юзера    

                    if (margin_pars[2] > 0)
                    {
                        if (currency == base_currency) //снятие базовой валюты
                        {
                            if (margin_pars[2] >= amount)
                            {
                                acc.BaseCFunds.AvailableFunds -= amount;
                                //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                                return StatusCodes.Success;
                            }
                            else return StatusCodes.ErrorInsufficientFunds;
                        }
                        else //снятие производной валюты
                        {
                            DerivedFunds funds;
                            if (acc.DerivedCFunds.TryGetValue(currency, out funds))
                            {
                                //пересчёт свободной маржи в производную валюту
                                OrderBook book = OrderBooks[currency]; //получение стакана для пары с текущей производной валютой
                                decimal accumulated_amount = 0m; //в производной валюте
                                decimal accumulated_total = 0m; //в базовой валюте
                                for (int i = book.ActiveSellOrders.Count - 1; i >= 0; i--)
                                {
                                    Order sell_ord = book.ActiveSellOrders[i];
                                    accumulated_total += sell_ord.ActualAmount * sell_ord.Rate;
                                    if (accumulated_total >= margin_pars[2]) //если накопленная сумма в заявках на продажу превышает величину свободной маржи
                                    {
                                        accumulated_amount += (margin_pars[2] - accumulated_total + sell_ord.ActualAmount * sell_ord.Rate) / sell_ord.Rate;
                                        break;
                                    }
                                    else //если ещё не превышает - учитываем amount производной валюты
                                    {
                                        accumulated_amount += sell_ord.ActualAmount;
                                    }
                                }

                                if (accumulated_amount >= amount)
                                {
                                    funds.AvailableFunds -= amount;
                                    //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                                    return StatusCodes.Success;
                                }
                                else return StatusCodes.ErrorInsufficientFunds;
                            }
                            else return StatusCodes.ErrorCurrencyNotFound;
                        }
                    }
                    else return StatusCodes.ErrorInsufficientFunds;
                }
                else return StatusCodes.ErrorNegativeOrZeroValue;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }
        
        internal StatusCodes PlaceLimit(int user_id, string derived_currency, bool side, decimal amount, decimal rate, decimal sl_rate, decimal tp_rate, decimal ts_offset, long func_call_id, FCSources fc_source, string external_data = null) //подать лимитную заявку
        {
            //реплицировать

            OrderBook book;
            if (OrderBooks.TryGetValue(derived_currency, out book)) //проверка на существование стакана
            {
                return BaseLimit(user_id, derived_currency, book, side, amount, rate, sl_rate, tp_rate, ts_offset, MessageTypes.NewPlaceLimit, func_call_id, fc_source, external_data);
            }
            else return StatusCodes.ErrorCurrencyPairNotFound;           
        }

        internal StatusCodes PlaceMarket(int user_id, string derived_currency, bool side, bool base_amount, decimal amount, decimal sl_rate, decimal tp_rate, decimal ts_offset, long func_call_id, FCSources fc_source, string external_data = null) //подать рыночную заявку
        {
            //реплицировать

            OrderBook book;
            if (OrderBooks.TryGetValue(derived_currency, out book)) //проверка на существование стакана
            {
                if (!side) //если заявка на покупку (0)
                {
                    if (!base_amount) //amount задан в производной валюте
                    {
                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m;
                        for (int i = book.ActiveSellOrders.Count - 1; i >= 0; i--)
                        {
                            accumulated_amount += book.ActiveSellOrders[i].ActualAmount;
                            if (accumulated_amount >= amount) //если объём накопленных заявок на продажу покрывает объём заявки на покупку
                            {
                                market_rate = book.ActiveSellOrders[i].Rate;
                                break;
                            }
                        }

                        if (market_rate == 0m) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, amount, market_rate, sl_rate, tp_rate, ts_offset, MessageTypes.NewPlaceMarket, func_call_id, fc_source, external_data);
                    }
                    else //amount задан в базовой валюте
                    {
                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m; //в производной валюте
                        decimal accumulated_total = 0m; //в базовой валюте
                        for (int i = book.ActiveSellOrders.Count - 1; i >= 0; i--)
                        {
                            Order sell_ord = book.ActiveSellOrders[i];
                            accumulated_total += sell_ord.ActualAmount * sell_ord.Rate;
                            if (accumulated_total >= amount) //если накопленная сумма в заявках на продажу превышает сумму в заявке на покупку
                            {
                                accumulated_amount += (amount - accumulated_total + sell_ord.ActualAmount * sell_ord.Rate) / sell_ord.Rate;
                                market_rate = sell_ord.Rate;
                                break;
                            }
                            else //если ещё не превышает - учитываем amount производной валюты
                            {
                                accumulated_amount += sell_ord.ActualAmount;
                            }
                        }

                        if (market_rate == 0m) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, accumulated_amount, market_rate, sl_rate, tp_rate, ts_offset, MessageTypes.NewPlaceInstant, func_call_id, fc_source, external_data);
                    }
                }
                else //если заявка на продажу (1)
                {
                    if (!base_amount) //amount задан в производной валюте
                    {
                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m;
                        for (int i = book.ActiveBuyOrders.Count - 1; i >= 0; i--)
                        {
                            accumulated_amount += book.ActiveBuyOrders[i].ActualAmount;
                            if (accumulated_amount >= amount) //если объём накопленных заявок на покупку покрывает объём заявки на продажу
                            {
                                market_rate = book.ActiveBuyOrders[i].Rate;
                                break;
                            }
                        }

                        if (market_rate == 0m) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, amount, market_rate, sl_rate, tp_rate, ts_offset, MessageTypes.NewPlaceMarket, func_call_id, fc_source, external_data);
                    }
                    else //amount задан в базовой валюте
                    {
                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m; //в производной валюте
                        decimal accumulated_total = 0m; //в базовой валюте
                        for (int i = book.ActiveBuyOrders.Count - 1; i >= 0; i--)
                        {
                            Order buy_ord = book.ActiveBuyOrders[i];
                            accumulated_total += buy_ord.ActualAmount * buy_ord.Rate;
                            if (accumulated_total >= amount) //если накопленная сумма в заявках на покупку превышает сумму в заявке на продажу
                            {
                                accumulated_amount += (amount - accumulated_total + buy_ord.ActualAmount * buy_ord.Rate) / buy_ord.Rate;
                                market_rate = buy_ord.Rate;
                                break;
                            }
                            else //если ещё не превышает - учитываем amount производной валюты
                            {
                                accumulated_amount += buy_ord.ActualAmount;
                            }
                        }

                        if (market_rate == 0m) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, accumulated_amount, market_rate, sl_rate, tp_rate, ts_offset, MessageTypes.NewPlaceInstant, func_call_id, fc_source, external_data);
                    }
                }
            }
            else return StatusCodes.ErrorCurrencyPairNotFound;
        }

        internal StatusCodes CancelOrder(int user_id, long order_id, long func_call_id, FCSources fc_source) //отменить активную заявку
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то отменяем заявку
            {
                if (order_id > 0) //проверка на положительность ID заявки
                {
                    CancOrdData ord_canc_data;
                    if (CancelOrderDict.TryGetValue(order_id, out ord_canc_data))
                    {
                        if (ord_canc_data.OrderType == CancOrdTypes.Limit)
                        {
                            if (!ord_canc_data.Side) //нужно отменить заявку на покупку
                            {
                                int buy_index = ord_canc_data.Book.ActiveBuyOrders.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    Order buy_order = ord_canc_data.Book.ActiveBuyOrders[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        if (buy_order.OriginalAmount == buy_order.ActualAmount) //отменяем условные заявки, если удаляемая заявка не начинала исполняться
                                        {
                                            if (buy_order.StopLoss != null)
                                            {
                                                int sell_index = ord_canc_data.Book.SellSLs.IndexOf(buy_order.StopLoss);
                                                if (sell_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveSellSL(sell_index);
                                                    //Pusher.NewOrder((int)MessageTypes.NewRemoveSL, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                                    CancelOrderDict.Remove(buy_order.StopLoss.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (buy_order.TakeProfit != null)
                                            {
                                                int sell_index = ord_canc_data.Book.SellTPs.IndexOf(buy_order.TakeProfit);
                                                if (sell_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveSellTP(sell_index);
                                                    //Pusher.NewOrder((int)MessageTypes.NewRemoveTP, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                                    CancelOrderDict.Remove(buy_order.TakeProfit.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (buy_order.TrailingStop != null)
                                            {
                                                int sell_index = ord_canc_data.Book.SellTSs.IndexOf(buy_order.TrailingStop);
                                                if (sell_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveSellTS(sell_index);
                                                    //Pusher.NewOrder((int)MessageTypes.NewRemoveTS, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                                    CancelOrderDict.Remove(buy_order.TrailingStop.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                        }

                                        ord_canc_data.Book.RemoveBuyOrder(buy_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewCancelOrder, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        decimal total = buy_order.ActualAmount * buy_order.Rate;
                                        acc.BaseCFunds.BlockedFunds -= total;
                                        acc.BaseCFunds.AvailableFunds += total;
                                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                                        
                                        //if (UpdTicker()) Pusher.NewTicker(bid_buf, ask_buf, DateTime.Now); //сообщение о новом тикере
                                        //if (UpdActiveBuyTop()) Pusher.NewActiveBuyTop(act_buy_buf, DateTime.Now); //сообщение о новом топе стакана на покупку
                                        //if (UpdActiveSellTop()) Pusher.NewActiveSellTop(act_sell_buf, DateTime.Now); //сообщение о новом топе стакана на продажу

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                            else //нужно отменить заявку на продажу
                            {
                                int sell_index = ord_canc_data.Book.ActiveSellOrders.FindIndex(i => i.OrderId == order_id);
                                if (sell_index >= 0) //заявка найдена в стакане на продажу
                                {
                                    Order sell_order = ord_canc_data.Book.ActiveSellOrders[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        if (sell_order.OriginalAmount == sell_order.ActualAmount) //отменяем условные заявки, если удаляемая заявка не начинала исполняться
                                        {
                                            if (sell_order.StopLoss != null)
                                            {
                                                int buy_index = ord_canc_data.Book.BuySLs.IndexOf(sell_order.StopLoss);
                                                if (buy_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveBuySL(buy_index);
                                                    //Pusher.NewOrder((int)MessageTypes.NewRemoveSL, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                                    CancelOrderDict.Remove(sell_order.StopLoss.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (sell_order.TakeProfit != null)
                                            {
                                                int buy_index = ord_canc_data.Book.BuyTPs.IndexOf(sell_order.TakeProfit);
                                                if (buy_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveBuyTP(buy_index);
                                                    //Pusher.NewOrder((int)MessageTypes.NewRemoveTP, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                                    CancelOrderDict.Remove(sell_order.TakeProfit.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (sell_order.TrailingStop != null)
                                            {
                                                int buy_index = ord_canc_data.Book.BuyTSs.IndexOf(sell_order.TrailingStop);
                                                if (buy_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveBuyTS(buy_index);
                                                    //Pusher.NewOrder((int)MessageTypes.NewRemoveTS, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                                    CancelOrderDict.Remove(sell_order.TrailingStop.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                        }

                                        ord_canc_data.Book.RemoveSellOrder(sell_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewCancelOrder, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        DerivedFunds derived_funds = acc.DerivedCFunds[ord_canc_data.DerivedCurrency];
                                        derived_funds.BlockedFunds -= sell_order.ActualAmount;
                                        derived_funds.AvailableFunds += sell_order.ActualAmount;
                                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе

                                        //if (UpdTicker()) Pusher.NewTicker(bid_buf, ask_buf, DateTime.Now); //сообщение о новом тикере
                                        //if (UpdActiveBuyTop()) Pusher.NewActiveBuyTop(act_buy_buf, DateTime.Now); //сообщение о новом топе стакана на покупку
                                        //if (UpdActiveSellTop()) Pusher.NewActiveSellTop(act_sell_buf, DateTime.Now); //сообщение о новом топе стакана на продажу

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                        }
                        else if (ord_canc_data.OrderType == CancOrdTypes.StopLoss)
                        {
                            if (!ord_canc_data.Side) //нужно отменить SL на покупку
                            {
                                int buy_index = ord_canc_data.Book.BuySLs.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    Order buy_order = ord_canc_data.Book.BuySLs[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveBuySL(buy_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewRemoveSL, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                            else //нужно отменить SL на продажу
                            {
                                int sell_index = ord_canc_data.Book.SellSLs.FindIndex(i => i.OrderId == order_id);
                                if (sell_index >= 0) //заявка найдена в стакане на продажу
                                {
                                    Order sell_order = ord_canc_data.Book.SellSLs[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveSellSL(sell_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewRemoveSL, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                        }
                        else if (ord_canc_data.OrderType == CancOrdTypes.TakeProfit)
                        {
                            if (!ord_canc_data.Side) //нужно отменить TP на покупку
                            {
                                int buy_index = ord_canc_data.Book.BuyTPs.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    Order buy_order = ord_canc_data.Book.BuyTPs[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveBuyTP(buy_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewRemoveTP, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                            else //нужно отменить TP на продажу
                            {
                                int sell_index = ord_canc_data.Book.SellTPs.FindIndex(i => i.OrderId == order_id);
                                if (sell_index >= 0) //заявка найдена в стакане на продажу
                                {
                                    Order sell_order = ord_canc_data.Book.SellTPs[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveSellTP(sell_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewRemoveTP, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                        }
                        else if (ord_canc_data.OrderType == CancOrdTypes.TrailingStop)
                        {
                            if (!ord_canc_data.Side) //нужно отменить TS на покупку
                            {
                                int buy_index = ord_canc_data.Book.BuyTSs.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    Order buy_order = ord_canc_data.Book.BuyTSs[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveBuyTS(buy_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewRemoveTS, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                            else //нужно отменить TS на продажу
                            {
                                int sell_index = ord_canc_data.Book.SellTSs.FindIndex(i => i.OrderId == order_id);
                                if (sell_index >= 0) //заявка найдена в стакане на продажу
                                {
                                    Order sell_order = ord_canc_data.Book.SellTSs[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveSellTS(sell_index);
                                        //Pusher.NewOrder((int)MessageTypes.NewRemoveTS, func_call_id, fc_source, order_kind, order); //сообщение о новой отмене заявки
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                        }
                        else return StatusCodes.ErrorOrderNotFound;
                    }
                    else return StatusCodes.ErrorOrderNotFound;
                }
                else return StatusCodes.ErrorNegativeOrZeroId;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }





        internal StatusCodes GetAvailableToWithdrawFunds(int user_id, string currency, out decimal amount) //получение суммы средств, доступной для вывода
        {
            amount = 0m;

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то получаем информацию о доступных к снятию средствах
            {
                if (acc.Suspended) return StatusCodes.ErrorAccountSuspended; //проверка на блокировку счёта

                decimal[] margin_pars = CalcAccMarginPars(acc); //расчёт маржинальных параметров юзера    

                if (margin_pars[2] > 0)
                {
                    if (currency == base_currency) //расчёт базовой валюты
                    {
                        amount = margin_pars[2];
                        return StatusCodes.Success;
                    }
                    else //расчёт производной валюты
                    {
                        DerivedFunds funds;
                        if (acc.DerivedCFunds.TryGetValue(currency, out funds))
                        {
                            //пересчёт свободной маржи в производную валюту
                            OrderBook book = OrderBooks[currency]; //получение стакана для пары с текущей производной валютой
                            decimal accumulated_amount = 0m; //в производной валюте
                            decimal accumulated_total = 0m; //в базовой валюте
                            for (int i = book.ActiveSellOrders.Count - 1; i >= 0; i--)
                            {
                                Order sell_ord = book.ActiveSellOrders[i];
                                accumulated_total += sell_ord.ActualAmount * sell_ord.Rate;
                                if (accumulated_total >= margin_pars[2]) //если накопленная сумма в заявках на продажу превышает величину свободной маржи
                                {
                                    accumulated_amount += (margin_pars[2] - accumulated_total + sell_ord.ActualAmount * sell_ord.Rate) / sell_ord.Rate;
                                    break;
                                }
                                else //если ещё не превышает - учитываем amount производной валюты
                                {
                                    accumulated_amount += sell_ord.ActualAmount;
                                }
                            }

                            amount = accumulated_amount;
                            return StatusCodes.Success;
                        }
                        else return StatusCodes.ErrorCurrencyNotFound;
                    }
                }
                else
                {
                    return StatusCodes.Success;
                }
            }
            else return StatusCodes.ErrorAccountNotFound;
        }



        #endregion

        #region GLOBAL FUNCTIONS

        internal StatusCodes CreateCurrencyPair(string derived_currency)
        {
            //реплицировать

            //проверка символа производной валюты на пустоту
            if (!String.IsNullOrEmpty(derived_currency))
            {
                //открываем клиентские счета в производной валюте
                foreach (KeyValuePair<int, Account> acc in Accounts)
                {
                    if (!acc.Value.DerivedCFunds.ContainsKey(derived_currency)) acc.Value.DerivedCFunds.Add(derived_currency, new DerivedFunds());
                }

                //создаём стакан для данной производной валюты
                if (!OrderBooks.ContainsKey(derived_currency))
                {
                    OrderBooks.Add(derived_currency, new OrderBook());
                    return StatusCodes.Success;
                }
                else return StatusCodes.ErrorCurrencyPairAlreadyExists;
            }
            else return StatusCodes.ErrorInvalidCurrency;
        }

        internal StatusCodes GetCurrencyPairs(out List<string> currency_pairs)
        {
            currency_pairs = new List<string>(10);

            foreach (string derived_currency in OrderBooks.Keys) //формируем текущие валютные пары
            {
                currency_pairs.Add(derived_currency + currency_pair_separator + base_currency);
            }
            
            return StatusCodes.Success;
        }

        internal StatusCodes GetDerivedCurrencies(out List<string> derived_currencies)
        {
            derived_currencies = new List<string>(OrderBooks.Keys);
            return StatusCodes.Success;
        }

        internal StatusCodes DeleteCurrencyPair() // TODO IN MARCH
        {
            //реплицировать

            return StatusCodes.Success;
        }
        
        #endregion

        #endregion

        #region SERVICE CORE FUNCTIONS

        #region ORDER PLACEMENT & MATCHING

        private StatusCodes BaseLimit(int user_id, string derived_currency, OrderBook book, bool side, decimal amount, decimal rate, decimal sl_rate, decimal tp_rate, decimal ts_offset, MessageTypes msg_type, long func_call_id, FCSources fc_source, string external_data) //базовая функция размещения заявки 
        {
            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то снимаем с него сумму и подаём заявку
            {
                if (acc.Suspended) return StatusCodes.ErrorAccountSuspended; //проверка на блокировку счёта
                if (String.IsNullOrEmpty(derived_currency)) return StatusCodes.ErrorInvalidCurrency; //проверка на корректность производной валюты
                if (amount <= 0m || rate <= 0m || sl_rate < 0m || tp_rate < 0m || ts_offset < 0m) return StatusCodes.ErrorNegativeOrZeroValue; //проверка значений на положительность 
                
                if (!side) //если заявка на покупку (0)
                {
                    bool use_sl_rate = false;
                    if (sl_rate > 0m)
                    {
                        if (book.ActiveBuyOrders.Count > 0) //проверка уровня SL по отношению к рыночной цене на покупку
                        {
                            if (sl_rate < book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate) //сравнение с рыночным курсом на покупку (SL будет на продажу)
                            {
                                use_sl_rate = true;
                            }
                            else return StatusCodes.ErrorIncorrectStopLossRate;
                        }
                        else return StatusCodes.ErrorStopLossUnavailable;
                    }

                    bool use_tp_rate = false;
                    if (tp_rate > 0m)
                    {
                        if (book.ActiveBuyOrders.Count > 0) //проверка уровня TP по отношению к рыночной цене на покупку
                        {
                            if (tp_rate > book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate) //сравнение с рыночным курсом на покупку (TP будет на продажу)
                            {
                                use_tp_rate = true;
                            }
                            else return StatusCodes.ErrorIncorrectTakeProfitRate;
                        }
                        else return StatusCodes.ErrorTakeProfitUnavailable;
                    }

                    bool use_ts_offset = false;
                    decimal ts_rate = 0m;
                    if (ts_offset > 0m)
                    {
                        if (book.ActiveBuyOrders.Count > 0) //проверка уровня TS по отношению к рыночной цене на покупку
                        {
                            ts_rate = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate - ts_offset;
                            if (ts_rate > 0m) //сравнение с рыночным курсом на покупку (TS будет на продажу)
                            {
                                use_ts_offset = true;
                            }
                            else return StatusCodes.ErrorIncorrectTrailingStopOffset;
                        }
                        else return StatusCodes.ErrorTrailingStopUnavailable;
                    }

                    decimal total = amount * rate;
                    if (acc.BaseCFunds.AvailableFunds >= total) //проверка на платежеспособность по базовой валюте
                    {
                        acc.BaseCFunds.AvailableFunds -= total; //снимаем средства с доступных средств
                        acc.BaseCFunds.BlockedFunds += total; //блокируем средства в заявке на покупку
                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                        
                        Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                        book.InsertBuyOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                        //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
                        
                        if (use_sl_rate)
                        {
                            Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                            order.StopLoss = sl_order;
                            book.InsertSellSL(sl_order);
                            CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder((int)MessageTypes.NewAddSL, func_call_id, fc_source, position_type, order); //сообщение о новом SL
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                        }
                        
                        if (use_tp_rate)
                        {
                            Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                            order.TakeProfit = tp_order;
                            book.InsertSellTP(tp_order);
                            CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder((int)MessageTypes.NewAddTP, func_call_id, fc_source, position_type, order); //сообщение о новом TP
                        }

                        if (use_ts_offset)
                        {
                            TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                            order.TrailingStop = ts_order;
                            book.InsertSellTS(ts_order);
                            CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder((int)MessageTypes.NewAddTS, func_call_id, fc_source, position_type, order); //сообщение о новом TS
                        }

                        InterlinkCondOrds(order); //линковка SL/TP/TS

                        Match(derived_currency, book);
                        return StatusCodes.Success;
                    }
                    else //проверка лонга с плечом
                    {
                        decimal[] margin_pars = CalcAccMarginPars(acc); //расчёт маржинальных параметров юзера

                        if (margin_pars[2] * acc.MaxLeverage >= total)
                        {
                            if (!Debitors.ContainsKey(user_id)) Debitors.Add(user_id, acc); //добавление юзера в словарь дебиторов
                            acc.BaseCFunds.AvailableFunds -= total; //снимаем средства с доступных средств
                            acc.BaseCFunds.BlockedFunds += total; //блокируем средства в заявке на покупку                            
                            //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе

                            Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                            book.InsertBuyOrder(order);
                            CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                            //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                            if (use_sl_rate)
                            {
                                Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                                order.StopLoss = sl_order;
                                book.InsertSellSL(sl_order);
                                CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                                //Pusher.NewOrder((int)MessageTypes.NewAddSL, func_call_id, fc_source, position_type, order); //сообщение о новом SL
                                //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                            }

                            if (use_tp_rate)
                            {
                                Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                                order.TakeProfit = tp_order;
                                book.InsertSellTP(tp_order);
                                CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                                //Pusher.NewOrder((int)MessageTypes.NewAddTP, func_call_id, fc_source, position_type, order); //сообщение о новом TP
                            }

                            if (use_ts_offset)
                            {
                                TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                                order.TrailingStop = ts_order;
                                book.InsertSellTS(ts_order);
                                CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                                //Pusher.NewOrder((int)MessageTypes.NewAddTS, func_call_id, fc_source, position_type, order); //сообщение о новом TS
                            }

                            InterlinkCondOrds(order); //линковка SL/TP/TS

                            Match(derived_currency, book);
                            return StatusCodes.Success;
                        }
                        else return StatusCodes.ErrorInsufficientFunds;                        
                    }
                }
                else //если заявка на продажу (0)
                {
                    bool use_sl_rate = false;
                    if (sl_rate > 0m)
                    {
                        if (book.ActiveSellOrders.Count > 0) //проверка уровня SL по отношению к рыночной цене на продажу
                        {
                            if (sl_rate > book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate) //сравнение с рыночным курсом на продажу (SL будет на покупку)
                            {
                                use_sl_rate = true;
                            }
                            else return StatusCodes.ErrorIncorrectStopLossRate;
                        }
                        else return StatusCodes.ErrorStopLossUnavailable;
                    }

                    bool use_tp_rate = false;
                    if (tp_rate > 0m)
                    {
                        if (book.ActiveSellOrders.Count > 0) //проверка уровня TP по отношению к рыночной цене на продажу
                        {
                            if (tp_rate < book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate) //сравнение с рыночным курсом на продажу (TP будет на покупку)
                            {
                                use_tp_rate = true;
                            }
                            else return StatusCodes.ErrorIncorrectTakeProfitRate;
                        }
                        else return StatusCodes.ErrorTakeProfitUnavailable;
                    }

                    bool use_ts_offset = false;
                    decimal ts_rate = 0m;
                    if (ts_offset > 0m)
                    {
                        if (book.ActiveSellOrders.Count > 0) //проверка уровня TS по отношению к рыночной цене на продажу
                        {
                            ts_rate = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate + ts_offset;
                            if (ts_rate > 0m) //сравнение с рыночным курсом на продажу (TS будет на покупку)
                            {
                                use_ts_offset = true;
                            }
                            else return StatusCodes.ErrorIncorrectTrailingStopOffset;
                        }
                        else return StatusCodes.ErrorTrailingStopUnavailable;
                    }

                    DerivedFunds derived_funds = acc.DerivedCFunds[derived_currency];
                    if (derived_funds.AvailableFunds >= amount) //проверка на платежеспособность по производной валюте
                    {
                        derived_funds.AvailableFunds -= amount; //снимаем средства с доступных средств
                        derived_funds.BlockedFunds += amount; //блокируем средства в заявке на продажу
                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе

                        Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                        book.InsertSellOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                        //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                        if (use_sl_rate)
                        {
                            Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                            order.StopLoss = sl_order;
                            book.InsertBuySL(sl_order);
                            CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder((int)MessageTypes.NewAddSL, func_call_id, fc_source, position_type, order); //сообщение о новом SL
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                        }

                        if (use_tp_rate)
                        {
                            Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                            order.TakeProfit = tp_order;
                            book.InsertBuyTP(tp_order);
                            CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder((int)MessageTypes.NewAddTP, func_call_id, fc_source, position_type, order); //сообщение о новом TP
                        }

                        if (use_ts_offset)
                        {
                            TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                            order.TrailingStop = ts_order;
                            book.InsertBuyTS(ts_order);
                            CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder((int)MessageTypes.NewAddTS, func_call_id, fc_source, position_type, order); //сообщение о новом TS
                        }

                        InterlinkCondOrds(order); //линковка SL/TP/TS

                        Match(derived_currency, book);
                        return StatusCodes.Success;
                    }
                    else //проверка шорта с плечом
                    {
                        decimal[] margin_pars = CalcAccMarginPars(acc); //расчёт маржинальных параметров юзера

                        if (margin_pars[2] * acc.MaxLeverage >= amount * rate)
                        {
                            if (!Debitors.ContainsKey(user_id)) Debitors.Add(user_id, acc); //добавление юзера в словарь дебиторов
                            derived_funds.AvailableFunds -= amount; //снимаем средства с доступных средств
                            derived_funds.BlockedFunds += amount; //блокируем средства в заявке на продажу
                            //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе

                            Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                            book.InsertSellOrder(order);
                            CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                            //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                            //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                            if (use_sl_rate)
                            {
                                Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                                order.StopLoss = sl_order;
                                book.InsertBuySL(sl_order);
                                CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                                //Pusher.NewOrder((int)MessageTypes.NewAddSL, func_call_id, fc_source, position_type, order); //сообщение о новом SL
                                //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                            }

                            if (use_tp_rate)
                            {
                                Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                                order.TakeProfit = tp_order;
                                book.InsertBuyTP(tp_order);
                                CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                                //Pusher.NewOrder((int)MessageTypes.NewAddTP, func_call_id, fc_source, position_type, order); //сообщение о новом TP
                            }

                            if (use_ts_offset)
                            {
                                TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                                order.TrailingStop = ts_order;
                                book.InsertBuyTS(ts_order);
                                CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                                //Pusher.NewOrder((int)MessageTypes.NewAddTS, func_call_id, fc_source, position_type, order); //сообщение о новом TS
                            }

                            InterlinkCondOrds(order); //линковка SL/TP/TS

                            Match(derived_currency, book);
                            return StatusCodes.Success;
                        }
                        else return StatusCodes.ErrorInsufficientFunds;
                    }
                }
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        private void Match(string derived_currency, OrderBook book) //выполняет метчинг текущих активных заявок в заданном стакане
        {
            //проверка коллекций на пустоту
            if (Accounts.Count == 0 || book.ActiveBuyOrders.Count == 0 || book.ActiveSellOrders.Count == 0)
            {
                //if (UpdTicker()) Pusher.NewTicker(bid_buf, ask_buf, DateTime.Now); //сообщение о новом тикере
                //if (UpdActiveBuyTop()) Pusher.NewActiveBuyTop(act_buy_buf, DateTime.Now); //сообщение о новом топе стакана на покупку
                //if (UpdActiveSellTop()) Pusher.NewActiveSellTop(act_sell_buf, DateTime.Now); //сообщение о новом топе стакана на продажу
                return;
            }

            //итеративный алгоритм метчинга по топовым заявкам
            while (book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate >= book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate)
            {
                //сохранение указателей на заявки, валютные средства, комиссии
                Order buy_ord = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1];
                Order sell_ord = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1];
                Account buyer = Accounts[buy_ord.UserId];
                Account seller = Accounts[sell_ord.UserId];
                DerivedFunds buyer_derived_funds = buyer.DerivedCFunds[derived_currency];
                DerivedFunds seller_derived_funds = seller.DerivedCFunds[derived_currency];
                
                //поиск наиболее ранней заявки для определения trade_side, trade_rate (и комиссий)
                bool trade_side;
                decimal trade_rate;
                if (buy_ord.OrderId < sell_ord.OrderId)
                {
                    trade_side = false; //buy
                    trade_rate = buy_ord.Rate;
                    //дифференциация комиссий maker-taker тут
                }
                else
                {
                    trade_side = true; //sell
                    trade_rate = sell_ord.Rate;
                    //дифференциация комиссий maker-taker тут
                }
                
                //три варианта в зависимости от объёма каждой из 2-х выполняемых заявок
                if (buy_ord.ActualAmount > sell_ord.ActualAmount) //1-ый вариант - объём buy-заявки больше
                {
                    //добавляем объект Trade в коллекцию
                    Trade trade = new Trade(buy_ord.OrderId, sell_ord.OrderId, buy_ord.UserId, sell_ord.UserId, trade_side, sell_ord.ActualAmount, trade_rate, sell_ord.ActualAmount * buyer_derived_funds.Fee, sell_ord.ActualAmount * trade_rate * seller_derived_funds.Fee);
                    //Pusher.NewTrade(trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_derived_funds.BlockedFunds -= sell_ord.ActualAmount;
                    seller.BaseCFunds.AvailableFunds += sell_ord.ActualAmount * trade_rate * (1m - seller_derived_funds.Fee);
                    //Pusher.NewBalance(sell_ord.UserId, seller, DateTime.Now); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer.BaseCFunds.BlockedFunds -= sell_ord.ActualAmount * buy_ord.Rate;
                    buyer_derived_funds.AvailableFunds += sell_ord.ActualAmount * (1m - buyer_derived_funds.Fee);
                    buyer.BaseCFunds.AvailableFunds += sell_ord.ActualAmount * (buy_ord.Rate - trade_rate);
                    //Pusher.NewBalance(buy_ord.UserId, buyer, DateTime.Now); //сообщение о новом балансе

                    //увеличивается ActualAmount привязанных к buy-заявке SL/TP/TS заявок
                    if (buy_ord.StopLoss != null) buy_ord.StopLoss.ActualAmount += sell_ord.ActualAmount;
                    if (buy_ord.TakeProfit != null) buy_ord.TakeProfit.ActualAmount += sell_ord.ActualAmount;
                    if (buy_ord.TrailingStop != null) buy_ord.TrailingStop.ActualAmount += sell_ord.ActualAmount;

                    //buy-заявка становится partially filled => уменьшается её ActualAmount
                    buy_ord.ActualAmount -= sell_ord.ActualAmount;
                    //Pusher.NewOrderStatus(buy_ord.OrderId, buy_ord.UserId, (int)OrdExecStatus.PartiallyFilled, DateTime.Now); //сообщение о новом статусе заявки
                    
                    //увеличивается ActualAmount привязанных к sell-заявке SL/TP/TS заявок
                    if (sell_ord.StopLoss != null) sell_ord.StopLoss.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TakeProfit != null) sell_ord.TakeProfit.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TrailingStop != null) sell_ord.TrailingStop.ActualAmount += sell_ord.ActualAmount;

                    //sell-заявка становится filled => её ActualAmount становится нулевым
                    sell_ord.ActualAmount = 0m;
                    //Pusher.NewOrderStatus(sell_ord.OrderId, sell_ord.UserId, (int)OrdExecStatus.Filled, DateTime.Now); //сообщение о новом статусе заявки                    

                    //FIX multicast
                    //FixMessager.NewMarketDataIncrementalRefresh(trade);

                    //FIX-сообщения о новых сделках
                    //if (buy_ord.FCSource == (int)FCSources.FixApi) FixMessager.NewExecutionReport(buy_ord.ExternalData, false, buy_ord, trade);
                    //if (sell_ord.FCSource == (int)FCSources.FixApi) FixMessager.NewExecutionReport(sell_ord.ExternalData, true, sell_ord, trade);
                                        
                    //т.к. объём buy-заявки больше, sell-заявка удаляется из списка активных заявок
                    book.ActiveSellOrders.RemoveAt(book.ActiveSellOrders.Count - 1);

                    //удаление ID заявки из словаря на закрытие
                    CancelOrderDict.Remove(sell_ord.OrderId);
                }
                else if (buy_ord.ActualAmount < sell_ord.ActualAmount) //2-ой вариант - объём sell-заявки больше
                {
                    //добавляем объект Trade в коллекцию
                    Trade trade = new Trade(buy_ord.OrderId, sell_ord.OrderId, buy_ord.UserId, sell_ord.UserId, trade_side, buy_ord.ActualAmount, trade_rate, buy_ord.ActualAmount * buyer_derived_funds.Fee, buy_ord.ActualAmount * trade_rate * seller_derived_funds.Fee);
                    //Pusher.NewTrade(trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_derived_funds.BlockedFunds -= buy_ord.ActualAmount;
                    seller.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * trade_rate * (1m - seller_derived_funds.Fee);
                    //Pusher.NewBalance(sell_ord.UserId, seller, DateTime.Now); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer.BaseCFunds.BlockedFunds -= buy_ord.ActualAmount * buy_ord.Rate;
                    buyer_derived_funds.AvailableFunds += buy_ord.ActualAmount * (1m - buyer_derived_funds.Fee);
                    buyer.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * (buy_ord.Rate - trade_rate);
                    //Pusher.NewBalance(buy_ord.UserId, buyer, DateTime.Now); //сообщение о новом балансе

                    //увеличивается ActualAmount привязанных к sell-заявке SL/TP/TS заявок
                    if (sell_ord.StopLoss != null) sell_ord.StopLoss.ActualAmount += buy_ord.ActualAmount;
                    if (sell_ord.TakeProfit != null) sell_ord.TakeProfit.ActualAmount += buy_ord.ActualAmount;
                    if (sell_ord.TrailingStop != null) sell_ord.TrailingStop.ActualAmount += buy_ord.ActualAmount;

                    //sell-заявка становится partially filled => уменьшается её ActualAmount
                    sell_ord.ActualAmount -= buy_ord.ActualAmount;
                    //Pusher.NewOrderStatus(sell_ord.OrderId, sell_ord.UserId, (int)OrdExecStatus.PartiallyFilled, DateTime.Now); //сообщение о новом статусе заявки

                    //увеличивается ActualAmount привязанных к buy-заявке SL/TP/TS заявок
                    if (buy_ord.StopLoss != null) buy_ord.StopLoss.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TakeProfit != null) buy_ord.TakeProfit.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TrailingStop != null) buy_ord.TrailingStop.ActualAmount += buy_ord.ActualAmount;

                    //buy-заявка становится filled => её ActualAmount становится нулевым
                    buy_ord.ActualAmount = 0m;
                    //Pusher.NewOrderStatus(buy_ord.OrderId, buy_ord.UserId, (int)OrdExecStatus.Filled, DateTime.Now); //сообщение о новом статусе заявки

                    //FIX multicast
                    //FixMessager.NewMarketDataIncrementalRefresh(trade);

                    //FIX-сообщения о новых сделках
                    //if (buy_ord.FCSource == (int)FCSources.FixApi) FixMessager.NewExecutionReport(buy_ord.ExternalData, false, buy_ord, trade);
                    //if (sell_ord.FCSource == (int)FCSources.FixApi) FixMessager.NewExecutionReport(sell_ord.ExternalData, true, sell_ord, trade);

                    //т.к. объём sell-заявки больше, buy-заявка удаляется из списка активных заявок
                    book.ActiveBuyOrders.RemoveAt(book.ActiveBuyOrders.Count - 1);

                    //удаление ID заявки из словаря на закрытие
                    CancelOrderDict.Remove(buy_ord.OrderId);
                }
                else if (buy_ord.ActualAmount == sell_ord.ActualAmount) //3-ий вариант - объёмы заявок равны
                {
                    //добавляем объект Trade в коллекцию
                    Trade trade = new Trade(buy_ord.OrderId, sell_ord.OrderId, buy_ord.UserId, sell_ord.UserId, trade_side, buy_ord.ActualAmount, trade_rate, sell_ord.ActualAmount * buyer_derived_funds.Fee, sell_ord.ActualAmount * trade_rate * seller_derived_funds.Fee);
                    //Pusher.NewTrade(trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_derived_funds.BlockedFunds -= buy_ord.ActualAmount;
                    seller.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * trade_rate * (1m - seller_derived_funds.Fee);
                    //Pusher.NewBalance(sell_ord.UserId, seller, DateTime.Now); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer.BaseCFunds.BlockedFunds -= buy_ord.ActualAmount * buy_ord.Rate;
                    buyer_derived_funds.AvailableFunds += buy_ord.ActualAmount * (1m - buyer_derived_funds.Fee);
                    buyer.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * (buy_ord.Rate - trade_rate);
                    //Pusher.NewBalance(buy_ord.UserId, buyer, DateTime.Now); //сообщение о новом балансе

                    //увеличивается ActualAmount привязанных к buy-заявке SL/TP/TS заявок
                    if (buy_ord.StopLoss != null) buy_ord.StopLoss.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TakeProfit != null) buy_ord.TakeProfit.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TrailingStop != null) buy_ord.TrailingStop.ActualAmount += buy_ord.ActualAmount;

                    //buy-заявка становится filled => её ActualAmount становится нулевым
                    buy_ord.ActualAmount = 0m;
                    //Pusher.NewOrderStatus(buy_ord.OrderId, buy_ord.UserId, (int)OrdExecStatus.Filled, DateTime.Now); //сообщение о новом статусе заявки

                    //увеличивается ActualAmount привязанных к sell-заявке SL/TP/TS заявок
                    if (sell_ord.StopLoss != null) sell_ord.StopLoss.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TakeProfit != null) sell_ord.TakeProfit.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TrailingStop != null) sell_ord.TrailingStop.ActualAmount += sell_ord.ActualAmount;

                    //sell-заявка становится filled => её ActualAmount становится нулевым
                    sell_ord.ActualAmount = 0m;
                    //Pusher.NewOrderStatus(sell_ord.OrderId, sell_ord.UserId, (int)OrdExecStatus.Filled, DateTime.Now); //сообщение о новом статусе заявки

                    //FIX multicast
                    //FixMessager.NewMarketDataIncrementalRefresh(trade);

                    //FIX-сообщения о новых сделках
                    //if (buy_ord.FCSource == (int)FCSources.FixApi) FixMessager.NewExecutionReport(buy_ord.ExternalData, false, buy_ord, trade);
                    //if (sell_ord.FCSource == (int)FCSources.FixApi) FixMessager.NewExecutionReport(sell_ord.ExternalData, true, sell_ord, trade);

                    //т.к. объёмы заявок равны, обе заявки удаляются из списка активных заявок
                    book.ActiveBuyOrders.RemoveAt(book.ActiveBuyOrders.Count - 1);
                    book.ActiveSellOrders.RemoveAt(book.ActiveSellOrders.Count - 1);

                    //удаление ID заявок из словаря на закрытие
                    CancelOrderDict.Remove(buy_ord.OrderId);
                    CancelOrderDict.Remove(sell_ord.OrderId);
                }

                //если все заявки в стакане (стаканах) были удалены - выходим из цикла
                if ((book.ActiveBuyOrders.Count == 0) || (book.ActiveSellOrders.Count == 0)) break;
            }

            //if (UpdTicker()) Pusher.NewTicker(bid_buf, ask_buf, DateTime.Now); //сообщение о новом тикере
            //if (UpdActiveBuyTop()) Pusher.NewActiveBuyTop(act_buy_buf, DateTime.Now); //сообщение о новом топе стакана на покупку
            //if (UpdActiveSellTop()) Pusher.NewActiveSellTop(act_sell_buf, DateTime.Now); //сообщение о новом топе стакана на продажу
        }

        #endregion

        #region MARGIN MANAGEMENT

        internal void ManageMargin() //расчёт маржинальных параметров, выполнение MC/FL в случае необходимости
        {
            //реплицировать

            //учёт id юзеров, закрывших позиции с плечом
            List<int> ids_to_rm = new List<int>();

            //проверка каждого дебитора
            foreach (KeyValuePair<int, Account> acc in Debitors)
            {
                decimal[] margin_pars = CalcAccMarginPars(acc.Value); //расчёт маржинальных параметров юзера

                //проверка на использование заёмных средств
                if (margin_pars[1] == 0m)
                {
                    if (acc.Value.MarginCall) acc.Value.MarginCall = false; //сбрасываем флаг Margin Call
                    acc.Value.Equity = 0m;
                    acc.Value.Margin = 0m;
                    acc.Value.FreeMargin = 0m;
                    acc.Value.MarginLevel = 0m;
                    //Pusher.NewMarginInfo(account.Key, 0m, 100m, DateTime.Now); //сообщение о новом уровне маржи
                    ids_to_rm.Add(acc.Key);
                    continue;
                }

                if (acc.Value.Equity != margin_pars[0]) //обновляем параметры аккаунта, если equity изменился
                {
                    acc.Value.Equity = margin_pars[0];
                    acc.Value.Margin = margin_pars[1];
                    acc.Value.FreeMargin = margin_pars[2];
                    acc.Value.MarginLevel = margin_pars[3];
                    //Pusher.NewMarginInfo(account.Key, account.Value.Equity, account.Value.MarginLevel * 100m, DateTime.Now); //сообщение о новом уровне маржи
                }

                //проверка условия Margin Call
                if (margin_pars[3] <= acc.Value.LevelMC)
                {
                    if (!acc.Value.MarginCall)
                    {
                        acc.Value.MarginCall = true;
                        //Pusher.NewMarginCall(account.Key, DateTime.Now); //сообщение о новом Margin Call
                    }
                }
                else if (acc.Value.MarginCall) acc.Value.MarginCall = false; //сброс флага Margin Call

                //проверка условия Forced Liquidation
                if (margin_pars[3] <= acc.Value.LevelFL)
                {
                    //поиск валюты с наибольшей рыночной стоимостью суммы
                    string fl_derived_currency = null;
                    OrderBook fl_book = null;
                    bool fl_side = new bool();
                    decimal fl_amount = 0m;
                    decimal fl_market_rate = 0m;
                    decimal fl_sum = 0m;

                    foreach (KeyValuePair<string, DerivedFunds> funds in acc.Value.DerivedCFunds)
                    {
                        if (funds.Value.AvailableFunds == 0m) continue;
                        OrderBook cur_book = OrderBooks[funds.Key]; //получение стакана для пары с текущей производной валютой
                        bool positive = (funds.Value.AvailableFunds > 0m);
                        List<Order> ActiveOrders = positive ? cur_book.ActiveBuyOrders : cur_book.ActiveSellOrders; //определение направления калькуляции рыночной цены (buy/sell)
                        decimal amount = positive ? funds.Value.AvailableFunds : funds.Value.AvailableFunds * (-1m) / (1m - funds.Value.Fee);

                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m;
                        for (int i = ActiveOrders.Count - 1; i >= 0; i--)
                        {
                            accumulated_amount += ActiveOrders[i].ActualAmount;
                            if (accumulated_amount >= amount) //если объём накопленных заявок превышает сумму в производной валюте на счёте
                            {
                                market_rate = ActiveOrders[i].Rate;
                                break;
                            }
                        }

                        decimal cur_sum = amount * market_rate;
                        if (cur_sum > fl_sum)
                        {
                            fl_derived_currency = funds.Key;
                            fl_book = cur_book;
                            fl_side = positive;
                            fl_amount = amount;
                            fl_market_rate = market_rate;
                            fl_sum = cur_sum;
                        }
                    }
                    
                    //ликвидация наибольшей позиции
                    if (!fl_side) //заявка на покупку
                    {
                        acc.Value.BaseCFunds.AvailableFunds -= fl_sum; //снимаем средства с доступных средств
                        acc.Value.BaseCFunds.BlockedFunds += fl_sum; //блокируем средства в заявке на покупку                            
                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                        Order order = new Order(acc.Key, fl_amount, fl_amount, fl_market_rate);
                        fl_book.InsertBuyOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(fl_derived_currency, fl_book, CancOrdTypes.Limit, fl_side)); //добавление заявки в словарь на закрытие
                        //Pusher.NewOrder((int)MessageTypes.NewForcedLiquidation, false, order); //сообщение о новой FL-заявке
                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
                        Match(fl_derived_currency, fl_book);
                    }
                    else //заявка на продажу
                    {
                        DerivedFunds derived_funds = acc.Value.DerivedCFunds[fl_derived_currency];
                        derived_funds.AvailableFunds -= fl_amount; //снимаем средства с доступных средств
                        derived_funds.BlockedFunds += fl_amount; //блокируем средства в заявке на продажу
                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                        Order order = new Order(acc.Key, fl_amount, fl_amount, fl_market_rate);
                        fl_book.InsertSellOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(fl_derived_currency, fl_book, CancOrdTypes.Limit, fl_side)); //добавление заявки в словарь на закрытие
                        //Pusher.NewOrder((int)MessageTypes.NewForcedLiquidation, true, order); //сообщение о новой FL-заявке
                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
                        Match(fl_derived_currency, fl_book);
                    }
                }
            }

            for (int i = 0; i < ids_to_rm.Count; i++)
            {
                Debitors.Remove(ids_to_rm[i]);
            }
        }

        private decimal[] CalcAccMarginPars(Account acc)
        {
            decimal[] margin_pars = new decimal[4]; //Equity, Margin, Free Margin, Margin Level

            //объявление необходимых переменных
            decimal debit = 0m;
            decimal credit = 0m;

            //оценка суммы в базовой валюте
            if (acc.BaseCFunds.AvailableFunds >= 0m) debit += acc.BaseCFunds.AvailableFunds; //начисляем дебету положительную сумму в базовой валюте
            else credit -= acc.BaseCFunds.AvailableFunds; //начисляем кредиту положительную сумму в базовой валюте

            //оценка сумм в производных валютах, приведённых к базовой
            foreach (KeyValuePair<string, DerivedFunds> funds in acc.DerivedCFunds)
            {
                if (funds.Value.AvailableFunds == 0m) continue;
                OrderBook cur_book = OrderBooks[funds.Key]; //получение стакана для пары с текущей производной валютой
                bool positive = (funds.Value.AvailableFunds > 0m);
                List<Order> ActiveOrders = positive ? cur_book.ActiveBuyOrders : cur_book.ActiveSellOrders; //определение направления калькуляции рыночной цены (buy/sell)
                decimal amount = positive ? funds.Value.AvailableFunds : funds.Value.AvailableFunds * (-1m) / (1m - funds.Value.Fee);

                decimal market_rate = 0m;
                decimal accumulated_amount = 0m;
                for (int i = ActiveOrders.Count - 1; i >= 0; i--)
                {
                    accumulated_amount += ActiveOrders[i].ActualAmount;
                    if (accumulated_amount >= amount) //если объём накопленных заявок превышает сумму в производной валюте на счёте
                    {
                        market_rate = ActiveOrders[i].Rate;
                        break;
                    }
                }

                if (positive) debit += amount * market_rate; //начисляем дебету положительную сумму в базовой валюте
                else credit += amount * market_rate; //начисляем кредиту положительную сумму в базовой валюте
            }

            margin_pars[0] = debit - credit; //калькуляция Equity
            margin_pars[1] = credit / acc.MaxLeverage; //калькуляция Margin
            margin_pars[2] = margin_pars[0] - margin_pars[1]; //калькуляция Free Margin
            margin_pars[3] = (margin_pars[1] != 0m) ? margin_pars[0] / margin_pars[1] : 0m; //калькуляция Margin Level

            return margin_pars;
        }

        #endregion

        #region CONDITIONAL ORDERS MANAGEMENT

        internal void ManageConditionalOrders() //проверка и расчёт условных заявок во всех стаканах
        {
            foreach (KeyValuePair<string, OrderBook> book in OrderBooks)
            {
                ManageSLs(book.Key, book.Value);
                ManageTPs(book.Key, book.Value);
                ManageTSs(book.Key, book.Value);
            }
        }

        private void ManageSLs(string derived_currency, OrderBook book)
        {
            //реплицировать

            //проверяем условия SL на продажу
            for (int i = book.SellSLs.Count - 1; i >= 0; i--)
            {
                if (book.ActiveBuyOrders.Count > 0)
                {
                    Order sell_sl = book.SellSLs[i];
                    if (book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate <= sell_sl.Rate) //сравнение с рыночным курсом на покупку (SL будет на продажу)
                    {
                        //создаём рыночную заявку на продажу по рынку
                        Account acc = Accounts[sell_sl.UserId];
                        DerivedFunds derived_funds = acc.DerivedCFunds[derived_currency];

                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m;
                        for (int j = book.ActiveBuyOrders.Count - 1; j >= 0; j--)
                        {
                            accumulated_amount += book.ActiveBuyOrders[j].ActualAmount;
                            if (accumulated_amount >= sell_sl.ActualAmount) //если объём накопленных заявок на покупку покрывает объём заявки на продажу
                            {
                                market_rate = book.ActiveBuyOrders[j].Rate;
                                break;
                            }
                        }

                        if (market_rate == 0) continue;
                                                
                        if (derived_funds.AvailableFunds >= sell_sl.ActualAmount) //проверка на платежеспособность по производной валюте
                        {
                            derived_funds.AvailableFunds -= sell_sl.ActualAmount; //снимаем средства с доступных средств
                            derived_funds.BlockedFunds += sell_sl.ActualAmount; //блокируем средства в заявке на продажу
                            //Pusher.NewBalance(sell_sl.UserId, acc, DateTime.Now); //сообщение о новом балансе
                                                        
                            book.SellSLs.RemoveAt(i); //удаляем SL из памяти
                            if (sell_sl.TakeProfit != null)
                            {
                                if (book.SellTPs.Remove(sell_sl.TakeProfit)) CancelOrderDict.Remove(sell_sl.TakeProfit.OrderId); //удаляем слинкованный TP из памяти
                            }
                            if (sell_sl.TrailingStop != null)
                            {
                                if (book.SellTSs.Remove(sell_sl.TrailingStop)) CancelOrderDict.Remove(sell_sl.TrailingStop.OrderId); //удаляем слинкованный TS из памяти
                            }

                            sell_sl.Rate = market_rate; //присвоение рыночной цены
                            book.InsertSellOrder(sell_sl);
                            CancelOrderDict[sell_sl.OrderId].OrderType = CancOrdTypes.Limit;
                            //Pusher.NewOrder((int)MessageTypes.NewExecSL, true, new_sell_order); //сообщение о срабатывании SL
                            //FixMessager.NewMarketDataIncrementalRefresh(true, new_sell_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.SellSLs.RemoveAt(i); //удаляем SL из памяти
                            CancelOrderDict.Remove(sell_sl.OrderId); //удаляем заявку из словаря на закрытие
                            //TODO сообщение о том, что исполнение провалилось из-за отсутствия кэша
                        }
                    }
                    else break;
                }
            }

            //проверяем условия SL на покупку
            for (int i = book.BuySLs.Count - 1; i >= 0; i--)
            {
                if (book.ActiveSellOrders.Count > 0)
                {
                    Order buy_sl = book.BuySLs[i];
                    if (book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate >= buy_sl.Rate) //сравнение с рыночным курсом на продажу (стоп-лосс будет на покупку)
                    {
                        //создаём рыночную заявку на покупку по рынку
                        Account acc = Accounts[buy_sl.UserId];
                        buy_sl.ActualAmount = buy_sl.ActualAmount / (1m - acc.DerivedCFunds[derived_currency].Fee); //поправка на списание комиссии

                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m;
                        for (int j = book.ActiveSellOrders.Count - 1; j >= 0; j--)
                        {
                            accumulated_amount += book.ActiveSellOrders[j].ActualAmount;
                            if (accumulated_amount >= buy_sl.ActualAmount) //если объём накопленных заявок на продажу покрывает объём заявки на покупку
                            {
                                market_rate = book.ActiveSellOrders[j].Rate;
                                break;
                            }
                        }

                        if (market_rate == 0) continue;

                        decimal total = buy_sl.ActualAmount * market_rate;
                        if (acc.BaseCFunds.AvailableFunds >= total) //проверка на платежеспособность по базовой валюте
                        {
                            acc.BaseCFunds.AvailableFunds -= total; //снимаем средства с доступных средств
                            acc.BaseCFunds.BlockedFunds += total; //блокируем средства в заявке на продажу
                            //Pusher.NewBalance(buy_sl.UserId, acc, DateTime.Now); //сообщение о новом балансе

                            book.BuySLs.RemoveAt(i); //удаляем SL из памяти
                            if (buy_sl.TakeProfit != null)
                            {
                                if (book.BuyTPs.Remove(buy_sl.TakeProfit)) CancelOrderDict.Remove(buy_sl.TakeProfit.OrderId); //удаляем слинкованный TP из памяти
                            }
                            if (buy_sl.TrailingStop != null)
                            {
                                if (book.BuyTSs.Remove(buy_sl.TrailingStop)) CancelOrderDict.Remove(buy_sl.TrailingStop.OrderId); //удаляем слинкованный TS из памяти
                            }

                            buy_sl.Rate = market_rate;
                            book.InsertBuyOrder(buy_sl);
                            CancelOrderDict[buy_sl.OrderId].OrderType = CancOrdTypes.Limit;
                            //Pusher.NewOrder((int)MessageTypes.NewExecSL, false, new_buy_order); //сообщение о срабатывании SL
                            //FixMessager.NewMarketDataIncrementalRefresh(false, new_buy_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.BuySLs.RemoveAt(i); //удаляем SL из памяти
                            CancelOrderDict.Remove(buy_sl.OrderId); //удаляем заявку из словаря на закрытие
                            //TODO сообщение о том, что исполнение провалилось из-за отсутствия кэша
                        }
                    }
                    else break;
                }
            }
        }

        private void ManageTPs(string derived_currency, OrderBook book)
        {
            //реплицировать

            //проверяем условия TP на продажу
            for (int i = book.SellTPs.Count - 1; i >= 0; i--)
            {
                if (book.ActiveBuyOrders.Count > 0)
                {
                    Order sell_tp = book.SellTPs[i];
                    if (book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate >= sell_tp.Rate) //сравнение с рыночным курсом на покупку (TP будет на продажу)
                    {
                        //создаём рыночную заявку на продажу по рынку
                        Account acc = Accounts[sell_tp.UserId];
                        DerivedFunds derived_funds = acc.DerivedCFunds[derived_currency];

                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m;
                        for (int j = book.ActiveBuyOrders.Count - 1; j >= 0; j--)
                        {
                            accumulated_amount += book.ActiveBuyOrders[j].ActualAmount;
                            if (accumulated_amount >= sell_tp.ActualAmount) //если объём накопленных заявок на покупку покрывает объём заявки на продажу
                            {
                                market_rate = book.ActiveBuyOrders[j].Rate;
                                break;
                            }
                        }

                        if (market_rate == 0) continue;

                        if (derived_funds.AvailableFunds >= sell_tp.ActualAmount) //проверка на платежеспособность по производной валюте
                        {
                            derived_funds.AvailableFunds -= sell_tp.ActualAmount; //снимаем средства с доступных средств
                            derived_funds.BlockedFunds += sell_tp.ActualAmount; //блокируем средства в заявке на продажу
                            //Pusher.NewBalance(sell_tp.UserId, acc, DateTime.Now); //сообщение о новом балансе

                            book.SellTPs.RemoveAt(i); //удаляем TP из памяти
                            if (sell_tp.StopLoss != null)
                            {
                                if (book.SellSLs.Remove(sell_tp.StopLoss)) CancelOrderDict.Remove(sell_tp.StopLoss.OrderId); //удаляем слинкованный SL из памяти
                            }
                            if (sell_tp.TrailingStop != null)
                            {
                                if (book.SellTSs.Remove(sell_tp.TrailingStop)) CancelOrderDict.Remove(sell_tp.TrailingStop.OrderId); //удаляем слинкованный TS из памяти
                            }

                            sell_tp.Rate = market_rate; //присвоение рыночной цены
                            book.InsertSellOrder(sell_tp);
                            CancelOrderDict[sell_tp.OrderId].OrderType = CancOrdTypes.Limit;
                            //Pusher.NewOrder((int)MessageTypes.NewExecTP, true, new_sell_order); //сообщение о срабатывании TP
                            //FixMessager.NewMarketDataIncrementalRefresh(true, new_sell_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.SellTPs.RemoveAt(i); //удаляем TP из памяти
                            CancelOrderDict.Remove(sell_tp.OrderId); //удаляем заявку из словаря на закрытие
                            //TODO сообщение о том, что исполнение провалилось из-за отсутствия кэша
                        }
                    }
                    else break;
                }
            }

            //проверяем условия TP на покупку
            for (int i = book.BuyTPs.Count - 1; i >= 0; i--)
            {
                if (book.ActiveSellOrders.Count > 0)
                {
                    Order buy_tp = book.BuyTPs[i];
                    if (book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate <= buy_tp.Rate) //сравнение с рыночным курсом на продажу (стоп-лосс будет на покупку)
                    {
                        //создаём рыночную заявку на покупку по рынку
                        Account acc = Accounts[buy_tp.UserId];
                        buy_tp.ActualAmount = buy_tp.ActualAmount / (1m - acc.DerivedCFunds[derived_currency].Fee); //поправка на списание комиссии

                        //калькуляция rate для немедленного исполнения по рынку
                        decimal market_rate = 0m;
                        decimal accumulated_amount = 0m;
                        for (int j = book.ActiveSellOrders.Count - 1; j >= 0; j--)
                        {
                            accumulated_amount += book.ActiveSellOrders[j].ActualAmount;
                            if (accumulated_amount >= buy_tp.ActualAmount) //если объём накопленных заявок на продажу покрывает объём заявки на покупку
                            {
                                market_rate = book.ActiveSellOrders[j].Rate;
                                break;
                            }
                        }

                        if (market_rate == 0) continue;

                        decimal total = buy_tp.ActualAmount * market_rate;
                        if (acc.BaseCFunds.AvailableFunds >= total) //проверка на платежеспособность по базовой валюте
                        {
                            acc.BaseCFunds.AvailableFunds -= total; //снимаем средства с доступных средств
                            acc.BaseCFunds.BlockedFunds += total; //блокируем средства в заявке на продажу
                            //Pusher.NewBalance(buy_tp.UserId, acc, DateTime.Now); //сообщение о новом балансе

                            book.BuyTPs.RemoveAt(i); //удаляем TP из памяти
                            if (buy_tp.StopLoss != null)
                            {
                                if (book.BuySLs.Remove(buy_tp.StopLoss)) CancelOrderDict.Remove(buy_tp.StopLoss.OrderId); //удаляем слинкованный SL из памяти
                            }
                            if (buy_tp.TrailingStop != null)
                            {
                                if (book.BuyTSs.Remove(buy_tp.TrailingStop)) CancelOrderDict.Remove(buy_tp.TrailingStop.OrderId); //удаляем слинкованный TS из памяти
                            }

                            buy_tp.Rate = market_rate;
                            book.InsertBuyOrder(buy_tp);
                            CancelOrderDict[buy_tp.OrderId].OrderType = CancOrdTypes.Limit;
                            //Pusher.NewOrder((int)MessageTypes.NewExecTP, false, new_buy_order); //сообщение о срабатывании TP
                            //FixMessager.NewMarketDataIncrementalRefresh(false, new_buy_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.BuyTPs.RemoveAt(i); //удаляем TP из памяти
                            CancelOrderDict.Remove(buy_tp.OrderId); //удаляем заявку из словаря на закрытие
                            //TODO сообщение о том, что исполнение провалилось из-за отсутствия кэша
                        }
                    }
                    else break;
                }
            }
        }

        private void ManageTSs(string derived_currency, OrderBook book)
        {
            //реплицировать

            //проверяем условия TS на продажу
            for (int i = book.SellTSs.Count - 1; i >= 0; i--)
            {
                if (book.ActiveBuyOrders.Count > 0)
                {
                    TSOrder sell_ts = book.SellTSs[i];
                    decimal cur_ts_rate = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate - sell_ts.Offset;
                    if (cur_ts_rate > sell_ts.Rate) //произошло повышение рыночной цены на покупку
                    {
                        //подтягиваем наверх цену TS
                        sell_ts.Rate = cur_ts_rate;
                    }
                    else if (cur_ts_rate == sell_ts.Rate) break; //цена не изменилась
                    else //цена упала => проверяем условие на срабатывание TS
                    {
                        if (book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate <= sell_ts.Rate) //сравнение с рыночным курсом на покупку (заявка будет на продажу)
                        {
                            //создаём рыночную заявку на продажу по рынку
                            Account acc = Accounts[sell_ts.UserId];
                            DerivedFunds derived_funds = acc.DerivedCFunds[derived_currency];

                            //калькуляция rate для немедленного исполнения по рынку
                            decimal market_rate = 0m;
                            decimal accumulated_amount = 0m;
                            for (int j = book.ActiveBuyOrders.Count - 1; j >= 0; j--)
                            {
                                accumulated_amount += book.ActiveBuyOrders[j].ActualAmount;
                                if (accumulated_amount >= sell_ts.ActualAmount) //если объём накопленных заявок на покупку покрывает объём заявки на продажу
                                {
                                    market_rate = book.ActiveBuyOrders[j].Rate;
                                    break;
                                }
                            }

                            if (market_rate == 0) continue;

                            if (derived_funds.AvailableFunds >= sell_ts.ActualAmount) //проверка на платежеспособность по производной валюте
                            {
                                derived_funds.AvailableFunds -= sell_ts.ActualAmount; //снимаем средства с доступных средств
                                derived_funds.BlockedFunds += sell_ts.ActualAmount; //блокируем средства в заявке на продажу
                                //Pusher.NewBalance(sell_ts.UserId, acc, DateTime.Now); //сообщение о новом балансе

                                book.SellTSs.RemoveAt(i); //удаляем TS из памяти
                                if (sell_ts.StopLoss != null)
                                {
                                    if (book.SellSLs.Remove(sell_ts.StopLoss)) CancelOrderDict.Remove(sell_ts.StopLoss.OrderId); //удаляем слинкованный SL из памяти
                                }
                                if (sell_ts.TakeProfit != null)
                                {
                                    if (book.SellTPs.Remove(sell_ts.TakeProfit)) CancelOrderDict.Remove(sell_ts.TakeProfit.OrderId); //удаляем слинкованный TP из памяти
                                }

                                sell_ts.Rate = market_rate; //присвоение рыночной цены
                                book.InsertSellOrder(sell_ts);
                                CancelOrderDict[sell_ts.OrderId].OrderType = CancOrdTypes.Limit;
                                //Pusher.NewOrder((int)MessageTypes.NewExecTS, true, new_sell_order); //сообщение о срабатывании TS
                                //FixMessager.NewMarketDataIncrementalRefresh(true, new_sell_order); //FIX multicast

                                Match(derived_currency, book);
                            }
                            else
                            {
                                book.SellTSs.RemoveAt(i); //удаляем TS из памяти
                                CancelOrderDict.Remove(sell_ts.OrderId); //удаляем заявку из словаря на закрытие
                                //TODO сообщение о том, что исполнение провалилось из-за отсутствия кэша
                            }
                        }
                    }
                }
            }

            //проверяем условия TS на покупку
            for (int i = book.BuyTSs.Count - 1; i >= 0; i--)
            {
                if (book.ActiveSellOrders.Count > 0)
                {
                    TSOrder buy_ts = book.BuyTSs[i];
                    decimal cur_ts_rate = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate + buy_ts.Offset;
                    if (cur_ts_rate < buy_ts.Rate) //произошло понижение рыночной цены на продажу
                    {
                        //подтягиваем вниз цену TS
                        buy_ts.Rate = cur_ts_rate;
                    }
                    else if (cur_ts_rate == buy_ts.Rate) break; //цена не изменилась
                    else //цена выросла => проверяем условие на срабатывание TS
                    {
                        if (book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate >= buy_ts.Rate) //сравнение с рыночным курсом на покупку (заявка будет на покупку)
                        {
                            //создаём рыночную заявку на покупку по рынку
                            Account acc = Accounts[buy_ts.UserId];
                            buy_ts.ActualAmount = buy_ts.ActualAmount / (1m - acc.DerivedCFunds[derived_currency].Fee); //поправка на списание комиссии

                            //калькуляция rate для немедленного исполнения по рынку
                            decimal market_rate = 0m;
                            decimal accumulated_amount = 0m;
                            for (int j = book.ActiveSellOrders.Count - 1; j >= 0; j--)
                            {
                                accumulated_amount += book.ActiveSellOrders[j].ActualAmount;
                                if (accumulated_amount >= buy_ts.ActualAmount) //если объём накопленных заявок на продажу покрывает объём заявки на покупку
                                {
                                    market_rate = book.ActiveSellOrders[j].Rate;
                                    break;
                                }
                            }

                            if (market_rate == 0) continue;

                            decimal total = buy_ts.ActualAmount * market_rate;
                            if (acc.BaseCFunds.AvailableFunds >= total) //проверка на платежеспособность по базовой валюте
                            {
                                acc.BaseCFunds.AvailableFunds -= total; //снимаем средства с доступных средств
                                acc.BaseCFunds.BlockedFunds += total; //блокируем средства в заявке на продажу
                                //Pusher.NewBalance(buy_ts.UserId, acc, DateTime.Now); //сообщение о новом балансе

                                book.BuyTSs.RemoveAt(i); //удаляем TS из памяти
                                if (buy_ts.StopLoss != null)
                                {
                                    if (book.BuySLs.Remove(buy_ts.StopLoss)) CancelOrderDict.Remove(buy_ts.StopLoss.OrderId); //удаляем слинкованный SL из памяти
                                }
                                if (buy_ts.TakeProfit != null)
                                {
                                    if (book.BuyTPs.Remove(buy_ts.TakeProfit)) CancelOrderDict.Remove(buy_ts.TakeProfit.OrderId); //удаляем слинкованный TP из памяти
                                }

                                buy_ts.Rate = market_rate; //присвоение рыночной цены
                                book.InsertBuyOrder(buy_ts);
                                CancelOrderDict[buy_ts.OrderId].OrderType = CancOrdTypes.Limit;
                                //Pusher.NewOrder((int)MessageTypes.NewExecTS, false, new_buy_order); //сообщение о срабатывании TS
                                //FixMessager.NewMarketDataIncrementalRefresh(false, new_buy_order); //FIX multicast

                                Match(derived_currency, book);
                            }
                            else
                            {
                                book.BuyTSs.RemoveAt(i); //удаляем TS из памяти
                                CancelOrderDict.Remove(buy_ts.OrderId); //удаляем заявку из словаря на закрытие
                                //TODO сообщение о том, что исполнение провалилось из-за отсутствия кэша
                            }
                        }
                    }
                }
            }
        }

        private void InterlinkCondOrds(Order order)
        {
            if (order.StopLoss != null && order.TakeProfit != null) //линковка SL/TP
            {
                order.StopLoss.TakeProfit = order.TakeProfit;
                order.TakeProfit.StopLoss = order.StopLoss;
            }

            if (order.TakeProfit != null && order.TrailingStop != null) //линковка TP/TS
            {
                order.TakeProfit.TrailingStop = order.TrailingStop;
                order.TrailingStop.TakeProfit = order.TakeProfit;
            }

            if (order.StopLoss != null && order.TrailingStop != null) //линковка SL/TS
            {
                order.StopLoss.TrailingStop = order.TrailingStop;
                order.TrailingStop.StopLoss = order.StopLoss;
            }
        }

        #endregion
        
        #region CURRENCY PAIR MANAGEMENT

        private string[] SplitCurrencyPair(string currency_pair) //получение валют, входящих в пару
        {
            int separator_index = currency_pair.IndexOf(currency_pair_separator);
            if (separator_index != -1) return new string[] { currency_pair.Substring(0, separator_index), currency_pair.Substring(separator_index + 1) };
            else return new string[0];
        }

        #endregion

        #endregion
        
    }
}