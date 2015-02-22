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
        private Dictionary<string, OrderBook> OrderBooks; //словарь "производная валюта -> стакан"	
        //private Dictionary<long, OrdCancData> OrderCancellationData; //словарь для закрытия заявок
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
            OrderBooks = new Dictionary<string, OrderBook>(10);            
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

        internal StatusCodes DeleteAccount(int user_id) //удалить торговый счёт TODO отменять заявки, возвращать массив средств на выход
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
                    book.ActiveBuyStops.RemoveAll(i => i.UserId == user_id);
                    book.ActiveSellStops.RemoveAll(i => i.UserId == user_id);
                }
                
                //RemoveUserFixAccounts(user_id);
                //RemoveUserApiKeys(user_id);

                //удаляем юзера
                Accounts.Remove(user_id);
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes DepositFunds(int user_id, string currency, decimal sum) //пополнить торговый счёт
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то пополняем
            {
                if (sum > 0m) //проверка на положительность суммы пополнения
                {
                    if (currency == base_currency) //пополнение базовой валюты
                    {
                        acc.BaseCFunds.AvailableFunds += sum;
                        //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                        return StatusCodes.Success;
                    }
                    else //пополнение производной валюты
                    {
                        DerivedFunds funds;
                        if (acc.DerivedCFunds.TryGetValue(currency, out funds))
                        {
                            funds.AvailableFunds += sum;
                            //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                            return StatusCodes.Success;
                        }
                        else return StatusCodes.ErrorCurrencyNotFound;
                    }                    
                }
                else return StatusCodes.ErrorNegativeOrZeroSum;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes WithdrawFunds(int user_id, string currency, decimal sum) //снять с торгового счёта //TODO MARGIN CHECK
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то снимаем
            {
                if (acc.Suspended) return StatusCodes.ErrorAccountSuspended; //проверка на блокировку счёта

                if (sum > 0m) //проверка на положительность суммы вывода
                {
                    if (currency == base_currency) //снятие базовой валюты
                    {
                        if (acc.BaseCFunds.AvailableFunds >= sum)
                        {
                            acc.BaseCFunds.AvailableFunds -= sum;
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
                            if (funds.AvailableFunds >= sum)
                            {
                                funds.AvailableFunds -= sum;
                                //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                                return StatusCodes.Success;
                            }
                            else return StatusCodes.ErrorInsufficientFunds;
                        }
                        else return StatusCodes.ErrorCurrencyNotFound;
                    }
                }
                else return StatusCodes.ErrorNegativeOrZeroSum;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }
        
        internal StatusCodes PlaceLimit(int user_id, string derived_currency, bool side, decimal amount, decimal rate, long func_call_id, int fc_source, string external_data = null) //подать лимитную заявку
        {
            OrderBook book;
            if (OrderBooks.TryGetValue(derived_currency, out book)) //проверка на существование стакана
            {
                return BaseLimit(user_id, derived_currency, book, side, amount, rate, (int)MessageTypes.NewPlaceLimit, func_call_id, fc_source, external_data);
            }
            else return StatusCodes.ErrorCurrencyPairNotFound;           
        }

        internal StatusCodes PlaceMarket(int user_id, string derived_currency, bool side, bool base_amount, decimal amount, long func_call_id, int fc_source, string external_data = null) //подать рыночную заявку
        {
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

                        if (market_rate == 0) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, amount, market_rate, (int)MessageTypes.NewPlaceMarket, func_call_id, fc_source, external_data);
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

                        if (market_rate == 0) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, accumulated_amount, market_rate, (int)MessageTypes.NewPlaceInstant, func_call_id, fc_source, external_data);
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

                        if (market_rate == 0) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, amount, market_rate, (int)MessageTypes.NewPlaceMarket, func_call_id, fc_source, external_data);
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

                        if (market_rate == 0) return StatusCodes.ErrorInsufficientMarketVolume;

                        return BaseLimit(user_id, derived_currency, book, side, accumulated_amount, market_rate, (int)MessageTypes.NewPlaceInstant, func_call_id, fc_source, external_data);
                    }
                }
            }
            else return StatusCodes.ErrorCurrencyPairNotFound;
        }



        internal void PrintAccountFunds(int user_id)
        {
            Account acc = Accounts[user_id];

            Console.WriteLine("User ID: " + user_id.ToString());
            Console.WriteLine("Base currency (EUR):");
            Console.WriteLine("Available Funds: " + acc.BaseCFunds.AvailableFunds);
            Console.WriteLine("Blocked Funds: " + acc.BaseCFunds.BlockedFunds);
            Console.WriteLine("Derived currencies:");
            foreach (KeyValuePair<string, DerivedFunds> funds in acc.DerivedCFunds)
            {
                Console.WriteLine("Currency: " + funds.Key);
                Console.WriteLine("Available Funds: " + funds.Value.AvailableFunds);
                Console.WriteLine("Blocked Funds: " + funds.Value.BlockedFunds);
            }
        }


        #endregion

        #region GLOBAL FUNCTIONS

        internal StatusCodes CreateCurrencyPair(string derived_currency)
        {
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
            return StatusCodes.Success;
        }
        
        #endregion

        #endregion

        #region SERVICE CORE FUNCTIONS

        #region ORDER PLACEMENT & MATCHING

        private StatusCodes BaseLimit(int user_id, string derived_currency, OrderBook book, bool side, decimal amount, decimal rate, int msg_type, long func_call_id, int fc_source, string external_data) //базовый метод размещения заявки 
        {
            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то снимаем с него сумму и подаём заявку
            {
                if (acc.Suspended) return StatusCodes.ErrorAccountSuspended; //проверка на блокировку счёта
                if (String.IsNullOrEmpty(derived_currency)) return StatusCodes.ErrorInvalidCurrency; //проверка на корректность производной валюты
                if (amount <= 0m || rate <= 0m) return StatusCodes.ErrorNegativeOrZeroSum; //проверка на положительность rate и amount 

                if (!side) //если заявка на покупку (0)
                {
                    decimal sum = amount * rate;
                    if (acc.BaseCFunds.AvailableFunds >= sum) //проверка на платежеспособность по базовой валюте
                    {
                        acc.BaseCFunds.AvailableFunds -= sum; //снимаем средства с доступных средств
                        acc.BaseCFunds.BlockedFunds += sum; //блокируем средства в заявке на покупку
                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                        Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                        book.InsertBuyOrder(order);
                        //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
                        Match(derived_currency, book);
                        return StatusCodes.Success;
                    }
                    else //проверка лонга с плечом
                    {
                        //расчёт свободной маржи
                        decimal debit = 0m;
                        decimal credit = 0m;
                        if (acc.BaseCFunds.AvailableFunds > 0m) debit += acc.BaseCFunds.AvailableFunds; //начисляем дебету положительную сумму в базовой валюте
                        else credit += acc.BaseCFunds.AvailableFunds; //начисляем кредиту отрицательную сумму в базовой валюте
                        foreach (KeyValuePair<string, DerivedFunds> funds in acc.DerivedCFunds) //учёт сумм в производных валютах, приведённых к базовой
                        {
                            if (funds.Value.AvailableFunds == 0m) continue;
                            //калькуляция рыночного курса на покупку (лонг будет распродаваться)
                            OrderBook cur_book = OrderBooks[funds.Key];
                            decimal long_market_rate = 0m;
                            decimal long_accumulated_amount = 0m;
                            for (int i = cur_book.ActiveBuyOrders.Count - 1; i >= 0; i--)
                            {
                                long_accumulated_amount += cur_book.ActiveBuyOrders[i].ActualAmount;
                                if (long_accumulated_amount >= Math.Abs(funds.Value.AvailableFunds)) //если объём накопленных заявок на продажу превышает сумму в производной валюте на счёте
                                {
                                    long_market_rate = cur_book.ActiveBuyOrders[i].Rate;
                                    break;
                                }
                            }

                            if (funds.Value.AvailableFunds > 0m) debit += funds.Value.AvailableFunds * long_market_rate; //начисляем дебету положительную сумму в базовой валюте
                            else credit += funds.Value.AvailableFunds * long_market_rate; //начисляем кредиту отрицательную сумму в базовой валюте
                        }
                        if ((debit + credit) * acc.MaxLeverage + credit >= sum)
                        {
                            acc.BaseCFunds.AvailableFunds -= sum; //снимаем средства с доступных средств
                            acc.BaseCFunds.BlockedFunds += sum; //блокируем средства в заявке на покупку
                            //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                            Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                            book.InsertBuyOrder(order);
                            //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                            //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
                            Match(derived_currency, book);
                            return StatusCodes.Success;
                        }
                        else return StatusCodes.ErrorInsufficientFunds;                        
                    }
                }
                else //если заявка на продажу (0)
                {
                    DerivedFunds derived_funds = acc.DerivedCFunds[derived_currency];
                    if (derived_funds.AvailableFunds >= amount) //проверка на платежеспособность по производной валюте
                    {
                        derived_funds.AvailableFunds -= amount; //снимаем средства с доступных средств
                        derived_funds.BlockedFunds += amount; //блокируем средства в заявке на продажу
                        //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                        Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                        book.InsertSellOrder(order);
                        //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                        //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                        //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
                        Match(derived_currency, book);
                        return StatusCodes.Success;
                    }
                    else //проверка шорта с плечом
                    {
                        //расчёт свободной маржи
                        decimal debit = 0m;
                        decimal credit = 0m;
                        if (acc.BaseCFunds.AvailableFunds > 0m) debit += acc.BaseCFunds.AvailableFunds; //начисляем дебету положительную сумму в базовой валюте
                        else credit += acc.BaseCFunds.AvailableFunds; //начисляем кредиту отрицательную сумму в базовой валюте
                        foreach (KeyValuePair<string, DerivedFunds> funds in acc.DerivedCFunds) //учёт сумм в производных валютах, приведённых к базовой
                        {
                            if (funds.Value.AvailableFunds == 0m) continue;
                            //калькуляция рыночного курса на продажу (шорт будет выкупаться)
                            OrderBook cur_book = OrderBooks[funds.Key];                            
                            decimal short_market_rate = 0m;
                            decimal short_accumulated_amount = 0m;
                            for (int i = cur_book.ActiveSellOrders.Count - 1; i >= 0; i--)
                            {
                                short_accumulated_amount += cur_book.ActiveSellOrders[i].ActualAmount;
                                if (short_accumulated_amount >= Math.Abs(funds.Value.AvailableFunds)) //если объём накопленных заявок на продажу превышает сумму в производной валюте на счёте
                                {
                                    short_market_rate = cur_book.ActiveSellOrders[i].Rate;
                                    break;
                                }
                            }

                            if (funds.Value.AvailableFunds > 0m) debit += funds.Value.AvailableFunds * short_market_rate; //начисляем дебету положительную сумму в базовой валюте
                            else credit += funds.Value.AvailableFunds * short_market_rate; //начисляем кредиту отрицательную сумму в базовой валюте
                        }
                        if ((debit + credit) * acc.MaxLeverage + credit >= amount * rate)
                        {
                            derived_funds.AvailableFunds -= amount; //снимаем средства с доступных средств
                            derived_funds.BlockedFunds += amount; //блокируем средства в заявке на продажу
                            //Pusher.NewBalance(user_id, acc, DateTime.Now); //сообщение о новом балансе
                            Order order = new Order(user_id, amount, amount, rate, fc_source, external_data);
                            book.InsertSellOrder(order);
                            //Pusher.NewOrder(msg_type, func_call_id, fc_source, side, order); //сообщение о новой заявке
                            //FixMessager.NewMarketDataIncrementalRefresh(side, order); //FIX multicast
                            //if (fc_source == (int)FCSources.FixApi) FixMessager.NewExecutionReport(external_data, func_call_id, side, order); //FIX-сообщение о новой заявке
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

                    //buy-заявка становится partially filled => уменьшается её ActualAmount
                    buy_ord.ActualAmount -= sell_ord.ActualAmount;
                    //Pusher.NewOrderStatus(buy_ord.OrderId, buy_ord.UserId, (int)OrdExecStatus.PartiallyFilled, DateTime.Now); //сообщение о новом статусе заявки

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

                    //sell-заявка становится partially filled => уменьшается её ActualAmount
                    sell_ord.ActualAmount -= buy_ord.ActualAmount;
                    //Pusher.NewOrderStatus(sell_ord.OrderId, sell_ord.UserId, (int)OrdExecStatus.PartiallyFilled, DateTime.Now); //сообщение о новом статусе заявки

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

                    //buy-заявка становится filled => её ActualAmount становится нулевым
                    buy_ord.ActualAmount = 0m;
                    //Pusher.NewOrderStatus(buy_ord.OrderId, buy_ord.UserId, (int)OrdExecStatus.Filled, DateTime.Now); //сообщение о новом статусе заявки

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
                }

                //если все заявки в стакане (стаканах) были удалены - выходим из цикла
                if ((book.ActiveBuyOrders.Count == 0) || (book.ActiveSellOrders.Count == 0)) break;
            }

            //if (UpdTicker()) Pusher.NewTicker(bid_buf, ask_buf, DateTime.Now); //сообщение о новом тикере
            //if (UpdActiveBuyTop()) Pusher.NewActiveBuyTop(act_buy_buf, DateTime.Now); //сообщение о новом топе стакана на покупку
            //if (UpdActiveSellTop()) Pusher.NewActiveSellTop(act_sell_buf, DateTime.Now); //сообщение о новом топе стакана на продажу
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