using System;
using System.IO;
using System.Text;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 远程控制台通信协议。
    /// 消息格式: [4字节长度(big-endian)] + [UTF-8 JSON]
    /// </summary>
    public static class RemoteConsoleProtocol
    {
        public const int DefaultPort = 34999;
        public const int HeaderSize = 4;
        public const int MaxMessageSize = 1024 * 1024; // 1MB

        // Message types
        public const string TypeHandshake = "handshake";
        public const string TypeLog = "log";
        public const string TypeWatch = "watch";
        public const string TypeWatchRemove = "watchRemove";
        public const string TypeWatchClear = "watchClear";
        public const string TypePing = "ping";
        public const string TypePong = "pong";

        /// <summary>
        /// 将消息编码为带长度前缀的二进制数据。
        /// </summary>
        public static byte[] EncodeMessage(string json)
        {
            byte[] body = Encoding.UTF8.GetBytes(json);
            byte[] msg = new byte[HeaderSize + body.Length];
            msg[0] = (byte)((body.Length >> 24) & 0xFF);
            msg[1] = (byte)((body.Length >> 16) & 0xFF);
            msg[2] = (byte)((body.Length >> 8) & 0xFF);
            msg[3] = (byte)(body.Length & 0xFF);
            Buffer.BlockCopy(body, 0, msg, HeaderSize, body.Length);
            return msg;
        }

        /// <summary>
        /// 从流中读取一条完整消息。返回 null 表示连接关闭。
        /// </summary>
        public static string ReadMessage(Stream stream, byte[] headerBuffer)
        {
            if (!ReadExact(stream, headerBuffer, 0, HeaderSize))
                return null;

            int length = (headerBuffer[0] << 24) | (headerBuffer[1] << 16) |
                         (headerBuffer[2] << 8) | headerBuffer[3];

            if (length <= 0 || length > MaxMessageSize)
                return null;

            byte[] body = new byte[length];
            if (!ReadExact(stream, body, 0, length))
                return null;

            return Encoding.UTF8.GetString(body);
        }

        private static bool ReadExact(Stream stream, byte[] buffer, int offset, int count)
        {
            int read = 0;
            while (read < count)
            {
                int n = stream.Read(buffer, offset + read, count - read);
                if (n <= 0) return false;
                read += n;
            }
            return true;
        }

        // ---- Simple JSON builder (no external dependency) ----

        public static string BuildLogMessage(string condition, string stackTrace, int logType,
            double timestamp, int frameCount)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"t\":\"log\",\"c\":");
            AppendJsonString(sb, condition);
            sb.Append(",\"s\":");
            AppendJsonString(sb, stackTrace ?? "");
            sb.Append(",\"lt\":").Append(logType);
            sb.Append(",\"ts\":").Append(timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(",\"fc\":").Append(frameCount);
            sb.Append('}');
            return sb.ToString();
        }

        public static string BuildWatchMessage(string name, string value, string formattedValue,
            int valueType, double timestamp, int frameCount, string stackTrace = null)
        {
            var sb = new StringBuilder(256);
            sb.Append("{\"t\":\"watch\",\"n\":");
            AppendJsonString(sb, name);
            sb.Append(",\"v\":");
            AppendJsonString(sb, value ?? "");
            sb.Append(",\"fv\":");
            AppendJsonString(sb, formattedValue ?? "");
            sb.Append(",\"vt\":").Append(valueType);
            sb.Append(",\"ts\":").Append(timestamp.ToString(System.Globalization.CultureInfo.InvariantCulture));
            sb.Append(",\"fc\":").Append(frameCount);
            if (stackTrace != null)
            {
                sb.Append(",\"st\":");
                AppendJsonString(sb, stackTrace);
            }
            sb.Append('}');
            return sb.ToString();
        }

        public static string BuildWatchRemoveMessage(string name)
        {
            var sb = new StringBuilder(64);
            sb.Append("{\"t\":\"watchRemove\",\"n\":");
            AppendJsonString(sb, name);
            sb.Append('}');
            return sb.ToString();
        }

        public static string BuildHandshakeMessage(string appName, string platform, string version)
        {
            var sb = new StringBuilder(128);
            sb.Append("{\"t\":\"handshake\",\"app\":");
            AppendJsonString(sb, appName ?? "");
            sb.Append(",\"plat\":");
            AppendJsonString(sb, platform ?? "");
            sb.Append(",\"ver\":");
            AppendJsonString(sb, version ?? "");
            sb.Append('}');
            return sb.ToString();
        }

        public static string BuildSimpleMessage(string type)
        {
            return "{\"t\":\"" + type + "\"}";
        }

        // ---- Simple JSON parser (field extraction) ----

        public static string GetStringField(string json, string field)
        {
            string key = "\"" + field + "\":";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return null;
            idx += key.Length;

            // skip whitespace
            while (idx < json.Length && json[idx] == ' ') idx++;

            if (idx >= json.Length) return null;

            if (json[idx] == '"')
            {
                idx++; // skip opening quote
                var sb = new StringBuilder();
                while (idx < json.Length)
                {
                    char c = json[idx++];
                    if (c == '\\' && idx < json.Length)
                    {
                        char next = json[idx++];
                        switch (next)
                        {
                            case '"': sb.Append('"'); break;
                            case '\\': sb.Append('\\'); break;
                            case 'n': sb.Append('\n'); break;
                            case 'r': sb.Append('\r'); break;
                            case 't': sb.Append('\t'); break;
                            default: sb.Append(next); break;
                        }
                    }
                    else if (c == '"')
                    {
                        return sb.ToString();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                return sb.ToString();
            }
            return null;
        }

        public static int GetIntField(string json, string field, int defaultValue = 0)
        {
            string key = "\"" + field + "\":";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return defaultValue;
            idx += key.Length;
            while (idx < json.Length && json[idx] == ' ') idx++;

            int start = idx;
            while (idx < json.Length && (char.IsDigit(json[idx]) || json[idx] == '-'))
                idx++;
            if (idx == start) return defaultValue;
            if (int.TryParse(json.Substring(start, idx - start), out int val))
                return val;
            return defaultValue;
        }

        public static double GetDoubleField(string json, string field, double defaultValue = 0)
        {
            string key = "\"" + field + "\":";
            int idx = json.IndexOf(key, StringComparison.Ordinal);
            if (idx < 0) return defaultValue;
            idx += key.Length;
            while (idx < json.Length && json[idx] == ' ') idx++;

            int start = idx;
            while (idx < json.Length && (char.IsDigit(json[idx]) || json[idx] == '.' || json[idx] == '-' || json[idx] == 'E' || json[idx] == 'e' || json[idx] == '+'))
                idx++;
            if (idx == start) return defaultValue;
            if (double.TryParse(json.Substring(start, idx - start),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out double val))
                return val;
            return defaultValue;
        }

        private static void AppendJsonString(StringBuilder sb, string s)
        {
            sb.Append('"');
            if (s != null)
            {
                foreach (char c in s)
                {
                    switch (c)
                    {
                        case '"': sb.Append("\\\""); break;
                        case '\\': sb.Append("\\\\"); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default:
                            if (c < 0x20)
                                sb.Append("\\u").Append(((int)c).ToString("X4"));
                            else
                                sb.Append(c);
                            break;
                    }
                }
            }
            sb.Append('"');
        }
    }
}
