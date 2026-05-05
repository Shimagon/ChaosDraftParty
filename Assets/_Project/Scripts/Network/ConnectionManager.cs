using System;
using Unity.Netcode;
using UnityEngine;

namespace ChaosDraftParty.Network
{
    /// <summary>
    /// アプリ全体で 1 つだけ存在する接続管理。Boot シーンに配置 → DontDestroyOnLoad。
    /// UI (MainMenuController など) は本クラス経由で Host/Join を呼ぶ。
    ///
    /// イベント:
    /// - OnHostStarted(joinCode): host 起動成功 → UI に JoinCode 表示
    /// - OnClientConnected(): self が host or client として接続完了
    /// - OnClientDisconnected(): self が disconnect された
    /// - OnConnectionFailed(reason): 接続試行失敗
    /// </summary>
    public class ConnectionManager : MonoBehaviour
    {
        [Tooltip("host を起動した時にコンソールへ JoinCode を強調表示する")]
        [SerializeField] bool logJoinCodeProminently = true;

        public static ConnectionManager Instance { get; private set; }

        public string CurrentJoinCode { get; private set; }
        public bool IsBusy { get; private set; }

        public event Action<string> OnHostStarted;
        public event Action OnClientConnected;
        public event Action OnClientDisconnected;
        public event Action<string> OnConnectionFailed;

        void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        void Start()
        {
            HookNetworkManagerCallbacks();
        }

        void OnDestroy()
        {
            UnhookNetworkManagerCallbacks();
            if (Instance == this) Instance = null;
        }

        void HookNetworkManagerCallbacks()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null)
            {
                Debug.LogWarning("[ConnectionManager] NetworkManager.Singleton not found in Start. Boot scene に NetworkManager を配置して。");
                return;
            }
            nm.OnClientConnectedCallback += HandleClientConnected;
            nm.OnClientDisconnectCallback += HandleClientDisconnected;
            nm.OnServerStarted += HandleServerStarted;
        }

        void UnhookNetworkManagerCallbacks()
        {
            var nm = NetworkManager.Singleton;
            if (nm == null) return;
            nm.OnClientConnectedCallback -= HandleClientConnected;
            nm.OnClientDisconnectCallback -= HandleClientDisconnected;
            nm.OnServerStarted -= HandleServerStarted;
        }

        void HandleServerStarted()
        {
            Debug.Log("[ConnectionManager] Server started");
        }

        void HandleClientConnected(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && clientId == nm.LocalClientId)
            {
                Debug.Log($"[ConnectionManager] Self connected (clientId={clientId})");
                OnClientConnected?.Invoke();
            }
            else
            {
                Debug.Log($"[ConnectionManager] Peer connected (clientId={clientId})");
            }
        }

        void HandleClientDisconnected(ulong clientId)
        {
            var nm = NetworkManager.Singleton;
            if (nm != null && clientId == nm.LocalClientId)
            {
                Debug.Log("[ConnectionManager] Self disconnected");
                OnClientDisconnected?.Invoke();
            }
            else
            {
                Debug.Log($"[ConnectionManager] Peer disconnected (clientId={clientId})");
            }
        }

        public async void StartHost(int maxPeers = 3)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[ConnectionManager] 既に接続処理中");
                return;
            }
            IsBusy = true;
            string code = await RelayConnection.StartHost(maxPeers);
            IsBusy = false;
            if (string.IsNullOrEmpty(code))
            {
                OnConnectionFailed?.Invoke("Host 起動失敗");
                return;
            }
            CurrentJoinCode = code;
            if (logJoinCodeProminently)
                Debug.Log($"[ConnectionManager] ====== JoinCode: {code} ======");
            OnHostStarted?.Invoke(code);
        }

        public async void StartClient(string joinCode)
        {
            if (IsBusy)
            {
                Debug.LogWarning("[ConnectionManager] 既に接続処理中");
                return;
            }
            IsBusy = true;
            bool ok = await RelayConnection.StartClient(joinCode);
            IsBusy = false;
            if (!ok)
            {
                OnConnectionFailed?.Invoke("Client 接続失敗");
            }
            // 成功時の OnClientConnected は HandleClientConnected で発火
        }

        public void Disconnect()
        {
            RelayConnection.Shutdown();
            CurrentJoinCode = null;
        }

        /// <summary>localhost / LAN テスト用 Direct UTP Host。Relay/UGS 不要。port 衝突時は自動 retry。</summary>
        public void StartHostLocal(ushort startPort = RelayConnection.DefaultPort)
        {
            if (IsBusy) { Debug.LogWarning("[ConnectionManager] 既に接続処理中"); return; }
            IsBusy = true;
            ushort actualPort = RelayConnection.StartHostDirect(startPort);
            IsBusy = false;
            if (actualPort == 0) { OnConnectionFailed?.Invoke("Local Host 起動失敗 (全 port bind 不可)"); return; }
            CurrentJoinCode = $"localhost:{actualPort}";
            if (logJoinCodeProminently) Debug.Log($"[ConnectionManager] ====== Local Host: {CurrentJoinCode} ======");
            OnHostStarted?.Invoke(CurrentJoinCode);
        }

        /// <summary>localhost / LAN テスト用 Direct UTP Client。Relay/UGS 不要。</summary>
        public void StartClientLocal(string serverIp = "127.0.0.1", ushort port = RelayConnection.DefaultPort)
        {
            if (IsBusy) { Debug.LogWarning("[ConnectionManager] 既に接続処理中"); return; }
            IsBusy = true;
            bool ok = RelayConnection.StartClientDirect(serverIp, port);
            IsBusy = false;
            if (!ok) { OnConnectionFailed?.Invoke("Local Client 接続失敗"); return; }
            // 成功時の OnClientConnected は HandleClientConnected で発火
        }
    }
}
