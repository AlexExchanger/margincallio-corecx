using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class CancOrdData
    {
        internal string DerivedCurrency { get; private set; }
        internal OrderBook Book { get; private set; }
        internal CancOrdTypes OrderType { get; private set; }
        internal bool Side { get; private set; }

        internal CancOrdData(string derived_currency, OrderBook book, CancOrdTypes order_type, bool side)
        {
            DerivedCurrency = derived_currency;            
            Book = book;
            OrderType = order_type;
            Side = side;
        }
    }
}
