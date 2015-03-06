using System;

namespace CoreCX.Gateways.TCP.Messages
{
    class AccountFeeMsg : IJsonSerializable
    {
        private long func_call_id;
        private int user_id;
        private string derived_currency;
        private decimal fee_in_perc;
        private DateTime dt_made;

        internal AccountFeeMsg(long func_call_id, int user_id, string derived_currency, decimal fee_in_perc) //конструктор сообщения
        {
            this.func_call_id = func_call_id;
            this.user_id = user_id;
            this.derived_currency = derived_currency;
            this.fee_in_perc = fee_in_perc;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewAccountFee, func_call_id, user_id, derived_currency, fee_in_perc, dt_made);
        }
    }
}
