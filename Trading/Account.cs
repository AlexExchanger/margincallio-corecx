using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class Account
    {
        internal BaseFunds BaseCFunds { get; set; } //средства в базовой валюте
        internal Dictionary<string, DerivedFunds> DerivedCFunds { get; set; } //"производная валюта -> средства и комиссия"
        internal decimal MaxLeverage { get; set; } //максимальное плечо
        internal decimal LevelMC { get; set; } //уровень Margin Call
        internal decimal LevelFL { get; set; } //уровень Forced Liquidation
        internal decimal Equity { get; set; } //текущие собственные средства
        internal decimal Margin { get; set; } //текущая маржа
        internal decimal FreeMargin { get; set; } //текущая свободная маржа
        internal decimal MarginLevel { get; set; } //текущий уровень маржи
        internal bool Suspended { get; set; } //флаг блокировки торгового счёта

        internal Account()
        {
            BaseCFunds = new BaseFunds();
            DerivedCFunds = new Dictionary<string, DerivedFunds>(10);
            MaxLeverage = 5m;
            LevelMC = 0.14m;
            LevelFL = 0.07m;
            Equity = 0m;
            Margin = 0m;
            FreeMargin = 0m;
            MarginLevel = 0m;
            Suspended = new bool();
        }
    }
}