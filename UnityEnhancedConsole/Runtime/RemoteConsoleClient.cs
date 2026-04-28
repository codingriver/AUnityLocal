using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;

namespace UnityEnhancedConsole
{
    /// <summary>
    /// 远程控制台客户端。运行在构建的Player中，将日志和Watch数据发送到Editor。
    /// 用法:
    ///   RemoteConsoleClient.Connect("192.168.1.100");
    ///   // 日志自动转发
    ///   // Watch 数据通过 RemoteConsoleClient.SetWatch() 发送
    /// </summary>
    public class RemoteConsoleClient : MonoBehaviour
    {
        // ---- 配置 ----
        public string ServerAddress = "127.0.0.1";
        public int ServerPort = RemoteConsoleProtocol.DefaultPort;
        public bool AutoReconnect = true;
        public float ReconnectInterval = 3f;
        public bool ForwardLogs = true;
        public bool ForwardWatch = true;

        // ---- 单例 ----
        private static RemoteConsoleClient _instance;
        public static RemoteConsoleClient Instance => _instance;

        // ---- 状态 ----
        public bool IsConnected => _client != null && _client.Connected;
        public string RemoteEndpoint => _remoteEndpoint;

        public static event Action OnConnected;
        public static event Action OnDisconnected;

        // ---- 内部 ----
        private TcpClient _client;
        private NetworkStream _stream;
        private Thread _receiveThread;
        private readonly object _sendLock = new object();
        private readonly Queue<byte[]> _sendQueue = new Queue<byte[]>();
        private volatile bool _isRunning;
        private float _reconnectTimer;
        private string _remoteEndpoint = "";
        private volatile bool _connectionLost;
        private readonly byte[] _headerBuffer = new byte[RemoteConsoleProtocol.HeaderSize];

        // ---- 公开 API ----

        /// <summary>
        /// 创建单例并连接到指定 Editor。
        /// </summary>
        public static RemoteConsoleClient Connect(string address, int port = 0)
        {
            if (port <= 0) port = RemoteConsoleProtocol.DefaultPort;

            if (_instance == null)
            {
                var go = new GameObject("[RemoteConsole]");
                DontDestroyOnLoad(go);
                _instance = go.AddComponent<RemoteConsoleClient>();
            }

            _instance.ServerAddress = address;
            _instance.ServerPort = port;
            _instance.StartConnection();
            return _instance;
        }

        /// <summary>
        /// 断开连接。
        /// </summary>
        public static void Disconnect()
        {
            if (_instance != null)
            {
                _instance.CloseConnection();
            }
        }

        /// <summary>
        /// 发送 Watch 变量数据到 Editor。
        /// </summary>
        public static void SetWatch(string name, object value, int valueType = 0,
            string format = null, string stackTrace = null)
        {
            if (_instance == null || !_instance.IsConnected || !_instance.ForwardWatch)
                return;

            string formattedValue = value?.ToString() ?? "null";
            double timestamp = Time.realtimeSinceStartupAsDouble;
            int frameCount = Time.frameCount;

            string json = RemoteConsoleProtocol.BuildWatchMessage(
                name, value?.ToString() ?? "null", formattedValue, valueType,
                timestamp, frameCount, stackTrace);

            _instance.EnqueueMessage(json);
        }

        /// <summary>
        /// 通知 Editor 移除 Watch 变量。
        /// </summary>
        public static void RemoveWatch(string name)
        {
            if (_instance == null || !_instance.IsConnected) return;
            _instance.EnqueueMessage(RemoteConsoleProtocol.BuildWatchRemoveMessage(name));
        }

        // ---- Unity 生命周期 ----

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }
            _instance = this;
        }

        private void OnDestroy()
        {
            CloseConnection();
            if (_instance == this) _instance = null;
        }

        private void Update()
        {
            // 检查连接断开
            if (_connectionLost)
            {
                _connectionLost = false;
                CloseConnection();
                OnDisconnected?.Invoke();
            }

            // 自动重连
            if (AutoReconnect && !IsConnected && _isRunning)
            {
                _reconnectTimer -= Time.unscaledDeltaTime;
                if (_reconnectTimer <= 0)
                {
                    _reconnectTimer = ReconnectInterval;
                    StartConnection();
                }
            }

            // 发送队列
            FlushSendQueue();
        }

        // ---- 连接管理 ----

        private void StartConnection()
        {
            CloseConnection();

            try
            {
                _client = new TcpClient();
                _client.NoDelay = true;
                _client.SendTimeout = 5000;
                _client.ReceiveTimeout = 0; // blocking read in thread

                var result = _client.BeginConnect(ServerAddress, ServerPort, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(TimeSpan.FromSeconds(3));

                if (!success || !_client.Connected)
                {
                    _client.Close();
                    _client = null;
                    return;
                }

                _client.EndConnect(result);
                _stream = _client.GetStream();
                _remoteEndpoint = $"{ServerAddress}:{ServerPort}";
                _isRunning = true;

                // 发送握手
                string handshake = RemoteConsoleProtocol.BuildHandshakeMessage(
                    Application.productName,
                    Application.platform.ToString(),
                    Application.version);
                SendImmediate(handshake);

                // 注册日志转发
                if (ForwardLogs)
                {
                    Application.logMessageReceivedThreaded += OnLogReceived;
                }

                // 启动接收线程
                _receiveThread = new Thread(ReceiveLoop)
                {
                    IsBackground = true,
                    Name = "RemoteConsole-Recv"
                };
                _receiveThread.Start();

                OnConnected?.Invoke();
            }
            catch (Exception)
            {
                CloseConnection();
            }
        }

        private void CloseConnection()
        {
            _isRunning = false;

            Application.logMessageReceivedThreaded -= OnLogReceived;

            try { _stream?.Close(); } catch { }
            try { _client?.Close(); } catch { }
            _stream = null;
            _client = null;
            _remoteEndpoint = "";

            lock (_sendLock) _sendQueue.Clear();

            if (_receiveThread != null && _receiveThread.IsAlive)
            {
                _receiveThread.Join(1000);
                _receiveThread = null;
            }
        }

        // ---- 日志转发 ----

        private void OnLogReceived(string condition, string stackTrace, LogType type)
        {
            if (!_isRunning || !ForwardLogs) return;

            double timestamp = 0;
            int frameCount = 0;
            try
            {
                timestamp = Time.realtimeSinceStartupAsDouble;
                frameCount = Time.frameCount;
            }
            catch { }

            string json = RemoteConsoleProtocol.BuildLogMessage(
                condition, stackTrace, (int)type, timestamp, frameCount);

            EnqueueMessage(json);
        }

        // ---- 消息发送 ----

        private void EnqueueMessage(string json)
        {
            byte[] data = RemoteConsoleProtocol.EncodeMessage(json);
            lock (_sendLock)
            {
                if (_sendQueue.Count < 10000) // 防止内存溢出
                    _sendQueue.Enqueue(data);
            }
        }

        private void SendImmediate(string json)
        {
            try
            {
                byte[] data = RemoteConsoleProtocol.EncodeMessage(json);
                _stream?.Write(data, 0, data.Length);
                _stream?.Flush();
            }
            catch { }
        }

        private void FlushSendQueue()
        {
            if (_stream == null) return;

            byte[][] toSend;
            lock (_sendLock)
            {
                if (_sendQueue.Count == 0) return;
                toSend = new byte[_sendQueue.Count][];
                int i = 0;
                while (_sendQueue.Count > 0)
                    toSend[i++] = _sendQueue.Dequeue();
            }

            try
            {
                foreach (var data in toSend)
                {
                    _stream.Write(data, 0, data.Length);
                }
                _stream.Flush();
            }
            catch
            {
                _connectionLost = true;
            }
        }

        // ---- 接收线程 ----

        private void ReceiveLoop()
        {
            try
            {
                while (_isRunning && _client != null && _client.Connected)
                {
                    string json = RemoteConsoleProtocol.ReadMessage(_stream, _headerBuffer);
                    if (json == null) break;

                    string type = RemoteConsoleProtocol.GetStringField(json, "t");
                    switch (type)
                    {
                        case RemoteConsoleProtocol.TypePing:
                            SendImmediate(RemoteConsoleProtocol.BuildSimpleMessage(RemoteConsoleProtocol.TypePong));
                            break;
                        case RemoteConsoleProtocol.TypeWatchClear:
                            // Editor 请求清空本地 watch
                            break;
                    }
                }
            }
            catch { }
            finally
            {
                if (_isRunning)
                    _connectionLost = true;
            }
        }
    }
}
