using System;
using System.Collections.Generic;

namespace CoreCX.Trading
{
    [Serializable]
    class Account
    {
        internal BaseFunds BaseCFunds { get; set; } //средства в базовой валюте
        internal Dictionary<string, DerivedFunds> DerivedCFunds { get; set; } //"производная валюта -> средства и параметры"
        internal bool Suspended { get; set; } //флаг блокировки торгового счёта

        internal Account()
        {
            BaseCFunds = new BaseFunds();
            DerivedCFunds = new Dictionary<string, DerivedFunds>(10);
            Suspended = new bool();
        }
    }
}