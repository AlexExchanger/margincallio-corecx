using System;
using CoreCX.Trading;

namespace CoreCX.Gateways.TCP.Messages
{
    class TradeMsg : IJsonSerializable
    {
        private Trade trade;

        internal TradeMsg(Trade trade) //конструктор сообщения
        {
            this.trade = trade;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewTrade, trade);
        }
    }
}
