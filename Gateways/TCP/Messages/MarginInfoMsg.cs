using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreCX.Gateways.TCP.Messages
{
    class MarginInfoMsg : IJsonSerializable
    {
        private int user_id;
        private decimal equity;
        private decimal margin;
        private decimal free_margin;
        private decimal margin_level;
        private DateTime dt_made;

        internal MarginInfoMsg(int user_id, decimal equity, decimal margin, decimal free_margin, decimal margin_level) //конструктор сообщения
        {
            this.user_id = user_id;
            this.equity = equity;
            this.margin = margin;
            this.free_margin = free_margin;
            this.margin_level = margin_level;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewMarginInfo, user_id, equity, margin, free_margin, margin_level, dt_made);
        }
    }
}
