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
            char currency_pair_separator = Convert.ToChar(ConfigurationManager.AppSettings["currency_pair_separator"]);

            core = new Core(currency_pair_separator);
            proc = new Processor();

            Console.WriteLine(core.CreateCurrency("doge"));
            Console.WriteLine(core.CreateCurrency("usdr"));
            Console.WriteLine(core.CreateCurrencyPair("dogeusd_r"));

            proc.Start(); //старт исполнения функций из очередей в Main Thread
        }
    }
}
