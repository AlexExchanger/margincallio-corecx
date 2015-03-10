using System;
using System.Linq;
using System.Collections.Generic;
using CoreCX.Gateways.TCP;

namespace CoreCX.Trading
{
    [Serializable]
    class Core
    {
        #region CORE VARIABLES

        #region DATA COLLECTIONS

        private Dictionary<int, Account> Accounts; //торговые счета "ID юзера -> торговый счёт"
        private Dictionary<string, OrderBook> OrderBooks; //словарь "производная валюта -> стакан"	
        private Dictionary<long, CancOrdData> CancelOrderDict; //словарь "ID заявки -> параметры и стакан"
        private Dictionary<string, FixAccount> FixAccounts; //FIX-аккаунты
        private Dictionary<string, ApiKey> ApiKeys; //API-ключи

        #endregion

        #region CURRENCY EXCHANGE PARAMETERS

        private string base_currency;
        private char currency_pair_separator;

        #endregion
               
        #endregion

        #region CORE CONSTRUCTORS

        internal Core(string base_currency, char currency_pair_separator)
        {
            Accounts = new Dictionary<int, Account>(3000);
            OrderBooks = new Dictionary<string, OrderBook>(10);
            CancelOrderDict = new Dictionary<long, CancOrdData>(2000);
            FixAccounts = new Dictionary<string, FixAccount>(500);
            ApiKeys = new Dictionary<string, ApiKey>(500);

            this.base_currency = base_currency;
            this.currency_pair_separator = currency_pair_separator;
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
                        Pusher.NewBalance(user_id, currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                        return StatusCodes.Success;
                    }
                    else //пополнение производной валюты
                    {
                        DerivedFunds funds;
                        if (acc.DerivedCFunds.TryGetValue(currency, out funds))
                        {
                            funds.AvailableFunds += amount;
                            Pusher.NewBalance(user_id, currency, funds.AvailableFunds, funds.BlockedFunds); //сообщение о новом балансе
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

                    if (margin_pars[1] == 0m) //юзер не использует заёмные средства (credit = 0)
                    {
                        if (currency == base_currency) //снятие базовой валюты
                        {
                            if (acc.BaseCFunds.AvailableFunds >= amount)
                            {
                                acc.BaseCFunds.AvailableFunds -= amount;
                                Pusher.NewBalance(user_id, currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                                return StatusCodes.Success;
                            }
                            else return StatusCodes.ErrorInsufficientFunds;
                        }
                        else //снятие производной валюты
                        {
                            DerivedFunds funds;
                            if (acc.DerivedCFunds.TryGetValue(currency, out funds))
                            {
                                if (funds.AvailableFunds >= amount)
                                {
                                    funds.AvailableFunds -= amount;
                                    Pusher.NewBalance(user_id, currency, funds.AvailableFunds, funds.BlockedFunds); //сообщение о новом балансе
                                    return StatusCodes.Success;
                                }
                                else return StatusCodes.ErrorInsufficientFunds;
                            }
                            else return StatusCodes.ErrorCurrencyNotFound;
                        }
                    }
                    else return StatusCodes.ErrorBorrowedFundsUse;
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
                return BaseLimit(user_id, derived_currency, book, side, amount, rate, sl_rate, tp_rate, ts_offset, OrderEvents.PlaceLimit, func_call_id, fc_source, external_data);
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

                        return BaseLimit(user_id, derived_currency, book, side, amount, market_rate, sl_rate, tp_rate, ts_offset, OrderEvents.PlaceMarket, func_call_id, fc_source, external_data);
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

                        return BaseLimit(user_id, derived_currency, book, side, accumulated_amount, market_rate, sl_rate, tp_rate, ts_offset, OrderEvents.PlaceMarket, func_call_id, fc_source, external_data);
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

                        return BaseLimit(user_id, derived_currency, book, side, amount, market_rate, sl_rate, tp_rate, ts_offset, OrderEvents.PlaceMarket, func_call_id, fc_source, external_data);
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

                        return BaseLimit(user_id, derived_currency, book, side, accumulated_amount, market_rate, sl_rate, tp_rate, ts_offset, OrderEvents.PlaceMarket, func_call_id, fc_source, external_data);
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
                                                    CancelOrderDict.Remove(buy_order.StopLoss.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (buy_order.TakeProfit != null)
                                            {
                                                int sell_index = ord_canc_data.Book.SellTPs.IndexOf(buy_order.TakeProfit);
                                                if (sell_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveSellTP(sell_index);
                                                    CancelOrderDict.Remove(buy_order.TakeProfit.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (buy_order.TrailingStop != null)
                                            {
                                                int sell_index = ord_canc_data.Book.SellTSs.IndexOf(buy_order.TrailingStop);
                                                if (sell_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveSellTS(sell_index);
                                                    CancelOrderDict.Remove(buy_order.TrailingStop.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                        }

                                        ord_canc_data.Book.RemoveBuyOrder(buy_index);                                        
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        decimal total = buy_order.ActualAmount * buy_order.Rate;
                                        acc.BaseCFunds.BlockedFunds -= total;
                                        acc.BaseCFunds.AvailableFunds += total;

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, buy_order); //сообщение о новой отмене заявки
                                        Pusher.NewBalance(user_id, base_currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                                        PushOrderBookUpdates(ord_canc_data.DerivedCurrency, ord_canc_data.Book);

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
                                                    CancelOrderDict.Remove(sell_order.StopLoss.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (sell_order.TakeProfit != null)
                                            {
                                                int buy_index = ord_canc_data.Book.BuyTPs.IndexOf(sell_order.TakeProfit);
                                                if (buy_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveBuyTP(buy_index);
                                                    CancelOrderDict.Remove(sell_order.TakeProfit.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                            if (sell_order.TrailingStop != null)
                                            {
                                                int buy_index = ord_canc_data.Book.BuyTSs.IndexOf(sell_order.TrailingStop);
                                                if (buy_index >= 0)
                                                {
                                                    ord_canc_data.Book.RemoveBuyTS(buy_index);
                                                    CancelOrderDict.Remove(sell_order.TrailingStop.OrderId); //удаляем заявку из словаря на закрытие
                                                }
                                            }
                                        }

                                        ord_canc_data.Book.RemoveSellOrder(sell_index);
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        DerivedFunds derived_funds = acc.DerivedCFunds[ord_canc_data.DerivedCurrency];
                                        derived_funds.BlockedFunds -= sell_order.ActualAmount;
                                        derived_funds.AvailableFunds += sell_order.ActualAmount;

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, sell_order); //сообщение о новой отмене заявки
                                        Pusher.NewBalance(user_id, ord_canc_data.DerivedCurrency, derived_funds.AvailableFunds, derived_funds.BlockedFunds); //сообщение о новом балансе
                                        PushOrderBookUpdates(ord_canc_data.DerivedCurrency, ord_canc_data.Book);

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
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, buy_order); //сообщение о новой отмене заявки

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
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, sell_order); //сообщение о новой отмене заявки

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
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, buy_order); //сообщение о новой отмене заявки

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
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, sell_order); //сообщение о новой отмене заявки

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
                                    TSOrder buy_order = ord_canc_data.Book.BuyTSs[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveBuyTS(buy_index);
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, buy_order); //сообщение о новой отмене заявки

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
                                    TSOrder sell_order = ord_canc_data.Book.SellTSs[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        ord_canc_data.Book.RemoveSellTS(sell_index);
                                        CancelOrderDict.Remove(order_id); //удаляем заявку из словаря на закрытие

                                        Pusher.NewOrder(OrderEvents.Cancel, fc_source, func_call_id, ord_canc_data.DerivedCurrency, ord_canc_data.Side, sell_order); //сообщение о новой отмене заявки

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

        internal StatusCodes SetAccountFee(int user_id, string derived_currency, decimal fee_in_perc, long func_call_id) //установить размер комиссии для торгового счёта
        {
            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то получаем комиссию
            {
                DerivedFunds funds;
                if (acc.DerivedCFunds.TryGetValue(derived_currency, out funds))
                {
                    if (fee_in_perc >= 0 && fee_in_perc <= 100) //проверка на корректность процентного значения
                    {
                        funds.Fee = fee_in_perc / 100m;
                        Pusher.NewAccountFee(func_call_id, user_id, derived_currency, fee_in_perc); //сообщение о новой комиссии

                        return StatusCodes.Success;
                    }
                    else return StatusCodes.ErrorIncorrectPercValue;
                }
                else return StatusCodes.ErrorCurrencyPairNotFound;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }
        
        internal StatusCodes GetAccountBalance(int user_id, string currency, out BaseFunds funds) //получить баланс торгового счёта
        {
            funds = null;

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то получаем баланс
            {
                if (currency == base_currency) //баланс базовой валюты
                {
                    funds = new BaseFunds(acc.BaseCFunds);
                    return StatusCodes.Success;
                }
                else //баланс производной валюты
                {
                    DerivedFunds derived_funds;
                    if (acc.DerivedCFunds.TryGetValue(currency, out derived_funds))
                    {
                        funds = new BaseFunds(derived_funds);
                        return StatusCodes.Success;
                    }
                    else return StatusCodes.ErrorCurrencyNotFound;
                }
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes GetAccountBalance(int user_id, out Dictionary<string, BaseFunds> funds) //получить все балансы торгового счёта
        {
            funds = null;

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то получаем баланс
            {
                funds = new Dictionary<string, BaseFunds>();
                funds.Add(base_currency, new BaseFunds(acc.BaseCFunds));
                
                foreach (KeyValuePair<string, DerivedFunds> derived_funds in acc.DerivedCFunds)
                {
                    funds.Add(derived_funds.Key, new BaseFunds(derived_funds.Value));
                }

                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes GetAccountParameters(int user_id, out Account acc_pars) //получить значения параметров торгового счёта
        {
            acc_pars = null;

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то получаем параметры
            {
                acc_pars = new Account(acc);
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes GetAccountFee(int user_id, string derived_currency, out decimal fee_in_perc) //получить размер комиссии для торгового счёта
        {
            fee_in_perc = 0m;

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то получаем комиссию
            {
                DerivedFunds funds;
                if (acc.DerivedCFunds.TryGetValue(derived_currency, out funds))
                {
                    fee_in_perc = funds.Fee * 100m;
                    return StatusCodes.Success;
                }
                else return StatusCodes.ErrorCurrencyPairNotFound;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes GetOpenOrders(int user_id, string derived_currency, out List<Order> buy_limit, out List<Order> sell_limit, out List<Order> buy_sl, out List<Order> sell_sl, out List<Order> buy_tp, out List<Order> sell_tp, out List<TSOrder> buy_ts, out List<TSOrder> sell_ts) //получить открытые заявки
        {
            buy_limit = null;
            sell_limit = null;
            buy_sl = null;
            sell_sl = null;
            buy_tp = null;
            sell_tp = null;
            buy_ts = null;
            sell_ts = null;

            if (Accounts.ContainsKey(user_id)) //если счёт существует, то получаем активные заявки
            {
                OrderBook book;
                if (OrderBooks.TryGetValue(derived_currency, out book)) //проверка на существование стакана
                {
                    buy_limit = new List<Order>();
                    sell_limit = new List<Order>();
                    buy_sl = new List<Order>();
                    sell_sl = new List<Order>();
                    buy_tp = new List<Order>();
                    sell_tp = new List<Order>();
                    buy_ts = new List<TSOrder>();
                    sell_ts = new List<TSOrder>();

                    for (int i = book.ActiveBuyOrders.Count - 1; i >= 0; i--)
                    {
                        Order cur_ord = book.ActiveBuyOrders[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            buy_limit.Add(new Order(cur_ord));
                        }
                    }
                    for (int i = book.ActiveSellOrders.Count - 1; i >= 0; i--)
                    {
                        Order cur_ord = book.ActiveSellOrders[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            sell_limit.Add(new Order(cur_ord));
                        }
                    }
                    for (int i = book.BuySLs.Count - 1; i >= 0; i--)
                    {
                        Order cur_ord = book.BuySLs[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            buy_sl.Add(new Order(cur_ord));
                        }
                    }
                    for (int i = book.SellSLs.Count - 1; i >= 0; i--)
                    {
                        Order cur_ord = book.SellSLs[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            sell_sl.Add(new Order(cur_ord));
                        }
                    }
                    for (int i = book.BuyTPs.Count - 1; i >= 0; i--)
                    {
                        Order cur_ord = book.BuyTPs[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            buy_tp.Add(new Order(cur_ord));
                        }
                    }
                    for (int i = book.SellTPs.Count - 1; i >= 0; i--)
                    {
                        Order cur_ord = book.SellTPs[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            sell_tp.Add(new Order(cur_ord));
                        }
                    }
                    for (int i = book.BuyTSs.Count - 1; i >= 0; i--)
                    {
                        TSOrder cur_ord = book.BuyTSs[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            buy_ts.Add(new TSOrder(cur_ord, cur_ord.Offset));
                        }
                    }
                    for (int i = book.SellTSs.Count - 1; i >= 0; i--)
                    {
                        TSOrder cur_ord = book.SellTSs[i];
                        if (cur_ord.UserId == user_id) //deep clone
                        {
                            sell_ts.Add(new TSOrder(cur_ord, cur_ord.Offset));
                        }
                    }

                    return StatusCodes.Success;
                }
                else return StatusCodes.ErrorCurrencyPairNotFound;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes GetOrderInfo(int user_id, long order_id, out string derived_currency, out bool side, out Order order) //получить параметры заявки
        {            
            derived_currency = null;
            side = new bool();
            order = null;

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то получаем параметры заявки
            {
                if (order_id > 0) //проверка на положительность ID заявки
                {
                    CancOrdData ord_canc_data;
                    if (CancelOrderDict.TryGetValue(order_id, out ord_canc_data))
                    {
                        if (ord_canc_data.OrderType == CancOrdTypes.Limit)
                        {
                            if (!ord_canc_data.Side) //получаем параметры заявки на покупку
                            {
                                int buy_index = ord_canc_data.Book.ActiveBuyOrders.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    Order buy_order = ord_canc_data.Book.ActiveBuyOrders[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new Order(buy_order);
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
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new Order(sell_order);
                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                        }
                        else if (ord_canc_data.OrderType == CancOrdTypes.StopLoss)
                        {
                            if (!ord_canc_data.Side) //получаем параметры SL на покупку
                            {
                                int buy_index = ord_canc_data.Book.BuySLs.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    Order buy_order = ord_canc_data.Book.BuySLs[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new Order(buy_order);
                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                            else //получаем параметры SL на продажу
                            {
                                int sell_index = ord_canc_data.Book.SellSLs.FindIndex(i => i.OrderId == order_id);
                                if (sell_index >= 0) //заявка найдена в стакане на продажу
                                {
                                    Order sell_order = ord_canc_data.Book.SellSLs[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new Order(sell_order);
                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                        }
                        else if (ord_canc_data.OrderType == CancOrdTypes.TakeProfit)
                        {
                            if (!ord_canc_data.Side) //получаем параметры TP на покупку
                            {
                                int buy_index = ord_canc_data.Book.BuyTPs.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    Order buy_order = ord_canc_data.Book.BuyTPs[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new Order(buy_order);
                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                            else //получаем параметры TP на продажу
                            {
                                int sell_index = ord_canc_data.Book.SellTPs.FindIndex(i => i.OrderId == order_id);
                                if (sell_index >= 0) //заявка найдена в стакане на продажу
                                {
                                    Order sell_order = ord_canc_data.Book.SellTPs[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new Order(sell_order);
                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                        }
                        else if (ord_canc_data.OrderType == CancOrdTypes.TrailingStop)
                        {
                            if (!ord_canc_data.Side) //получаем параметры TS на покупку
                            {
                                int buy_index = ord_canc_data.Book.BuyTSs.FindIndex(i => i.OrderId == order_id);
                                if (buy_index >= 0) //заявка найдена в стакане на покупку
                                {
                                    TSOrder buy_order = ord_canc_data.Book.BuyTSs[buy_index];
                                    if (buy_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new TSOrder(buy_order, buy_order.Offset);
                                        return StatusCodes.Success;
                                    }
                                    else return StatusCodes.ErrorCrossUserAccessDenied;
                                }
                                else return StatusCodes.ErrorOrderNotFound;
                            }
                            else //получаем параметры TS на продажу
                            {
                                int sell_index = ord_canc_data.Book.SellTSs.FindIndex(i => i.OrderId == order_id);
                                if (sell_index >= 0) //заявка найдена в стакане на продажу
                                {
                                    TSOrder sell_order = ord_canc_data.Book.SellTSs[sell_index];
                                    if (sell_order.UserId == user_id) //данная заявка принадлежит данному юзеру
                                    {
                                        derived_currency = ord_canc_data.DerivedCurrency;
                                        side = ord_canc_data.Side;
                                        order = new TSOrder(sell_order, sell_order.Offset);
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

        internal StatusCodes GetTicker(string derived_currency, out decimal bid, out decimal ask)
        {
            bid = 0m;
            ask = 0m;

            OrderBook book;
            if (OrderBooks.TryGetValue(derived_currency, out book)) //проверка на существование стакана
            {
                if (book.ActiveBuyOrders.Count > 0) bid = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate; //bid price
                if (book.ActiveSellOrders.Count > 0) ask = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate; //ask price

                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorCurrencyPairNotFound;
        }

        internal StatusCodes GetDepth(string derived_currency, int limit, out List<OrderBuf> bids, out List<OrderBuf> asks, out decimal bids_vol, out decimal asks_vol, out int bids_num, out int asks_num) //получить стаканы
        {
            bids = null;
            asks = null;
            bids_vol = 0m;
            asks_vol = 0m;
            bids_num = 0;
            asks_num = 0;

            OrderBook book;
            if (OrderBooks.TryGetValue(derived_currency, out book)) //проверка на существование стакана
            {
                if (limit > 0) //проверка на положительность лимита
                {
                    bids = new List<OrderBuf>();
                    asks = new List<OrderBuf>();

                    //получаем заявки на покупку
                    if (book.ActiveBuyOrders.Count > 0)
                    {
                        decimal accumulated_amount = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].ActualAmount;
                        decimal last_rate = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate;
                        for (int i = book.ActiveBuyOrders.Count - 2; i >= 0; i--)
                        {
                            if (book.ActiveBuyOrders[i].Rate == last_rate)
                            {
                                accumulated_amount += book.ActiveBuyOrders[i].ActualAmount;
                            }
                            else
                            {
                                if (bids.Count == limit - 1) break;

                                //добавляем в буфер accumulated_amount и last_rate до их изменения                    
                                bids.Add(new OrderBuf(accumulated_amount, last_rate));

                                Order buy_ord = book.ActiveBuyOrders[i];
                                accumulated_amount = buy_ord.ActualAmount;
                                last_rate = buy_ord.Rate;
                            }
                        }
                        bids.Add(new OrderBuf(accumulated_amount, last_rate));
                    }

                    //получаем заявки на продажу
                    if (book.ActiveSellOrders.Count > 0)
                    {
                        decimal accumulated_amount = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].ActualAmount;
                        decimal last_rate = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate;
                        for (int i = book.ActiveSellOrders.Count - 2; i >= 0; i--)
                        {
                            if (book.ActiveSellOrders[i].Rate == last_rate)
                            {
                                accumulated_amount += book.ActiveSellOrders[i].ActualAmount;
                            }
                            else
                            {
                                if (asks.Count == limit - 1) break;

                                //добавляем в буфер accumulated_amount и last_rate до их изменения                    
                                asks.Add(new OrderBuf(accumulated_amount, last_rate));

                                Order sell_ord = book.ActiveSellOrders[i];
                                accumulated_amount = sell_ord.ActualAmount;
                                last_rate = sell_ord.Rate;
                            }
                        }
                        asks.Add(new OrderBuf(accumulated_amount, last_rate));
                    }

                    bids_vol = book.ActiveBuyOrders.Sum(item => item.ActualAmount * item.Rate); //bids' volume (в базовой валюте)
                    asks_vol = book.ActiveSellOrders.Sum(item => item.ActualAmount); //asks' volume (в производной валюте) 
                    bids_num = book.ActiveBuyOrders.Count; //bids' num                       
                    asks_num = book.ActiveSellOrders.Count; //asks' num

                    return StatusCodes.Success;
                }
                else return StatusCodes.ErrorNegativeOrZeroLimit;
            }
            else return StatusCodes.ErrorCurrencyPairNotFound;
        }
        
        #endregion

        #endregion

        #region SERVICE CORE FUNCTIONS

        #region ORDER MANAGEMENT

        private StatusCodes BaseLimit(int user_id, string derived_currency, OrderBook book, bool side, decimal amount, decimal rate, decimal sl_rate, decimal tp_rate, decimal ts_offset, OrderEvents order_event, long func_call_id, FCSources fc_source, string external_data) //базовая функция размещения заявки 
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
                        Pusher.NewBalance(user_id, base_currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                        
                        Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                        book.InsertBuyOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                        Pusher.NewOrder(order_event, fc_source, func_call_id, derived_currency, side, new Order(order)); //сообщение о новой заявке

                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
                        
                        if (use_sl_rate)
                        {
                            Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                            order.StopLoss = sl_order;
                            book.InsertSellSL(sl_order);
                            CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(OrderEvents.AddSL, fc_source, func_call_id, derived_currency, !side, new Order(sl_order)); //сообщение о новой заявке

                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                        }
                        
                        if (use_tp_rate)
                        {
                            Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                            order.TakeProfit = tp_order;
                            book.InsertSellTP(tp_order);
                            CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(OrderEvents.AddTP, fc_source, func_call_id, derived_currency, !side, new Order(tp_order)); //сообщение о новой заявке
                        }

                        if (use_ts_offset)
                        {
                            TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                            order.TrailingStop = ts_order;
                            book.InsertSellTS(ts_order);
                            CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(OrderEvents.AddTS, fc_source, func_call_id, derived_currency, !side, new Order(ts_order)); //сообщение о новой заявке
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
                            acc.BaseCFunds.AvailableFunds -= total; //снимаем средства с доступных средств
                            acc.BaseCFunds.BlockedFunds += total; //блокируем средства в заявке на покупку
                            Pusher.NewBalance(user_id, base_currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе

                            Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                            book.InsertBuyOrder(order);
                            CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(order_event, fc_source, func_call_id, derived_currency, side, new Order(order)); //сообщение о новой заявке

                            //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                            if (use_sl_rate)
                            {
                                Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                                order.StopLoss = sl_order;
                                book.InsertSellSL(sl_order);
                                CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                                Pusher.NewOrder(OrderEvents.AddSL, fc_source, func_call_id, derived_currency, !side, new Order(sl_order)); //сообщение о новой заявке

                                //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                            }

                            if (use_tp_rate)
                            {
                                Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                                order.TakeProfit = tp_order;
                                book.InsertSellTP(tp_order);
                                CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                                Pusher.NewOrder(OrderEvents.AddTP, fc_source, func_call_id, derived_currency, !side, new Order(tp_order)); //сообщение о новой заявке
                            }

                            if (use_ts_offset)
                            {
                                TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                                order.TrailingStop = ts_order;
                                book.InsertSellTS(ts_order);
                                CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                                Pusher.NewOrder(OrderEvents.AddTS, fc_source, func_call_id, derived_currency, !side, new Order(ts_order)); //сообщение о новой заявке
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
                        Pusher.NewBalance(user_id, derived_currency, derived_funds.AvailableFunds, derived_funds.BlockedFunds); //сообщение о новом балансе

                        Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                        book.InsertSellOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                        Pusher.NewOrder(order_event, fc_source, func_call_id, derived_currency, side, new Order(order)); //сообщение о новой заявке

                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                        if (use_sl_rate)
                        {
                            Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                            order.StopLoss = sl_order;
                            book.InsertBuySL(sl_order);
                            CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(OrderEvents.AddSL, fc_source, func_call_id, derived_currency, !side, new Order(sl_order)); //сообщение о новой заявке

                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                        }

                        if (use_tp_rate)
                        {
                            Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                            order.TakeProfit = tp_order;
                            book.InsertBuyTP(tp_order);
                            CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(OrderEvents.AddTP, fc_source, func_call_id, derived_currency, !side, new Order(tp_order)); //сообщение о новой заявке
                        }

                        if (use_ts_offset)
                        {
                            TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                            order.TrailingStop = ts_order;
                            book.InsertBuyTS(ts_order);
                            CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(OrderEvents.AddTS, fc_source, func_call_id, derived_currency, !side, new Order(ts_order)); //сообщение о новой заявке
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
                            derived_funds.AvailableFunds -= amount; //снимаем средства с доступных средств
                            derived_funds.BlockedFunds += amount; //блокируем средства в заявке на продажу
                            Pusher.NewBalance(user_id, derived_currency, derived_funds.AvailableFunds, derived_funds.BlockedFunds); //сообщение о новом балансе

                            Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                            book.InsertSellOrder(order);
                            CancelOrderDict.Add(order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.Limit, side)); //добавление заявки в словарь на закрытие
                            Pusher.NewOrder(order_event, fc_source, func_call_id, derived_currency, side, new Order(order)); //сообщение о новой заявке

                            //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                            if (use_sl_rate)
                            {
                                Order sl_order = new Order(user_id, amount, 0m, sl_rate, fc_source, external_data);
                                order.StopLoss = sl_order;
                                book.InsertBuySL(sl_order);
                                CancelOrderDict.Add(sl_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.StopLoss, !side)); //добавление заявки в словарь на закрытие
                                Pusher.NewOrder(OrderEvents.AddSL, fc_source, func_call_id, derived_currency, !side, new Order(sl_order)); //сообщение о новой заявке

                                //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, position_type, order); //FIX-сообщение о новом SL
                            }

                            if (use_tp_rate)
                            {
                                Order tp_order = new Order(user_id, amount, 0m, tp_rate, fc_source, external_data);
                                order.TakeProfit = tp_order;
                                book.InsertBuyTP(tp_order);
                                CancelOrderDict.Add(tp_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TakeProfit, !side)); //добавление заявки в словарь на закрытие
                                Pusher.NewOrder(OrderEvents.AddTP, fc_source, func_call_id, derived_currency, !side, new Order(tp_order)); //сообщение о новой заявке
                            }

                            if (use_ts_offset)
                            {
                                TSOrder ts_order = new TSOrder(user_id, amount, 0m, ts_rate, fc_source, external_data, ts_offset);
                                order.TrailingStop = ts_order;
                                book.InsertBuyTS(ts_order);
                                CancelOrderDict.Add(ts_order.OrderId, new CancOrdData(derived_currency, book, CancOrdTypes.TrailingStop, !side)); //добавление заявки в словарь на закрытие
                                Pusher.NewOrder(OrderEvents.AddTS, fc_source, func_call_id, derived_currency, !side, new Order(ts_order)); //сообщение о новой заявке
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
                PushOrderBookUpdates(derived_currency, book);
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
                    Pusher.NewTrade(derived_currency, trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_derived_funds.BlockedFunds -= sell_ord.ActualAmount;
                    seller.BaseCFunds.AvailableFunds += sell_ord.ActualAmount * trade_rate * (1m - seller_derived_funds.Fee);
                    Pusher.NewBalance(sell_ord.UserId, base_currency, seller.BaseCFunds.AvailableFunds, seller.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                    Pusher.NewBalance(sell_ord.UserId, derived_currency, seller_derived_funds.AvailableFunds, seller_derived_funds.BlockedFunds); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer.BaseCFunds.BlockedFunds -= sell_ord.ActualAmount * buy_ord.Rate;
                    buyer_derived_funds.AvailableFunds += sell_ord.ActualAmount * (1m - buyer_derived_funds.Fee);
                    buyer.BaseCFunds.AvailableFunds += sell_ord.ActualAmount * (buy_ord.Rate - trade_rate);
                    Pusher.NewBalance(buy_ord.UserId, base_currency, buyer.BaseCFunds.AvailableFunds, buyer.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                    Pusher.NewBalance(buy_ord.UserId, derived_currency, buyer_derived_funds.AvailableFunds, buyer_derived_funds.BlockedFunds); //сообщение о новом балансе

                    //увеличивается ActualAmount привязанных к buy-заявке SL/TP/TS заявок
                    if (buy_ord.StopLoss != null) buy_ord.StopLoss.ActualAmount += sell_ord.ActualAmount;
                    if (buy_ord.TakeProfit != null) buy_ord.TakeProfit.ActualAmount += sell_ord.ActualAmount;
                    if (buy_ord.TrailingStop != null) buy_ord.TrailingStop.ActualAmount += sell_ord.ActualAmount;

                    //buy-заявка становится partially filled => уменьшается её ActualAmount
                    buy_ord.ActualAmount -= sell_ord.ActualAmount;
                    Pusher.NewOrderMatch(derived_currency, buy_ord.OrderId, buy_ord.UserId, buy_ord.ActualAmount, OrderStatuses.PartiallyFilled); //сообщение о новом статусе заявки
                    
                    //увеличивается ActualAmount привязанных к sell-заявке SL/TP/TS заявок
                    if (sell_ord.StopLoss != null) sell_ord.StopLoss.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TakeProfit != null) sell_ord.TakeProfit.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TrailingStop != null) sell_ord.TrailingStop.ActualAmount += sell_ord.ActualAmount;

                    //sell-заявка становится filled => её ActualAmount становится нулевым
                    sell_ord.ActualAmount = 0m;
                    Pusher.NewOrderMatch(derived_currency, sell_ord.OrderId, sell_ord.UserId, sell_ord.ActualAmount, OrderStatuses.Filled); //сообщение о новом статусе заявки                    

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
                    Pusher.NewTrade(derived_currency, trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_derived_funds.BlockedFunds -= buy_ord.ActualAmount;
                    seller.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * trade_rate * (1m - seller_derived_funds.Fee);
                    Pusher.NewBalance(sell_ord.UserId, base_currency, seller.BaseCFunds.AvailableFunds, seller.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                    Pusher.NewBalance(sell_ord.UserId, derived_currency, seller_derived_funds.AvailableFunds, seller_derived_funds.BlockedFunds); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer.BaseCFunds.BlockedFunds -= buy_ord.ActualAmount * buy_ord.Rate;
                    buyer_derived_funds.AvailableFunds += buy_ord.ActualAmount * (1m - buyer_derived_funds.Fee);
                    buyer.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * (buy_ord.Rate - trade_rate);
                    Pusher.NewBalance(buy_ord.UserId, base_currency, buyer.BaseCFunds.AvailableFunds, buyer.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                    Pusher.NewBalance(buy_ord.UserId, derived_currency, buyer_derived_funds.AvailableFunds, buyer_derived_funds.BlockedFunds); //сообщение о новом балансе

                    //увеличивается ActualAmount привязанных к sell-заявке SL/TP/TS заявок
                    if (sell_ord.StopLoss != null) sell_ord.StopLoss.ActualAmount += buy_ord.ActualAmount;
                    if (sell_ord.TakeProfit != null) sell_ord.TakeProfit.ActualAmount += buy_ord.ActualAmount;
                    if (sell_ord.TrailingStop != null) sell_ord.TrailingStop.ActualAmount += buy_ord.ActualAmount;

                    //sell-заявка становится partially filled => уменьшается её ActualAmount
                    sell_ord.ActualAmount -= buy_ord.ActualAmount;
                    Pusher.NewOrderMatch(derived_currency, sell_ord.OrderId, sell_ord.UserId, sell_ord.ActualAmount, OrderStatuses.PartiallyFilled); //сообщение о новом статусе заявки

                    //увеличивается ActualAmount привязанных к buy-заявке SL/TP/TS заявок
                    if (buy_ord.StopLoss != null) buy_ord.StopLoss.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TakeProfit != null) buy_ord.TakeProfit.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TrailingStop != null) buy_ord.TrailingStop.ActualAmount += buy_ord.ActualAmount;

                    //buy-заявка становится filled => её ActualAmount становится нулевым
                    buy_ord.ActualAmount = 0m;
                    Pusher.NewOrderMatch(derived_currency, buy_ord.OrderId, buy_ord.UserId, buy_ord.ActualAmount, OrderStatuses.Filled); //сообщение о новом статусе заявки

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
                    Pusher.NewTrade(derived_currency, trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_derived_funds.BlockedFunds -= buy_ord.ActualAmount;
                    seller.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * trade_rate * (1m - seller_derived_funds.Fee);
                    Pusher.NewBalance(sell_ord.UserId, base_currency, seller.BaseCFunds.AvailableFunds, seller.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                    Pusher.NewBalance(sell_ord.UserId, derived_currency, seller_derived_funds.AvailableFunds, seller_derived_funds.BlockedFunds); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer.BaseCFunds.BlockedFunds -= buy_ord.ActualAmount * buy_ord.Rate;
                    buyer_derived_funds.AvailableFunds += buy_ord.ActualAmount * (1m - buyer_derived_funds.Fee);
                    buyer.BaseCFunds.AvailableFunds += buy_ord.ActualAmount * (buy_ord.Rate - trade_rate);
                    Pusher.NewBalance(buy_ord.UserId, base_currency, buyer.BaseCFunds.AvailableFunds, buyer.BaseCFunds.BlockedFunds); //сообщение о новом балансе
                    Pusher.NewBalance(buy_ord.UserId, derived_currency, buyer_derived_funds.AvailableFunds, buyer_derived_funds.BlockedFunds); //сообщение о новом балансе

                    //увеличивается ActualAmount привязанных к buy-заявке SL/TP/TS заявок
                    if (buy_ord.StopLoss != null) buy_ord.StopLoss.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TakeProfit != null) buy_ord.TakeProfit.ActualAmount += buy_ord.ActualAmount;
                    if (buy_ord.TrailingStop != null) buy_ord.TrailingStop.ActualAmount += buy_ord.ActualAmount;

                    //buy-заявка становится filled => её ActualAmount становится нулевым
                    buy_ord.ActualAmount = 0m;
                    Pusher.NewOrderMatch(derived_currency, buy_ord.OrderId, buy_ord.UserId, buy_ord.ActualAmount, OrderStatuses.Filled); //сообщение о новом статусе заявки

                    //увеличивается ActualAmount привязанных к sell-заявке SL/TP/TS заявок
                    if (sell_ord.StopLoss != null) sell_ord.StopLoss.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TakeProfit != null) sell_ord.TakeProfit.ActualAmount += sell_ord.ActualAmount;
                    if (sell_ord.TrailingStop != null) sell_ord.TrailingStop.ActualAmount += sell_ord.ActualAmount;

                    //sell-заявка становится filled => её ActualAmount становится нулевым
                    sell_ord.ActualAmount = 0m;
                    Pusher.NewOrderMatch(derived_currency, sell_ord.OrderId, sell_ord.UserId, sell_ord.ActualAmount, OrderStatuses.Filled); //сообщение о новом статусе заявки

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

            PushOrderBookUpdates(derived_currency, book);
        }

        #endregion

        #region ORDERBOOK UPDATE MANAGEMENT

        private void PushOrderBookUpdates(string derived_currency, OrderBook book)
        {
            if (UpdTicker(book)) Pusher.NewTicker(derived_currency, book.bid_buf, book.ask_buf); //сообщение о новом тикере
            if (UpdActiveBuyTop(book)) Pusher.NewOrderBookTop(derived_currency, false, book.act_buy_buf); //сообщение о новом топе стакана на покупку
            if (UpdActiveSellTop(book)) Pusher.NewOrderBookTop(derived_currency, true, book.act_sell_buf); //сообщение о новом топе стакана на продажу
            MarginManager.QueueDelayedExecution();
        }

        private bool UpdTicker(OrderBook book)
        {
            //обновление тикера
            bool _upd = false;
            if (book.ActiveBuyOrders.Count > 0)
            {
                Order top_buy_order = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1];
                if (book.bid_buf != top_buy_order.Rate)
                {
                    //расчёт % изменения рыночной цены на покупку
                    decimal bid_deviation = Math.Abs(book.bid_buf / top_buy_order.Rate - 1m);
                    MarginManager.QueueExecution(bid_deviation);
                    CondOrdManager.QueueExecution(bid_deviation);
                    book.bid_buf = top_buy_order.Rate;
                    _upd = true;
                }
            }
            if (book.ActiveSellOrders.Count > 0)
            {
                Order top_sell_ord = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1];
                if (book.ask_buf != top_sell_ord.Rate)
                {
                    //расчёт % изменения рыночной цены на продажу
                    decimal ask_deviation = Math.Abs(book.ask_buf / top_sell_ord.Rate - 1m);
                    MarginManager.QueueExecution(ask_deviation);
                    CondOrdManager.QueueExecution(ask_deviation);
                    book.ask_buf = top_sell_ord.Rate;
                    _upd = true;
                }
            }
            return _upd;
        }

        private bool UpdActiveBuyTop(OrderBook book)
        {
            //обновление топа ActiveBuyOrders
            bool _upd = false;

            List<OrderBuf> ActBuyCopy = new List<OrderBuf>(30);
            if (book.ActiveBuyOrders.Count > 0)
            {
                decimal accumulated_amount = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].ActualAmount;
                decimal last_rate = book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate;
                for (int i = book.ActiveBuyOrders.Count - 2; i >= 0; i--)
                {
                    if (book.ActiveBuyOrders[i].Rate == last_rate)
                    {
                        accumulated_amount += book.ActiveBuyOrders[i].ActualAmount;
                    }
                    else
                    {
                        if (ActBuyCopy.Count == book.act_buy_buf_max_size - 1) break;

                        //добавляем в буфер accumulated_amount и last_rate до их изменения                    
                        ActBuyCopy.Add(new OrderBuf(accumulated_amount, last_rate));

                        Order buy_ord = book.ActiveBuyOrders[i];
                        accumulated_amount = buy_ord.ActualAmount;
                        last_rate = buy_ord.Rate;
                    }
                }
                ActBuyCopy.Add(new OrderBuf(accumulated_amount, last_rate));
            }

            if (!EqualOrderBooks(book.act_buy_buf, ActBuyCopy))
            {
                book.act_buy_buf = ActBuyCopy;
                _upd = true;
            }
            return _upd;
        }

        private bool UpdActiveSellTop(OrderBook book)
        {
            //обновление топа ActiveSellOrders
            bool _upd = false;

            List<OrderBuf> ActSellCopy = new List<OrderBuf>(30);
            if (book.ActiveSellOrders.Count > 0)
            {
                decimal accumulated_amount = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].ActualAmount;
                decimal last_rate = book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate;
                for (int i = book.ActiveSellOrders.Count - 2; i >= 0; i--)
                {
                    if (book.ActiveSellOrders[i].Rate == last_rate)
                    {
                        accumulated_amount += book.ActiveSellOrders[i].ActualAmount;
                    }
                    else
                    {
                        if (ActSellCopy.Count == book.act_sell_buf_max_size - 1) break;

                        //добавляем в буфер accumulated_amount и last_rate до их изменения                    
                        ActSellCopy.Add(new OrderBuf(accumulated_amount, last_rate));

                        Order sell_ord = book.ActiveSellOrders[i];
                        accumulated_amount = sell_ord.ActualAmount;
                        last_rate = sell_ord.Rate;
                    }
                }
                ActSellCopy.Add(new OrderBuf(accumulated_amount, last_rate));
            }

            if (!EqualOrderBooks(book.act_sell_buf, ActSellCopy))
            {
                book.act_sell_buf = ActSellCopy;
                _upd = true;
            }
            return _upd;
        }

        private bool EqualOrderBooks(List<OrderBuf> book1, List<OrderBuf> book2)
        {
            if (book1.Count != book2.Count) return false;
            else
            {
                for (int i = 0; i < book1.Count; i++)
                {
                    if (!book1[i].Equals(book2[i])) return false;
                }
                return true;
            }
        }
        
        #endregion

        #region MARGIN MANAGEMENT

        internal void ManageMargin() //расчёт маржинальных параметров, выполнение MC/FL в случае необходимости
        {
            //реплицировать
            Console.WriteLine(DateTime.Now + " ManageMargin()");

            //управление маржинальными параметрами каждого клиента
            foreach (KeyValuePair<int, Account> acc in Accounts)
            {
                decimal[] margin_pars = CalcAccMarginPars(acc.Value); //расчёт маржинальных параметров юзера
                            
                if (acc.Value.Equity != margin_pars[0]) //обновляем параметры аккаунта, если equity изменился
                {
                    acc.Value.Equity = margin_pars[0];
                    acc.Value.Margin = margin_pars[1];
                    acc.Value.FreeMargin = margin_pars[2];
                    acc.Value.MarginLevel = margin_pars[3];
                    Pusher.NewMarginInfo(acc.Key, acc.Value.Equity, acc.Value.Margin, acc.Value.FreeMargin, acc.Value.MarginLevel * 100m); //сообщение о новом уровне маржи
                }

                //проверка на использование заёмных средств
                if (margin_pars[1] == 0m)
                {
                    if (acc.Value.MarginCall) acc.Value.MarginCall = false; //сброс флага Margin Call
                    continue;
                }

                //проверка условия Margin Call
                if (margin_pars[3] <= acc.Value.LevelMC)
                {
                    if (!acc.Value.MarginCall)
                    {
                        acc.Value.MarginCall = true;
                        Pusher.NewMarginCall(acc.Key); //сообщение о новом Margin Call
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
                        Pusher.NewBalance(acc.Key, base_currency, acc.Value.BaseCFunds.AvailableFunds, acc.Value.BaseCFunds.BlockedFunds); //сообщение о новом балансе

                        Order order = new Order(acc.Key, fl_amount, fl_amount, fl_market_rate);
                        fl_book.InsertBuyOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(fl_derived_currency, fl_book, CancOrdTypes.Limit, fl_side)); //добавление заявки в словарь на закрытие
                        Pusher.NewOrder(OrderEvents.ForcedLiquidation, FCSources.Core, 0L, fl_derived_currency, fl_side, new Order(order)); //сообщение о новой заявке

                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                        Match(fl_derived_currency, fl_book);
                    }
                    else //заявка на продажу
                    {
                        DerivedFunds derived_funds = acc.Value.DerivedCFunds[fl_derived_currency];
                        derived_funds.AvailableFunds -= fl_amount; //снимаем средства с доступных средств
                        derived_funds.BlockedFunds += fl_amount; //блокируем средства в заявке на продажу
                        Pusher.NewBalance(acc.Key, fl_derived_currency, derived_funds.AvailableFunds, derived_funds.BlockedFunds); //сообщение о новом балансе

                        Order order = new Order(acc.Key, fl_amount, fl_amount, fl_market_rate);
                        fl_book.InsertSellOrder(order);
                        CancelOrderDict.Add(order.OrderId, new CancOrdData(fl_derived_currency, fl_book, CancOrdTypes.Limit, fl_side)); //добавление заявки в словарь на закрытие
                        Pusher.NewOrder(OrderEvents.ForcedLiquidation, FCSources.Core, 0L, fl_derived_currency, fl_side, new Order(order)); //сообщение о новой заявке

                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке

                        Match(fl_derived_currency, fl_book);
                    }
                }
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
            //реплицировать

            Console.WriteLine(DateTime.Now + " ManageConditionalOrders()");

            foreach (KeyValuePair<string, OrderBook> book in OrderBooks)
            {
                ManageSLs(book.Key, book.Value);
                ManageTPs(book.Key, book.Value);
                ManageTSs(book.Key, book.Value);
            }
        }

        private void ManageSLs(string derived_currency, OrderBook book)
        {
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
                            Pusher.NewBalance(sell_sl.UserId, derived_currency, derived_funds.AvailableFunds, derived_funds.BlockedFunds); //сообщение о новом балансе

                                                        
                            book.SellSLs.RemoveAt(i); //удаляем SL из памяти
                            if (sell_sl.TakeProfit != null)
                            {
                                if (book.SellTPs.Remove(sell_sl.TakeProfit)) //удаляем слинкованный TP из памяти
                                {
                                    CancelOrderDict.Remove(sell_sl.TakeProfit.OrderId);
                                    sell_sl.TakeProfit = null;
                                }
                            }
                            if (sell_sl.TrailingStop != null)
                            {
                                if (book.SellTSs.Remove(sell_sl.TrailingStop)) //удаляем слинкованный TS из памяти
                                {
                                    CancelOrderDict.Remove(sell_sl.TrailingStop.OrderId);
                                    sell_sl.TrailingStop = null;
                                }
                            }

                            sell_sl.Rate = market_rate; //присвоение рыночной цены
                            book.InsertSellOrder(sell_sl);
                            CancelOrderDict[sell_sl.OrderId].OrderType = CancOrdTypes.Limit;
                            Pusher.NewOrder(OrderEvents.ExecSL, FCSources.Core, 0L, derived_currency, true, new Order(sell_sl)); //сообщение о новой заявке

                            //FixMessager.NewMarketDataIncrementalRefresh(true, new_sell_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.SellSLs.RemoveAt(i); //удаляем SL из памяти
                            CancelOrderDict.Remove(sell_sl.OrderId); //удаляем заявку из словаря на закрытие
                            Pusher.NewOrder(OrderEvents.Cancel, FCSources.Core, 0L, derived_currency, true, sell_sl); //сообщение о новой отмене заявке
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
                    if (book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate >= buy_sl.Rate) //сравнение с рыночным курсом на продажу (SL будет на покупку)
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
                            Pusher.NewBalance(buy_sl.UserId, base_currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе

                            book.BuySLs.RemoveAt(i); //удаляем SL из памяти
                            if (buy_sl.TakeProfit != null)
                            {
                                if (book.BuyTPs.Remove(buy_sl.TakeProfit)) //удаляем слинкованный TP из памяти
                                {
                                    CancelOrderDict.Remove(buy_sl.TakeProfit.OrderId);
                                    buy_sl.TakeProfit = null;
                                }
                            }
                            if (buy_sl.TrailingStop != null)
                            {
                                if (book.BuyTSs.Remove(buy_sl.TrailingStop)) //удаляем слинкованный TS из памяти
                                {
                                    CancelOrderDict.Remove(buy_sl.TrailingStop.OrderId);
                                    buy_sl.TrailingStop = null;
                                }
                            }

                            buy_sl.Rate = market_rate;
                            book.InsertBuyOrder(buy_sl);
                            CancelOrderDict[buy_sl.OrderId].OrderType = CancOrdTypes.Limit;
                            Pusher.NewOrder(OrderEvents.ExecSL, FCSources.Core, 0L, derived_currency, false, new Order(buy_sl)); //сообщение о новой заявке

                            //FixMessager.NewMarketDataIncrementalRefresh(false, new_buy_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.BuySLs.RemoveAt(i); //удаляем SL из памяти
                            CancelOrderDict.Remove(buy_sl.OrderId); //удаляем заявку из словаря на закрытие
                            Pusher.NewOrder(OrderEvents.Cancel, FCSources.Core, 0L, derived_currency, false, buy_sl); //сообщение о новой отмене заявке
                        }
                    }
                    else break;
                }
            }
        }

        private void ManageTPs(string derived_currency, OrderBook book)
        {
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
                            Pusher.NewBalance(sell_tp.UserId, derived_currency, derived_funds.AvailableFunds, derived_funds.BlockedFunds); //сообщение о новом балансе

                            book.SellTPs.RemoveAt(i); //удаляем TP из памяти
                            if (sell_tp.StopLoss != null)
                            {
                                if (book.SellSLs.Remove(sell_tp.StopLoss)) //удаляем слинкованный SL из памяти
                                {
                                    CancelOrderDict.Remove(sell_tp.StopLoss.OrderId);
                                    sell_tp.StopLoss = null;
                                }
                            }
                            if (sell_tp.TrailingStop != null)
                            {
                                if (book.SellTSs.Remove(sell_tp.TrailingStop)) //удаляем слинкованный TS из памяти
                                {
                                    CancelOrderDict.Remove(sell_tp.TrailingStop.OrderId);
                                    sell_tp.TrailingStop = null;
                                }
                            }

                            sell_tp.Rate = market_rate; //присвоение рыночной цены
                            book.InsertSellOrder(sell_tp);
                            CancelOrderDict[sell_tp.OrderId].OrderType = CancOrdTypes.Limit;
                            Pusher.NewOrder(OrderEvents.ExecTP, FCSources.Core, 0L, derived_currency, true, new Order(sell_tp)); //сообщение о новой заявке

                            //FixMessager.NewMarketDataIncrementalRefresh(true, new_sell_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.SellTPs.RemoveAt(i); //удаляем TP из памяти
                            CancelOrderDict.Remove(sell_tp.OrderId); //удаляем заявку из словаря на закрытие
                            Pusher.NewOrder(OrderEvents.Cancel, FCSources.Core, 0L, derived_currency, true, sell_tp); //сообщение о новой отмене заявке
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
                    if (book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate <= buy_tp.Rate) //сравнение с рыночным курсом на продажу (TP будет на покупку)
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
                            Pusher.NewBalance(buy_tp.UserId, base_currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе

                            book.BuyTPs.RemoveAt(i); //удаляем TP из памяти
                            if (buy_tp.StopLoss != null)
                            {
                                if (book.BuySLs.Remove(buy_tp.StopLoss)) //удаляем слинкованный SL из памяти
                                {
                                    CancelOrderDict.Remove(buy_tp.StopLoss.OrderId); 
                                    buy_tp.StopLoss = null;
                                }
                            }
                            if (buy_tp.TrailingStop != null)
                            {
                                if (book.BuyTSs.Remove(buy_tp.TrailingStop)) //удаляем слинкованный TS из памяти
                                {
                                    CancelOrderDict.Remove(buy_tp.TrailingStop.OrderId);
                                    buy_tp.TrailingStop = null;
                                }
                            }

                            buy_tp.Rate = market_rate;
                            book.InsertBuyOrder(buy_tp);
                            CancelOrderDict[buy_tp.OrderId].OrderType = CancOrdTypes.Limit;
                            Pusher.NewOrder(OrderEvents.ExecTP, FCSources.Core, 0L, derived_currency, false, new Order(buy_tp)); //сообщение о новой заявке

                            //FixMessager.NewMarketDataIncrementalRefresh(false, new_buy_order); //FIX multicast

                            Match(derived_currency, book);
                        }
                        else
                        {
                            book.BuyTPs.RemoveAt(i); //удаляем TP из памяти
                            CancelOrderDict.Remove(buy_tp.OrderId); //удаляем заявку из словаря на закрытие
                            Pusher.NewOrder(OrderEvents.Cancel, FCSources.Core, 0L, derived_currency, false, buy_tp); //сообщение о новой отмене заявке
                        }
                    }
                    else break;
                }
            }
        }

        private void ManageTSs(string derived_currency, OrderBook book)
        {
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
                        if (book.ActiveBuyOrders[book.ActiveBuyOrders.Count - 1].Rate <= sell_ts.Rate) //сравнение с рыночным курсом на покупку (TS будет на продажу)
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
                                Pusher.NewBalance(sell_ts.UserId, derived_currency, derived_funds.AvailableFunds, derived_funds.BlockedFunds); //сообщение о новом балансе
                                
                                book.SellTSs.RemoveAt(i); //удаляем TS из памяти
                                if (sell_ts.StopLoss != null)
                                {
                                    if (book.SellSLs.Remove(sell_ts.StopLoss)) //удаляем слинкованный SL из памяти
                                    {
                                        CancelOrderDict.Remove(sell_ts.StopLoss.OrderId);
                                        sell_ts.StopLoss = null;
                                    }
                                }
                                if (sell_ts.TakeProfit != null)
                                {
                                    if (book.SellTPs.Remove(sell_ts.TakeProfit)) //удаляем слинкованный TP из памяти
                                    {
                                        CancelOrderDict.Remove(sell_ts.TakeProfit.OrderId);
                                        sell_ts.TakeProfit = null;
                                    }
                                }

                                sell_ts.Rate = market_rate; //присвоение рыночной цены
                                book.InsertSellOrder(sell_ts);
                                CancelOrderDict[sell_ts.OrderId].OrderType = CancOrdTypes.Limit;
                                Pusher.NewOrder(OrderEvents.ExecTS, FCSources.Core, 0L, derived_currency, true, new Order(sell_ts)); //сообщение о новой заявке
                                //FixMessager.NewMarketDataIncrementalRefresh(true, new_sell_order); //FIX multicast

                                Match(derived_currency, book);
                            }
                            else
                            {
                                book.SellTSs.RemoveAt(i); //удаляем TS из памяти
                                CancelOrderDict.Remove(sell_ts.OrderId); //удаляем заявку из словаря на закрытие
                                Pusher.NewOrder(OrderEvents.Cancel, FCSources.Core, 0L, derived_currency, true, sell_ts); //сообщение о новой отмене заявке
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
                        if (book.ActiveSellOrders[book.ActiveSellOrders.Count - 1].Rate >= buy_ts.Rate) //сравнение с рыночным курсом на покупку (TS будет на покупку)
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
                                Pusher.NewBalance(buy_ts.UserId, base_currency, acc.BaseCFunds.AvailableFunds, acc.BaseCFunds.BlockedFunds); //сообщение о новом балансе

                                book.BuyTSs.RemoveAt(i); //удаляем TS из памяти
                                if (buy_ts.StopLoss != null)
                                {
                                    if (book.BuySLs.Remove(buy_ts.StopLoss)) //удаляем слинкованный SL из памяти
                                    {
                                        CancelOrderDict.Remove(buy_ts.StopLoss.OrderId); 
                                        buy_ts.StopLoss = null;
                                    }
                                }
                                if (buy_ts.TakeProfit != null)
                                {
                                    if (book.BuyTPs.Remove(buy_ts.TakeProfit)) //удаляем слинкованный TP из памяти
                                    {
                                        CancelOrderDict.Remove(buy_ts.TakeProfit.OrderId); 
                                        buy_ts.TakeProfit = null;
                                    }
                                }

                                buy_ts.Rate = market_rate; //присвоение рыночной цены
                                book.InsertBuyOrder(buy_ts);
                                CancelOrderDict[buy_ts.OrderId].OrderType = CancOrdTypes.Limit;
                                Pusher.NewOrder(OrderEvents.ExecTS, FCSources.Core, 0L, derived_currency, false, new Order(buy_ts)); //сообщение о новой заявке
                                //FixMessager.NewMarketDataIncrementalRefresh(false, new_buy_order); //FIX multicast

                                Match(derived_currency, book);
                            }
                            else
                            {
                                book.BuyTSs.RemoveAt(i); //удаляем TS из памяти
                                CancelOrderDict.Remove(buy_ts.OrderId); //удаляем заявку из словаря на закрытие
                                Pusher.NewOrder(OrderEvents.Cancel, FCSources.Core, 0L, derived_currency, false, buy_ts); //сообщение о новой отмене заявке
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