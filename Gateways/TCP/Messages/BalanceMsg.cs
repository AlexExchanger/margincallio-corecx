using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreCX.Gateways.TCP.Messages
{
    class BalanceMsg : IJsonSerializable
    {
        private int user_id;
        private string currency;
        private decimal available_funds;
        private decimal blocked_funds;
        private DateTime dt_made;

        internal BalanceMsg(int user_id, string currency, decimal available_funds, decimal blocked_funds) //конструктор сообщения
        {
            this.user_id = user_id;
            this.currency = currency;
            this.available_funds = available_funds;
            this.blocked_funds = blocked_funds;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewBalance, user_id, currency, available_funds, blocked_funds, dt_made);
        }
    }
}
