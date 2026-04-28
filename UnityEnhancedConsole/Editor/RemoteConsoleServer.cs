using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEditor;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// Editor-side TCP server that receives remote log and watch messages from builds.
    /// Integrates with EnhancedConsoleWindow and WatchManager.
    /// </summary>
    [InitializeOnLoad]
    public static class RemoteConsoleServer
    {
        // ── State ──────────────────────────────────────────────
        private static TcpListener _listener;
        private static readonly List<ClientConnection> _clients = new List<ClientConnection>();
        private static readonly object _clientsLock = new object();
        private static bool _isRunning;
        private static int _port = RemoteConsoleProtocol.DefaultPort;
        private static Thread _acceptThread;

        // ── Pending messages (thread-safe) ─────────────────────
        private static readonly List<RemoteMessage> _pendingMessages = new List<RemoteMessage>();
        private static readonly object _pendingLock = new object();
        private const int MaxPendingMessages = 50000;

        // ── Events ─────────────────────────────────────────────
        public static event Action<string> OnClientConnected;
        public static event Action<string> OnClientDisconnected;
        public static event Action<int> OnClientCountChanged;

        // ── Properties ─────────────────────────────────────────
        public static bool IsRunning => _isRunning;
        public static int Port => _port;
        public static int ClientCount
        {
            get { lock (_clientsLock) return _clients.Count; }
        }

        // ── Prefs ──────────────────────────────────────────────
        private const string PrefAutoStart = "EnhancedConsole_RemoteAutoStart";
        private const string PrefPort = "EnhancedConsole_RemotePort";

        static RemoteConsoleServer()
        {
            _port = EditorPrefs.GetInt(PrefPort, RemoteConsoleProtocol.DefaultPort);
            EditorApplication.update += ProcessPendingMessages;
            EditorApplication.quitting += Stop;

            if (EditorPrefs.GetBool(PrefAutoStart, false))
            {
                Start();
            }
        }

        // ── Public API ─────────────────────────────────────────

        public static void Start(int port = -1)
        {
            if (_isRunning) return;

            if (port > 0)
            {
                _port = port;
                EditorPrefs.SetInt(PrefPort, _port);
            }

            try
            {
                _listener = new TcpListener(IPAddress.Any, _port);
                _listener.Start();
                _isRunning = true;

                _acceptThread = new Thread(AcceptLoop)
                {
                    IsBackground = true,
                    Name = "EnhancedConsole-RemoteAccept"
                };
                _acceptThread.Start();

                EditorPrefs.SetBool(PrefAutoStart, true);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[EnhancedConsole Remote] Failed to start server on port {_port}: {e.Message}");
                _isRunning = false;
            }
        }

        public static void Stop()
        {
            if (!_isRunning) return;
            _isRunning = false;

            try { _listener?.Stop(); } catch { }

            lock (_clientsLock)
            {
                foreach (var c in _clients)
                    c.Close();
                _clients.Clear();
            }

            EditorPrefs.SetBool(PrefAutoStart, false);
            OnClientCountChanged?.Invoke(0);
        }

        public static void SetAutoStart(bool auto)
        {
            EditorPrefs.SetBool(PrefAutoStart, auto);
        }

        public static bool GetAutoStart()
        {
            return EditorPrefs.GetBool(PrefAutoStart, false);
        }

        /// <summary>Send a command to all connected clients (e.g., watchClear).</summary>
        public static void SendToAll(string message)
        {
            byte[] data = RemoteConsoleProtocol.EncodeMessage(message);
            lock (_clientsLock)
            {
                foreach (var c in _clients)
                    c.SendAsync(data);
            }
        }

        // ── Accept Loop ────────────────────────────────────────

        private static void AcceptLoop()
        {
            while (_isRunning)
            {
                try
                {
                    var tcp = _listener.AcceptTcpClient();
                    tcp.NoDelay = true;
                    tcp.ReceiveTimeout = 0;
                    tcp.SendTimeout = 5000;

                    var client = new ClientConnection(tcp);
                    lock (_clientsLock) _clients.Add(client);

                    var readThread = new Thread(() => ReadLoop(client))
                    {
                        IsBackground = true,
                        Name = $"EnhancedConsole-Remote-{client.Id}"
                    };
                    readThread.Start();

                    // Notify on main thread
                    lock (_pendingLock)
                    {
                        _pendingMessages.Add(new RemoteMessage
                        {
                            Type = RemoteMessageType.ClientConnected,
                            ClientId = client.Id
                        });
                    }
                }
                catch (SocketException) when (!_isRunning) { break; }
                catch (ObjectDisposedException) { break; }
                catch (Exception e)
                {
                    if (_isRunning)
                        Debug.LogWarning($"[EnhancedConsole Remote] Accept error: {e.Message}");
                    Thread.Sleep(100);
                }
            }
        }

        // ── Read Loop (per client) ─────────────────────────────

        private static void ReadLoop(ClientConnection client)
        {
            try
            {
                var stream = client.Tcp.GetStream();
                var headerBuffer = new byte[RemoteConsoleProtocol.HeaderSize];
                while (_isRunning && client.IsConnected)
                {
                    string json = RemoteConsoleProtocol.ReadMessage(stream, headerBuffer);
                    if (json == null) break;

                    string msgType = RemoteConsoleProtocol.GetStringField(json, "t");
                    if (msgType == null) continue;

                    lock (_pendingLock)
                    {
                        if (_pendingMessages.Count < MaxPendingMessages)
                        {
                            _pendingMessages.Add(new RemoteMessage
                            {
                                Type = ParseMessageType(msgType),
                                Json = json,
                                ClientId = client.Id
                            });
                        }
                    }

                    // Respond to ping immediately
                    if (msgType == "ping")
                    {
                        client.SendAsync(RemoteConsoleProtocol.EncodeMessage(
                            RemoteConsoleProtocol.BuildSimpleMessage("pong")));
                    }
                }
            }
            catch (IOException) { }
            catch (ObjectDisposedException) { }
            catch (Exception e)
            {
                if (_isRunning)
                    Debug.LogWarning($"[EnhancedConsole Remote] Read error from {client.Id}: {e.Message}");
            }
            finally
            {
                client.Close();
                lock (_clientsLock) _clients.Remove(client);
                lock (_pendingLock)
                {
                    _pendingMessages.Add(new RemoteMessage
                    {
                        Type = RemoteMessageType.ClientDisconnected,
                        ClientId = client.Id
                    });
                }
            }
        }

        // ── Main Thread Processing ─────────────────────────────

        private static readonly List<RemoteMessage> _processBuffer = new List<RemoteMessage>();

        private static void ProcessPendingMessages()
        {
            lock (_pendingLock)
            {
                if (_pendingMessages.Count == 0) return;
                _processBuffer.AddRange(_pendingMessages);
                _pendingMessages.Clear();
            }

            foreach (var msg in _processBuffer)
            {
                try
                {
                    switch (msg.Type)
                    {
                        case RemoteMessageType.Log:
                            ProcessLogMessage(msg.Json);
                            break;
                        case RemoteMessageType.Watch:
                            ProcessWatchMessage(msg.Json);
                            break;
                        case RemoteMessageType.WatchRemove:
                            ProcessWatchRemoveMessage(msg.Json);
                            break;
                        case RemoteMessageType.Handshake:
                            ProcessHandshake(msg.Json, msg.ClientId);
                            break;
                        case RemoteMessageType.ClientConnected:
                            OnClientConnected?.Invoke(msg.ClientId);
                            OnClientCountChanged?.Invoke(ClientCount);
                            break;
                        case RemoteMessageType.ClientDisconnected:
                            OnClientDisconnected?.Invoke(msg.ClientId);
                            OnClientCountChanged?.Invoke(ClientCount);
                            break;
                    }
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[EnhancedConsole Remote] Process error: {e.Message}");
                }
            }

            _processBuffer.Clear();
        }

        private static void ProcessLogMessage(string json)
        {
            string condition = RemoteConsoleProtocol.GetStringField(json, "c") ?? "";
            string stackTrace = RemoteConsoleProtocol.GetStringField(json, "s") ?? "";
            int logTypeInt = RemoteConsoleProtocol.GetIntField(json, "lt");
            int frameCount = RemoteConsoleProtocol.GetIntField(json, "fc");

            LogType logType = LogType.Log;
            if (logTypeInt >= 0 && logTypeInt <= 4)
                logType = (LogType)logTypeInt;

            // Prefix with [Remote] tag for identification
            string remoteCondition = $"[Remote] {condition}";

            // Inject into Unity's log system so EnhancedConsoleWindow captures it
            switch (logType)
            {
                case LogType.Error:
                case LogType.Exception:
                    Debug.LogError(remoteCondition);
                    break;
                case LogType.Warning:
                    Debug.LogWarning(remoteCondition);
                    break;
                default:
                    Debug.Log(remoteCondition);
                    break;
            }
        }

        private static void ProcessWatchMessage(string json)
        {
            string name = RemoteConsoleProtocol.GetStringField(json, "n");
            if (string.IsNullOrEmpty(name)) return;

            string formattedValue = RemoteConsoleProtocol.GetStringField(json, "fv") ?? "";
            string rawValue = RemoteConsoleProtocol.GetStringField(json, "v") ?? formattedValue;
            int valueType = RemoteConsoleProtocol.GetIntField(json, "vt");
            string stackTrace = RemoteConsoleProtocol.GetStringField(json, "st");

            // Prefix with [Remote] for identification
            string remoteName = $"[Remote] {name}";

            // Determine value to set
            object value = rawValue;
            bool captureStack = !string.IsNullOrEmpty(stackTrace);

            // Try to parse numeric values for proper type handling
            switch ((WatchValueType)valueType)
            {
                case WatchValueType.Float:
                    if (double.TryParse(rawValue, System.Globalization.NumberStyles.Float,
                        System.Globalization.CultureInfo.InvariantCulture, out double dv))
                        value = (float)dv;
                    break;
                case WatchValueType.Integer:
                    if (int.TryParse(rawValue, out int iv))
                        value = iv;
                    break;
                case WatchValueType.Boolean:
                    if (bool.TryParse(rawValue, out bool bv))
                        value = bv;
                    break;
            }

            WatchManager.SetValue(remoteName, value, (WatchValueType)valueType, null, captureStack);
        }

        private static void ProcessWatchRemoveMessage(string json)
        {
            string name = RemoteConsoleProtocol.GetStringField(json, "n");
            if (!string.IsNullOrEmpty(name))
                WatchManager.RemoveEntry($"[Remote] {name}");
        }

        private static void ProcessHandshake(string json, string clientId)
        {
            string app = RemoteConsoleProtocol.GetStringField(json, "app") ?? "Unknown";
            string platform = RemoteConsoleProtocol.GetStringField(json, "plat") ?? "Unknown";
            // Could display connection info in UI
        }

        // ── Helpers ────────────────────────────────────────────

        private static RemoteMessageType ParseMessageType(string t)
        {
            switch (t)
            {
                case "log": return RemoteMessageType.Log;
                case "watch": return RemoteMessageType.Watch;
                case "watchRemove": return RemoteMessageType.WatchRemove;
                case "handshake": return RemoteMessageType.Handshake;
                case "ping": return RemoteMessageType.Ping;
                default: return RemoteMessageType.Unknown;
            }
        }

        // ── Inner Types ────────────────────────────────────────

        private enum RemoteMessageType
        {
            Unknown, Log, Watch, WatchRemove, Handshake, Ping,
            ClientConnected, ClientDisconnected
        }

        private struct RemoteMessage
        {
            public RemoteMessageType Type;
            public string Json;
            public string ClientId;
        }

        private class ClientConnection
        {
            public readonly TcpClient Tcp;
            public readonly string Id;
            public bool IsConnected { get; private set; }
            private readonly object _sendLock = new object();

            public ClientConnection(TcpClient tcp)
            {
                Tcp = tcp;
                Id = ((IPEndPoint)tcp.Client.RemoteEndPoint).ToString();
                IsConnected = true;
            }

            public void SendAsync(byte[] data)
            {
                if (!IsConnected) return;
                try
                {
                    lock (_sendLock)
                    {
                        Tcp.GetStream().Write(data, 0, data.Length);
                    }
                }
                catch
                {
                    Close();
                }
            }

            public void Close()
            {
                IsConnected = false;
                try { Tcp?.Close(); } catch { }
            }
        }
    }
}
