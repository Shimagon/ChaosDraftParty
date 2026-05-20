using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT 用 表情アニメ v2 — Mesh Swap 方式 (Mario Odyssey / Animal Crossing defacto)。
    ///
    /// v1 失敗の真因 (2026-05-19 User フィードバック):
    /// - localScale.Y = 0 で blink → 3D 凸 mesh が板化して目が「消えた」感
    /// - localScale.Y = 1.8 で「Oh」口 → decal mesh の pivot offset で「下に伸びるだけ」
    /// 「scale で消す / decal を伸ばす」は AAA defacto で禁忌、心理学的にも違和感大。
    ///
    /// v2 方針 (並列 agent 3 個 + 心理学 + ゲーム理論調査):
    /// - **Mesh Swap (Mario Odyssey defacto)**: 表情ごとに別 GO を SetActive 切替
    ///   - Eye: 開き mesh (Eye_L/R、既存) ↔ 閉じ mesh (EyeClosed_L/R、横棒 quad、新規)
    ///   - Mouth: shape enum で Dictionary 管理、SetActive 切替
    /// - **心理学的 defacto** (Lorenz Kindchenschema / Disney Research / Shawn Kelly ILM):
    ///   - blink hold 60-100ms (microexpression range)、周期 3-5s ランダム、15% で double blink
    ///   - **完全消失 NG** → 横棒 quad で「閉じた目印」表現
    ///   - Falling 中は blink 抑制 (恐怖中の人間生理)
    /// - **「Oh!」 縦長 ovaloid** (Cylinder 円板で代用、Mario Odyssey "Oh" surprise defacto)
    ///   - 横長は「叫び」「悲鳴」感、縦長が「びっくり」 (cute design 文献)
    /// - **頬 emissive** (Animal Crossing 系): MaterialPropertyBlock で HeadMesh のみ操作
    ///   - Surprised +0.3 / Falling -0.2 (青ざめ)
    /// - **Anticipation blink** (Disney "Tangled"): Jump 踏切で PlayBlink() を外部から呼出
    /// </summary>
    [DefaultExecutionOrder(80)] // PlayerController(50) → Squash(70) → 自分(80) → Crouch(100)
    public class EggybotFaceAnimator : MonoBehaviour
    {
        public enum Expression { Neutral, Surprised, KO, Happy, Hit }
        public enum MouthShape { Smile, Open, Sad, Happy, KO }

        [Header("Eye Swap (open ↔ closed)")]
        [Tooltip("既存 Eye_L mesh GO (開いた目)。SetActive 切替で blink。")]
        [SerializeField] GameObject eyeL_Open;

        [Tooltip("既存 Eye_R mesh GO。")]
        [SerializeField] GameObject eyeR_Open;

        [Tooltip("新規 EyeClosed_L (横棒 quad、閉じた目)。SetActive(false) default、blink で切替。")]
        [SerializeField] GameObject eyeL_Closed;

        [Tooltip("新規 EyeClosed_R。")]
        [SerializeField] GameObject eyeR_Closed;

        [Header("Mouth Swap (shape enum で切替)")]
        [Tooltip("既存 Mouth (Smile = U 字スマイル decal)。default 表示。")]
        [SerializeField] GameObject mouthSmile;

        [Tooltip("新規 Mouth_Open (Cylinder 円板、Oh 字)。落下中 / Surprise で SetActive。")]
        [SerializeField] GameObject mouthOpen;

        [Tooltip("悲しい・無表情口 (Cube 横棒、5×0.8×0.5cm)。SetExpression(Hit) で表示。")]
        [SerializeField] GameObject mouthSad;

        [Tooltip("勝利・大笑顔 (Cylinder 大円板、7×0.3×10cm)。SetExpression(Happy) で表示。")]
        [SerializeField] GameObject mouthHappy;

        [Tooltip("KO・倒れた表情 (× 字、2 Cube 交差)。SetExpression(KO) で表示。")]
        [SerializeField] GameObject mouthKO;

        [Header("Detect (落下 / 着地)")]
        [Tooltip("落下検出用 PlayerController。null で fall expression 無効。")]
        [SerializeField] PlayerController player;

        [Tooltip("落下中口開けを有効化。OFF で常時 Smile。")]
        [SerializeField] bool fallExpressionEnabled = true;

        [Tooltip("空中時間がこれを超えると口を開ける (秒)。0.3 推奨 (ジャンプ頂点 ~0.25s 直後の誤発火を防ぐ)。")]
        [Range(0.05f, 1f)]
        [SerializeField] float fallHysteresisSec = 0.3f;

        [Tooltip("落下発火 する垂直速度 (m/s、負値)。-3 ぐらい以下で発火、Agent A defacto は -8。")]
        [SerializeField] float fallVelocityThreshold = -3f;

        [Tooltip("落下中 blink を抑制する (恐怖中は blink しない、Agent A 心理学)。")]
        [SerializeField] bool suppressBlinkOnFall = true;

        [Header("Blink (Eye scale tween、Disney Research asymmetric defacto)")]
        [Tooltip("自動 blink を有効化。OFF で blink 完全停止。")]
        [SerializeField] bool autoBlink = true;

        [Tooltip("blink 周期 (秒、min-max ランダム)。4-6.5s 推奨 (人間 average + 活発感)。")]
        [SerializeField] Vector2 blinkInterval = new Vector2(4f, 6.5f);

        [Tooltip("目を閉じるのにかける時間 (秒)。User 2026-05-19「ゆっくり閉じる感」要求で smoothstep tween。0.10 推奨 (Disney close 70-100ms)。")]
        [Range(0.04f, 0.3f)]
        [SerializeField] float blinkCloseTime = 0.10f;

        [Tooltip("閉じた状態の hold (秒)。0.08 推奨 (microexpression range)。")]
        [Range(0.02f, 0.3f)]
        [SerializeField] float blinkHoldTime = 0.08f;

        [Tooltip("目を開くのにかける時間 (秒)。close の 1.5-2x が natural (Disney asymmetric defacto)。0.18 推奨。")]
        [Range(0.04f, 0.4f)]
        [SerializeField] float blinkOpenTime = 0.18f;

        [Tooltip("blink 閉時の scale Z 倍率 (mesh local Z = world Y、縦方向)。0.05 推奨。" +
                 "Eye_L mesh は rotation X -90° なので scale.Y は world Z (前後)、scale.Z が world Y (縦) に対応。" +
                 "縦に潰すと「閉じた目」感が確実に出る。0 NG (mesh 消失)。")]
        [Range(0f, 0.5f)]
        [SerializeField] float blinkClosedY = 0.05f;

        [Tooltip("double blink の発生確率 (0..1)。0.15-0.25 推奨 (Shawn Kelly ILM の人間味)。")]
        [Range(0f, 0.5f)]
        [SerializeField] float doubleBlinkChance = 0.20f;

        [Tooltip("double blink 間の interval (秒)。0.15 推奨。")]
        [SerializeField] float doubleBlinkGap = 0.15f;

        [Header("頬 emissive (Animal Crossing defacto)")]
        [Tooltip("頬 emissive を有効化。HeadMesh の emission に加算。")]
        [SerializeField] bool cheekEmissiveEnabled = true;

        [Tooltip("Surprised / 着地時の emit boost。0.3 推奨。")]
        [SerializeField] float emissiveSurprise = 0.3f;

        [Tooltip("Falling 中の emit decrease (青ざめ表現)。-0.2 推奨 (負値で減衰)。")]
        [SerializeField] float emissiveFalling = -0.2f;

        [Tooltip("emissive 復元時間 (秒)。0.5 推奨。")]
        [SerializeField] float emissiveRestoreTime = 0.5f;

        [Tooltip("HeadMesh Renderer (cheek emissive 操作対象)。null で emissive 無効。")]
        [SerializeField] Renderer headMeshRenderer;

        public Expression CurrentExpression { get; private set; } = Expression.Neutral;
        public bool IsBlinking { get; private set; }

        Dictionary<MouthShape, GameObject> mouthMap;
        MouthShape currentMouth = MouthShape.Smile;
        Coroutine blinkLoop;
        float airborneTime;
        bool fallExpressionActive;
        MaterialPropertyBlock mpb;
        Color baseEmissive;
        float currentEmissiveOffset;
        Coroutine emissiveRoutine;
        Vector3 eyeLBase, eyeRBase;
        Vector3 eyeLBasePos, eyeRBasePos;
        float eyeMeshBoundsCenterZ; // mesh pivot offset 補正用 (Eye_L/R 共通値 0.6845 想定)
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            mouthMap = new Dictionary<MouthShape, GameObject>
            {
                { MouthShape.Smile, mouthSmile },
                { MouthShape.Open,  mouthOpen  },
                { MouthShape.Sad,   mouthSad   },
                { MouthShape.Happy, mouthHappy },
                { MouthShape.KO,    mouthKO    },
            };

            if (headMeshRenderer != null && headMeshRenderer.sharedMaterial != null)
            {
                if (headMeshRenderer.sharedMaterial.HasProperty(EmissionColorId))
                    baseEmissive = headMeshRenderer.sharedMaterial.GetColor(EmissionColorId);
                mpb = new MaterialPropertyBlock();
            }

            // Eye の base scale / position / mesh bounds を記録 (blink で scale.Z 縮 + 位置補正に使用)
            // mesh.bounds.center.z = 0.6845 (Eye mesh の頂点 origin オフセット)、scale で縮むと world -Y に動くため
            // localPosition.Y を + offset 加算で補正して visible 中心を維持する。
            if (eyeL_Open != null)
            {
                eyeLBase = eyeL_Open.transform.localScale;
                eyeLBasePos = eyeL_Open.transform.localPosition;
                var mfL = eyeL_Open.GetComponent<MeshFilter>();
                if (mfL != null && mfL.sharedMesh != null)
                    eyeMeshBoundsCenterZ = mfL.sharedMesh.bounds.center.z;
            }
            if (eyeR_Open != null)
            {
                eyeRBase = eyeR_Open.transform.localScale;
                eyeRBasePos = eyeR_Open.transform.localPosition;
            }

            // 初期状態: 開いた目、Smile 口、emissive 復元、EyeClosed は KO/sleepy 用に保留 (blink では使わない)
            SetEyeYScale(1f);
            if (eyeL_Closed != null) eyeL_Closed.SetActive(false);
            if (eyeR_Closed != null) eyeR_Closed.SetActive(false);
            SetMouth(MouthShape.Smile);
            currentMouth = MouthShape.Smile;
        }

        void OnEnable()
        {
            if (autoBlink && (eyeL_Open != null || eyeR_Open != null))
                blinkLoop = StartCoroutine(BlinkLoopRoutine());
        }

        void OnDisable()
        {
            if (blinkLoop != null) { StopCoroutine(blinkLoop); blinkLoop = null; }
            if (emissiveRoutine != null) { StopCoroutine(emissiveRoutine); emissiveRoutine = null; }
            SetEyeYScale(1f); // base scale に復元 (前回失敗回避、完全 0 にしない)
            if (eyeL_Closed != null) eyeL_Closed.SetActive(false);
            if (eyeR_Closed != null) eyeR_Closed.SetActive(false);
            SetMouth(MouthShape.Smile);
            ApplyEmissiveOffset(0f);
            airborneTime = 0f;
            fallExpressionActive = false;
        }

        void Update()
        {
            if (!fallExpressionEnabled || player == null) return;

            // 着地判定 + 空中時間 tracking
            if (player.IsGrounded)
            {
                airborneTime = 0f;
                if (fallExpressionActive)
                {
                    fallExpressionActive = false;
                    SetMouth(MouthShape.Smile);
                    StartEmissiveTo(0f);
                }
                return;
            }

            // 空中: hysteresis 超え + 落下速度判定で発火
            airborneTime += Time.deltaTime;
            float vy = player.VerticalVelocity;
            if (!fallExpressionActive
                && airborneTime >= fallHysteresisSec
                && vy <= fallVelocityThreshold)
            {
                fallExpressionActive = true;
                SetMouth(MouthShape.Open);
                StartEmissiveTo(emissiveFalling); // 青ざめ
            }
        }

        // =====================================================================
        //  Blink
        // =====================================================================
        IEnumerator BlinkLoopRoutine()
        {
            while (autoBlink)
            {
                float wait = Random.Range(blinkInterval.x, blinkInterval.y);
                yield return new WaitForSeconds(wait);

                // 落下中は blink 抑制 (Agent A 心理学: 恐怖中の人間は blink しない)
                if (suppressBlinkOnFall && fallExpressionActive) continue;

                yield return BlinkOnce();
                if (Random.value < doubleBlinkChance)
                {
                    yield return new WaitForSeconds(doubleBlinkGap);
                    yield return BlinkOnce();
                }
            }
        }

        IEnumerator BlinkOnce()
        {
            IsBlinking = true;

            // Phase 1 Close: Eye の **localScale.Z** (= world Y 縦方向) を 1 → blinkClosedY (0.05) へ smoothstep
            // 「ゆっくり縦に閉じる」動き。前回失敗 (scale.Y) は mesh rotation X -90° で world Z (前後) を潰してた、根本修正。
            float t = 0f;
            while (t < blinkCloseTime)
            {
                float u = t / blinkCloseTime;
                float s = u * u * (3f - 2f * u);
                SetEyeYScale(Mathf.Lerp(1f, blinkClosedY, s));
                t += Time.deltaTime;
                yield return null;
            }
            SetEyeYScale(blinkClosedY);

            // Phase 2 Hold: scale Z=0.05 のまま hold (細い横線で「閉じた目」表現)
            // EyeClosed (∪字 cube) は使わない (Transform 配置が顔前面に出ないため M3 で sleepy 等に保留)
            if (blinkHoldTime > 0f)
                yield return new WaitForSeconds(blinkHoldTime);

            // Phase 3 Open: close の 1.5-2x 時間でゆっくり開く (Disney asymmetric defacto)
            t = 0f;
            while (t < blinkOpenTime)
            {
                float u = t / blinkOpenTime;
                float s = u * u * (3f - 2f * u);
                SetEyeYScale(Mathf.Lerp(blinkClosedY, 1f, s));
                t += Time.deltaTime;
                yield return null;
            }
            SetEyeYScale(1f);

            IsBlinking = false;
        }

        /// <summary>外部 API: 手動 blink (Jump 踏切 / Hit / Anticipation 用、Disney "Tangled" defacto)。</summary>
        public void PlayBlink()
        {
            // suppress 中でも明示 PlayBlink は通す (anticipation 等は意図的発火)
            StartCoroutine(BlinkOnce());
        }

        /// <summary>Eye_L/R を「縦に閉じる」変形 (scale.Z + position 補正で pivot offset 打ち消し)。</summary>
        /// <remarks>
        /// Eye_L mesh は Transform origin から bounds.center.z=0.6845 オフセット。scale.Z 縮で
        /// visible 中心が Transform origin に向かって動き、rotation X -90° で world -Y (下) に「目が下がる」現象。
        /// localPosition.Y を + displacement 加算で打ち消し、visible 中心を base 位置に維持する。
        /// User 2026-05-19「目が下に行く」フィードバックの根本修正。関数名は互換性維持。
        /// </remarks>
        void SetEyeYScale(float zMul)
        {
            // scale.Z 縮で生じる mesh-local Z displacement (visible 中心が動く距離)
            float displacement = eyeMeshBoundsCenterZ * (1f - zMul);
            // rotation X -90° により mesh-local +Z = world -Y、補正は localPosition.Y に + displacement
            if (eyeL_Open != null)
            {
                eyeL_Open.transform.localScale = new Vector3(eyeLBase.x, eyeLBase.y, eyeLBase.z * zMul);
                eyeL_Open.transform.localPosition = new Vector3(eyeLBasePos.x, eyeLBasePos.y + displacement, eyeLBasePos.z);
            }
            if (eyeR_Open != null)
            {
                eyeR_Open.transform.localScale = new Vector3(eyeRBase.x, eyeRBase.y, eyeRBase.z * zMul);
                eyeR_Open.transform.localPosition = new Vector3(eyeRBasePos.x, eyeRBasePos.y + displacement, eyeRBasePos.z);
            }
        }

        // =====================================================================
        //  Mouth
        // =====================================================================
        void SetMouth(MouthShape s)
        {
            foreach (var kv in mouthMap)
                if (kv.Value != null) kv.Value.SetActive(kv.Key == s);
            currentMouth = s;
        }

        // =====================================================================
        //  Expression API
        // =====================================================================
        /// <summary>外部 API: 表情切替 (M3 で texture/eye 表情も追加予定)。</summary>
        public void SetExpression(Expression e)
        {
            CurrentExpression = e;
            switch (e)
            {
                case Expression.Surprised:
                    SetMouth(MouthShape.Open);
                    StartEmissiveTo(emissiveSurprise);
                    PlayBlink();
                    break;
                case Expression.KO:
                    SetMouth(MouthShape.KO);
                    StartEmissiveTo(0f);
                    break;
                case Expression.Happy:
                    SetMouth(MouthShape.Happy);
                    StartEmissiveTo(emissiveSurprise * 0.5f);
                    break;
                case Expression.Hit:
                    SetMouth(MouthShape.Sad);
                    PlayBlink();
                    break;
                default: // Neutral
                    SetMouth(MouthShape.Smile);
                    StartEmissiveTo(0f);
                    break;
            }
        }

        // =====================================================================
        //  Cheek emissive (Animal Crossing defacto)
        // =====================================================================
        void StartEmissiveTo(float target)
        {
            if (!cheekEmissiveEnabled || headMeshRenderer == null || mpb == null) return;
            if (emissiveRoutine != null) StopCoroutine(emissiveRoutine);
            emissiveRoutine = StartCoroutine(EmissiveLerp(target));
        }

        IEnumerator EmissiveLerp(float target)
        {
            float from = currentEmissiveOffset;
            float t = 0f;
            while (t < emissiveRestoreTime)
            {
                float u = t / emissiveRestoreTime;
                float s = u * u * (3f - 2f * u);
                ApplyEmissiveOffset(Mathf.Lerp(from, target, s));
                t += Time.deltaTime;
                yield return null;
            }
            ApplyEmissiveOffset(target);
            emissiveRoutine = null;
        }

        void ApplyEmissiveOffset(float offset)
        {
            currentEmissiveOffset = offset;
            if (headMeshRenderer == null || mpb == null) return;
            headMeshRenderer.GetPropertyBlock(mpb);
            // base emissive (HeadMatte の場合通常黒) に offset を加算。負値で「青ざめ」(暗くする) のため Color clamp
            float r = Mathf.Max(0f, baseEmissive.r + offset);
            float g = Mathf.Max(0f, baseEmissive.g + offset);
            float b = Mathf.Max(0f, baseEmissive.b + offset);
            mpb.SetColor(EmissionColorId, new Color(r, g, b, 1f));
            headMeshRenderer.SetPropertyBlock(mpb);
        }
    }
}
