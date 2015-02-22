using System;
using System.Configuration;
using CoreCX.Trading;

namespace CoreCX
{
    static class App
    {
        //объекты уровня приложения
        internal static Core core;
        internal static Processor proc;

        static void Main(string[] args)
        {
            core = new Core(ConfigurationManager.AppSettings["base_currency"], ConfigurationManager.AppSettings["currency_pair_separator"][0]);
            proc = new Processor();


            Console.WriteLine(core.CreateAccount(1));
            Console.WriteLine(core.CreateCurrencyPair("btc"));
            Console.WriteLine(core.CreateAccount(2));
            Console.WriteLine(core.CreateCurrencyPair("ltc"));
            Console.WriteLine(core.CreateAccount(3));
            Console.WriteLine();
            Console.WriteLine(core.DepositFunds(1, "btc", 99999999m));
            Console.WriteLine(core.DepositFunds(1, "ltc", 99999999m));
            Console.WriteLine(core.DepositFunds(1, "eur", 9999999999m));

            Console.WriteLine(core.DepositFunds(2, "btc", 0.5m));
            Console.WriteLine(core.DepositFunds(2, "eur", 150m));
            Console.WriteLine(core.DepositFunds(2, "ltc", 10m));
            Console.WriteLine();

            Console.WriteLine(core.PlaceLimit(1, "btc", false, 15m, 300m, 12L, 2));
            Console.WriteLine(core.PlaceLimit(1, "btc", true, 15m, 310m, 13L, 2));
            Console.WriteLine(core.PlaceLimit(1, "ltc", false, 250m, 10m, 16L, 2));
            Console.WriteLine(core.PlaceLimit(1, "ltc", true, 250m, 11m, 17L, 2));
            Console.WriteLine(core.PlaceMarket(2, "btc", true, false, 6m, 15L, 2));
            Console.WriteLine(core.PlaceMarket(2, "btc", false, false, 5m, 14L, 2));            

            proc.Start(); //старт исполнения функций из очередей в Main Thread
        }
    }
}
