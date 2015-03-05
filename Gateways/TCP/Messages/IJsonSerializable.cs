using System;

namespace CoreCX.Gateways.TCP.Messages
{
    interface IJsonSerializable
    {
        string Serialize();
    }
}
