using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class Account
    {
        internal Dictionary<string, Funds> CFunds { get; set; } //"валюта -> средства"
        internal Dictionary<string, decimal> Fees { get; set; } //"валютная пара -> комиссия"
        internal Dictionary<string, decimal> MaxLeverages { get; set; } //"валютная пара -> максимальное плечо"
        internal Dictionary<string, decimal> LevelsMC { get; set; } //"валютная пара -> уровень Margin Call"
        internal Dictionary<string, decimal> LevelsFL { get; set; } //"валютная пара -> уровень Forced Liquidation"
        internal bool Suspended { get; set; } //флаг блокировки торгового счёта

        internal Account()
        {
            CFunds = new Dictionary<string, Funds>(10);
            Fees = new Dictionary<string, decimal>(10);
            MaxLeverages = new Dictionary<string, decimal>(10);
            LevelsMC = new Dictionary<string, decimal>(10);
            LevelsFL = new Dictionary<string, decimal>(10);
            Suspended = new bool();
        }
    }
}