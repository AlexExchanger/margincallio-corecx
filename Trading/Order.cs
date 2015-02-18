using System;

namespace CoreCX.Trading
{
    [Serializable]
    class Order
    {
        private static long next_id; //в целях автоинкремента id заявки
        internal long OrderId { get; private set; }
        internal int UserId { get; private set; }
        internal decimal OriginalAmount { get; private set; }
        internal decimal ActualAmount { get; set; } //текущий объём заявки может изменяться
        internal decimal Rate { get; private set; }
        internal Position PosPtr { get; set; } //указатель на объект позиции присваивается после создания объекта заявки
        internal DateTime DtMade { get; private set; }

        internal Order() //конструктор заявки по умолчанию
        {
            OrderId = 0L;
            UserId = 0;
            OriginalAmount = 0m;
            ActualAmount = 0m;
            Rate = 0m;
            PosPtr = null;
            DtMade = new DateTime();
        }

        internal Order(int user_id, decimal original_amount, decimal actual_amount, decimal rate) //конструктор заявки
        {
            OrderId = ++next_id; //инкремент id предыдущей заявки
            UserId = user_id;
            OriginalAmount = original_amount;
            ActualAmount = actual_amount;
            Rate = rate;
            PosPtr = null;
            DtMade = DateTime.Now;
        }
    }

    [Serializable]
    struct OrderBuf : IEquatable<OrderBuf>
    {
        internal decimal ActualAmount;
        internal decimal Rate;

        public OrderBuf(decimal actual_amount, decimal rate)
        {
            ActualAmount = actual_amount;
            Rate = rate;
        }

        public bool Equals(OrderBuf other)
        {
            if (this.ActualAmount != other.ActualAmount) return false;
            if (this.Rate != other.Rate) return false;

            return true;
        }

        public override int GetHashCode()
        {
            return 0; //добавить имплементацию, если понадобится
        }
    }
}
