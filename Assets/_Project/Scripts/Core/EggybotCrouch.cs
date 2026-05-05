using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT のしゃがみ動作: FaceVisual GameObject の localPosition.y を Lerp で下げて
    /// 顔 (HeadMesh + Eye + Mouth) を bowl 内部に沈める。
    ///
    /// 設計 (2026-05-04 user 提案):
    /// - mixamorig:Head bone の child に HeadAnchor (empty + SphereCollider 当たり判定用、固定)
    /// - HeadAnchor の child に FaceVisual (empty、これが crouch で上下する)
    /// - FaceVisual の child に 4 つの face mesh (MeshRenderer 化済)
    /// - crouch = FaceVisual.localPosition.y を 0 (立ち) ↔ -crouchDepth (収納) で Lerp
    /// - HeadAnchor は Head bone 追従、SphereCollider は常に頭位置に残る (game logic 用)
    ///
    /// なぜこの設計か:
    /// - face mesh は元々 Hips bone で skinning されてた → Head bone 動かしても visual 動かなかった
    /// - 静的 Mesh 化 + GameObject 階層で動かす方が確実
    /// </summary>
    public class EggybotCrouch : MonoBehaviour
    {
        [Tooltip("crouch 動作で上下動する FaceVisual Transform。HeadAnchor の child。Inspector で割当。")]
        [SerializeField] Transform faceVisual;

        [Tooltip("crouch 時に FaceVisual を下げる量 (m)。0.60 で頭が完全に bowl 内に沈む。")]
        [SerializeField] float crouchDepth = 0.60f;

        [Tooltip("立ち↔しゃがみの Lerp 速度。値が大きいほど速く切り替わる。8 推奨。")]
        [SerializeField] float lerpSpeed = 8f;

        [Tooltip("Inspector toggle でテスト。Play 中に切替可。M2 でスキル / ノックアウトに繋ぐ。")]
        [SerializeField] public bool isCrouching;

        [Header("CharacterController resize (crouch 時に縮める)")]
        [Tooltip("crouch 時の CC height 比率。0.6 = 60% で半分しゃがみ。")]
        [Range(0.3f, 1.0f)]
        [SerializeField] float crouchHeightRatio = 0.6f;

        [Tooltip("crouch 立ち上がり時、頭上に天井があるかチェック。塞がれてたら stand up を block。")]
        [SerializeField] bool checkCeilingOnStandUp = true;

        [Tooltip("天井検査用 layer (player レイヤーは除外、weapon 装備等も)。")]
        [SerializeField] LayerMask ceilingMask = ~0;

        [Header("Bonk reaction (天井に頭ぶつけ演出)")]
        [Tooltip("天井 block 時に蓋を一瞬開いて閉じる reaction。Hat in Time / Mario の頭潰れ風。")]
        [SerializeField] bool enableBonkReaction = true;

        [Tooltip("BonkSequence の総時間 (秒)。0.35 推奨。")]
        [SerializeField] float bonkDuration = 0.35f;

        [Tooltip("Bonk overshoot 角度 (度)。蓋が一瞬開く量、35 推奨。")]
        [SerializeField] float bonkOpenAngle = 35f;

        [Tooltip("連打 spam 防止 cooldown (秒)。")]
        [SerializeField] float bonkCooldown = 0.3f;

        [Tooltip("Bonk 効果音 (任意、AudioSource 経由再生)。")]
        [SerializeField] AudioSource bonkSfx;

        [Header("Camera target follow (crouch 中に target 高さも下げる)")]
        [Tooltip("Cinemachine の Follow / LookAt 対象 Transform。crouch state に応じて localPosition.y を SmoothDamp。")]
        [SerializeField] Transform cameraTarget;

        [Tooltip("立ち時の cameraTarget.localPosition.y。0.5 推奨 (胸位置)。")]
        [SerializeField] float cameraTargetStandY = 0.5f;

        [Tooltip("crouch 時の cameraTarget.localPosition.y。0.25 推奨 (しゃがみ胸位置)。")]
        [SerializeField] float cameraTargetCrouchY = 0.25f;

        [Tooltip("camera target Y SmoothDamp time (秒)。0.12 推奨 (crouch アニメと揃える)。")]
        [SerializeField] float cameraTargetDampTime = 0.12f;

        public bool IsCrouching => isCrouching;
        public Transform FaceVisual => faceVisual;
        public float GetCrouchDepth() => crouchDepth;

        // Sequence script からの外部制御中は lerp を停止 (ぴょんッ→スポン アニメ用)
        public bool ExternalControl { get; set; }

        float currentY;
        bool synced;

        // CharacterController resize 用 cache (Awake 時の stand 値)
        UnityEngine.CharacterController cachedCC;
        float standHeight;
        float standCenterY;
        float currentCCHeight;
        float currentCCCenterY;
        bool ccCached;

        // CameraTarget Y SmoothDamp 用
        float cameraTargetVelocity;

        void Awake()
        {
#if UNITY_EDITOR
            // Editor の Game view 非フォーカス時でも Editor playerLoop を動かす
            // (ビルド後に副作用が残らないよう Editor only)
            Application.runInBackground = true;
#endif
            cachedCC = GetComponent<UnityEngine.CharacterController>();
            if (cachedCC != null)
            {
                standHeight = cachedCC.height;
                standCenterY = cachedCC.center.y;
                currentCCHeight = standHeight;
                currentCCCenterY = standCenterY;
                ccCached = true;
            }
        }

        /// <summary>立ち上がり可能か (頭上に物体が無いか) を SphereCast で判定。塞がれてたら false。</summary>
        public bool CanStandUp()
        {
            if (!checkCeilingOnStandUp || cachedCC == null) return true;
            float curTop = transform.position.y + cachedCC.center.y + cachedCC.height * 0.5f;
            float needed = standHeight - cachedCC.height; // 不足分
            if (needed <= 0.001f) return true;

            // SphereCast origin を sphere radius 分下げて、初期 overlap (天井がぴったり頭上にある時の検出漏れ) を回避。
            // 球の TOP = curTop で開始 → 距離 needed 分上に cast → 球の TOP = standTop に到達。
            float r = cachedCC.radius * 0.95f;
            var origin = new Vector3(transform.position.x, curTop - r, transform.position.z);
            return !UnityEngine.Physics.SphereCast(origin, r, Vector3.up, out _, needed, ceilingMask, UnityEngine.QueryTriggerInteraction.Ignore);
        }

        void LateUpdate()
        {
            if (faceVisual == null) return;
            // 初回 LateUpdate で scene 保存値に同期 (Play 開始時の snap 防止)
            if (!synced)
            {
                currentY = faceVisual.localPosition.y;
                synced = true;
            }
            // Sequence からの外部制御中は lerp 停止 (ぴょんッ アニメ等)
            if (ExternalControl)
            {
                currentY = faceVisual.localPosition.y; // 外部値を吸収して制御戻り時 snap しない
                return;
            }
            float targetY = isCrouching ? -crouchDepth : 0f;
            currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * lerpSpeed);
            var p = faceVisual.localPosition;
            p.y = currentY;
            faceVisual.localPosition = p;

            // CharacterController height/center を crouch 状態に合わせて Lerp
            // 足元固定 (center.y = standCenterY - (standHeight - height)/2) で接地維持
            if (ccCached && cachedCC != null)
            {
                float targetH = isCrouching ? standHeight * crouchHeightRatio : standHeight;
                float targetC = standCenterY - (standHeight - targetH) * 0.5f;
                currentCCHeight = Mathf.Lerp(currentCCHeight, targetH, Time.deltaTime * lerpSpeed);
                currentCCCenterY = Mathf.Lerp(currentCCCenterY, targetC, Time.deltaTime * lerpSpeed);
                cachedCC.height = currentCCHeight;
                var c = cachedCC.center;
                c.y = currentCCCenterY;
                cachedCC.center = c;
            }

            // CameraTarget の Y を crouch state に合わせて SmoothDamp
            if (cameraTarget != null)
            {
                float targetCamY = isCrouching ? cameraTargetCrouchY : cameraTargetStandY;
                var lp = cameraTarget.localPosition;
                lp.y = Mathf.SmoothDamp(lp.y, targetCamY, ref cameraTargetVelocity, cameraTargetDampTime);
                cameraTarget.localPosition = lp;
            }
        }

        public void Crouch() => isCrouching = true;
        /// <summary>立ち上がり要求。天井で塞がれてたら crouch 維持 + Bonk 演出。</summary>
        public void StandUp()
        {
            if (CanStandUp()) { isCrouching = false; return; }
            if (enableBonkReaction) PlayBonk();
        }
        public void Toggle() { if (isCrouching) StandUp(); else isCrouching = true; }

        float lastBonkTime = -10f;

        /// <summary>天井に頭ぶつけ演出: 蓋 (capsule の UpperLid) を一瞬開いて閉じる + SE。crouch 維持。
        /// 外部 (PlayerController 等) からも呼べるよう public。</summary>
        public void PlayBonk()
        {
            if (Time.time - lastBonkTime < bonkCooldown) return;
            lastBonkTime = Time.time;
            var capsule = GetComponent<EggybotCapsule>();
            if (capsule == null || capsule.UpperLid == null) return;
            if (bonkSfx != null) bonkSfx.Play();
            StartCoroutine(BonkSequence(capsule));
        }

        System.Collections.IEnumerator BonkSequence(EggybotCapsule capsule)
        {
            capsule.ExternalControl = true;
            var lid = capsule.UpperLid;
            var closedRot = capsule.GetClosedLocalRot();
            var bonkRot = closedRot * Quaternion.Euler(bonkOpenAngle, 0f, 0f);
            float t = 0f;
            while (t < bonkDuration)
            {
                t += Time.deltaTime;
                float k = t / bonkDuration;
                // sin 半周で overshoot → close (1 往復、減衰)
                float u = Mathf.Sin(k * Mathf.PI) * (1f - k); // 0→peak→0、減衰付き
                lid.localRotation = Quaternion.Slerp(closedRot, bonkRot, u);
                yield return null;
            }
            lid.localRotation = closedRot;
            capsule.ExternalControl = false;
        }
    }
}
