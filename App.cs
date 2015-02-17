using System;
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
            core = new Core();
            proc = new Processor();

            Console.WriteLine(core.CreateCurrency("doge"));
            Console.WriteLine(core.CreateCurrency("usdr"));
            Console.WriteLine(core.CreateCurrencyPair("doge_usdr"));

            proc.Start(); //старт исполнения функций из очередей в Main Thread
        }
    }
}
