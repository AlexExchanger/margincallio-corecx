using System;

namespace CoreCX.Trading
{
    [Serializable]
    class FixAccount
    {
        internal int UserId { get; private set; }
        internal string Password { get; set; } //FIX-пароль
        internal bool Active { get; set; } //флаг установки аккаунта в FIX-настройки
        internal DateTime DtGenerated { get; private set; }

        internal FixAccount(int user_id, string password) //конструктор FIX-аккаунта
        {
            UserId = user_id;
            Password = password;
            Active = false;
            DtGenerated = DateTime.Now;
        }
    }
}
