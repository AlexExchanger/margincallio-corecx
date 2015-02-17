﻿using System;
using System.Threading;

namespace CoreCX
{
    class FuncCall
    {
        private static long next_id; //в целях автоинкремента id вызова функции ядра
        internal long FuncCallId { get; private set; }
        internal Action Action { get; set; }

        internal FuncCall() //конструктор вызова функции
        {
            FuncCallId = Interlocked.Increment(ref next_id); //многопоточный инкремент id предыдущего вызова
        }
    }
}
