using System;

namespace CoreCX.Trading
{
    [Serializable]
    class TSOrder : Order
    {
        internal decimal Offset { get; private set; }

        internal TSOrder(int user_id, decimal original_amount, decimal actual_amount, decimal rate, FCSources fc_source, string external_data, decimal offset)
            : base(user_id, original_amount, actual_amount, rate, fc_source, external_data) //конструктор TS
        {
            Offset = offset;
        }

        internal TSOrder(Order ord, decimal offset) : base(ord) //конструктор копирования
        {
            Offset = offset;
        }
    }
}
