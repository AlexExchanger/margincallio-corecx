using System;

namespace CoreCX.Trading
{
    [Serializable]
    class DerivedFunds : BaseFunds
    {
        internal decimal Fee { get; set; } //торговая комиссия
        
        internal DerivedFunds() : base() //конструктор по умолчанию
        {
            Fee = 0.002m;            
        }
    }
}
