using System;
using System.Collections.Concurrent;
using QuickFix;

namespace CoreCX
{
    static class Queues
    {
        //на исполнение
        internal static ConcurrentQueue<FuncCall> stdf_queue = new ConcurrentQueue<FuncCall>(); //очередь вызовов стандартных функций
        internal static ConcurrentQueue<Action> prdf_queue = new ConcurrentQueue<Action>(); //очередь вызовов периодических функций

        //на передачу данных
        //internal static ConcurrentQueue<IJsonSerializable> daemon_queue = new ConcurrentQueue<IJsonSerializable>(); //очередь сообщений для демона        
        //internal static ConcurrentQueue<FuncCallReplica> slave_queue = new ConcurrentQueue<FuncCallReplica>(); //очередь FC и снэпшотов для Slave-ядра
        //internal static ConcurrentDictionary<string, ConcurrentQueue<Message>> fix_dict = new ConcurrentDictionary<string, ConcurrentQueue<Message>>(); //словарь очередей для FIX-сессий
        //internal static ConcurrentQueue<Message> fix_orders_queue = new ConcurrentQueue<Message>(); //очередь FIX multicast - заявки
        //internal static ConcurrentQueue<Message> fix_trades_queue = new ConcurrentQueue<Message>(); //очередь FIX multicast - сделки
    }
}
