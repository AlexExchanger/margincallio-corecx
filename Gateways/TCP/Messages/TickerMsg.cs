using System;

namespace CoreCX.Gateways.TCP.Messages
{
    class TickerMsg : IJsonSerializable
    {
        private string derived_currency;
        private decimal bid;
        private decimal ask;
        private DateTime dt_made;

        internal TickerMsg(string derived_currency, decimal bid, decimal ask) //конструктор сообщения
        {
            this.derived_currency = derived_currency;
            this.bid = bid;
            this.ask = ask;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewTicker, derived_currency, bid, ask, dt_made);
        }
    }
}
