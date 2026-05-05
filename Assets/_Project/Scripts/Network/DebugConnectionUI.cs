using UnityEngine;
using ChaosDraftParty.Core.Managers;

namespace ChaosDraftParty.Network
{
    /// <summary>
    /// M1 疎通プロト用の最小 OnGUI Connect UI。
    /// Boot scene の NetworkManager GO に attach。Editor / Development Build のみ動く。
    /// MainMenu UI 完成後は撤去 (M2 で正式 UI に置換)。
    ///
    /// 操作:
    /// - Host ボタン → Relay 起動 → JoinCode 表示 → matchSceneName を NGO SceneManager で load
    /// - JoinCode 入力 → Join ボタン → Client 接続
    /// </summary>
    public class DebugConnectionUI : MonoBehaviour
    {
        [Tooltip("Host が成功した後にロードする Match シーン名 (build settings に登録要)。")]
        [SerializeField] string matchSceneName = "Sandbox_v19_player_controller_test";

        string joinCodeInput = "";
        string status = "";
        string currentJoinCode = "";

        void OnEnable()
        {
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.OnHostStarted += OnHostStarted;
                ConnectionManager.Instance.OnClientConnected += OnClientConnected;
                ConnectionManager.Instance.OnConnectionFailed += OnConnectionFailed;
            }
        }

        void OnDisable()
        {
            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.OnHostStarted -= OnHostStarted;
                ConnectionManager.Instance.OnClientConnected -= OnClientConnected;
                ConnectionManager.Instance.OnConnectionFailed -= OnConnectionFailed;
            }
        }

        void OnHostStarted(string joinCode)
        {
            currentJoinCode = joinCode;
            status = $"Host 起動完了 (code={joinCode})";
            // Bootstrap が起動時に Match scene まで load する設計に統一したので、
            // ここでは scene 切替しない (start host 後の auto scene change はタイミング不安定)
        }

        void OnClientConnected()
        {
            status = "Client 接続成功 (host scene に同期される)";
        }

        void OnConnectionFailed(string reason)
        {
            status = $"接続失敗: {reason}";
        }

        void OnGUI()
        {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
            const int W = 380, H = 240;
            GUILayout.BeginArea(new Rect(20, 20, W, H), GUI.skin.box);
            GUILayout.Label("<b>ChaosDraftParty Debug Connect UI (M1)</b>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14 });
            GUILayout.Space(6);

            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null)
            {
                GUILayout.Label("NetworkManager.Singleton が無い");
                GUILayout.EndArea();
                return;
            }

            if (!nm.IsHost && !nm.IsClient && !nm.IsServer)
            {
                GUILayout.Label("<b>Local (UTP localhost)</b> ← M1 疎通推奨", new GUIStyle(GUI.skin.label) { richText = true });
                if (GUILayout.Button("Host (Local)"))
                {
                    status = "Local Host 起動中...";
                    ConnectionManager.Instance?.StartHostLocal();
                }
                if (GUILayout.Button("Join (Local 127.0.0.1)"))
                {
                    status = "Local Client 接続中...";
                    ConnectionManager.Instance?.StartClientLocal();
                }

                GUILayout.Space(8);
                GUILayout.Label("<b>Relay (Cloud 必須)</b>", new GUIStyle(GUI.skin.label) { richText = true });
                if (GUILayout.Button("Host (Relay)"))
                {
                    status = "Host 起動中...";
                    ConnectionManager.Instance?.StartHost();
                }
                GUILayout.Label("JoinCode:");
                joinCodeInput = GUILayout.TextField(joinCodeInput, GUILayout.Width(180));
                if (GUILayout.Button("Join (Relay Client)"))
                {
                    if (string.IsNullOrWhiteSpace(joinCodeInput))
                        status = "JoinCode を入れて";
                    else
                    {
                        status = $"接続中... ({joinCodeInput})";
                        ConnectionManager.Instance?.StartClient(joinCodeInput);
                    }
                }
            }
            else
            {
                GUILayout.Label($"Mode: {(nm.IsHost ? "Host" : nm.IsServer ? "Server" : "Client")}");
                if (!string.IsNullOrEmpty(currentJoinCode))
                    GUILayout.Label($"JoinCode: {currentJoinCode}");
                if (GUILayout.Button("Disconnect"))
                {
                    ConnectionManager.Instance?.Disconnect();
                    currentJoinCode = "";
                    status = "Disconnected";
                }
            }

            GUILayout.Space(10);
            GUILayout.Label($"Status: {status}");
            GUILayout.EndArea();
#endif
        }
    }
}
