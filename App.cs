﻿using System;
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
            Console.WriteLine(core.DepositFunds(2, "eur", 350m));
            Console.WriteLine(core.DepositFunds(2, "ltc", 10m));
            Console.WriteLine();

            Console.WriteLine(core.PlaceLimit(1, "btc", false, 50m, 300m, 0m, 0m, 0m, 12L, FCSources.Core));
            Console.WriteLine(core.PlaceLimit(1, "btc", true, 50m, 310m, 0m, 0m, 0m, 13L, FCSources.Core));
            Console.WriteLine(core.PlaceLimit(1, "ltc", false, 250m, 10m, 0m, 0m, 0m, 16L, FCSources.Core));
            Console.WriteLine(core.PlaceLimit(1, "ltc", true, 250m, 11m, 0m, 0m, 0m, 17L, FCSources.Core));

            Console.WriteLine(core.PlaceMarket(2, "btc", true, false, 3m, 320m, 290m, 20m, 18L, FCSources.Core));
            Console.WriteLine(core.PlaceMarket(2, "btc", true, false, 2m, 315m, 280m, 30m, 18L, FCSources.Core));
            Console.WriteLine(core.PlaceMarket(2, "btc", true, false, 2m, 350m, 270m, 30m, 18L, FCSources.Core));
            Console.WriteLine(core.CancelOrder(2, 5, 19L, FCSources.Core));

            Console.WriteLine(core.PlaceLimit(1, "btc", false, 150m, 320m, 0m, 0m, 0m, 19L, FCSources.Core));
            Console.WriteLine(core.PlaceLimit(1, "btc", true, 150m, 325m, 0m, 0m, 0m, 19L, FCSources.Core));


            //core.ManageMargin();

            //core.WithdrawFunds(2, "eur", 74.73m);

            //Console.WriteLine(core.PlaceLimit(1, "btc", false, 50m, 240m, 12L, 2));
            //Console.WriteLine(core.PlaceLimit(1, "btc", true, 100m, 242m, 13L, 2));
            //core.ManageMargin();

            //Console.WriteLine(core.PlaceMarket(2, "ltc", false, true, 114m, 0m, 0m, 18L, 2));
            //core.ManageMargin();

            proc.Start(); //старт исполнения функций из очередей в Main Thread
        }
    }
}
