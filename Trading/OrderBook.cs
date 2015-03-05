using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class OrderBook
    {
        #region DATA COLLECTIONS

        internal List<Order> ActiveBuyOrders { get; set; }
        internal List<Order> ActiveSellOrders { get; set; }
        internal List<Order> BuySLs { get; set; }
        internal List<Order> SellSLs { get; set; }
        internal List<Order> BuyTPs { get; set; }
        internal List<Order> SellTPs { get; set; }
        internal List<TSOrder> BuyTSs { get; set; }
        internal List<TSOrder> SellTSs { get; set; }

        #endregion

        #region BUFFER VARIABLES

        internal decimal bid_buf { get; set; }
        internal decimal ask_buf { get; set; }
        internal int act_buy_buf_max_size { get; set; }
        internal int act_sell_buf_max_size { get; set; }
        internal List<OrderBuf> act_buy_buf { get; set; }
        internal List<OrderBuf> act_sell_buf { get; set; }

        #endregion

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

            bid_buf = 0m;
            ask_buf = 0m;
            act_buy_buf_max_size = 30;
            act_sell_buf_max_size = 30;
            act_buy_buf = new List<OrderBuf>(act_buy_buf_max_size);
            act_sell_buf = new List<OrderBuf>(act_sell_buf_max_size);
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