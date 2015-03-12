using System;
using System.Configuration;
using CoreCX.Trading;
using CoreCX.Gateways.TCP;

namespace CoreCX
{
    static class App
    {
        //объекты уровня приложения
        internal static Core core;
        internal static TcpServer tcpsrv;
        internal static Processor proc;

        static void Main(string[] args)
        {
            core = new Core(ConfigurationManager.AppSettings["base_currency"], ConfigurationManager.AppSettings["currency_pair_separator"][0]);
            tcpsrv = new TcpServer(int.Parse(ConfigurationManager.AppSettings["web_app_port"]), int.Parse(ConfigurationManager.AppSettings["marketmaker_port"]), int.Parse(ConfigurationManager.AppSettings["daemon_port"]), int.Parse(ConfigurationManager.AppSettings["reserve_port"]));
            proc = new Processor();

            core.CreateCurrencyPair("btc");
            Console.WriteLine(DateTime.Now + " CORE: created 'btc_eur' currency pair");

            core.CreateCurrencyPair("ltc");
            Console.WriteLine(DateTime.Now + " CORE: created 'ltc_eur' currency pair");

            core.CreateCurrencyPair("doge");
            Console.WriteLine(DateTime.Now + " CORE: created 'doge_eur' currency pair");
            
            proc.Start(); //старт исполнения функций из очередей в Main Thread
        }
    }
}
