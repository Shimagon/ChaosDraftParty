using UnityEngine;
using UnityEngine.UI;
using ChaosDraftParty.Core.Managers;

namespace ChaosDraftParty.UI
{
    /// <summary>
    /// MainMenu シーンのボタンを GameManager の scene 遷移と配線する。
    /// Boot → MainMenu 到着時に Bootstrap が Auth 済の状態を前提。
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Buttons")]
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;

        [Header("Navigation")]
        [Tooltip("Host/Join どちらでも進む先 (M1 では Lobby スキップ → Match 直行でも可)")]
        [SerializeField] private string lobbySceneName = "Lobby";

        void OnEnable()
        {
            if (hostButton != null) hostButton.onClick.AddListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.AddListener(OnJoinClicked);
            if (quitButton != null) quitButton.onClick.AddListener(OnQuitClicked);
        }

        void OnDisable()
        {
            if (hostButton != null) hostButton.onClick.RemoveListener(OnHostClicked);
            if (joinButton != null) joinButton.onClick.RemoveListener(OnJoinClicked);
            if (quitButton != null) quitButton.onClick.RemoveListener(OnQuitClicked);
        }

        void OnHostClicked()
        {
            Debug.Log("[MainMenu] Host clicked → Lobby (M1 はまだ疎通のみ、RelayManager は M1 後半)");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(SessionState.Lobby);
                GameManager.Instance.LoadScene(lobbySceneName);
            }
        }

        void OnJoinClicked()
        {
            Debug.Log("[MainMenu] Join clicked → Lobby (JoinCode 入力は M1 後半で実装)");
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(SessionState.Lobby);
                GameManager.Instance.LoadScene(lobbySceneName);
            }
        }

        void OnQuitClicked()
        {
            Debug.Log("[MainMenu] Quit clicked");
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
