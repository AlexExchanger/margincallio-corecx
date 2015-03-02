using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace CoreCX.Gateways.TCP
{
    static class SocketIO
    {
        internal static string Read(TcpClient client)
        {
            byte[] buffer = new byte[4096];
            int totalRead = 0;

            try
            {
                //читаем байты, пока ни одного не останется
                do
                {
                    int read = client.GetStream().Read(buffer, totalRead, buffer.Length - totalRead);
                    totalRead += read;
                } while (client.GetStream().DataAvailable);

                if (totalRead == 0) return "dc"; //пустое сообщение - отсоединяем клиента
                return Encoding.ASCII.GetString(buffer, 0, totalRead);
            }
            catch
            {
                //ошибка чтения из потока - отсоединяем клиента
                return "dc";
            }
        }
        
        internal static bool Write(TcpClient client, string tech_json)
        {          
            try
            {
                tech_json += '\n';
                byte[] bytes = Encoding.ASCII.GetBytes(tech_json);
                client.GetStream().Write(bytes, 0, bytes.Length);
            }
            catch
            {
                //произошла ошибка сокета
                return false;
            }
            return true;
        }

    }
}
