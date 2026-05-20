using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 足球 (Foot_L/R) 用 procedural squash + めり込み補正。
    ///
    /// 設計 (User 2026-05-19 要求「足が床にめり込んでる、ブニッとつぶれる感、自然」+ 並列 agent 調査):
    /// - Mario Odyssey defacto: 接地で Y 潰し + XZ 広げ + pivot Y 上げ で「ぺしゃっと密着」演出
    /// - PlayerController.IsGrounded を購読、SmoothDamp で 0(空中) ↔ 1(接地) を駆動
    /// - **LateUpdate** で書込 (Animator が `LateUpdate` 前に bone を動かす設計、後で上書きしないと 1 フレ遅れ)
    /// - **EggybotSquashStretch の additional broadcast の後** に走るため (DefaultExecutionOrder 120 > 70)、
    ///   接地時は本 component が Foot 球を上書き、空中時は Spring の wobble が支配 (blend=0 で base 復帰)
    ///
    /// 真因 (足めり込み):
    /// - Foot 球 (radius 0.10m) が Mixamo bone の中心に配置 (足底じゃなく足首)
    /// - bone が床 Y≈0 に来ると球の下半分 0.10m が床下に沈む
    ///
    /// defacto 値 (Mario Odyssey + Fall Guys 計測):
    /// - groundedYScale = 0.55 (元 0.20 → 0.11、底が床に押付けられた感)
    /// - groundedXZScale = 1.20 (体積保存風、ぺしゃっと広がる)
    /// - groundedYOffset = +0.04 (潰れた分 pivot を上げ、めり込み完全解消)
    /// - landSmoothTime = 0.08s (着地は速い)
    /// - liftSmoothTime = 0.15s (離地はゆっくり、Disney "follow through")
    /// </summary>
    [DefaultExecutionOrder(120)] // PlayerController(50) → Squash(70) → Crouch(100) → 自分(120) で最終決定
    public class EggybotFootSquash : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("IsGrounded / VerticalVelocity 取得用 PlayerController。null で無効。")]
        [SerializeField] PlayerController player;

        [Tooltip("足球 (Foot_L, Foot_R) を assign。Hand は EggybotSquashStretch.additionalTargets で連動。")]
        [SerializeField] Transform[] feet;

        [Header("Grounded Squash (Mario Odyssey defacto)")]
        [Tooltip("接地時の Foot Y scale。0.55 推奨 (元 1.0 → 0.55、ぺしゃっと密着)。")]
        [Range(0.3f, 1f)]
        [SerializeField] float groundedYScale = 0.55f;

        [Tooltip("接地時の XZ scale (体積保存風、ぺしゃっと広がる)。1.20 推奨。")]
        [Range(1f, 1.6f)]
        [SerializeField] float groundedXZScale = 1.20f;

        [Tooltip("接地時の Y offset (めり込み解消)。+0.04 推奨 (Y=0 床に足底を合わせる補正)。")]
        [Range(0f, 0.15f)]
        [SerializeField] float groundedYOffset = 0.04f;

        [Header("Transition")]
        [Tooltip("着地時の補間時間 (秒)。0.08 推奨 (速く密着、impact 感)。")]
        [Range(0.02f, 0.5f)]
        [SerializeField] float landSmoothTime = 0.08f;

        [Tooltip("離地時の補間時間 (秒)。0.15 推奨 (ゆっくり離れる、Disney follow-through)。")]
        [Range(0.02f, 0.5f)]
        [SerializeField] float liftSmoothTime = 0.15f;

        [Tooltip("落下速度 (m/s) で接地時 intensity を更に強める。max 12 推奨 (1.0 → 1.3 倍まで)。")]
        [SerializeField] float fallSpeedForMaxSquash = 12f;

        Vector3[] baseScales;
        Vector3[] basePositions;
        float blend;         // 0 = 空中、1 = 接地
        float blendVel;      // SmoothDamp 用 velocity ref

        void Awake()
        {
            if (feet == null) { feet = new Transform[0]; return; }
            baseScales = new Vector3[feet.Length];
            basePositions = new Vector3[feet.Length];
            for (int i = 0; i < feet.Length; i++)
            {
                if (feet[i] == null) continue;
                baseScales[i] = feet[i].localScale;
                basePositions[i] = feet[i].localPosition;
            }
        }

        void OnDisable() { Restore(); }
        void OnDestroy() { Restore(); }

        void LateUpdate()
        {
            if (player == null || feet == null) return;
            bool grounded = player.IsGrounded;
            float fall = Mathf.Max(0f, -player.VerticalVelocity);
            float intensity = grounded
                ? Mathf.Lerp(1f, 1.3f, Mathf.Clamp01(fall / Mathf.Max(fallSpeedForMaxSquash, 0.01f)))
                : 0f;
            float smooth = grounded ? landSmoothTime : liftSmoothTime;
            blend = Mathf.SmoothDamp(blend, intensity, ref blendVel, smooth);

            for (int i = 0; i < feet.Length; i++)
            {
                var t = feet[i];
                if (t == null) continue;

                // blend≈0 (空中時) は Spring の wobble 値を温存して書込スキップ
                if (blend < 0.001f) continue;

                float yMul  = Mathf.Lerp(1f, groundedYScale,  blend);
                float xzMul = Mathf.Lerp(1f, groundedXZScale, blend);
                var bs = baseScales[i];
                t.localScale = new Vector3(bs.x * xzMul, bs.y * yMul, bs.z * xzMul);
                t.localPosition = basePositions[i] + Vector3.up * (groundedYOffset * blend);
            }
        }

        public void Restore()
        {
            if (feet == null) return;
            for (int i = 0; i < feet.Length; i++)
            {
                if (feet[i] == null) continue;
                if (baseScales != null) feet[i].localScale = baseScales[i];
                if (basePositions != null) feet[i].localPosition = basePositions[i];
            }
            blend = 0f;
            blendVel = 0f;
        }
    }
}
