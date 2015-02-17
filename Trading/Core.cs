using System;
using System.Collections.Generic;
using System.Configuration;

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

        internal Core()
        {
            Accounts = new Dictionary<int, Account>(3000);
            Currencies = new List<string>(10);
            OrderBooks = new Dictionary<string, OrderBook>(10);            
            FixAccounts = new Dictionary<string, FixAccount>(500);
            ApiKeys = new Dictionary<string, ApiKey>(500);

            currency_pair_separator = Convert.ToChar(ConfigurationManager.AppSettings["currency_pair_separator"]);

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
            //получаем валюты, входящие в добавляемую пару
            string[] currencies = SplitCurrencyPair(currency_pair);
            if (currencies.Length != 2) return StatusCodes.ErrorInvalidCurrencyPair;

            //проверяем, созданы ли валюты, входящие в пару
            if (!Currencies.Contains(currencies[0]) || !Currencies.Contains(currencies[1])) return StatusCodes.ErrorCurrencyNotFound;
            foreach (KeyValuePair<int, Account> acc in Accounts)
            {
                if (!acc.Value.CFunds.ContainsKey(currencies[0]) || !acc.Value.CFunds.ContainsKey(currencies[1])) return StatusCodes.ErrorCurrencyNotFound;
            }

            //создаём новый стакан для валютной пары, если ещё не создан
            if (!OrderBooks.ContainsKey(currency_pair))
            {
                OrderBooks.Add(currency_pair, new OrderBook());
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

        private string[] SplitCurrencyPair(string currency_pair) //получение валют, входящих в пару
        {
            int separator_index = currency_pair.IndexOf(currency_pair_separator);
            if (separator_index != -1) return new string[] { currency_pair.Substring(0, separator_index), currency_pair.Substring(separator_index + 1) };
            else return new string[0];
        }

        #endregion




        






        //новый метчинг
        private void Match(string currency_pair)
        {
            //получаем стаканы
            List<Order> ActiveBuyOrders = OrderBooks[currency_pair].ActiveBuyOrders;
            List<Order> ActiveSellOrders = OrderBooks[currency_pair].ActiveSellOrders;

            //получаем валюты, входящие в добавляемую пару
            string[] currencies = SplitCurrencyPair(currency_pair);

            //далее с стандартный цикл метчинга, но юзеров получаем по-другому
            while (ActiveBuyOrders[ActiveBuyOrders.Count - 1].Rate >= ActiveSellOrders[ActiveSellOrders.Count - 1].Rate)
            {
                Order buy_ord = ActiveBuyOrders[ActiveBuyOrders.Count - 1];
                Order sell_ord = ActiveSellOrders[ActiveSellOrders.Count - 1];
                Funds buyer_funds1 = Accounts[buy_ord.UserId].CFunds[currencies[0]];
                Funds buyer_funds2 = Accounts[buy_ord.UserId].CFunds[currencies[1]];
                Funds seller_funds1 = Accounts[sell_ord.UserId].CFunds[currencies[0]];
                Funds seller_funds2 = Accounts[sell_ord.UserId].CFunds[currencies[1]];

                //дальше метчим как обычно
                // <...>
            }


        }




        


    }
}