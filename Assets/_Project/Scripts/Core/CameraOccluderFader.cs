using System.Collections.Generic;
using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// 業界 defacto: Camera と Player の間にある障害物を **screen-door dither** で網戸状に透過させる。
    ///
    /// 仕組み (Genshin / FF14 / モンハン / Hat in Time の defacto):
    /// 1. LateUpdate で Camera→Player 方向に SphereCast → hit Renderer を集める
    /// 2. 各 Renderer に「dither shader を使う + 元 material の見た目 (_BaseMap/_BaseColor/_BumpMap/_Metallic/_Smoothness 等) を copy した instance material」を swap
    /// 3. instance の `_DitherAmount` (0=不透明、1=最大網戸) を <see cref="UnityEngine.Mathf.SmoothDamp"/> で補間
    /// 4. fade=0 復帰で元 sharedMaterials 戻し + instance Destroy (GC リーク防止)
    ///
    /// なぜ dither (網戸) ?
    /// - **元 material の色 / texture / 法線が完全保持される** (案 A=Transparent swap だと白支配)
    /// - **Z 書込・影が壊れない** (Opaque pass のまま、Alpha Clip で discard するだけ)
    /// - **SRP Batcher 維持** (各 instance に直書きで MPB 不要)
    /// - 近づくと点描パターンが見えるが、これがカートゥーン調 (EGGYBOT) と相性◎
    ///
    /// パラメータ業界標準:
    /// - fade out (透過): ~0.10s (素早く隠す)
    /// - fade in (戻り): ~0.25s (チラつき防止で遅め)
    /// - hold (hysteresis): 0.10s
    /// - max dither: 0.70-0.85 (1.0 で完全消失すると酔うので網戸残し)
    /// </summary>
    [DefaultExecutionOrder(60)]
    public class CameraOccluderFader : MonoBehaviour
    {
        [Header("Targets")]
        [Tooltip("追跡対象 (player の体中央、または CameraTarget)。")]
        [SerializeField] Transform target;

        [Tooltip("カメラ Transform (CinemachineBrain 付き Main Camera 推奨、null=Camera.main)。")]
        [SerializeField] Transform cameraTransform;

        [Header("Detection")]
        [Tooltip("障害物 layer mask。OcclusionFade レイヤーのみ推奨。")]
        [SerializeField] LayerMask occlusionMask = ~0;

        [Tooltip("SphereCast の半径 (m)。0.6 推奨 (経路がギリギリ天井をかすめても安定 hit する余裕)。0.3 だと毎フレ hit/no-hit を振動して dither が出ない。")]
        [SerializeField] float sphereRadius = 0.6f;

        [Tooltip("camera→target の距離より少し短くして自分自身は当てない。0.2 推奨 (0.5 だと 0.7m など低い天井に届かないことがある)。")]
        [SerializeField] float distanceShorten = 0.2f;

        [Tooltip("SphereCast 終点の Y offset (m)。target が胸位置 (Y=0.5) だと camera-target 線が天井の下を通るので、+1.0 で player 頭上を通すと天井検出する。1.0 推奨。")]
        [SerializeField] float targetUpOffset = 1.0f;

        [Tooltip("最大同時 fade 数。SphereCastNonAlloc バッファサイズ。8 で十分。")]
        [SerializeField] int maxOccluders = 8;

        [Header("Dither Material")]
        [Tooltip("OcclusionDither shader を使う material。元 material の _BaseMap/_BaseColor 等は runtime で copy する。Inspector で OcclusionDither.mat を割当。")]
        [SerializeField] Material ditherMaterial;

        [Tooltip("最大 dither 量 (0=不透明、1=完全消失)。0.75 推奨 (酔い防止で網戸残し)。")]
        [Range(0f, 1f)]
        [SerializeField] float maxDither = 0.75f;

        [Header("Timing — Anticipation Tick + Smooth Tail (Genshin / Hat in Time defacto)")]
        [Tooltip("入り瞬間の即時表示比率 (0..1)。0.3 = 即時 maxDither × 30% 表示で「精度感」、その後 SmoothDamp で maxDither まで「ぬるっと」。0 だと遅延感、1 だとパッと感。0.3 推奨。")]
        [Range(0f, 1f)]
        [SerializeField] float anticipationStart = 0.3f;

        [Tooltip("透過 SmoothDamp 時間 (秒)。anticipationStart から maxDither まで滑らか上昇。0.15 推奨。")]
        [SerializeField] float fadeOutTime = 0.15f;

        [Tooltip("不透明復帰までの時間 (秒)。AAA Cinemachine Deoccluder defacto は出 = 入り × 2。0.30 推奨。")]
        [SerializeField] float fadeInTime = 0.30f;

        [Tooltip("hit 解除後の hysteresis 保持時間 (秒)。チラつき防止。0.25 推奨 (短いと SphereCast 振動で dither が消える)。")]
        [SerializeField] float holdTime = 0.25f;

        // per-renderer state
        class FadeState
        {
            public float ditherAmount;          // 0=opaque, 1=full screen-door
            public float velocity;               // SmoothDamp ref
            public float lastHitTime;            // hysteresis 用
            public Material[] originals;         // restore 用 (sharedMaterials snapshot)
            public Material[] ditherInstances;   // 各 submesh 毎の dither material instance (Destroy 対象)
            public bool swapped;
        }

        readonly Dictionary<Renderer, FadeState> states = new();
        readonly HashSet<Renderer> hitThisFrame = new();
        readonly List<Renderer> removeList = new();
        RaycastHit[] hitBuffer;

        // Property ID cache
        static readonly int DitherAmountId = Shader.PropertyToID("_DitherAmount");
        static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");
        static readonly int BumpMapId = Shader.PropertyToID("_BumpMap");
        static readonly int BumpScaleId = Shader.PropertyToID("_BumpScale");
        static readonly int MetallicId = Shader.PropertyToID("_Metallic");
        static readonly int SmoothnessId = Shader.PropertyToID("_Smoothness");
        static readonly int OcclusionMapId = Shader.PropertyToID("_OcclusionMap");
        static readonly int OcclusionStrengthId = Shader.PropertyToID("_OcclusionStrength");
        static readonly int EmissionMapId = Shader.PropertyToID("_EmissionMap");
        static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        void Awake()
        {
            hitBuffer = new RaycastHit[maxOccluders];
            if (cameraTransform == null && Camera.main != null) cameraTransform = Camera.main.transform;
        }

        void LateUpdate()
        {
            if (target == null || cameraTransform == null || ditherMaterial == null) return;
            hitThisFrame.Clear();

            // SphereCast 終点を player 頭上に上げる (camera-target 線が天井を通るように)
            // target=CameraTarget は胸位置 (Y=0.5) なので、+targetUpOffset で頭上にして天井検出する
            var aimPoint = target.position + Vector3.up * targetUpOffset;
            var diff = aimPoint - cameraTransform.position;
            float dist = diff.magnitude - distanceShorten;
            if (dist > 0.05f)
            {
                int count = Physics.SphereCastNonAlloc(cameraTransform.position, sphereRadius, diff.normalized, hitBuffer, dist, occlusionMask, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    var rend = hitBuffer[i].collider.GetComponentInParent<Renderer>();
                    if (rend == null) continue;
                    hitThisFrame.Add(rend);
                    if (!states.ContainsKey(rend))
                    {
                        // Anticipation Tick + Smooth Tail (Hat in Time / Genshin defacto):
                        // 入り瞬間に anticipationStart の比率で即時表示 (精度感)、
                        // その後 SmoothDamp で maxDither まで「ぬるっと」上昇 (滑らか感)。
                        states[rend] = new FadeState { ditherAmount = maxDither * anticipationStart };
                    }
                    states[rend].lastHitTime = Time.time;
                }
            }


            // 全 state を update
            removeList.Clear();
            foreach (var kv in states)
            {
                var rend = kv.Key;
                var st = kv.Value;
                if (rend == null) { removeList.Add(rend); continue; }

                bool active = hitThisFrame.Contains(rend) || (Time.time - st.lastHitTime) < holdTime;
                float targetDither = active ? maxDither : 0f;
                float smoothTime = active ? fadeOutTime : fadeInTime;
                st.ditherAmount = Mathf.SmoothDamp(st.ditherAmount, targetDither, ref st.velocity, smoothTime);

                if (st.ditherAmount > 0.005f)
                {
                    EnsureSwapped(rend, st);
                    ApplyDitherAmount(st, st.ditherAmount);
                }
                else
                {
                    if (st.swapped) RestoreOriginal(rend, st);
                    removeList.Add(rend);
                }
            }
            foreach (var r in removeList) states.Remove(r);
        }

        /// <summary>元 sharedMaterials を保存し、各 submesh 毎に dither material の instance を作って差し替える。</summary>
        void EnsureSwapped(Renderer rend, FadeState st)
        {
            if (st.swapped) return;
            st.originals = rend.sharedMaterials;
            int n = st.originals.Length;
            st.ditherInstances = new Material[n];
            var swap = new Material[n];
            for (int i = 0; i < n; i++)
            {
                var src = st.originals[i];
                var inst = new Material(ditherMaterial);
                CopyMaterialProperties(src, inst);
                st.ditherInstances[i] = inst;
                swap[i] = inst;
            }
            rend.sharedMaterials = swap;
            st.swapped = true;
        }

        /// <summary>元 material の URP Lit プロパティを dither instance に転写。texture/color/normal 等を保持。</summary>
        static void CopyMaterialProperties(Material src, Material dst)
        {
            if (src == null) return;
            if (src.HasProperty(BaseMapId)) dst.SetTexture(BaseMapId, src.GetTexture(BaseMapId));
            if (src.HasProperty(BaseColorId)) dst.SetColor(BaseColorId, src.GetColor(BaseColorId));
            if (src.HasProperty(BumpMapId)) dst.SetTexture(BumpMapId, src.GetTexture(BumpMapId));
            if (src.HasProperty(BumpScaleId)) dst.SetFloat(BumpScaleId, src.GetFloat(BumpScaleId));
            if (src.HasProperty(MetallicId)) dst.SetFloat(MetallicId, src.GetFloat(MetallicId));
            if (src.HasProperty(SmoothnessId)) dst.SetFloat(SmoothnessId, src.GetFloat(SmoothnessId));
            if (src.HasProperty(OcclusionMapId)) dst.SetTexture(OcclusionMapId, src.GetTexture(OcclusionMapId));
            if (src.HasProperty(OcclusionStrengthId)) dst.SetFloat(OcclusionStrengthId, src.GetFloat(OcclusionStrengthId));
            if (src.HasProperty(EmissionMapId)) dst.SetTexture(EmissionMapId, src.GetTexture(EmissionMapId));
            if (src.HasProperty(EmissionColorId)) dst.SetColor(EmissionColorId, src.GetColor(EmissionColorId));
            // keyword 転写は行わない。dst (OcclusionDither shader) は自身の shader_feature で
            // texture が non-null なら _NORMALMAP 等を自動 enable するため、src の keyword を
            // 引きずると無関連 keyword が混入する。
        }

        void ApplyDitherAmount(FadeState st, float amount)
        {
            if (st.ditherInstances == null) return;
            for (int i = 0; i < st.ditherInstances.Length; i++)
            {
                var m = st.ditherInstances[i];
                if (m != null) m.SetFloat(DitherAmountId, amount);
            }
        }

        void RestoreOriginal(Renderer rend, FadeState st)
        {
            if (st.originals != null) rend.sharedMaterials = st.originals;
            if (st.ditherInstances != null)
            {
                for (int i = 0; i < st.ditherInstances.Length; i++)
                {
                    if (st.ditherInstances[i] != null) Destroy(st.ditherInstances[i]);
                }
            }
            st.swapped = false;
            st.originals = null;
            st.ditherInstances = null;
        }

        void OnDisable() { RestoreAll(); }
        void OnDestroy() { RestoreAll(); }

        void RestoreAll()
        {
            foreach (var kv in states)
            {
                var rend = kv.Key;
                if (rend != null && kv.Value.swapped) RestoreOriginal(rend, kv.Value);
            }
            states.Clear();
        }
    }
}
