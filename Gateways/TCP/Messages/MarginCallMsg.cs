using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreCX.Gateways.TCP.Messages
{
    class MarginCallMsg : IJsonSerializable
    {
        private int user_id;
        private DateTime dt_made;

        internal MarginCallMsg(int user_id) //конструктор сообщения
        {
            this.user_id = user_id;
            this.dt_made = DateTime.Now;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson((int)MessageTypes.NewMarginCall, user_id, dt_made);
        }
    }
}
