using System;
using CoreCX.Trading;

namespace CoreCX.Gateways.TCP.Messages
{
    class OrderMsg : IJsonSerializable
    {
        private int msg_type;
        private long func_call_id;
        private int fc_source;
        private string derived_currency;
        private bool side;
        private Order order;

        internal OrderMsg(int msg_type, long func_call_id, int fc_source, bool side, Order order) //конструктор сообщения
        {
            this.msg_type = msg_type;
            this.func_call_id = func_call_id;
            this.fc_source = fc_source;
            this.side = side;
            this.order = order;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson(msg_type, func_call_id, fc_source, order_kind, order);
        }
    }
}
