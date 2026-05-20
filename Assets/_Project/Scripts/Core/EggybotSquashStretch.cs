using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 用 Squash &amp; Stretch (Spring-Damper 物理ベース、2026-05-20 移行)。
    ///
    /// 設計 (User 2026-05-19 要求「戻り中ジャンプでも自然にブニブニ」「もっと自然」「物理調べてみて」):
    /// - **状態 = (currentY, velocityY) 2 変数** (coroutine 不使用、Update 単一駆動)
    /// - **semi-implicit Euler spring step** (Allen Chou defacto、ζ&lt;1 / ω&lt;10π で stable)
    ///   - accel = (target - current) * stiffness - velocity * damping
    ///   - velocity += accel * dt
    ///   - current  += velocity * dt
    /// - **velocity 継承**: target 変化や impulse 加算で discrete reset なし
    ///   → 「戻り途中でジャンプ → 現状から自然にブニブニ」が自動発生
    /// - **指数減衰**: ζ=0.35 で under-damped、振動 2 回 visible → 自然消失
    /// - **impulse 加算性**: Play(Landing) → velocityY += -12 (負で squash 方向衝撃)
    ///
    /// パラメータ (Daniel Holden / Allen Chou / Maxime Heckel defacto):
    /// - springFrequency  = 2.5 Hz  (振動周期 0.4s、EGGYBOT サイズ感)
    /// - dampingRatio     = 0.45    (under-damped、0.45 推奨 (User 2026-05-19 体感: 頭少し柔らかすぎで 0.35→0.45))
    /// - landingImpulseMax = -10    (vy 連動で 0〜-10 範囲、-10 推奨 (User 2026-05-19 体感で -12→-10))
    /// - jumpAnticipationImpulse = -3 (弱め sink、すぐジャンプ実行)
    ///
    /// 適用先: BodyVisual or HeadAnchor (User 「カプセル固く、頭やわらか」要求で HeadAnchor 採用)
    /// 体積保存: Y * XZ² ≈ 1 (sqrt(Y) で XZ 補正)
    /// </summary>
    [DefaultExecutionOrder(70)] // PlayerController(50) → 自分(70) → EggybotCrouch(100)
    public class EggybotSquashStretch : MonoBehaviour
    {
        public enum EventType { Landing, JumpAnticipation, IdleBreathing }

        [Header("Target")]
        [Tooltip("scale を書き換える Transform。User 要求 (2026-05-19): 「カプセル容器は固く、頭は柔らかく」のため "
               + "HeadAnchor (頭のみ squash) を assign。BodyVisual (全身 squash) も選択可。"
               + "root に assign すると EggybotArmLock / Hand_Rigid が壊れるため NG。")]
        [SerializeField] Transform target;

        // ---- Spring-Damper Physics --------------------------------------------------
        [Header("Spring-Damper (2026-05-20 物理化、AAA defacto)")]

        [Tooltip("自然振動数 (Hz)。Allen Chou / Daniel Holden defacto。2.5 推奨 (周期 0.4s、ジェリー)。" +
                 "低=ゆっくり波打つ、高=速いブニブニ。10 以上は数値発散リスク。")]
        [Range(0.5f, 8f)]
        [SerializeField] float springFrequency = 2.5f;

        [Tooltip("Damping ratio。0=無減衰発散、1=critical (overshoot なし)、0.45 推奨 (User 2026-05-19 体感: 頭少し柔らかすぎで 0.35→0.45)。" +
                 "Maxime Heckel: 0.3-0.5 sweet spot、User「ブニブニ」要求に合致。")]
        [Range(0.05f, 1.5f)]
        [SerializeField] float dampingRatio = 0.45f;

        [Tooltip("着地時の最大 impulse (vy=max 時)。-10 推奨 (User 2026-05-19 体感で -12→-10、負で squash 方向衝撃)。" +
                 "Play(Landing, intensity) で velocityY += this * intensity。")]
        [Range(-30f, 0f)]
        [SerializeField] float landingImpulseMax = -10f;

        [Tooltip("Jump anticipation 時の impulse。-3 推奨 (弱め sink、すぐジャンプ実行)。")]
        [Range(-10f, 0f)]
        [SerializeField] float jumpAnticipationImpulse = -3f;

        // ---- Landing intensity normalization (vy → 0..1) ----------------------------
        [Header("Landing intensity (vy 連動)")]

        [Tooltip("intensity = Mathf.InverseLerp(min, max, fallSpeed)。max 落下速度 m/s。12 推奨。")]
        [SerializeField] float landingMaxFallSpeed = 12f;

        [Tooltip("intensity 適用の下限。0.5 m/s 未満の小さな着地では squash を効かせない。")]
        [SerializeField] float landingMinFallSpeed = 0.5f;

        // ---- Airborne (ジャンプ重力連動) --------------------------------------------
        [Header("Airborne — 空中中の target Y stretch (重力連動)")]

        [Tooltip("空中中の squash target Y を重力連動で駆動するか。OFF で着地 impulse のみ。")]
        [SerializeField] bool airborneEnabled = true;

        [Tooltip("vy 取得用 PlayerController。null で airborne 無効。")]
        [SerializeField] PlayerController player;

        [Tooltip("上昇中 (vy > threshold) の target Y stretch。1.10 推奨 (Mario Odyssey defacto)。")]
        [Range(1f, 1.3f)]
        [SerializeField] float airborneUpStretchY = 1.10f;

        [Tooltip("落下中 (vy < -threshold) の target Y stretch。1.15 推奨 (Fall Guys 豆 defacto、上昇より強)。")]
        [Range(1f, 1.3f)]
        [SerializeField] float airborneDownStretchY = 1.15f;

        [Tooltip("vy 判定閾値 (m/s)。|vy| <= これは「頂点」扱いで target=1。1.0 推奨。")]
        [SerializeField] float airborneVelocityThreshold = 1.0f;

        [Tooltip("vy 正規化用の最大速度 (m/s)。8 推奨 (jump 5 + 重力で typical 6-8)。")]
        [SerializeField] float airborneVelocityMax = 8f;

        [Tooltip("Wobble: target に乗せる Sin 振動 (Spring と干渉軽減で 0.04→0.03 に控えめ)。")]
        [Range(0f, 0.10f)]
        [SerializeField] float airborneWobbleAmplitude = 0.03f;

        [Tooltip("Wobble 周期 (Hz)。Spring freq (2.5Hz) と離して干渉軽減、5 推奨。")]
        [Range(0f, 10f)]
        [SerializeField] float airborneWobbleFrequency = 5f;

        // ---- Additional Targets (手足 broadcast、2026-05-19 User 要求) -------------
        [Header("Additional Targets (手足 broadcast)")]

        [Tooltip("頭以外で同じ Spring 物理に追従させる Transform。Hand_L/R, Foot_L/R を assign。" +
                 "全部 primitive Sphere (pivot offset なし) なので scale 直書きで「同位置で縮む」が成立。" +
                 "Foot は EggybotFootSquash が LateUpdate で上書きするため接地時 squash と共存。")]
        [SerializeField] Transform[] additionalTargets;

        [Tooltip("追加 target の squash 強度倍率。0=動かない、1=頭と同強度、0.5 推奨 (Mario / Splatoon 型「頭メイン手足サブ」defacto)。")]
        [Range(0f, 1.5f)]
        [SerializeField] float additionalIntensity = 0.5f;

        // ---- Idle Breathing (Spring bypass、直接 scale 書込) ------------------------
        [Header("Idle Breathing — 呼吸感 (Spring bypass)")]

        [Tooltip("Idle 呼吸を有効化。OFF で完全停止 (M2 OFF 推奨、まず Landing 体感)。" +
                 "Spring bypass で直接 scale 書込 (現状互換維持、振幅抑制なし)。")]
        [SerializeField] bool idleBreathingEnabled = false;

        [Tooltip("Y 振幅 (相対 +/-)。0.015 = ±1.5% 推奨 (subtle)。")]
        [Range(0f, 0.05f)]
        [SerializeField] float idleAmplitudeY = 0.015f;

        [Tooltip("呼吸周期 (秒)。3.8 推奨 (~16 BPM = 安静時人間呼吸)。")]
        [SerializeField] float idlePeriod = 3.8f;

        // ---- Safety (Agent 落とし穴対策) -------------------------------------------
        [Header("Safety (Agent 落とし穴対策)")]

        [Tooltip("currentY の下限。強 impulse で 0 以下にならないよう clamp、sqrt 用。0.2 推奨。")]
        [Range(0.05f, 0.5f)]
        [SerializeField] float currentYMinClamp = 0.2f;

        [Tooltip("dt の上限 (秒)。frame spike で積分誤差暴発を防ぐ。1/30=33ms 推奨。")]
        [Range(1f / 120f, 1f / 10f)]
        [SerializeField] float dtMaxClamp = 1f / 30f;

        // ---- State (Spring 物理の 2 変数 + base) -----------------------------------
        Vector3 baseScale;
        Vector3[] additionalBaseScales;
        float currentY = 1f;
        float velocityY = 0f;
        float targetY = 1f;

        void Awake()
        {
            if (target == null) target = transform;
            baseScale = target.localScale;
            currentY = 1f;
            velocityY = 0f;
            targetY = 1f;
            // 手足の base scale を保存 (Awake snapshot、後で Foot/Hand 設定変更しても固定値)
            if (additionalTargets != null)
            {
                additionalBaseScales = new Vector3[additionalTargets.Length];
                for (int i = 0; i < additionalTargets.Length; i++)
                {
                    if (additionalTargets[i] != null)
                        additionalBaseScales[i] = additionalTargets[i].localScale;
                }
            }
        }

        void OnDisable() { Restore(); }
        void OnDestroy() { Restore(); }

        void Update()
        {
            // ---- target 決定 ----
            if (idleBreathingEnabled && (player == null || player.IsGrounded || !airborneEnabled))
            {
                // Spring が静止中 (impulse 残ってない) なら Idle bypass で直接書込
                if (Mathf.Abs(velocityY) < 0.1f)
                {
                    float phase = (Time.time / Mathf.Max(idlePeriod, 0.01f)) * Mathf.PI * 2f;
                    float y = 1f + Mathf.Sin(phase) * idleAmplitudeY;
                    float xz = 1f - Mathf.Sin(phase) * (idleAmplitudeY * 0.5f);
                    target.localScale = new Vector3(baseScale.x * xz, baseScale.y * y, baseScale.z * xz);
                    currentY = y;
                    // velocityY はリセットせず (impulse が来た時に Spring branch に流れる)
                    targetY = 1f;
                    // 手足にも Idle 呼吸 broadcast (subtle、intensity 倍率)
                    ApplyToAdditional(y);
                    return;
                }
                // Spring が振動中 (impulse 有効): fall through で Spring 物理を回す
                targetY = 1f;
            }

            if (airborneEnabled && player != null && !player.IsGrounded)
            {
                // 空中: vy 連動 target + Wobble (Spring が滑らかに追従)
                float vy = player.VerticalVelocity;
                if (vy > airborneVelocityThreshold)
                {
                    float k = Mathf.Clamp01(vy / Mathf.Max(airborneVelocityMax, 0.01f));
                    targetY = Mathf.Lerp(1f, airborneUpStretchY, k);
                }
                else if (vy < -airborneVelocityThreshold)
                {
                    float k = Mathf.Clamp01(-vy / Mathf.Max(airborneVelocityMax, 0.01f));
                    targetY = Mathf.Lerp(1f, airborneDownStretchY, k);
                }
                else
                {
                    targetY = 1f;
                }
                if (airborneWobbleAmplitude > 0.001f)
                {
                    float wobble = Mathf.Sin(Time.time * airborneWobbleFrequency * Mathf.PI * 2f) * airborneWobbleAmplitude;
                    targetY += wobble;
                }
            }
            else
            {
                // 地上 (Landing 後の戻り含む): target = 1.0、Spring が impulse で振動して自然減衰
                targetY = 1f;
            }

            // ---- semi-implicit Euler spring step (Allen Chou defacto) ----
            float omega = springFrequency * 2f * Mathf.PI;
            float stiffness = omega * omega;
            float damping   = 2f * dampingRatio * omega;
            float dt = Mathf.Min(Time.deltaTime, dtMaxClamp); // frame spike 安全策
            float accel = (targetY - currentY) * stiffness - velocityY * damping;
            velocityY += accel * dt;
            currentY  += velocityY * dt;
            currentY = Mathf.Max(currentY, currentYMinClamp); // sqrt(0) 防止

            // ---- 体積保存 + 適用 (head + additional) ----
            float xzFinal = 1f / Mathf.Sqrt(currentY);
            target.localScale = new Vector3(baseScale.x * xzFinal, baseScale.y * currentY, baseScale.z * xzFinal);

            // 手足 broadcast (intensity で強度調整)、Foot は EggybotFootSquash が LateUpdate で上書き
            ApplyToAdditional(currentY);
        }

        /// <summary>手足 (Hand_L/R, Foot_L/R) に弱め強度で squash を broadcast。Mario / Splatoon 型「頭メイン手足サブ」。</summary>
        void ApplyToAdditional(float headCurrentY)
        {
            if (additionalTargets == null || additionalBaseScales == null || additionalIntensity <= 0.001f) return;
            // 倍率適用: currentY=1 (静止) は不動、ズレ分のみ scaling
            float yAdd = Mathf.Lerp(1f, headCurrentY, additionalIntensity);
            yAdd = Mathf.Max(yAdd, currentYMinClamp);
            float xzAdd = 1f / Mathf.Sqrt(yAdd);
            for (int i = 0; i < additionalTargets.Length; i++)
            {
                if (additionalTargets[i] == null) continue;
                var bs = additionalBaseScales[i];
                additionalTargets[i].localScale = new Vector3(bs.x * xzAdd, bs.y * yAdd, bs.z * xzAdd);
            }
        }

        /// <summary>外部 API。Spring 物理に impulse を加算 (velocity 継承、連続的)。</summary>
        public void Play(EventType type, float intensity = 1f)
        {
            intensity = Mathf.Clamp01(intensity);
            if (intensity <= 0.001f) return;
            switch (type)
            {
                case EventType.Landing:
                    // 着地: 強い squash 方向 impulse (負で Y 縮)
                    velocityY += landingImpulseMax * intensity;
                    break;
                case EventType.JumpAnticipation:
                    // 踏切: 弱め sink impulse
                    velocityY += jumpAnticipationImpulse * intensity;
                    break;
                case EventType.IdleBreathing:
                    // Update() で常時駆動するため明示 Play は no-op (toggle は idleBreathingEnabled で)
                    break;
            }
        }

        /// <summary>落下速度 (m/s、正の値) を渡すと min..max を 0..1 に正規化して Landing を発火。</summary>
        public void PlayLandingFromFallSpeed(float fallSpeed)
        {
            if (fallSpeed < landingMinFallSpeed) return;
            float k = Mathf.InverseLerp(landingMinFallSpeed, landingMaxFallSpeed, fallSpeed);
            Play(EventType.Landing, k);
        }

        public void Restore()
        {
            currentY = 1f;
            velocityY = 0f;
            targetY = 1f;
            if (target != null) target.localScale = baseScale;
            // additional targets も base に復元 (OnDisable / OnDestroy 後の Edit mode 残留防止)
            if (additionalTargets != null && additionalBaseScales != null)
            {
                for (int i = 0; i < additionalTargets.Length; i++)
                {
                    if (additionalTargets[i] != null)
                        additionalTargets[i].localScale = additionalBaseScales[i];
                }
            }
        }
    }
}
