using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using Newtonsoft.Json;
using CoreCX.Trading;

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

                if (status_code == 0)
                {
                    writer.WritePropertyName("2");
                    writer.WriteValue(amount);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, decimal bid, decimal ask)
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
                    writer.WriteValue(bid);
                    writer.WritePropertyName("3");
                    writer.WriteValue(ask);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, BaseFunds funds)
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
                    writer.WriteValue(funds.AvailableFunds);
                    writer.WritePropertyName("3");
                    writer.WriteValue(funds.BlockedFunds);
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, Dictionary<string, BaseFunds> funds)
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
                    foreach (KeyValuePair<string, BaseFunds> val in funds)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(val.Key);
                        writer.WriteValue(val.Value.AvailableFunds);
                        writer.WriteValue(val.Value.BlockedFunds);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, Account acc_pars)
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
                    writer.WriteValue(acc_pars.MaxLeverage);
                    writer.WritePropertyName("3");
                    writer.WriteValue(acc_pars.LevelMC * 100m);
                    writer.WritePropertyName("4");
                    writer.WriteValue(acc_pars.LevelFL * 100m);
                    writer.WritePropertyName("5");
                    writer.WriteValue(acc_pars.Equity);
                    writer.WritePropertyName("6");
                    writer.WriteValue(acc_pars.Margin);
                    writer.WritePropertyName("7");
                    writer.WriteValue(acc_pars.FreeMargin);
                    writer.WritePropertyName("8");
                    writer.WriteValue(acc_pars.MarginLevel * 100m);
                    writer.WritePropertyName("9");
                    writer.WriteValue(acc_pars.MarginCall.ToInt32());
                    writer.WritePropertyName("10");
                    writer.WriteValue(acc_pars.Suspended.ToInt32());
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<string> strings)
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
                    for (int i = 0; i < strings.Count; i++)
                    {
                        writer.WriteValue(strings[i]);
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, List<Order> buy_limit, List<Order> sell_limit, List<Order> buy_sl, List<Order> sell_sl, List<Order> buy_tp, List<Order> sell_tp, List<TSOrder> buy_ts, List<TSOrder> sell_ts)
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
                    for (int i = 0; i < buy_limit.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(buy_limit[i].OrderId);
                        writer.WriteValue(buy_limit[i].UserId);
                        writer.WriteValue(buy_limit[i].OriginalAmount);
                        writer.WriteValue(buy_limit[i].ActualAmount);
                        writer.WriteValue(buy_limit[i].Rate);
                        writer.WriteValue(buy_limit[i].FCSource);
                        writer.WriteValue(buy_limit[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("3");
                    writer.WriteStartArray();
                    for (int i = 0; i < sell_limit.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(sell_limit[i].OrderId);
                        writer.WriteValue(sell_limit[i].UserId);
                        writer.WriteValue(sell_limit[i].OriginalAmount);
                        writer.WriteValue(sell_limit[i].ActualAmount);
                        writer.WriteValue(sell_limit[i].Rate);
                        writer.WriteValue(sell_limit[i].FCSource);
                        writer.WriteValue(sell_limit[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("4");
                    writer.WriteStartArray();
                    for (int i = 0; i < buy_sl.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(buy_sl[i].OrderId);
                        writer.WriteValue(buy_sl[i].UserId);
                        writer.WriteValue(buy_sl[i].OriginalAmount);
                        writer.WriteValue(buy_sl[i].ActualAmount);
                        writer.WriteValue(buy_sl[i].Rate);
                        writer.WriteValue(buy_sl[i].FCSource);
                        writer.WriteValue(buy_sl[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("5");
                    writer.WriteStartArray();
                    for (int i = 0; i < sell_sl.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(sell_sl[i].OrderId);
                        writer.WriteValue(sell_sl[i].UserId);
                        writer.WriteValue(sell_sl[i].OriginalAmount);
                        writer.WriteValue(sell_sl[i].ActualAmount);
                        writer.WriteValue(sell_sl[i].Rate);
                        writer.WriteValue(sell_sl[i].FCSource);
                        writer.WriteValue(sell_sl[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("6");
                    writer.WriteStartArray();
                    for (int i = 0; i < buy_tp.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(buy_tp[i].OrderId);
                        writer.WriteValue(buy_tp[i].UserId);
                        writer.WriteValue(buy_tp[i].OriginalAmount);
                        writer.WriteValue(buy_tp[i].ActualAmount);
                        writer.WriteValue(buy_tp[i].Rate);
                        writer.WriteValue(buy_tp[i].FCSource);
                        writer.WriteValue(buy_tp[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("7");
                    writer.WriteStartArray();
                    for (int i = 0; i < sell_tp.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(sell_tp[i].OrderId);
                        writer.WriteValue(sell_tp[i].UserId);
                        writer.WriteValue(sell_tp[i].OriginalAmount);
                        writer.WriteValue(sell_tp[i].ActualAmount);
                        writer.WriteValue(sell_tp[i].Rate);
                        writer.WriteValue(sell_tp[i].FCSource);
                        writer.WriteValue(sell_tp[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("8");
                    writer.WriteStartArray();
                    for (int i = 0; i < buy_ts.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(buy_ts[i].OrderId);
                        writer.WriteValue(buy_ts[i].UserId);
                        writer.WriteValue(buy_ts[i].OriginalAmount);
                        writer.WriteValue(buy_ts[i].ActualAmount);
                        writer.WriteValue(buy_ts[i].Rate);
                        writer.WriteValue(buy_ts[i].FCSource);
                        writer.WriteValue(buy_ts[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();

                    writer.WritePropertyName("9");
                    writer.WriteStartArray();
                    for (int i = 0; i < sell_ts.Count; i++)
                    {
                        writer.WriteStartArray();
                        writer.WriteValue(sell_ts[i].OrderId);
                        writer.WriteValue(sell_ts[i].UserId);
                        writer.WriteValue(sell_ts[i].OriginalAmount);
                        writer.WriteValue(sell_ts[i].ActualAmount);
                        writer.WriteValue(sell_ts[i].Offset);
                        writer.WriteValue(sell_ts[i].FCSource);
                        writer.WriteValue(sell_ts[i].DtMade.Ticks);
                        writer.WriteEndArray();
                    }
                    writer.WriteEndArray();
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(long func_call_id, int status_code, string derived_currency, bool side, Order order)
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
                    writer.WriteValue(derived_currency);
                    writer.WritePropertyName("3");
                    writer.WriteValue(order.OrderId);
                    writer.WritePropertyName("4");
                    writer.WriteValue(order.UserId);
                    writer.WritePropertyName("5");
                    writer.WriteValue(side.ToInt32());
                    writer.WritePropertyName("6");
                    writer.WriteValue(order.OriginalAmount);
                    writer.WritePropertyName("7");
                    writer.WriteValue(order.ActualAmount);
                    writer.WritePropertyName("8");
                    writer.WriteValue(order.Rate);

                    TSOrder ts_order = order as TSOrder;
                    if (ts_order == null)
                    {
                        writer.WritePropertyName("9");
                        writer.WriteValue(order.DtMade.Ticks);
                    }
                    else
                    {
                        writer.WritePropertyName("9");
                        writer.WriteValue(ts_order.Offset);
                        writer.WritePropertyName("10");
                        writer.WriteValue(ts_order.DtMade.Ticks);
                    }
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

        #region DAEMON PUSH MESSAGES

        internal static string FormTechJson(int message_type, int user_id, string currency, decimal available_funds, decimal blocked_funds, DateTime dt_made)
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
                writer.WriteValue(currency);
                writer.WritePropertyName("3");
                writer.WriteValue(available_funds);
                writer.WritePropertyName("4");
                writer.WriteValue(blocked_funds);
                writer.WritePropertyName("5");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, int user_id, decimal equity, decimal margin, decimal free_margin, decimal margin_level, DateTime dt_made)
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
                writer.WriteValue(margin);
                writer.WritePropertyName("4");
                writer.WriteValue(free_margin);
                writer.WritePropertyName("5");
                writer.WriteValue(margin_level);
                writer.WritePropertyName("6");
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

        internal static string FormTechJson(int message_type, string derived_currency, decimal bid, decimal ask, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(derived_currency);
                writer.WritePropertyName("2");
                writer.WriteValue(bid);
                writer.WritePropertyName("3");
                writer.WriteValue(ask);
                writer.WritePropertyName("4");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, string derived_currency, bool side, List<OrderBuf> act_top, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(derived_currency);
                writer.WritePropertyName("2");
                writer.WriteValue(side.ToInt32());
                writer.WritePropertyName("3");
                writer.WriteStartArray();
                for (int i = 0; i < act_top.Count; i++)
                {
                    writer.WriteStartArray();
                    writer.WriteValue(act_top[i].ActualAmount);
                    writer.WriteValue(act_top[i].Rate);
                    writer.WriteEndArray();
                }
                writer.WriteEndArray();
                writer.WritePropertyName("4");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, int order_event, int fc_source, long func_call_id, string derived_currency, bool side, Order order)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(order_event);
                writer.WritePropertyName("2");
                writer.WriteValue(fc_source);
                writer.WritePropertyName("3");
                writer.WriteValue(func_call_id);
                writer.WritePropertyName("4");
                writer.WriteValue(derived_currency);
                writer.WritePropertyName("5");
                writer.WriteValue(order.OrderId);
                writer.WritePropertyName("6");
                writer.WriteValue(order.UserId);
                writer.WritePropertyName("7");
                writer.WriteValue(side.ToInt32());
                writer.WritePropertyName("8");
                writer.WriteValue(order.OriginalAmount);
                writer.WritePropertyName("9");
                writer.WriteValue(order.ActualAmount);
                writer.WritePropertyName("10");
                writer.WriteValue(order.Rate);
                writer.WritePropertyName("11");

                TSOrder ts_order = order as TSOrder;
                if (ts_order == null) writer.WriteValue(0m);
                else writer.WriteValue(ts_order.Offset);

                writer.WritePropertyName("12");
                writer.WriteValue(order.DtMade.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, string derived_currency, long order_id, int user_id, decimal actual_amount, int order_status, DateTime dt_made)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(derived_currency);
                writer.WritePropertyName("2");
                writer.WriteValue(order_id);
                writer.WritePropertyName("3");
                writer.WriteValue(user_id);
                writer.WritePropertyName("4");
                writer.WriteValue(actual_amount);
                writer.WritePropertyName("5");
                writer.WriteValue(order_status);
                writer.WritePropertyName("6");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, string derived_currency, Trade trade)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(message_type);
                writer.WritePropertyName("1");
                writer.WriteValue(derived_currency);
                writer.WritePropertyName("2");
                writer.WriteValue(trade.TradeId);
                writer.WritePropertyName("3");
                writer.WriteValue(trade.BuyOrderId);
                writer.WritePropertyName("4");
                writer.WriteValue(trade.SellOrderId);
                writer.WritePropertyName("5");
                writer.WriteValue(trade.BuyerUserId);
                writer.WritePropertyName("6");
                writer.WriteValue(trade.SellerUserId);
                writer.WritePropertyName("7");
                writer.WriteValue(trade.Side.ToInt32());
                writer.WritePropertyName("8");
                writer.WriteValue(trade.Amount);
                writer.WritePropertyName("9");
                writer.WriteValue(trade.Rate);
                writer.WritePropertyName("10");
                writer.WriteValue(trade.BuyerFee);
                writer.WritePropertyName("11");
                writer.WriteValue(trade.SellerFee);
                writer.WritePropertyName("12");
                writer.WriteValue(trade.DtMade.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        internal static string FormTechJson(int message_type, long func_call_id, int user_id, string derived_currency, decimal fee_in_perc, DateTime dt_made)
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
                writer.WriteValue(derived_currency);
                writer.WritePropertyName("4");
                writer.WriteValue(fee_in_perc);
                writer.WritePropertyName("5");
                writer.WriteValue(dt_made.Ticks);
                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        #endregion

        #region RECOVERY PUSH REPLICAS
        
        internal static string FormTechJson(int func_id, string[] str_args)
        {
            StringBuilder sb = new StringBuilder();
            StringWriter sw = new StringWriter(sb);

            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.WriteStartObject();
                writer.WritePropertyName("0");
                writer.WriteValue(func_id);

                if (str_args != null)
                {
                    for (int i = 0; i < str_args.Length; i++)
                    {
                        writer.WritePropertyName((i + 1).ToString());
                        writer.WriteValue(str_args[i]);
                    }
                }

                writer.WriteEndObject();
            }

            return sb.ToString();
        }

        #endregion

    }
}
