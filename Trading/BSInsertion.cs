using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    static class BSInsertion //глобальный класс упорядоченный вставки с помощью Binary Search
    {
        private static decimal manage_margin_rate_deviation = 0.05m; //TODO менять на лету

        internal static void AddBuyOrder(List<Order> orders, Order new_buy_order) //ASC для buy-заявок
        {
            if (orders.Count == 0)
            {
                orders.Add(new_buy_order);
                return;
            }

            Order top_buy_order = orders[orders.Count - 1];
            if (top_buy_order.Rate.CompareTo(new_buy_order.Rate) <= 0)
            {
                orders.Add(new_buy_order);
                if (new_buy_order.Rate / top_buy_order.Rate - 1m > manage_margin_rate_deviation) MarginManager.QueueManageMarginExecution();
                return;
            }

            if (orders[0].Rate.CompareTo(new_buy_order.Rate) >= 0)
            {
                orders.Insert(0, new_buy_order);
                return;
            }

            OrderAscComparer odc = new OrderAscComparer();
            int index = orders.BinarySearch(new_buy_order, odc);
            if (index < 0)
                index = ~index;
            orders.Insert(index, new_buy_order);
        }

        internal static void AddSellOrder(List<Order> orders, Order new_sell_order) //DESC для sell-заявок
        {
            if (orders.Count == 0)
            {
                orders.Add(new_sell_order);
                return;
            }

            Order top_sell_order = orders[orders.Count - 1];
            if (top_sell_order.Rate.CompareTo(new_sell_order.Rate) >= 0)
            {
                orders.Add(new_sell_order);
                if (1m - new_sell_order.Rate / top_sell_order.Rate > manage_margin_rate_deviation) MarginManager.QueueManageMarginExecution();
                return;
            }

            if (orders[0].Rate.CompareTo(new_sell_order.Rate) <= 0)
            {
                orders.Insert(0, new_sell_order);
                return;
            }

            OrderDescComparer odc = new OrderDescComparer();
            int index = orders.BinarySearch(new_sell_order, odc);
            if (index < 0)
                index = ~index;
            orders.Insert(index, new_sell_order);
        }
    }

    class OrderAscComparer : IComparer<Order> //кастомный Comparer для сравнения по Rate ASC
    {
        public int Compare(Order x, Order y)
        {
            return x.Rate.CompareTo(y.Rate);
        }
    }

    class OrderDescComparer : IComparer<Order> //кастомный Comparer для сравнения по Rate DESC
    {
        public int Compare(Order x, Order y)
        {
            return y.Rate.CompareTo(x.Rate);
        }
    }

}
