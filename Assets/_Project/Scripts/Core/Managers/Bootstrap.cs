using System.Threading.Tasks;
using UnityEngine;
using Unity.Services.Core;
using Unity.Services.Authentication;

namespace ChaosDraftParty.Core.Managers
{
    /// <summary>
    /// Boot シーンに配置。起動時に UGS 初期化 + 匿名 Auth + GameManager state 遷移。
    /// CLAUDE.md / TODO.md の Bootstrap.cs タスク準拠。
    /// DoD: Unity 起動 → Console に '[Boot] Authenticated as &lt;playerId&gt;' が出る。
    /// </summary>
    public class Bootstrap : MonoBehaviour
    {
        [Header("Scene Navigation")]
        [SerializeField] private string nextSceneName = "MainMenu";
        [SerializeField] private bool autoAdvance = true;

        [Header("Network Mode")]
        [Tooltip("ON: UGS 初期化 + 匿名 Auth (Relay 利用時)。OFF: skip (localhost UTP テスト時)。")]
        [SerializeField] private bool requireUGS = true;

        void Awake()
        {
            // Editor の Game view が focus 外れた時 (Inspector クリック等) に Update 停止すると、
            // dither / camera / animation が止まって「マウス動かさないと処理されない」現象になる。
            // build 時も同様に常時 update を保つために常時有効化。Multiplayer 開発で必須。
            Application.runInBackground = true;
        }

        async void Start()
        {
            Debug.Log("[Boot] 開始");

            if (requireUGS)
            {
                try
                {
                    await InitializeUnityServices();
                    Debug.Log($"[Boot] Authenticated as {AuthenticationService.Instance.PlayerId}");
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[Boot] UGS init failed: {e.Message}");
                    return;
                }
            }
            else
            {
                Debug.Log("[Boot] UGS init skipped (requireUGS=false、localhost UTP モード)");
            }

            if (autoAdvance && GameManager.Instance != null)
            {
                GameManager.Instance.SetState(SessionState.MainMenu);
                if (!string.IsNullOrEmpty(nextSceneName))
                    GameManager.Instance.LoadScene(nextSceneName);
            }
        }

        private static async Task InitializeUnityServices()
        {
            if (UnityServices.State != ServicesInitializationState.Initialized)
            {
                await UnityServices.InitializeAsync();
            }

            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
            }
        }
    }
}
