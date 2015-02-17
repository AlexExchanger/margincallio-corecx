using System;

namespace CoreCX.Trading
{
    [Serializable]
    class Position
    {
        private static long next_id; //в целях автоинкремента id позиции
        internal long PositionId { get; private set; }
        internal int UserId { get; private set; }
        internal decimal TargetDebit { get; set; } //целевая сумма собственных средств юзера в позиции
        internal decimal CurrentDebit { get; set; } //текущая сумма собственных средств юзера в позиции
        internal decimal Credit { get; set; } //текущая сумма заёмных средств юзера в позиции
        internal decimal Size { get; set; }
        internal decimal Rate { get; private set; }
        internal decimal RateSL { get; set; }
        internal decimal RateTP { get; set; }
        internal decimal RateTS { get; set; }
        internal decimal OffsetTS { get; set; }
        internal decimal Equity { get; set; }
        internal decimal MarginLevel { get; set; }
        internal bool MarginCall { get; set; }        
        internal DateTime DtMade { get; private set; }
        
        internal Position(int user_id, decimal target_debit, decimal current_debit, decimal credit, decimal size, decimal rate, decimal rate_sl, decimal rate_tp, decimal rate_ts, decimal offset_ts) //конструктор позиции
        {
            PositionId = ++next_id; //инкремент id предыдущей позиции
            UserId = user_id;
            TargetDebit = target_debit;
            CurrentDebit = current_debit;
            Credit = credit;
            Size = size;
            Rate = rate;
            RateSL = rate_sl;
            RateTP = rate_tp;
            RateTS = rate_ts;
            OffsetTS = offset_ts;
            Equity = 0m;
            MarginLevel = 1m;
            MarginCall = false;
            DtMade = DateTime.Now;
        }
    }
}
