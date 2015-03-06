using System;

namespace CoreCX.Gateways.TCP.Messages
{
    class OrderMatchMsg : IJsonSerializable
    {
        private string derived_currency;
        private long order_id;
        private int user_id;
        private decimal actual_amount;
        private int order_status;
        private DateTime dt_made;

        internal OrderMatchMsg(string derived_currency, long order_id, int user_id, decimal actual_amount, int order_status) //конструктор сообщения
        {
            this.derived_currency = derived_currency;
            this.order_id = order_id;
            this.user_id = user_id;
            this.actual_amount = actual_amount;
            this.order_status = order_status;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewOrderMatch, derived_currency, order_id, user_id, actual_amount, order_status, dt_made);
        }
    }
}
