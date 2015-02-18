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
        private List<string> Currencies; //список "валюты"
        private Dictionary<string, OrderBook> OrderBooks; //словарь "валютная пара -> стакан"	
        private Dictionary<long, OrdCancData> OrderCancellationData; //словарь для закрытия заявок
        private Dictionary<string, FixAccount> FixAccounts; //FIX-аккаунты
        private Dictionary<string, ApiKey> ApiKeys; //API-ключи

        #endregion

        #region PARAMETERS

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

        internal Core(char currency_pair_separator)
        {
            Accounts = new Dictionary<int, Account>(3000);
            Currencies = new List<string>(10);
            OrderBooks = new Dictionary<string, OrderBook>(10);            
            FixAccounts = new Dictionary<string, FixAccount>(500);
            ApiKeys = new Dictionary<string, ApiKey>(500);

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
                Account acc = new Account();

                for (int i = 0; i < Currencies.Count; i++) //добавляем балансы в текущих валютах
                {
                    acc.CFunds.Add(Currencies[i], new Funds());
                }

                foreach (string currency_pair in OrderBooks.Keys)
                {
                    acc.Fees.Add(currency_pair, OrderBooks[currency_pair].DefaultFee); //задаём комиссии по умолчанию
                    acc.MaxLeverages.Add(currency_pair, OrderBooks[currency_pair].DefaultMaxLeverage); //задаём максимальные плечи по умолчанию
                    acc.LevelsMC.Add(currency_pair, OrderBooks[currency_pair].DefaultLevelMC); //задаём уровни Margin Call по умолчанию
                    acc.LevelsFL.Add(currency_pair, OrderBooks[currency_pair].DefaultLevelFL); //задаём уровни Forced Liquidation по умолчанию
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

        internal StatusCodes DeleteAccount(int user_id) //удалить торговый счёт TODO ликвидировать позиции, потом заявки, возвращать массив средств на выход
        {
            //реплицировать

            if (Accounts.ContainsKey(user_id)) //если счёт существует, то удаляем
            {
                //удаляем все зависимости
                foreach (OrderBook book in OrderBooks.Values)
                {
                    book.ActiveBuyOrders.RemoveAll(i => i.UserId == user_id);
                    book.ActiveSellOrders.RemoveAll(i => i.UserId == user_id);
                    book.ActiveBuyStops.RemoveAll(i => i.UserId == user_id);
                    book.ActiveSellStops.RemoveAll(i => i.UserId == user_id);
                    //TODO ликвидировать все позиции юзера
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
                if (sum > 0) //проверка на положительность суммы пополнения
                {
                    if (!Currencies.Contains(currency)) return StatusCodes.ErrorCurrencyNotFound;

                    Funds funds;
                    if (acc.CFunds.TryGetValue(currency, out funds))
                    {
                        funds.AvailableFunds += sum;
                        //Pusher.NewBalance(user_id, currency, available_funds, blocked_funds, DateTime.Now); //сообщение о новом балансе
                        return StatusCodes.Success;
                    }
                    else return StatusCodes.ErrorCurrencyNotFound;
                }
                else return StatusCodes.ErrorNegativeOrZeroSum;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }

        internal StatusCodes WithdrawFunds(int user_id, string currency, decimal sum) //снять с торгового счёта
        {
            //реплицировать

            Account acc;
            if (Accounts.TryGetValue(user_id, out acc)) //если счёт существует, то снимаем
            {
                if (acc.Suspended) return StatusCodes.ErrorAccountSuspended; //проверка на блокировку счёта

                if (sum > 0) //проверка на положительность суммы вывода
                {
                    if (!Currencies.Contains(currency)) return StatusCodes.ErrorCurrencyNotFound;

                    Funds funds;
                    if (acc.CFunds.TryGetValue(currency, out funds))
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
                else return StatusCodes.ErrorNegativeOrZeroSum;
            }
            else return StatusCodes.ErrorAccountNotFound;
        }


        #endregion

        #region GLOBAL FUNCTIONS

        internal StatusCodes CreateCurrency(string currency)
        {
            bool _created = false;

            //добавляем валюту в список "валюты", если валюта ещё не добавлена
            if (!Currencies.Contains(currency))
            {
                Currencies.Add(currency);
                _created = true;
            }

            foreach (KeyValuePair<int, Account> acc in Accounts)
            {
                //добавляем валюту к аккаунту юзера, если валюта ещё не добавлена
                if (!acc.Value.CFunds.ContainsKey(currency))
                {
                    acc.Value.CFunds.Add(currency, new Funds());
                    _created = true;
                }
            }

            if (_created) return StatusCodes.Success;
            else return StatusCodes.ErrorCurrencyAlreadyExists;
        }

        internal StatusCodes GetCurrencies(out List<string> currencies)
        {
            currencies = new List<string>(Currencies);
            return StatusCodes.Success;
        }

        internal StatusCodes DeleteCurrency() // TODO IN MARCH
        {
            return StatusCodes.Success;
        }

        internal StatusCodes CreateCurrencyPair(string currency_pair)
        {
            //получаем валюты, входящие в пару
            string[] currencies = SplitCurrencyPair(currency_pair);
            if (currencies.Length != 2) return StatusCodes.ErrorInvalidCurrencyPair;
            if (String.IsNullOrEmpty(currencies[0]) || String.IsNullOrEmpty(currencies[1])) return StatusCodes.ErrorInvalidCurrencyPair;
            
            //проверяем, созданы ли валюты, входящие в пару
            if (!Currencies.Contains(currencies[0]) || !Currencies.Contains(currencies[1])) return StatusCodes.ErrorCurrencyNotFound;
            foreach (KeyValuePair<int, Account> acc in Accounts)
            {
                if (!acc.Value.CFunds.ContainsKey(currencies[0]) || !acc.Value.CFunds.ContainsKey(currencies[1])) return StatusCodes.ErrorCurrencyNotFound;
            }

            //создаём новый стакан для валютной пары, если ещё не создан
            if (!OrderBooks.ContainsKey(currency_pair))
            {
                OrderBook book = new OrderBook();
                OrderBooks.Add(currency_pair, book);

                foreach (KeyValuePair<int, Account> acc in Accounts)
                {
                    //добавляем настройки валютной пары к аккаунту юзера
                    if (!acc.Value.Fees.ContainsKey(currency_pair)) acc.Value.Fees.Add(currency_pair, book.DefaultFee);
                    if (!acc.Value.MaxLeverages.ContainsKey(currency_pair)) acc.Value.MaxLeverages.Add(currency_pair, book.DefaultMaxLeverage);
                    if (!acc.Value.LevelsMC.ContainsKey(currency_pair)) acc.Value.LevelsMC.Add(currency_pair, book.DefaultLevelMC);
                    if (!acc.Value.LevelsFL.ContainsKey(currency_pair)) acc.Value.LevelsFL.Add(currency_pair, book.DefaultLevelFL);
                }
                
                return StatusCodes.Success;
            }
            else return StatusCodes.ErrorCurrencyPairAlreadyExists;
        }

        internal StatusCodes GetCurrencyPairs(out List<string> currency_pairs)
        {
            currency_pairs = new List<string>(OrderBooks.Keys);
            return StatusCodes.Success;
        }

        internal StatusCodes DeleteCurrencyPair() // TODO IN MARCH
        {
            return StatusCodes.Success;
        }

        #endregion

        #endregion

        #region SERVICE CORE FUNCTIONS

        #region MATCHING

        //ПРИНИМАЕТ НА ВХОД ГАРАНТИРОВАННО ВЕРНЫЕ ПАРАМЕТРЫ
        private void Match(OrderBook book, string currency_pair, string[] currencies) //выполняет метчинг текущих активных заявок в заданном стакане
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
                Funds buyer_funds1 = buyer.CFunds[currencies[0]];
                Funds buyer_funds2 = buyer.CFunds[currencies[1]];
                Funds seller_funds1 = seller.CFunds[currencies[0]];
                Funds seller_funds2 = seller.CFunds[currencies[1]];
                decimal buyer_fee = buyer.Fees[currency_pair];
                decimal seller_fee = seller.Fees[currency_pair];
                
                //поиск наиболее ранней заявки для определения initiator_kind, trade_rate (и комиссий)
                bool initiator_kind;
                decimal trade_rate = 0;
                if (buy_ord.OrderId < sell_ord.OrderId)
                {
                    initiator_kind = false; //buy
                    trade_rate = buy_ord.Rate;
                    //дифференциация комиссий maker-taker тут
                }
                else
                {
                    initiator_kind = true; //sell
                    trade_rate = sell_ord.Rate;
                    //дифференциация комиссий maker-taker тут
                }
                
                //три варианта в зависимости от объёма каждой из 2-х выполняемых заявок
                if (buy_ord.ActualAmount > sell_ord.ActualAmount) //1-ый вариант - объём buy-заявки больше
                {
                    //добавляем объект Trade в коллекцию
                    Trade trade = new Trade(buy_ord.OrderId, sell_ord.OrderId, buy_ord.UserId, sell_ord.UserId, initiator_kind, sell_ord.ActualAmount, trade_rate, sell_ord.ActualAmount * buyer_fee, sell_ord.ActualAmount * trade_rate * seller_fee);
                    //Pusher.NewTrade(trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_funds1.BlockedFunds -= sell_ord.ActualAmount;
                    seller_funds2.AvailableFunds += sell_ord.ActualAmount * trade_rate * (1m - seller_fee);
                    //Pusher.NewBalance(sell_ord.UserId, seller, DateTime.Now); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer_funds2.BlockedFunds -= sell_ord.ActualAmount * buy_ord.Rate;
                    buyer_funds1.AvailableFunds += sell_ord.ActualAmount * (1m - buyer_fee);
                    buyer_funds2.AvailableFunds += sell_ord.ActualAmount * (buy_ord.Rate - trade_rate);
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
                    Trade trade = new Trade(buy_ord.OrderId, sell_ord.OrderId, buy_ord.UserId, sell_ord.UserId, initiator_kind, buy_ord.ActualAmount, trade_rate, buy_ord.ActualAmount * buyer_fee, buy_ord.ActualAmount * trade_rate * seller_fee);
                    //Pusher.NewTrade(trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_funds1.BlockedFunds -= buy_ord.ActualAmount;
                    seller_funds2.AvailableFunds += buy_ord.ActualAmount * trade_rate * (1m - seller_fee);
                    //Pusher.NewBalance(sell_ord.UserId, seller, DateTime.Now); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer_funds2.BlockedFunds -= buy_ord.ActualAmount * buy_ord.Rate;
                    buyer_funds1.AvailableFunds += buy_ord.ActualAmount * (1m - buyer_fee);
                    buyer_funds2.AvailableFunds += buy_ord.ActualAmount * (buy_ord.Rate - trade_rate);
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
                    Trade trade = new Trade(buy_ord.OrderId, sell_ord.OrderId, buy_ord.UserId, sell_ord.UserId, initiator_kind, buy_ord.ActualAmount, trade_rate, sell_ord.ActualAmount * buyer_fee, sell_ord.ActualAmount * trade_rate * seller_fee);
                    //Pusher.NewTrade(trade); //сообщение о новой сделке

                    //начисляем продавцу сумму минус комиссия
                    seller_funds1.BlockedFunds -= buy_ord.ActualAmount;
                    seller_funds2.AvailableFunds += buy_ord.ActualAmount * trade_rate * (1m - seller_fee);
                    //Pusher.NewBalance(sell_ord.UserId, seller, DateTime.Now); //сообщение о новом балансе

                    //начисляем покупателю сумму минус комиссия плюс разницу
                    buyer_funds2.BlockedFunds -= buy_ord.ActualAmount * buy_ord.Rate;
                    buyer_funds1.AvailableFunds += buy_ord.ActualAmount * (1m - buyer_fee);
                    buyer_funds2.AvailableFunds += buy_ord.ActualAmount * (buy_ord.Rate - trade_rate);
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