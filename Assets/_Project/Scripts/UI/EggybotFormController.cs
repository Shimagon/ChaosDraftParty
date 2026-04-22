using UnityEngine;

namespace ChaosDraftParty.UI
{
    /// <summary>
    /// EGGYBOT の 2 形態（A: フル、B: 収納カプセル）切替。
    /// Animator パラメータ "Retract" (0-1 float) を時間で動かす bone-driven 方式を前提。
    /// 頭 bone を AnimationClip で下降させる実装と協調。
    /// </summary>
    public sealed class EggybotFormController : MonoBehaviour
    {
        [SerializeField] Animator animator;
        [SerializeField] string retractParameter = "Retract";
        [SerializeField] float retractSpeed = 4f;
        [SerializeField] float autoToggleMinInterval = 8f;
        [SerializeField] float autoToggleMaxInterval = 20f;
        [SerializeField] bool autoToggle = true;

        float target;
        float current;
        float nextAutoToggleAt;

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
        }

        void Start()
        {
            ScheduleNextToggle();
        }

        void Update()
        {
            if (autoToggle && Time.time >= nextAutoToggleAt)
            {
                target = target > 0.5f ? 0f : 1f;
                ScheduleNextToggle();
            }

            current = Mathf.MoveTowards(current, target, retractSpeed * Time.deltaTime);
            if (animator != null && !string.IsNullOrEmpty(retractParameter))
            {
                animator.SetFloat(retractParameter, current);
            }
        }

        public void TriggerRetract() => target = 1f;
        public void TriggerExpand() => target = 0f;
        public void Toggle() => target = target > 0.5f ? 0f : 1f;

        void ScheduleNextToggle()
        {
            nextAutoToggleAt = Time.time + Random.Range(autoToggleMinInterval, autoToggleMaxInterval);
        }
    }
}
