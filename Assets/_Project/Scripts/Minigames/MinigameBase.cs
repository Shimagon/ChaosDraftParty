using UnityEngine;

namespace ChaosDraftParty.Minigames
{
    public enum MinigamePhase
    {
        NotStarted,
        Countdown,
        Playing,
        Ending,
        Completed,
    }

    /// <summary>
    /// すべてのミニゲームの基底クラス。縮む島・爆弾鬼・しっぽ狩り×2 が継承する。
    /// サーバー権威で状態遷移、クライアントは見た目更新のみ。
    /// </summary>
    public abstract class MinigameBase : MonoBehaviour
    {
        [Header("Definition (SO)")]
        [SerializeField] protected MinigameDefinition definition;

        [Header("Runtime")]
        [SerializeField] protected MinigamePhase phase = MinigamePhase.NotStarted;
        [SerializeField] protected float timeRemaining;

        public MinigameDefinition Definition => definition;
        public MinigamePhase Phase => phase;
        public float TimeRemaining => timeRemaining;

        public event System.Action<MinigamePhase, MinigamePhase> OnPhaseChanged;
        public event System.Action OnMinigameCompleted;

        /// <summary>サーバー側で 1 ラウンド開始。</summary>
        public virtual void StartMinigame()
        {
            if (definition == null)
            {
                Debug.LogError($"[MinigameBase] definition 未 assign on {gameObject.name}");
                return;
            }
            timeRemaining = definition.durationSeconds;
            SetPhase(MinigamePhase.Countdown);
        }

        /// <summary>カウントダウン完了時にサーバーから呼ばれる。</summary>
        public virtual void BeginPlay()
        {
            SetPhase(MinigamePhase.Playing);
        }

        /// <summary>Update で呼ばれる。サーバー権威で時間・勝利判定進行。</summary>
        public virtual void Tick(float deltaTime)
        {
            if (phase != MinigamePhase.Playing) return;

            timeRemaining -= deltaTime;
            if (timeRemaining <= 0f)
            {
                timeRemaining = 0f;
                CompleteMinigame();
            }

            CheckVictoryCondition();
        }

        /// <summary>勝利条件チェック。サブクラスで実装 (LastSurvivor 判定、スコア比較等)。</summary>
        protected abstract void CheckVictoryCondition();

        /// <summary>ミニゲーム終了。サブクラス or Tick から呼ばれる。</summary>
        public virtual void CompleteMinigame()
        {
            SetPhase(MinigamePhase.Ending);
            OnMinigameCompleted?.Invoke();
        }

        protected void SetPhase(MinigamePhase next)
        {
            if (next == phase) return;
            var prev = phase;
            phase = next;
            OnPhaseChanged?.Invoke(prev, next);
        }
    }
}
