using System;

namespace CoreCX.Trading
{
    [Serializable]
    class BaseFunds
    {
        internal decimal AvailableFunds { get; set; } //��������� ��������
        internal decimal BlockedFunds { get; set; } //��������������� � ������� ��������
        
        internal BaseFunds()
        {
            AvailableFunds = 0m;
            BlockedFunds = 0m;            
        }

        internal BaseFunds(BaseFunds funds) //����������� �����������
        {
            AvailableFunds = funds.AvailableFunds;
            BlockedFunds = funds.BlockedFunds;
        }
    }
}