using System;
using CoreCX.Trading;

namespace CoreCX.Gateways.TCP.Messages
{
    class TradeMsg : IJsonSerializable
    {
        private string derived_currency;
        private Trade trade;

        internal TradeMsg(string derived_currency, Trade trade) //конструктор сообщения
        {
            this.derived_currency = derived_currency;
            this.trade = trade;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewTrade, derived_currency, trade);
        }
    }
}
