using System;

namespace CoreCX.Trading
{
    [Serializable]
    class Order
    {
        internal static long next_id; //в целях автоинкремента id заявки
        internal long OrderId { get; private set; }
        internal int UserId { get; private set; }
        internal decimal OriginalAmount { get; private set; }
        internal decimal ActualAmount { get; set; } //текущий объём заявки может изменяться
        internal decimal Rate { get; set; } //цена изменяется при срабатывании условной заявки
        internal Order StopLoss { get; set; } //указатель на SL-заявку, который назначается после создания основной заявки
        internal Order TakeProfit { get; set; } //указатель на TP-заявку, который назначается после создания основной заявки
        internal TSOrder TrailingStop { get; set; } //указатель на TS-заявку, который назначается после создания основной заявки
        internal FCSources FCSource { get; private set; }        
        internal string ExternalData { get; private set; }        
        internal DateTime DtMade { get; private set; }
        
        internal Order(int user_id, decimal original_amount, decimal actual_amount, decimal rate) //конструктор FL-заявки
        {
            OrderId = ++next_id; //инкремент id предыдущей заявки
            UserId = user_id;
            OriginalAmount = original_amount;
            ActualAmount = actual_amount;
            Rate = rate;
            StopLoss = null;
            TakeProfit = null;
            TrailingStop = null;
            FCSource = FCSources.Core;            
            ExternalData = null;            
            DtMade = DateTime.Now;
        }

        internal Order(int user_id, decimal original_amount, decimal actual_amount, decimal rate, FCSources fc_source, string external_data) //конструктор заявки
        {
            OrderId = ++next_id; //инкремент id предыдущей заявки
            UserId = user_id;
            OriginalAmount = original_amount;
            ActualAmount = actual_amount;
            Rate = rate;
            StopLoss = null;
            TakeProfit = null;
            TrailingStop = null;
            FCSource = fc_source;
            ExternalData = external_data;
            DtMade = DateTime.Now;
        }

        internal Order(Order ord) //конструктор копирования
        {
            OrderId = ord.OrderId;
            UserId = ord.UserId;
            OriginalAmount = ord.OriginalAmount;
            ActualAmount = ord.ActualAmount;
            Rate = ord.Rate;
            StopLoss = null;
            TakeProfit = null;
            TrailingStop = null;
            FCSource = ord.FCSource;
            ExternalData = ord.ExternalData;
            DtMade = ord.DtMade;
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
