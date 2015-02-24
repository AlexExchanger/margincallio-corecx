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
            Debitors = new Dictionary<int, Account>(1000);
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

        internal StatusCodes DepositFunds(int user_id, string currency, decimal sum) //пополнить торговый счёт //TODO uncomment
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то пополняем
            {
                //if (sum > 0m) //проверка на положительность суммы пополнения
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
                //else return StatusCodes.ErrorNegativeOrZeroSum;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes GetAvailableToWithdrawFunds(int user_id, string currency, out decimal amount) //получение суммы средств, доступной для вывода
        {
            amount = 0m;

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то снимаем
            {
                if (acc.Suspended) return StatusCodes.ErrorAccountSuspended; //проверка на блокировку счёта

                decimal[] margin_pars = CalcAccMarginPars(acc); //расчёт маржинальных параметров юзера    

                if (margin_pars[2] > 0)
                {
                    if (currency == base_currency) //снятие базовой валюты
                    {
                        amount = margin_pars[2];
                        return StatusCodes.Success;
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
            //реплицировать

            OrderBook book;
            if (OrderBooks.TryGetValue(derived_currency, out book)) //проверка на существование стакана
            {
                return BaseLimit(user_id, derived_currency, book, side, amount, rate, (int)MessageTypes.NewPlaceLimit, func_call_id, fc_source, external_data);
            }
            else return StatusCodes.ErrorCurrencyPairNotFound;           
        }

        internal StatusCodes PlaceMarket(int user_id, string derived_currency, bool side, bool base_amount, decimal amount, long func_call_id, int fc_source, string external_data = null) //подать рыночную заявку
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

                        if (market_rate == 0m) return StatusCodes.ErrorInsufficientMarketVolume;

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

                        if (market_rate == 0m) return StatusCodes.ErrorInsufficientMarketVolume;

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

                        if (market_rate == 0m) return StatusCodes.ErrorInsufficientMarketVolume;

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
                            decimal cur_amount = positive ? funds.Value.AvailableFunds : funds.Value.AvailableFunds * (-1m) / (1m - funds.Value.Fee);

                            decimal market_rate = 0m;
                            decimal accumulated_amount = 0m;
                            for (int i = ActiveOrders.Count - 1; i >= 0; i--)
                            {
                                accumulated_amount += ActiveOrders[i].ActualAmount;
                                if (accumulated_amount >= cur_amount) //если объём накопленных заявок превышает сумму в производной валюте на счёте
                                {
                                    market_rate = ActiveOrders[i].Rate;
                                    break;
                                }
                            }

                            if (positive) debit += cur_amount * market_rate; //начисляем дебету положительную сумму в базовой валюте
                            else credit += cur_amount * market_rate; //начисляем кредиту положительную сумму в базовой валюте
                        }
                        if ((debit - credit) * acc.MaxLeverage - credit >= sum)
                        {
                            if (!Debitors.ContainsKey(user_id)) Debitors.Add(user_id, acc); //добавление юзера в словарь дебиторов
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
                            decimal cur_amount = positive ? funds.Value.AvailableFunds : funds.Value.AvailableFunds * (-1m) / (1m - funds.Value.Fee);

                            decimal market_rate = 0m;
                            decimal accumulated_amount = 0m;
                            for (int i = ActiveOrders.Count - 1; i >= 0; i--)
                            {
                                accumulated_amount += ActiveOrders[i].ActualAmount;
                                if (accumulated_amount >= cur_amount) //если объём накопленных заявок превышает сумму в производной валюте на счёте
                                {
                                    market_rate = ActiveOrders[i].Rate;
                                    break;
                                }
                            }

                            if (funds.Value.AvailableFunds > 0m) debit += cur_amount * market_rate; //начисляем дебету положительную сумму в базовой валюте
                            else credit += cur_amount * market_rate; //начисляем кредиту положительную сумму в базовой валюте
                        }
                        if ((debit - credit) * acc.MaxLeverage - credit >= amount * rate)
                        {
                            if (!Debitors.ContainsKey(user_id)) Debitors.Add(user_id, acc); //добавление юзера в словарь дебиторов
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

        #region MARGIN MANAGEMENT

        internal void ManageMargin() //расчёт маржинальных параметров, выполнение MC/FL в случае необходимости
        {
            //учёт id юзеров, закрывших позиции с плечом
            List<int> ids_to_rm = new List<int>();

            //проверка каждого дебитора
            foreach (KeyValuePair<int, Account> acc in Debitors)
            {
                //объявление необходимых переменных
                decimal debit = 0m;
                decimal credit = 0m;                

                //оценка суммы в базовой валюте
                if (acc.Value.BaseCFunds.AvailableFunds >= 0m) debit += acc.Value.BaseCFunds.AvailableFunds; //начисляем дебету положительную сумму в базовой валюте
                else credit -= acc.Value.BaseCFunds.AvailableFunds; //начисляем кредиту положительную сумму в базовой валюте

                //оценка сумм в производных валютах, приведённых к базовой
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

                    if (positive) debit += amount * market_rate; //начисляем дебету положительную сумму в базовой валюте
                    else credit += amount * market_rate; //начисляем кредиту положительную сумму в базовой валюте
                }

                //проверка на использование заёмных средств
                if (credit == 0m)
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

                //расчёт маржинальных показателей
                decimal equity = debit - credit;
                decimal margin = credit / acc.Value.MaxLeverage;
                decimal margin_level = equity / margin;
                if (acc.Value.Equity != equity) //обновляем параметры аккаунта, если equity изменился
                {
                    acc.Value.Equity = equity;
                    acc.Value.Margin = margin;
                    acc.Value.FreeMargin = equity - margin;
                    acc.Value.MarginLevel = margin_level * 100m;
                    //Pusher.NewMarginInfo(account.Key, account.Value.Equity, account.Value.MarginLevel * 100m, DateTime.Now); //сообщение о новом уровне маржи
                }

                //проверка условия Margin Call
                if (margin_level <= acc.Value.LevelMC)
                {
                    if (!acc.Value.MarginCall)
                    {
                        acc.Value.MarginCall = true;
                        //Pusher.NewMarginCall(account.Key, DateTime.Now); //сообщение о новом Margin Call
                    }
                }
                else if (acc.Value.MarginCall) acc.Value.MarginCall = false; //сброс флага Margin Call

                //проверка условия Forced Liquidation
                if (margin_level <= acc.Value.LevelFL)
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