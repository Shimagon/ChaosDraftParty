using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ChaosDraftParty.Core.Managers;
using ChaosDraftParty.Network;

namespace ChaosDraftParty.UI
{
    /// <summary>
    /// MainMenu シーンのボタンを ConnectionManager (Relay Host/Join) と GameManager の scene 遷移に配線する。
    /// Boot → MainMenu 到着時に Bootstrap が UGS Auth 済の状態を前提。
    ///
    /// UI 想定:
    /// - hostButton: 押すと Relay Host 起動 → JoinCode を joinCodeDisplay に表示 → matchSceneName へ load
    /// - joinCodeInput: TMP_InputField に code 入力
    /// - joinButton: 押すと joinCodeInput の値で Client 接続 → 接続完了で matchSceneName へ load (host 側がシーン load を担当)
    /// - quitButton: 終了
    ///
    /// M1 疎通プロトのため、Lobby scene はスキップして MainMenu → Match (Sandbox_v19 等) 直行で OK。
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;

        [Header("Join Code I/O")]
        [Tooltip("Client 側で JoinCode を入力する InputField。未 assign 可 (host のみ動かしたい時)。")]
        [SerializeField] private TMP_InputField joinCodeInput;

        [Tooltip("Host 起動時に JoinCode を表示する Text。未 assign 時は console のみ。")]
        [SerializeField] private TMP_Text joinCodeDisplay;

        [Tooltip("接続状態 / エラーを表示する Text (任意)。")]
        [SerializeField] private TMP_Text statusDisplay;

        [Header("Navigation")]
        [Tooltip("Host: 接続成功後にロードする Match シーン。Client は host のシーン同期に乗るので不要。")]
        [SerializeField] private string matchSceneName = "Sandbox_v19_player_controller_test";

        void OnEnable()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);

            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.OnHostStarted += HandleHostStarted;
                ConnectionManager.Instance.OnClientConnected += HandleClientConnected;
                ConnectionManager.Instance.OnConnectionFailed += HandleConnectionFailed;
            }

            SetStatus("");
            if (joinCodeDisplay != null) joinCodeDisplay.text = "";
        }

        void OnDisable()
        {
            if (hostButton != null) hostButton.onClick.RemoveListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.RemoveListener(OnJoinClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);

            if (ConnectionManager.Instance != null)
            {
                ConnectionManager.Instance.OnHostStarted -= HandleHostStarted;
                ConnectionManager.Instance.OnClientConnected -= HandleClientConnected;
                ConnectionManager.Instance.OnConnectionFailed -= HandleConnectionFailed;
            }
        }

        void OnHostClicked()
        {
            if (ConnectionManager.Instance == null)
            {
                SetStatus("ConnectionManager が無い (Boot に配置して)");
                return;
            }
            SetStatus("Host 起動中...");
            ConnectionManager.Instance.StartHost();
        }

        void OnJoinClicked()
        {
            if (ConnectionManager.Instance == null)
            {
                SetStatus("ConnectionManager が無い");
                return;
            }
            string code = joinCodeInput != null ? joinCodeInput.text : "";
            if (string.IsNullOrWhiteSpace(code))
            {
                SetStatus("JoinCode を入力して");
                return;
            }
            SetStatus($"接続中... ({code})");
            ConnectionManager.Instance.StartClient(code);
        }

        void OnQuitClicked()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        void HandleHostStarted(string joinCode)
        {
            if (joinCodeDisplay != null) joinCodeDisplay.text = $"Code: {joinCode}";
            SetStatus("Host 起動完了 → Match へ");
            if (GameManager.Instance != null && !string.IsNullOrEmpty(matchSceneName))
            {
                GameManager.Instance.SetState(SessionState.MatchInProgress);
                // TODO(M2): NetworkSceneManager.LoadScene() に置換して全 client にシーン同期させる
                // M1 疎通プロト段階では Unity SceneManager で host 側のみ load (client は別途同期予定)
                GameManager.Instance.LoadScene(matchSceneName);
            }
        }

        void HandleClientConnected()
        {
            SetStatus("接続成功");
            // NGO の Network Scene Management が有効なら、host が load した Match scene が自動的に同期される
        }

        void HandleConnectionFailed(string reason)
        {
            SetStatus($"接続失敗: {reason}");
        }

        void SetStatus(string s)
        {
            if (statusDisplay != null) statusDisplay.text = s;
        }
    }
}
