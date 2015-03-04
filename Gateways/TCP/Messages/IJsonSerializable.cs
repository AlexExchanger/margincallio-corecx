using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CoreCX.Gateways.TCP.Messages
{
    interface IJsonSerializable
    {
        string Serialize();
    }
}
