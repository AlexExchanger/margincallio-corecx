using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class OrdCancData
    {
        internal string DerivedCurrency { get; private set; }
        internal OrderBook Book { get; private set; }
        internal bool Side { get; private set; }

        internal OrdCancData(string derived_currency, OrderBook book, bool side)
        {
            DerivedCurrency = derived_currency;            
            Book = book;
            Side = side;
        }
    }
}
