using System;
using CoreCX.Trading;

namespace CoreCX.Gateways.TCP.Messages
{
    class OrderMsg : IJsonSerializable
    {
        private int order_event;        
        private int fc_source;
        private long func_call_id;
        private string derived_currency;
        private bool side;
        private Order order;

        internal OrderMsg(int order_event, int fc_source, long func_call_id, string derived_currency, bool side, Order order) //конструктор сообщения
        {
            this.order_event = order_event;            
            this.fc_source = fc_source;
            this.func_call_id = func_call_id;
            this.derived_currency = derived_currency;
            this.side = side;
            this.order = order;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewOrder, order_event, fc_source, func_call_id, derived_currency, side, order);
        }
    }
}
