using System;

namespace CoreCX.Trading
{
    [Serializable]
    class DerivedFunds : BaseFunds
    {
        internal decimal Fee { get; set; } //торговая комиссия
        internal decimal MaxLeverage { get; set; } //максимальное плечо
        internal decimal LevelMC { get; set; } //уровень Margin Call
        internal decimal LevelFL { get; set; } //уровень Forced Liquidation

        internal DerivedFunds() : base() //конструктор по умолчанию
        {
            Fee = 0.002m;
            MaxLeverage = 5m;
            LevelMC = 0.14m;
            LevelFL = 0.07m;
        }
    }
}
