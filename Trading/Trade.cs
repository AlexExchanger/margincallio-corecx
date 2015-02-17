using System;

namespace CoreCX.Trading
{
    [Serializable]
    class Trade
    {
        private static long next_id; //в целях автоинкремента id сделки
        internal long TradeId { get; private set; }
        internal long BuyOrderId { get; private set; }
        internal long SellOrderId { get; private set; }
        internal int BuyerUserId { get; private set; }
        internal int SellerUserId { get; private set; }
        internal bool Kind { get; private set; }
        internal decimal Amount { get; private set; }
        internal decimal Rate { get; private set; }
        internal decimal BuyerFee { get; private set; }
        internal decimal SellerFee { get; private set; }
        internal DateTime DtMade { get; private set; }

        internal Trade(long buy_order_id, long sell_order_id, int buyer_user_id, int seller_user_id, bool kind, decimal amount, decimal rate, decimal buyer_fee, decimal seller_fee)
        {
            TradeId = ++next_id; //инкремент id предыдущей сделки
            BuyOrderId = buy_order_id;
            SellOrderId = sell_order_id;
            BuyerUserId = buyer_user_id;
            SellerUserId = seller_user_id;
            Kind = kind;
            Amount = amount;
            Rate = rate;
            BuyerFee = buyer_fee;
            SellerFee = seller_fee;
            DtMade = DateTime.Now;
        }
    }
}
