using System;

namespace CoreCX.Trading
{
    [Serializable]
    class Funds
    {
        internal decimal AvailableFunds { get; set; }
        internal decimal BlockedFunds { get; set; }

        internal Funds()
        {
            AvailableFunds = 0m;
            BlockedFunds = 0m;
        }
    }
}