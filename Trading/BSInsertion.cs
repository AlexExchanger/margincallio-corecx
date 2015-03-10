using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    static class BSInsertion //глобальный класс упорядоченный вставки с помощью Binary Search
    {
        internal static void AddBuyOrder(List<Order> orders, Order new_buy_order) //ASC для buy-заявок
        {
            if (orders.Count == 0)
            {
                orders.Add(new_buy_order);
                return;
            }

            if (orders[orders.Count - 1].Rate.CompareTo(new_buy_order.Rate) <= 0)
            {
                orders.Add(new_buy_order);                
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

            if (orders[orders.Count - 1].Rate.CompareTo(new_sell_order.Rate) >= 0)
            {
                orders.Add(new_sell_order);
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
