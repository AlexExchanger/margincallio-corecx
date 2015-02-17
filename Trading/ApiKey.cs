using System;

namespace CoreCX.Trading
{
    [Serializable]
    class ApiKey
    {
        internal int UserId { get; private set; }
        internal byte[] Secret { get; private set; } //секретный ключ
        internal bool Rights { get; private set; } //0 - info, 1 - info+trade
        internal string LastSignature { get; set; } //hexadecimal
        internal long LastNonce { get; set; }
        internal DateTime DtGenerated { get; private set; }

        internal ApiKey(int user_id, byte[] secret, bool rights) //быстрый конструктор API-ключа
        {
            UserId = user_id;
            Secret = secret;
            Rights = rights;
            LastSignature = "";
            LastNonce = 0;
            DtGenerated = DateTime.Now;
        }
    }
}
