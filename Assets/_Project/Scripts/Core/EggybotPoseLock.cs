using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// Mecanim humanoid retarget で発生する Hips Y 浮上を LateUpdate で補正する。
    ///
    /// 真因: Walking クリップの RootT.y curve が Avatar T-pose の bodyPosition.y と乖離してて、
    /// Play 開始時に Hips が +0.19m 持ち上がる (Edit Hips Y=0.267 -> Play Hips Y=0.457)。
    /// Avatar 再構築では治らない (humanScale=0.3506 + Mixamo 想定の 1m 身長乖離が根本)。
    ///
    /// 解決: LateUpdate で Hips bone を直接 yOffset 分 shift する。
    /// 全 humanoid bone は Hips の親追従で一括シフトされるので、Walking 動作を保ったまま全身が下がる。
    ///
    /// yOffset 測定: ContextMenu の "Measure Y Offset From Hips" を Play 中に実行。
    ///
    /// 実行順依存: 本 LateUpdate と EggybotShaderBinder の LateUpdate は両方 LateUpdate なので
    /// 実行順は Script Execution Order 設定がない限り未定義。ShaderBinder が補正前の Hips を読む
    /// 可能性がある。本クラスで [DefaultExecutionOrder] を ShaderBinder (15000) より小さく設定する案があるが、
    /// SDF center は判定球用で 1 フレ遅延しても見た目影響は小さいため M1 では未調整のまま運用。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EggybotPoseLock : MonoBehaviour
    {
        [Tooltip("Play 時に発生する Y 浮上の補正量。Edit Hips Y - Play Hips Y で測定。EGGYBOT v16 は -0.19 前後。")]
        [SerializeField] float yOffset = -0.19f;

        [Tooltip("Animator.applyRootMotion を Awake で何に設定するか。Mixamo の in-place Walking なら false。\nXZ 移動が必要な走行クリップ等の将来用フラグ。通常は false のまま。")]
        [SerializeField] bool applyRootMotion = false;

        [Tooltip("Edit T-pose で記録した Hips world Y。Measure 機能で自動算出される基準値。\n注意: transform.localScale や Avatar humanScale を変更したら本値は陳腐化する。scale 変更後は必ず再測定 (Measure Y Offset From Hips) すること。")]
        [SerializeField] float editHipsY = 0.267f;

        Animator anim;
        Transform hips;

        void Awake()
        {
            anim = GetComponent<Animator>();
            anim.applyRootMotion = applyRootMotion;
            hips = anim.GetBoneTransform(HumanBodyBones.Hips);
        }

        // LateUpdate で Hips bone を直接 Y shift する。
        // 理由: anim.bodyPosition での補正は Mixamo の lockRootHeightY=true で baked された Y
        // を打ち消せないことが実測で判明。LateUpdate は Animator.Update 後に呼ばれるので、
        // Mecanim が書いた bone position に対して直接 offset 適用できる。
        // Hips を動かすと全 humanoid bone は親追従で一括シフトされる。
        void LateUpdate()
        {
            if (hips == null) return;
            var p = hips.position;
            p.y += yOffset;
            hips.position = p;
        }

#if UNITY_EDITOR
        [ContextMenu("Measure Y Offset From Hips")]
        void MeasureFromHips()
        {
            if (anim == null) anim = GetComponent<Animator>();
            var hips = anim != null ? anim.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (hips == null)
            {
                Debug.LogWarning("[EggybotPoseLock] Hips bone not found. Avatar 設定を確認");
                return;
            }
            float playY = hips.position.y;
            float suggested = editHipsY - playY;
            Debug.Log($"[EggybotPoseLock] Play Hips Y={playY:F4}, Edit Y={editHipsY:F4}, suggested yOffset={suggested:F4}");
        }
#endif
    }
}
