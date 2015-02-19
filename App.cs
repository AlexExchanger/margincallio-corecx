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

            Console.WriteLine(core.CreateCurrencyPair("btc"));

            proc.Start(); //старт исполнения функций из очередей в Main Thread
        }
    }
}
