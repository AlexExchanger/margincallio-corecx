using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace CoreCX.Gateways.TCP
{
    static class JsonManager
    {
        #region JSON PARSING

        internal static bool ParseTechJson(string received, out int func_id, out string[] str_args) //низкоуровневый JSON-парсинг
        {
            func_id = -1; //номер функции ядра
            str_args = new string[9]; //аргументы функции ядра
            int index = 0;
            bool _parsed;
            try
            {
                JsonTextReader reader = new JsonTextReader(new StringReader(received));
                while (reader.Read())
                {
                    if ((reader.TokenType != JsonToken.PropertyName) && (reader.Value != null))
                    {
                        if (func_id == -1) func_id = int.Parse(reader.Value.ToString());
                        else
                        {
                            str_args[index] = reader.Value.ToString();
                            if (index == 8) break;
                            index++;
                        }
                    }
                }
                _parsed = true;
            }
            catch
            { _parsed = false; }
            return _parsed;
        }

        #endregion

        #region WEB APP PRE-EXECUTION RESPONSES

        internal static string FormTechJson(int status_code)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(status_code);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int status_code, long func_call_id)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(status_code);
                writer.WritePropertyName("1");
                writer.WriteValue(func_call_id);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        #endregion

        #region WEB APP POST-EXECUTION RESPONSES

        internal static string FormTechJson(long func_call_id, int status_code)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(status_code);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, decimal amount)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(status_code);
                writer.WritePropertyName("2");
                writer.WriteValue(amount);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, decimal available_funds, decimal blocked_funds)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(status_code);
                writer.WritePropertyName("2");
                writer.WriteValue(available_funds);
                writer.WritePropertyName("3");
                writer.WriteValue(blocked_funds);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        #endregion

        

    }
}
