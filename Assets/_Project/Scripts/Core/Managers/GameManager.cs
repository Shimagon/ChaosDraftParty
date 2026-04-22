using UnityEngine;
using UnityEngine.SceneManagement;
using ChaosDraftParty.Core.Configs;

namespace ChaosDraftParty.Core.Managers
{
    public enum SessionState
    {
        Bootstrapping,
        MainMenu,
        Lobby,
        MatchInProgress,
        InterMatch,
        SeasonEnding,
        SessionEnd,
    }

    /// <summary>
    /// セッション全体を管理する DontDestroyOnLoad Singleton。
    /// シーン遷移、State Machine、GameConfig への統一アクセス。
    /// CLAUDE.md の State Machines セクション準拠。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private GameConfig config;

        [Header("Runtime State (Read-Only)")]
        [SerializeField] private SessionState currentState = SessionState.Bootstrapping;
        [SerializeField] private int currentMatchIndex;
        [SerializeField] private int currentRoundIndex;

        public GameConfig Config => config;
        public SessionState State => currentState;
        public int MatchIndex => currentMatchIndex;
        public int RoundIndex => currentRoundIndex;

        public event System.Action<SessionState, SessionState> OnStateChanged;

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

        public void SetState(SessionState next)
        {
            if (next == currentState) return;
            var prev = currentState;
            currentState = next;
            Debug.Log($"[GameManager] {prev} → {next}");
            OnStateChanged?.Invoke(prev, next);
        }

        public void LoadScene(string sceneName, LoadSceneMode mode = LoadSceneMode.Single)
        {
            SceneManager.LoadScene(sceneName, mode);
        }

        public void BeginMatch(int matchIndex)
        {
            currentMatchIndex = matchIndex;
            currentRoundIndex = 0;
            SetState(SessionState.MatchInProgress);
        }

        public void BeginRound(int roundIndex)
        {
            currentRoundIndex = roundIndex;
        }

        public void EndMatch()
        {
            SetState(SessionState.InterMatch);
        }

        public void EndSession()
        {
            SetState(SessionState.SessionEnd);
        }
    }
}
