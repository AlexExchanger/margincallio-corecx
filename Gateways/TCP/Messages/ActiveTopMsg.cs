using System;
using System.Collections.Generic;
using CoreCX.Trading;

namespace CoreCX.Gateways.TCP.Messages
{
    class ActiveTopMsg : IJsonSerializable
    {
        private string derived_currency;
        private bool side;
        private List<OrderBuf> act_top;
        private DateTime dt_made;

        internal ActiveTopMsg(string derived_currency, bool side, List<OrderBuf> act_top) //конструктор сообщения
        {
            this.derived_currency = derived_currency;
            this.side = side;
            this.act_top = act_top;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewOrderBookTop, derived_currency, side, act_top, dt_made);
        }
    }
}
