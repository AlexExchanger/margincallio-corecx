using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class OrderBook
    {
        internal List<Order> ActiveBuyOrders { get; set; }
        internal List<Order> ActiveSellOrders { get; set; }
        internal List<Order> BuySLs { get; set; } //TODO inheritance
        internal List<Order> SellSLs { get; set; } //TODO inheritance
        internal List<Order> BuyTPs { get; set; } //TODO inheritance
        internal List<Order> SellTPs { get; set; } //TODO inheritance
        internal List<TSOrder> BuyTSs { get; set; }
        internal List<TSOrder> SellTSs { get; set; }

        internal OrderBook()
        {
            ActiveBuyOrders = new List<Order>(5000);
            ActiveSellOrders = new List<Order>(5000);
            BuySLs = new List<Order>(2000);
            SellSLs = new List<Order>(2000);
            BuyTPs = new List<Order>(2000);
            SellTPs = new List<Order>(2000);
            BuyTSs = new List<TSOrder>(500);
            SellTSs = new List<TSOrder>(500);
        }

        internal void InsertBuyOrder(Order order)
        {
            BSInsertion.AddBuyOrder(ActiveBuyOrders, order);
        }

        internal void InsertSellOrder(Order order)
        {
            BSInsertion.AddSellOrder(ActiveSellOrders, order);
        }
               
        internal void InsertBuySL(Order order)
        {
            BSInsertion.AddSellOrder(BuySLs, order);
        }

        internal void InsertSellSL(Order order)
        {
            BSInsertion.AddBuyOrder(SellSLs, order);
        }

        internal void InsertBuyTP(Order order)
        {
            BSInsertion.AddBuyOrder(BuyTPs, order);
        }

        internal void InsertSellTP(Order order)
        {
            BSInsertion.AddSellOrder(SellTPs, order);
        }

        internal void InsertBuyTS(TSOrder ts_order)
        {
            BuyTSs.Add(ts_order);
        }

        internal void InsertSellTS(TSOrder ts_order)
        {
            SellTSs.Add(ts_order);
        }

        internal void RemoveBuyOrder(int index)
        {
            ActiveBuyOrders.RemoveAt(index);
        }

        internal void RemoveSellOrder(int index)
        {
            ActiveSellOrders.RemoveAt(index);
        }

        internal void RemoveBuySL(int index)
        {
            BuySLs.RemoveAt(index);
        }

        internal void RemoveSellSL(int index)
        {
            SellSLs.RemoveAt(index);
        }

        internal void RemoveBuyTP(int index)
        {
            BuyTPs.RemoveAt(index);
        }

        internal void RemoveSellTP(int index)
        {
            SellTPs.RemoveAt(index);
        }

        internal void RemoveBuyTS(int index)
        {
            BuyTSs.RemoveAt(index);
        }

        internal void RemoveSellTS(int index)
        {
            SellTSs.RemoveAt(index);
        }
    }
}