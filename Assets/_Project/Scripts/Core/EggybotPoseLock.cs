using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// Mecanim humanoid retarget で発生する Play 時の bone ズレを LateUpdate で補正する。
    ///
    /// 補正対象:
    /// 1. Hips Y 浮上 (Walking RootT.y vs Avatar T-pose の乖離で +0.19〜0.22m 持ち上がる)
    /// 2. 左右非対称 (Mecanim の humanScale + muscle 計算で L/R Hand/Shoulder の Y が 0.5〜1cm ずれる)
    ///
    /// 真因: Walking クリップの RootT.y curve が Avatar T-pose の bodyPosition.y と乖離 + humanScale=0.36 で
    /// Mixamo 1m 身長想定とのズレ + Avatar 内部の muscle DoF が完全 L/R 対称でない。
    /// Avatar 再構築は罠 #54 (user 手動編集を破壊) なので避ける。
    ///
    /// 解決: LateUpdate で Animator.Update 直後の bone position に対し
    /// (a) Hips に yOffset を加算して全身 Y シフト
    /// (b) Shoulder/Hand を world で強制対称化 (|Lx|=|Rx|, Ly=Ry, Lz=Rz)
    ///
    /// Edit mode では発動しない (user の手動 bone 編集を妨害しないため Application.isPlaying ガード)。
    ///
    /// トレードオフ: 強制対称化で Walking arm swing は減衰する。EGGYBOT は humanScale 0.36 で
    /// retarget 後の腕振りが元々小さいため、見た目影響は無視できる。
    ///
    /// 実行順: PoseLock(default 0) → ShaderBinder(15000) の順で実行されるため、ShaderBinder は補正済 Hips を読む。
    /// 意図せず順序が逆転すると SDF center が 1 フレ古くなる。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class EggybotPoseLock : MonoBehaviour
    {
        [Tooltip("Play 時の Hips Y 浮上の補正量。Edit Hips Y - Play Hips Y で測定。EGGYBOT v16 は -0.219 前後。")]
        // TODO(M2): EggybotConfig SO に移管
        [SerializeField] float yOffset = -0.219f;

        [Tooltip("Shoulder L/R を world で対称化する。Mecanim の muscle 計算ズレを打ち消す。")]
        [SerializeField] bool symmetrizeShoulders = true;

        [Tooltip("Hand L/R を world で対称化する。Walking arm swing の Z 揺れは消えるが humanScale 0.36 では元々小さい。")]
        [SerializeField] bool symmetrizeHands = true;

        [Tooltip("Animator.applyRootMotion を Awake で何に設定するか。Mixamo の in-place Walking なら false。\nXZ 移動が必要な走行クリップ等の将来用フラグ。通常は false のまま。")]
        [SerializeField] bool applyRootMotion = false;

        [Tooltip("Edit T-pose で記録した Hips world Y。Measure 機能で自動算出される基準値。\n注意: transform.localScale や Avatar humanScale を変更したら本値は陳腐化する。scale 変更後は必ず再測定 (Measure Y Offset From Hips) すること。")]
        // TODO(M2): EggybotConfig SO に移管
        [SerializeField] float editHipsY = 0.267f;

        Animator anim;
        Transform hips;
        Transform leftShoulder, rightShoulder;
        Transform leftHand, rightHand;

        void Awake()
        {
            anim = GetComponent<Animator>();
            anim.applyRootMotion = applyRootMotion;
            hips = anim.GetBoneTransform(HumanBodyBones.Hips);
            if (hips == null) Debug.LogWarning("[EggybotPoseLock] Hips bone not found. Avatar が Humanoid か確認");
            leftShoulder = anim.GetBoneTransform(HumanBodyBones.LeftShoulder);
            rightShoulder = anim.GetBoneTransform(HumanBodyBones.RightShoulder);
            leftHand = anim.GetBoneTransform(HumanBodyBones.LeftHand);
            rightHand = anim.GetBoneTransform(HumanBodyBones.RightHand);
        }

        void LateUpdate()
        {
            // Edit mode では発動しない (user の手動 bone 編集を妨害しないため)
            if (!Application.isPlaying) return;

            if (hips != null)
            {
                var p = hips.position;
                p.y += yOffset;
                hips.position = p;
            }

            if (symmetrizeShoulders) SymmetrizeWorld(leftShoulder, rightShoulder);
            if (symmetrizeHands) SymmetrizeWorld(leftHand, rightHand);
        }

        // L/R bone pair を world で完全対称化: |Lx|=|Rx|=avg, Ly=Ry=avg, Lz=Rz=avg
        // L は world X 負側、R は正側に配置 (Mixamo retarget 後の standard hierarchy)。
        // M1 制約: キャラの world rotation Y は 0/180° のみ前提。回転キャラには transform.right 軸対称化に置換要
        static void SymmetrizeWorld(Transform l, Transform r)
        {
            if (l == null || r == null) return;
            var lp = l.position;
            var rp = r.position;
            float xAbs = (Mathf.Abs(lp.x) + Mathf.Abs(rp.x)) * 0.5f;
            float avgY = (lp.y + rp.y) * 0.5f;
            float avgZ = (lp.z + rp.z) * 0.5f;
            l.position = new Vector3(-xAbs, avgY, avgZ);
            r.position = new Vector3(+xAbs, avgY, avgZ);
        }

#if UNITY_EDITOR
        [ContextMenu("Measure Y Offset From Hips")]
        void MeasureFromHips()
        {
            if (anim == null) anim = GetComponent<Animator>();
            var h = anim != null ? anim.GetBoneTransform(HumanBodyBones.Hips) : null;
            if (h == null)
            {
                Debug.LogWarning("[EggybotPoseLock] Hips bone not found. Avatar 設定を確認");
                return;
            }
            float playY = h.position.y;
            float suggested = editHipsY - playY;
            Debug.Log($"[EggybotPoseLock] Play Hips Y={playY:F4}, Edit Y={editHipsY:F4}, suggested yOffset={suggested:F4}");
        }
#endif
    }
}
