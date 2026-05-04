using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// Run 等の humanoid clip で腕が大きく振れて手 (Hand_L/R 球) が浮く問題を抑える。
    ///
    /// Animator の muscle 出力を LateUpdate で取得した後、肩〜手の bone localRotation を
    /// T-pose 静止 rotation に向けて Slerp で blend する。bone ごとに 0〜1 で強度指定。
    /// 1.0 = 完全に T-pose 固定、0.0 = 通常 (Animator のまま)。
    ///
    /// 使い方:
    /// - EGGYBOT_v16 root (Animator 付き GO) に attach
    /// - PoseLock の後 (DefaultExecutionOrder 高め) に動く
    /// - Awake 時の bone localRotation を rest として保存
    /// - Inspector で shoulderLock / upperArmLock / lowerArmLock / handLock を slider 調整
    ///
    /// 設計:
    /// - Animator.cullingMode は AlwaysAnimate 前提
    /// - LateUpdate で書き換え (Animator.Update 後)
    /// - Hand_L/R Transform は mixamorig:LeftHand bone の child なので、Hand bone を抑えれば球も付いてくる
    /// </summary>
    [DefaultExecutionOrder(20)] // PoseLock(0) の後、Crouch(100) の前
    [RequireComponent(typeof(Animator))]
    public class EggybotArmLock : MonoBehaviour
    {
        [Header("Body axis freeze (Hips/Spine/Chest のみ root に固定)")]
        [Range(0f, 1f)]
        [Tooltip("Hips/Spine/Chest/UpperChest の **world rotation を root に揃える**。1.0 で体軸完全固定。腕は自由に振れる。")]
        [SerializeField] float bodyAxisFreeze = 1f;

        [Range(0f, 1f)]
        [Tooltip("両腕 (Shoulder/UpperArm/LowerArm/Hand) の world rotation を root に揃える。0.0 で振り通常、1.0 で固定。")]
        [SerializeField] float armAxisFreeze = 0f;

        [Tooltip("ON: Hips の world position XZ も root の XZ に固定。横にずれない。")]
        [SerializeField] bool freezeHipsXZ = true;

        [Tooltip("ON: Hips の world position Y も初期値で固定。Y bob 完全消失。Run の上下揺れも止める。")]
        [SerializeField] bool freezeHipsY = true;

        [Header("(legacy) World upright (Hips のみ直立、他 bone は free)")]
        [Range(0f, 1f)]
        [Tooltip("Hips のみ world 直立に Slerp。upperBodyAxisFreeze=0 の時だけ意味あり。")]
        [SerializeField] float hipsWorldUprightLock = 0f;

        [Tooltip("ON にすると Y (yaw) も world で固定。デバッグ用、通常 OFF。")]
        [SerializeField] bool hipsLockYawToo;

        [Header("Body local lock (T-pose に寄せる — 0=動く / 1=完全固定)")]
        [Range(0f, 1f)]
        [Tooltip("Hips rotation 抑制 (local)。脚アニメに影響大、0〜0.3 推奨。worldUprightLock と併用可。")]
        [SerializeField] float hipsLock = 0f;

        [Range(0f, 1f)]
        [Tooltip("Spine 抑制。横揺れの主犯候補。0.5〜1.0 で揺れ抑制。")]
        [SerializeField] float spineLock = 0.7f;

        [Range(0f, 1f)]
        [Tooltip("Chest 抑制。0.5〜1.0 推奨。Spine と一緒にあげると上半身ピタ。")]
        [SerializeField] float chestLock = 0.7f;

        [Range(0f, 1f)]
        [Tooltip("UpperChest 抑制 (avatar に登録されてれば)。0.5〜1.0 推奨。")]
        [SerializeField] float upperChestLock = 0.5f;

        [Header("Arm lock (腕振り抑制 — 0=動く / 1=完全固定)")]
        [Range(0f, 1f)]
        [Tooltip("Shoulder の振り抑制。値大で肩が固定される。0.7〜1.0 推奨。")]
        [SerializeField] float shoulderLock = 0.8f;

        [Range(0f, 1f)]
        [Tooltip("UpperArm (二の腕) の振り抑制。0.5〜0.9 推奨。")]
        [SerializeField] float upperArmLock = 0.6f;

        [Range(0f, 1f)]
        [Tooltip("LowerArm (前腕) の振り抑制。0.3〜0.7 推奨。")]
        [SerializeField] float lowerArmLock = 0.4f;

        [Range(0f, 1f)]
        [Tooltip("Hand の振り抑制。Hand_L/R 球はこの bone の child なので最重要。0.5〜1.0 推奨。")]
        [SerializeField] float handLock = 0.7f;

        [Tooltip("ON: 全ての lock を 0 に強制 (anim まま)。デバッグ用。")]
        [SerializeField] bool bypass;

        Animator anim;
        Transform[] bones;       // [Hips, Spine, Chest, UpperChest, LShoulder, ..., RHand]
        Quaternion[] restRots;   // 同 index の T-pose localRotation
        Vector3 hipsRestLocalToRoot; // Hips の root からの相対位置 (Awake 時)
        bool initialized;

        static readonly HumanBodyBones[] BoneIndex = {
            HumanBodyBones.Hips, HumanBodyBones.Spine, HumanBodyBones.Chest, HumanBodyBones.UpperChest,
            HumanBodyBones.LeftShoulder,  HumanBodyBones.LeftUpperArm,  HumanBodyBones.LeftLowerArm,  HumanBodyBones.LeftHand,
            HumanBodyBones.RightShoulder, HumanBodyBones.RightUpperArm, HumanBodyBones.RightLowerArm, HumanBodyBones.RightHand
        };

        void Awake()
        {
            anim = GetComponent<Animator>();
            if (!anim.isHuman)
            {
                Debug.LogWarning("[EggybotArmLock] Animator が humanoid じゃない");
                return;
            }
            // bone reference だけ Awake で取得 (humanoid muscle 正規化完了前のため)
            bones = new Transform[BoneIndex.Length];
            restRots = new Quaternion[BoneIndex.Length];
            for (int i = 0; i < BoneIndex.Length; i++)
                bones[i] = anim.GetBoneTransform(BoneIndex[i]);
        }

        void Start()
        {
            if (bones == null) return; // Awake で humanoid チェック失敗
            // Start で restRots を記録 (Awake より後 = humanoid muscle 正規化が完了している想定)
            for (int i = 0; i < BoneIndex.Length; i++)
            {
                var b = bones[i];
                if (b != null) restRots[i] = b.localRotation;
            }
            // Hips の root 相対位置を記録 (PoseLock 補正後の値)
            if (bones[0] != null)
                hipsRestLocalToRoot = transform.InverseTransformPoint(bones[0].position);
            initialized = true;
        }

        void LateUpdate()
        {
            if (!initialized || bypass) return;

            // (1) Local lock: T-pose に Slerp
            float[] lockArr = {
                hipsLock, spineLock, chestLock, upperChestLock,
                shoulderLock, upperArmLock, lowerArmLock, handLock,
                shoulderLock, upperArmLock, lowerArmLock, handLock
            };
            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                if (b == null) continue;
                float k = Mathf.Clamp01(lockArr[i]);
                if (k <= 0f) continue;
                b.localRotation = Quaternion.Slerp(b.localRotation, restRots[i], k);
            }

            // (2) Axis freeze: 体軸と腕で別々に world rotation を root に揃える
            // 体軸 (i=0..3 = Hips/Spine/Chest/UpperChest) を固定すると、腕の親 chain も axis 固定される。
            // 各腕 bone の localRotation は anim で振られるので、world rotation は root.rot * arm.local
            // → 腕は **axis-locked body に対して相対的に振れる** ので自由に動く。
            // 脚 (UpperLeg/LowerLeg/Foot) は touch しないので走る動作はそのまま。
            var rootRotation = transform.rotation;
            float kBody = Mathf.Clamp01(bodyAxisFreeze);
            float kArm = Mathf.Clamp01(armAxisFreeze);
            for (int i = 0; i < bones.Length; i++)
            {
                var b = bones[i];
                if (b == null) continue;
                float k = (i < 4) ? kBody : kArm; // 0..3 = body, 4..11 = arms
                if (k <= 0f) continue;
                b.rotation = Quaternion.Slerp(b.rotation, rootRotation, k);
            }

            // (3) Hips position freeze (XZ / Y)
            if ((freezeHipsXZ || freezeHipsY) && bones[0] != null)
            {
                var hips = bones[0];
                var targetWorld = transform.TransformPoint(hipsRestLocalToRoot);
                var p = hips.position;
                if (freezeHipsXZ) { p.x = targetWorld.x; p.z = targetWorld.z; }
                if (freezeHipsY) p.y = targetWorld.y;
                hips.position = p;
            }

            // (4) (legacy) Hips のみ world 直立 (bodyAxisFreeze=0 の時の代替)
            if (bodyAxisFreeze <= 0f && hipsWorldUprightLock > 0f)
            {
                var hips = bones[0];
                if (hips != null)
                {
                    var rootYaw = transform.eulerAngles.y;
                    var uprightRot = hipsLockYawToo
                        ? Quaternion.Euler(0f, rootYaw, 0f)
                        : Quaternion.Euler(0f, hips.eulerAngles.y, 0f);
                    hips.rotation = Quaternion.Slerp(hips.rotation, uprightRot, Mathf.Clamp01(hipsWorldUprightLock));
                }
            }
        }

        /// <summary>外部から rest を再キャプチャ (Edit pose を変えた後に呼ぶ)。</summary>
        [ContextMenu("Capture Rest Rotations")]
        public void CaptureRest()
        {
            if (anim == null) anim = GetComponent<Animator>();
            for (int i = 0; i < BoneIndex.Length; i++)
            {
                var b = anim.GetBoneTransform(BoneIndex[i]);
                if (b != null) restRots[i] = b.localRotation;
            }
            Debug.Log("[EggybotArmLock] rest rotations captured");
        }
    }
}
