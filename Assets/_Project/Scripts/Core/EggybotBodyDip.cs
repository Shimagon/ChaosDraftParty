using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 体全体が着地で「ぐっと沈む」演出 (Mario / Yoshi defacto)。
    ///
    /// 設計 (User 2026-05-20 要求「体全体がちゃんと下に下がる」「Plan A: 体沈む演出」):
    /// - 着地で BodyVisual (or root の child) を Y で一時的に下げる → Spring 物理で 0 に戻る
    /// - EggybotSquashStretch と同 pattern (semi-implicit Euler、velocity 継承)
    /// - 戻り中ジャンプ等で再衝撃しても velocity 加算で連続的
    /// - 体全体 (BodyVisual) を動かすので、頭 + bowl + 手足全部が一体で沈む感
    ///
    /// EggybotFootSquash は無効化 (副作用「足が体に埋まる、体が浮く」を解消するため)。
    /// 代わりに本 component で「体全体が連動して沈む」を実現。
    /// </summary>
    [DefaultExecutionOrder(75)] // PlayerController(50) → Squash(70) → 自分(75) → Crouch(100)
    public class EggybotBodyDip : MonoBehaviour
    {
        [Header("Target")]
        [Tooltip("Y で dip させる Transform。BodyVisual を assign 推奨 (頭含む全身が連動して沈む)。" +
                 "root を指定すると CharacterController と物理干渉、NG。CameraTarget は root 直下で squash 影響受けず安全。")]
        [SerializeField] Transform target;

        [Header("References")]
        [Tooltip("着地検出 + 落下速度取得用 PlayerController。null で無効。")]
        [SerializeField] PlayerController player;

        [Header("Dip Physics (Spring-Damper、Allen Chou defacto)")]
        [Tooltip("Spring 振動数 (Hz)。3 推奨 (ゆっくり沈む → 戻る感)。低=ゆっくり、高=速い。")]
        [Range(0.5f, 8f)]
        [SerializeField] float dipFrequency = 3f;

        [Tooltip("Damping ratio。0.5 推奨 (overshoot 控えめ、Mario 着地感)。0.3 で「ぼよん」、0.8 で素早く戻る。")]
        [Range(0.05f, 1.5f)]
        [SerializeField] float dampingRatio = 0.5f;

        [Tooltip("着地最大 impulse (vy=max 時の velocity 加算)。-2 推奨 (BodyVisual を 0.05m 程度沈ませる)。")]
        [Range(-10f, 0f)]
        [SerializeField] float landingImpulseMax = -2f;

        [Header("Landing intensity")]
        [Tooltip("intensity = Mathf.InverseLerp(min, max, fallSpeed)。max 12 推奨。")]
        [SerializeField] float landingMaxFallSpeed = 12f;

        [Tooltip("intensity 下限。0.5 m/s 未満の小さな着地では dip しない。")]
        [SerializeField] float landingMinFallSpeed = 0.5f;

        [Header("Clamp / Safety")]
        [Tooltip("dip 最大距離 (m)。Spring が overshoot しすぎても visual に効く範囲を制限。0.10 推奨。")]
        [Range(0.01f, 0.30f)]
        [SerializeField] float dipMaxClamp = 0.10f;

        [Tooltip("dt 上限 (秒)。frame spike で積分誤差を防ぐ。1/30 推奨。")]
        [Range(1f / 120f, 1f / 10f)]
        [SerializeField] float dtMaxClamp = 1f / 30f;

        // State
        Vector3 basePos;
        float currentY = 0f;     // 0 = 元位置、負値 = 沈み込み量
        float velocityY = 0f;
        float targetY = 0f;       // 常時 0 を target (Spring が振動して 0 に減衰)

        // 着地検出用 (PlayerController と独立に追跡)
        bool wasGrounded = true;
        float prevVerticalVel = 0f;

        void Awake()
        {
            if (target == null) target = transform;
            // basePos は Start で取得 (Awake 順序依存回避、unity-reviewer 1 周目 MEDIUM 修正)
            currentY = 0f;
            velocityY = 0f;
            targetY = 0f;
        }

        void Start()
        {
            // 全 component の Awake 完了後に localPosition snapshot (順序依存解消)
            basePos = target.localPosition;
        }

        void OnDisable() { Restore(); }
        void OnDestroy() { Restore(); }

        void Update()
        {
            // 自前着地検出 (PlayerController も独自に追跡してるが、本 component は target=BodyVisual を動かす)
            if (player != null)
            {
                bool grounded = player.IsGrounded;
                if (!wasGrounded && grounded)
                {
                    float fallSpeed = Mathf.Max(0f, -prevVerticalVel);
                    if (fallSpeed >= landingMinFallSpeed)
                    {
                        float k = Mathf.InverseLerp(landingMinFallSpeed, landingMaxFallSpeed, fallSpeed);
                        velocityY += landingImpulseMax * k; // 負の velocity = 下方向 impulse
                    }
                }
                wasGrounded = grounded;
                prevVerticalVel = player.VerticalVelocity;
            }

            // Spring 物理 (semi-implicit Euler)
            float omega = dipFrequency * 2f * Mathf.PI;
            float stiffness = omega * omega;
            float damping = 2f * dampingRatio * omega;
            float dt = Mathf.Min(Time.deltaTime, dtMaxClamp);
            float accel = (targetY - currentY) * stiffness - velocityY * damping;
            velocityY += accel * dt;
            currentY += velocityY * dt;

            // Visual 範囲 clamp (overshoot 制限)
            currentY = Mathf.Clamp(currentY, -dipMaxClamp, dipMaxClamp);

            // 適用 (BodyVisual の Y を base から下げる)
            target.localPosition = basePos + Vector3.up * currentY;
        }

        /// <summary>外部 API: 着地以外でも明示 dip 発火 (Crouch impact 等)。intensity 0..1。</summary>
        public void AddImpulse(float intensity)
        {
            intensity = Mathf.Clamp01(intensity);
            velocityY += landingImpulseMax * intensity;
        }

        public void Restore()
        {
            currentY = 0f;
            velocityY = 0f;
            targetY = 0f;
            if (target != null) target.localPosition = basePos;
            wasGrounded = true;
            prevVerticalVel = 0f;
        }
    }
}
