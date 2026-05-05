using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

namespace ChaosDraftParty.Network
{
    /// <summary>
    /// Unity Relay 経由で NGO の Host/Client を立ち上げる static helper。
    /// ConnectionManager から呼ばれる thin layer (UI と疎結合)。
    ///
    /// 前提:
    /// - Bootstrap.cs で UnityServices.InitializeAsync + 匿名 Auth 済 (再呼出しは idempotent)
    /// - シーンに NetworkManager + UnityTransport が DontDestroyOnLoad で配置済
    ///
    /// CLAUDE.md 同期表:
    /// - プレイヤー位置 = ClientNetworkTransform Owner→All (NGO 2.x では NetworkTransform AuthorityMode=Owner)
    /// - HP / Score / RoundState = NetworkVariable<T> S→C
    /// - スキル発動 = ServerRpc → ClientRpc (CD 判定はホスト)
    /// </summary>
    public static class RelayConnection
    {
        /// <summary>DTLS=PC 専用、WSS=WebGL 用。本プロジェクトは PC のみなので dtls 固定。</summary>
        const string ConnectionType = "dtls";

        /// <summary>Host を起動して JoinCode を返す。失敗時 null。</summary>
        /// <param name="maxPeers">host 自身を含まない接続上限。4 人 party なら 3。</param>
        /// <param name="region">リージョン文字列 (null=自動、"asia-northeast1" 等)。</param>
        public static async Task<string> StartHost(int maxPeers = 3, string region = null)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[RelayConnection] NetworkManager.Singleton が無い");
                return null;
            }

            try
            {
                await EnsureInitializedAndSignedIn();

                Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxPeers, region);
                string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(new RelayServerData(allocation, ConnectionType));

                if (!NetworkManager.Singleton.StartHost())
                {
                    Debug.LogError("[RelayConnection] StartHost failed");
                    return null;
                }

                Debug.Log($"[RelayConnection] Host started, JoinCode={joinCode}");
                return joinCode;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RelayConnection] StartHost exception: {e}");
                return null;
            }
        }

        /// <summary>JoinCode で Client 接続。成功時 true。</summary>
        public static async Task<bool> StartClient(string joinCode)
        {
            if (string.IsNullOrWhiteSpace(joinCode))
            {
                Debug.LogError("[RelayConnection] JoinCode が空");
                return false;
            }
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[RelayConnection] NetworkManager.Singleton が無い");
                return false;
            }

            try
            {
                await EnsureInitializedAndSignedIn();

                JoinAllocation join = await RelayService.Instance.JoinAllocationAsync(joinCode.Trim());

                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetRelayServerData(new RelayServerData(join, ConnectionType));

                if (!NetworkManager.Singleton.StartClient())
                {
                    Debug.LogError("[RelayConnection] StartClient failed");
                    return false;
                }

                Debug.Log($"[RelayConnection] Client connecting via JoinCode={joinCode}");
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RelayConnection] StartClient exception: {e}");
                return false;
            }
        }

        /// <summary>UGS init + 匿名 Auth を idempotent に保証する。Bootstrap が走ってれば即 return。</summary>
        static async Task EnsureInitializedAndSignedIn()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
                await UnityServices.InitializeAsync();
            if (!AuthenticationService.Instance.IsSignedIn)
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
        }

        /// <summary>
        /// Direct UTP Host (Relay 経由しない、localhost / LAN 用)。
        /// MPPM 2 ウィンドウや LAN テスト向け。UGS init 不要。
        /// **Port が in-use なら自動で次の port (port+1, +2, ...) を試す** (Editor 再生で残った socket 対策)。
        /// </summary>
        /// <returns>成功時 bind した port、失敗時 0。</returns>
        public static ushort StartHostDirect(ushort startPort = DefaultPort, string listenAddress = "0.0.0.0", int maxRetries = 5)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[RelayConnection] NetworkManager.Singleton が無い");
                return 0;
            }
            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            for (int i = 0; i < maxRetries; i++)
            {
                ushort port = (ushort)(startPort + i);
                try
                {
                    transport.SetConnectionData("127.0.0.1", port, listenAddress);
                    if (NetworkManager.Singleton.StartHost())
                    {
                        Debug.Log($"[RelayConnection] StartHostDirect bound port={port}");
                        return port;
                    }
                    // StartHost が false 返した時、internal で transport failure → Shutdown 済の可能性
                    // 次の port で再試行
                    Debug.LogWarning($"[RelayConnection] port {port} bind 失敗、次の port を試す");
                    NetworkManager.Singleton.Shutdown();
                }
                catch (System.Exception e)
                {
                    Debug.LogWarning($"[RelayConnection] port {port} exception: {e.Message}");
                }
            }
            Debug.LogError($"[RelayConnection] StartHostDirect all {maxRetries} ports failed (start={startPort})");
            return 0;
        }

        /// <summary>Direct UTP Client。Host の IP / port を指定。</summary>
        public static bool StartClientDirect(string serverIp = "127.0.0.1", ushort port = DefaultPort)
        {
            if (NetworkManager.Singleton == null)
            {
                Debug.LogError("[RelayConnection] NetworkManager.Singleton が無い");
                return false;
            }
            try
            {
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                transport.SetConnectionData(serverIp, port);
                bool ok = NetworkManager.Singleton.StartClient();
                Debug.Log($"[RelayConnection] StartClientDirect ip={serverIp} port={port} ok={ok}");
                return ok;
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[RelayConnection] StartClientDirect exception: {e}");
                return false;
            }
        }

        /// <summary>Default UTP port (NGO Hello World / Mirror 慣例値)。</summary>
        public const ushort DefaultPort = 7777;

        /// <summary>Host / Client いずれの接続も終了。</summary>
        public static void Shutdown()
        {
            if (NetworkManager.Singleton == null) return;
            if (NetworkManager.Singleton.IsHost || NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
            {
                NetworkManager.Singleton.Shutdown();
                Debug.Log("[RelayConnection] Shutdown complete");
            }
        }
    }
}
