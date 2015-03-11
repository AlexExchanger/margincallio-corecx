using System;
using CoreCX.Gateways.TCP;

namespace CoreCX.Recovery
{
    class FuncCallReplica
    {
        internal int func_id { get; private set; }
        internal string[] str_args { get; private set; }

        internal FuncCallReplica(int func_id, string[] str_args) //конструктор реплицируемого FC
        {
            this.func_id = func_id;
            this.str_args = str_args;
        }

        internal FuncCallReplica(int func_id) //конструктор FC backup/restore
        {
            this.func_id = func_id;
            this.str_args = null;
        }

        public string Serialize()
        {
            return JsonManager.FormTechJson(func_id, str_args);
        }
    }
}
