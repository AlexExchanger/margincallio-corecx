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

        internal OrderBook()
        {
            ActiveBuyOrders = new List<Order>(5000);
            ActiveSellOrders = new List<Order>(5000);
            ActiveBuyStops = new List<Order>(2000);
            ActiveSellStops = new List<Order>(2000);
        }

        internal void InsertBuyOrder(Order order)
        {
            BSInsertion.AddBuyOrder(ActiveBuyOrders, order);
        }

        internal void InsertSellOrder(Order order)
        {
            BSInsertion.AddSellOrder(ActiveSellOrders, order);
        }
    }
}