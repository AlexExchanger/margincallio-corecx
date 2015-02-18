using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class OrderBook
    {
        internal List<Order> ActiveBuyOrders { get; set; }
        internal List<Order> ActiveSellOrders { get; set; }
        internal List<Order> ActiveBuyStops { get; set; } //TODO inheritance
        internal List<Order> ActiveSellStops { get; set; } //TODO inheritance
        internal List<Position> ActiveLongPositions { get; set; }
        internal List<Position> ActiveShortPositions { get; set; }
        internal decimal DefaultFee { get; set; }
        internal decimal DefaultMaxLeverage { get; set; }
        internal decimal DefaultLevelMC { get; set; }
        internal decimal DefaultLevelFL { get; set; }

        internal OrderBook()
        {
            ActiveBuyOrders = new List<Order>(5000);
            ActiveSellOrders = new List<Order>(5000);
            ActiveBuyStops = new List<Order>(2000);
            ActiveSellStops = new List<Order>(2000);
            ActiveLongPositions = new List<Position>(2000);
            ActiveShortPositions = new List<Position>(2000);
            DefaultFee = 0.002m;
            DefaultMaxLeverage = 5m;
            DefaultLevelMC = 0.14m;
            DefaultLevelFL = 0.07m;
        }

    }
}