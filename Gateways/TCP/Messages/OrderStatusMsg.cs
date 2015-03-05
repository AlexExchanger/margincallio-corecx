using System;

namespace CoreCX.Gateways.TCP.Messages
{
    class OrderStatusMsg : IJsonSerializable
    {
        private long order_id;
        private int user_id;
        private int order_status;
        private DateTime dt_made;

        internal OrderStatusMsg(long order_id, int user_id, int order_status) //конструктор сообщения
        {
            this.order_id = order_id;
            this.user_id = user_id;
            this.order_status = order_status;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewOrderStatus, order_id, user_id, order_status, dt_made);
        }
    }
}
