using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Newtonsoft.Json;

namespace CoreCX.Gateways.TCP
{
    static class JsonManager
    {
        internal static bool ParseTechJson(string received, out int func_id, out string[] str_args) //низкоуровневый JSON-парсинг
        {
            func_id = -1; //номер функции ядра
            str_args = new string[8]; //аргументы функции ядра
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
                            if (index == 7) break;
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
        
        #region INSTANT RESPONSES

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

        #region POST-EXECUTION RESPONSES

        #region PHP RESPONSES

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

        internal static string FormTechJson(long func_call_id, int status_code, string api_key, string secret)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2");
                    writer.WriteValue(api_key);
                    writer.WritePropertyName("3");
                    writer.WriteValue(secret);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, string password)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2");
                    writer.WriteValue(password);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, Dictionary<string, FixAccount> fix_accounts)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2"); //массив FIX-счетов
                    writer.WriteStartArray();
                    foreach (KeyValuePair<string, FixAccount> acc in fix_accounts)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(acc.Key);
                        writer.WriteValue(acc.Value.UserId);
                        writer.WriteValue(acc.Value.Password);
                        writer.WriteValue(acc.Value.Active.ToInt32());
                        writer.WriteValue(acc.Value.DtGenerated.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, Dictionary<string, ApiKey> api_keys)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2"); //массив API-ключей
                    writer.WriteStartArray();
                    foreach (KeyValuePair<string, ApiKey> key in api_keys)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(key.Value.Rights.ToInt32());
                        writer.WriteValue(key.Key);
                        writer.WriteValue(Encoding.ASCII.GetString(key.Value.Secret));
                        writer.WriteValue(key.Value.LastNonce);
                        writer.WriteValue(key.Value.DtGenerated.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, Account account)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2");
                    writer.WriteValue(account.AvailableFunds1);
                    writer.WritePropertyName("3");
                    writer.WriteValue(account.BlockedFunds1);
                    writer.WritePropertyName("4");
                    writer.WriteValue(account.AvailableFunds2);
                    writer.WritePropertyName("5");
                    writer.WriteValue(account.BlockedFunds2);
                    writer.WritePropertyName("6");
                    writer.WriteValue(account.Fee);
                    writer.WritePropertyName("7");
                    writer.WriteValue(account.Equity);
                    writer.WritePropertyName("8");
                    writer.WriteValue(account.MarginLevel * 100m);
                    writer.WritePropertyName("9");
                    writer.WriteValue(account.MarginCall.ToInt32());
                    writer.WritePropertyName("10");
                    writer.WriteValue(account.Suspended.ToInt32());
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<Order> open_buy, List<Order> open_sell)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2"); //заявки на покупку
                    writer.WriteStartArray();
                    for (int i = 0; i < open_buy.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(open_buy[i].OrderId);
                        writer.WriteValue(open_buy[i].UserId);
                        writer.WriteValue(open_buy[i].OriginalAmount);
                        writer.WriteValue(open_buy[i].ActualAmount);
                        writer.WriteValue(open_buy[i].Rate);
                        writer.WriteValue(open_buy[i].FCSource);
                        writer.WriteValue(open_buy[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("3"); //заявки на продажу
                    writer.WriteStartArray();
                    for (int i = 0; i < open_sell.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(open_sell[i].OrderId);
                        writer.WriteValue(open_sell[i].UserId);
                        writer.WriteValue(open_sell[i].OriginalAmount);
                        writer.WriteValue(open_sell[i].ActualAmount);
                        writer.WriteValue(open_sell[i].Rate);
                        writer.WriteValue(open_sell[i].FCSource);
                        writer.WriteValue(open_sell[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<Order> long_sls, List<Order> short_sls, List<Order> long_tps, List<Order> short_tps, List<TSOrder> long_tss, List<TSOrder> short_tss)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2");
                    writer.WriteStartArray();
                    for (int i = 0; i < long_sls.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(long_sls[i].OrderId);
                        writer.WriteValue(long_sls[i].UserId);
                        writer.WriteValue(long_sls[i].ActualAmount);
                        writer.WriteValue(long_sls[i].Rate);
                        writer.WriteValue(long_sls[i].FCSource);
                        writer.WriteValue(long_sls[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("3");
                    writer.WriteStartArray();
                    for (int i = 0; i < short_sls.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(short_sls[i].OrderId);
                        writer.WriteValue(short_sls[i].UserId);
                        writer.WriteValue(short_sls[i].ActualAmount);
                        writer.WriteValue(short_sls[i].Rate);
                        writer.WriteValue(short_sls[i].FCSource);
                        writer.WriteValue(short_sls[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("4");
                    writer.WriteStartArray();
                    for (int i = 0; i < long_tps.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(long_tps[i].OrderId);
                        writer.WriteValue(long_tps[i].UserId);
                        writer.WriteValue(long_tps[i].ActualAmount);
                        writer.WriteValue(long_tps[i].Rate);
                        writer.WriteValue(long_tps[i].FCSource);
                        writer.WriteValue(long_tps[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("5");
                    writer.WriteStartArray();
                    for (int i = 0; i < short_tps.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(short_tps[i].OrderId);
                        writer.WriteValue(short_tps[i].UserId);
                        writer.WriteValue(short_tps[i].ActualAmount);
                        writer.WriteValue(short_tps[i].Rate);
                        writer.WriteValue(short_tps[i].FCSource);
                        writer.WriteValue(short_tps[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("6");
                    writer.WriteStartArray();
                    for (int i = 0; i < long_tss.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(long_tss[i].OrderId);
                        writer.WriteValue(long_tss[i].UserId);
                        writer.WriteValue(long_tss[i].ActualAmount);
                        writer.WriteValue(long_tss[i].Rate);
                        writer.WriteValue(long_tss[i].Offset);
                        writer.WriteValue(long_tss[i].FCSource);
                        writer.WriteValue(long_tss[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("7");
                    writer.WriteStartArray();
                    for (int i = 0; i < short_tss.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(short_tss[i].OrderId);
                        writer.WriteValue(short_tss[i].UserId);
                        writer.WriteValue(short_tss[i].ActualAmount);
                        writer.WriteValue(short_tss[i].Rate);
                        writer.WriteValue(short_tss[i].Offset);
                        writer.WriteValue(short_tss[i].FCSource);
                        writer.WriteValue(short_tss[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, decimal bid_price, decimal ask_price)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2");
                    writer.WriteValue(bid_price);
                    writer.WritePropertyName("3");
                    writer.WriteValue(ask_price);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<OrderBuf> bids, List<OrderBuf> asks, decimal bids_vol, decimal asks_vol, int bids_num, int asks_num)
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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2");
                    writer.WriteValue(bids_vol);
                    writer.WritePropertyName("3");
                    writer.WriteValue(asks_vol);
                    writer.WritePropertyName("4");
                    writer.WriteValue(bids_num);
                    writer.WritePropertyName("5");
                    writer.WriteValue(asks_num);
                    writer.WritePropertyName("6");
                    writer.WriteStartArray();
                    for (int i = 0; i < bids.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(bids[i].ActualAmount);
                        writer.WriteValue(bids[i].Rate);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("7");
                    writer.WriteStartArray();
                    for (int i = 0; i < asks.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(asks[i].ActualAmount);
                        writer.WriteValue(asks[i].Rate);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        #endregion

        #region HTTP API RESPONSES

        internal static string FormTechJson(long func_call_id, int status_code, decimal bid_price, decimal ask_price, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(dt_made.Ticks);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);
                writer.WritePropertyName("3");
                writer.WriteValue(bid_price); //bid
                writer.WritePropertyName("4");
                writer.WriteValue(ask_price); //ask
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<OrderBuf> bids, List<OrderBuf> asks, decimal bids_vol, decimal asks_vol, int bids_num, int asks_num, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(dt_made.Ticks);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);

                if (status_code == 0)
                {
                    writer.WritePropertyName("3"); //bids' volume (in currency2)
                    writer.WriteValue(bids_vol);
                    writer.WritePropertyName("4"); //asks' volume (in currency1)
                    writer.WriteValue(asks_vol);
                    writer.WritePropertyName("5"); //bids' num
                    writer.WriteValue(bids_num);
                    writer.WritePropertyName("6"); //asks' num
                    writer.WriteValue(asks_num);
                    writer.WritePropertyName("7"); //bids
                    writer.WriteStartArray();
                    for (int i = 0; i < bids.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(bids[i].Rate);
                        writer.WriteValue(bids[i].ActualAmount);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("8"); //asks
                    writer.WriteStartArray();
                    for (int i = 0; i < asks.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(asks[i].Rate);
                        writer.WriteValue(asks[i].ActualAmount);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, Account account, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(dt_made.Ticks);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);
                
                if (status_code == 0)
                {
                    writer.WritePropertyName("3");
                    writer.WriteValue(account.AvailableFunds1); //AF1
                    writer.WritePropertyName("4");
                    writer.WriteValue(account.BlockedFunds1); //BF1         
                    writer.WritePropertyName("5");
                    writer.WriteValue(account.AvailableFunds2); //AF2
                    writer.WritePropertyName("6");
                    writer.WriteValue(account.BlockedFunds2); //BF2 
                    writer.WritePropertyName("7");
                    writer.WriteValue(account.Fee * 100m); //Fee (%)
                    writer.WritePropertyName("8");
                    writer.WriteValue(account.MarginLevel * 100m); //Margin Level (%) 
                    writer.WritePropertyName("9");
                    writer.WriteValue(account.MarginCall.ToInt32()); //Margin Call
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<Order> open_buy, List<Order> open_sell, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(dt_made.Ticks);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);

                if (status_code == 0)
                {
                    writer.WritePropertyName("3"); //на покупку
                    writer.WriteStartArray();
                    for (int i = 0; i < open_buy.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(open_buy[i].OrderId);
                        writer.WriteValue(open_buy[i].OriginalAmount);
                        writer.WriteValue(open_buy[i].ActualAmount);
                        writer.WriteValue(open_buy[i].Rate);
                        writer.WriteValue(open_buy[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("4"); //на продажу
                    writer.WriteStartArray();
                    for (int i = 0; i < open_sell.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(open_sell[i].OrderId);
                        writer.WriteValue(open_sell[i].OriginalAmount);
                        writer.WriteValue(open_sell[i].ActualAmount);
                        writer.WriteValue(open_sell[i].Rate);
                        writer.WriteValue(open_sell[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<Order> long_sls, List<Order> short_sls, List<Order> long_tps, List<Order> short_tps, List<TSOrder> long_tss, List<TSOrder> short_tss, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(dt_made.Ticks);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);

                if (status_code == 0)
                {
                    writer.WritePropertyName("3"); //стоп-лоссы для лонгов
                    writer.WriteStartArray();
                    for (int i = 0; i < long_sls.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(long_sls[i].OrderId);
                        writer.WriteValue(long_sls[i].ActualAmount);
                        writer.WriteValue(long_sls[i].Rate);
                        writer.WriteValue(long_sls[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("4"); //стоп-лоссы для шортов
                    writer.WriteStartArray();
                    for (int i = 0; i < short_sls.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(short_sls[i].OrderId);
                        writer.WriteValue(short_sls[i].ActualAmount);
                        writer.WriteValue(short_sls[i].Rate);
                        writer.WriteValue(short_sls[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("5"); //тейк-профиты для лонгов
                    writer.WriteStartArray();
                    for (int i = 0; i < long_tps.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(long_tps[i].OrderId);
                        writer.WriteValue(long_tps[i].ActualAmount);
                        writer.WriteValue(long_tps[i].Rate);
                        writer.WriteValue(long_tps[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("6"); //тейк-профиты для шортов
                    writer.WriteStartArray();
                    for (int i = 0; i < short_tps.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(short_tps[i].OrderId);
                        writer.WriteValue(short_tps[i].ActualAmount);
                        writer.WriteValue(short_tps[i].Rate);
                        writer.WriteValue(short_tps[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("7"); //трейлинг-стопы для лонгов
                    writer.WriteStartArray();
                    for (int i = 0; i < long_tss.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(long_tss[i].OrderId);
                        writer.WriteValue(long_tss[i].ActualAmount);
                        writer.WriteValue(long_tss[i].Rate);
                        writer.WriteValue(long_tss[i].Offset);
                        writer.WriteValue(long_tss[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                    writer.WritePropertyName("8"); //трейлинг-стопы для шортов
                    writer.WriteStartArray();
                    for (int i = 0; i < short_tss.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(short_tss[i].OrderId);
                        writer.WriteValue(short_tss[i].ActualAmount);
                        writer.WriteValue(short_tss[i].Rate);
                        writer.WriteValue(short_tss[i].Offset);
                        writer.WriteValue(short_tss[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, bool order_kind, Order order, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(dt_made.Ticks);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);

                if (status_code == 0)
                {
                    writer.WritePropertyName("3");
                    writer.WriteValue(order.OrderId);
                    writer.WritePropertyName("4");
                    writer.WriteValue(order_kind.ToInt32());
                    writer.WritePropertyName("5");
                    writer.WriteValue(order.OriginalAmount);
                    writer.WritePropertyName("6");
                    writer.WriteValue(order.ActualAmount);
                    writer.WritePropertyName("7");
                    writer.WriteValue(order.Rate);
                    writer.WritePropertyName("8");
                    writer.WriteValue(order.DtMade.Ticks);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, bool order_kind, TSOrder ts_order, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("1");
                writer.WriteValue(dt_made.Ticks);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);

                if (status_code == 0)
                {
                    writer.WritePropertyName("3");
                    writer.WriteValue(ts_order.OrderId);
                    writer.WritePropertyName("4");
                    writer.WriteValue(order_kind.ToInt32());
                    writer.WritePropertyName("5");
                    writer.WriteValue(ts_order.OriginalAmount);
                    writer.WritePropertyName("6");
                    writer.WriteValue(ts_order.ActualAmount);
                    writer.WritePropertyName("7");
                    writer.WriteValue(ts_order.Rate);
                    writer.WritePropertyName("8");
                    writer.WriteValue(ts_order.Offset);
                    writer.WritePropertyName("9");
                    writer.WriteValue(ts_order.DtMade.Ticks);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }




        #endregion

        #region DAEMON MESSAGES

        internal static string FormTechJson(int message_type, int user_id, Account account, DateTime dt_made) 
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(user_id);
                writer.WritePropertyName("2");
                writer.WriteValue(account.AvailableFunds1);
                writer.WritePropertyName("3");
                writer.WriteValue(account.BlockedFunds1);
                writer.WritePropertyName("4");
                writer.WriteValue(account.AvailableFunds2);
                writer.WritePropertyName("5");
                writer.WriteValue(account.BlockedFunds2);
                writer.WritePropertyName("6");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, decimal bid, decimal ask, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(bid);
                writer.WritePropertyName("2");
                writer.WriteValue(ask);
                writer.WritePropertyName("3");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, Trade trade)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(trade.TradeId);
                writer.WritePropertyName("2");
                writer.WriteValue(trade.BuyOrderId);
                writer.WritePropertyName("3");
                writer.WriteValue(trade.SellOrderId);
                writer.WritePropertyName("4");
                writer.WriteValue(trade.BuyerUserId);
                writer.WritePropertyName("5");
                writer.WriteValue(trade.SellerUserId);
                writer.WritePropertyName("6");
                writer.WriteValue(trade.Kind.ToInt32());
                writer.WritePropertyName("7");
                writer.WriteValue(trade.Amount);
                writer.WritePropertyName("8");
                writer.WriteValue(trade.Rate);
                writer.WritePropertyName("9");
                writer.WriteValue(trade.BuyerFee);
                writer.WritePropertyName("10");
                writer.WriteValue(trade.SellerFee);
                writer.WritePropertyName("11");
                writer.WriteValue(trade.DtMade.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, long func_call_id, int fc_source, bool order_kind, Order order)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("2");
                writer.WriteValue(fc_source);
                writer.WritePropertyName("3");
                writer.WriteValue(order.OrderId);
                writer.WritePropertyName("4");
                writer.WriteValue(order.UserId);
                writer.WritePropertyName("5");
                writer.WriteValue(order_kind.ToInt32());
                writer.WritePropertyName("6");
                writer.WriteValue(order.ActualAmount);
                writer.WritePropertyName("7");
                writer.WriteValue(order.Rate);

                TSOrder ts_order = order as TSOrder;
                if (ts_order == null)
                {
                    writer.WritePropertyName("8");
                    writer.WriteValue(order.DtMade.Ticks);
                }
                else
                {
                    writer.WritePropertyName("8");
                    writer.WriteValue(ts_order.Offset);
                    writer.WritePropertyName("9");
                    writer.WriteValue(ts_order.DtMade.Ticks);
                }
                
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, bool order_kind, Order order)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(order.OrderId);
                writer.WritePropertyName("2");
                writer.WriteValue(order.UserId);
                writer.WritePropertyName("3");
                writer.WriteValue(order_kind.ToInt32());
                writer.WritePropertyName("4");
                writer.WriteValue(order.ActualAmount);
                writer.WritePropertyName("5");
                writer.WriteValue(order.Rate);
                writer.WritePropertyName("6");
                writer.WriteValue(order.DtMade.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }
        
        internal static string FormTechJson(int message_type, long order_id, int user_id, int order_status, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(order_id);
                writer.WritePropertyName("2");
                writer.WriteValue(user_id);
                writer.WritePropertyName("3");
                writer.WriteValue(order_status);
                writer.WritePropertyName("4");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }
            
            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, long func_call_id, int user_id, decimal fee_in_perc, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("2");
                writer.WriteValue(user_id);
                writer.WritePropertyName("3");
                writer.WriteValue(fee_in_perc);
                writer.WritePropertyName("4");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, int user_id, decimal equity, decimal ml_in_perc, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(user_id);
                writer.WritePropertyName("2");
                writer.WriteValue(equity);
                writer.WritePropertyName("3");
                writer.WriteValue(ml_in_perc);
                writer.WritePropertyName("4");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, int user_id, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(user_id);
                writer.WritePropertyName("2");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, int op_code, int status_code, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(op_code);
                writer.WritePropertyName("2");
                writer.WriteValue(status_code);
                writer.WritePropertyName("3");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, List<OrderBuf> act_top, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteStartArray();
                for (int i = 0; i < act_top.Count; i++)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(act_top[i].ActualAmount);
                    writer.WriteValue(act_top[i].Rate);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.WritePropertyName("2");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        #endregion

        #region SLAVE CORE MESSAGES

        internal static string FormTechJson(int func_id, string[] str_args)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_id);

                for (int i = 0; i < str_args.Length; i++)
                {
                    writer.WritePropertyName((i + 1).ToString());
                    writer.WriteValue(str_args[i]);
                }
                
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        #endregion

        #endregion
        
    }
}
