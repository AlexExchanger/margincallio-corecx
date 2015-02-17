using System;

namespace CoreCX.Trading
{
    [Serializable]
    class OrdCancData
    {
        internal string CurrencyPair { get; set; }
        internal int OrderType { get; set; }
        internal bool Side { get; set; }        

        internal OrdCancData(string currency_pair, int order_type, bool side)
        {
            CurrencyPair = currency_pair;
            OrderType = order_type;
            Side = side;
        }
    }
}
