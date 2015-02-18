using System;

namespace CoreCX.Trading
{
    [Serializable]
    class OrdCancData
    {
        internal string CurrencyPair { get; private set; }
        internal int OrderType { get; private set; }
        internal bool Side { get; private set; }        

        internal OrdCancData(string currency_pair, int order_type, bool side)
        {
            CurrencyPair = currency_pair;
            OrderType = order_type;
            Side = side;
        }
    }
}
