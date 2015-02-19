using System;

namespace CoreCX.Trading
{
    [Serializable]
    class BaseFunds
    {
        internal decimal AvailableFunds { get; set; } //доступные средства
        internal decimal BlockedFunds { get; set; } //заблокированные в заявках средства
        
        internal BaseFunds()
        {
            AvailableFunds = 0m;
            BlockedFunds = 0m;            
        }
    }
}