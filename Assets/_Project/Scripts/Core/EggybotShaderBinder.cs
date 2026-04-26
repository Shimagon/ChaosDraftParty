using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT の Hand 用 ArmClip shader に「体の world 中心・半径・上端 Y」を毎フレーム配信する (v15.17)。
    ///
    /// v15.17 で MaterialPropertyBlock → Shader.SetGlobal* に切替。
    /// 理由: SRP Batcher 有効時、UnityPerMaterial CBUFFER 内 property への MPB 上書きは黙殺される。
    /// 動的に変わる SDF clip パラメータは material に縛らず global uniform として配信する。
    ///
    /// アニメ追従:
    /// - bodyRenderer.transform.position は static なので使わない
    /// - Animator.GetBoneTransform(HumanBodyBones.Hips) を base にして
    ///   Hips world position + bodyCenterOffsetLocal で sphere center を毎フレーム算出
    /// - 結果: walking で Hips が上下しても SDF discard が体に追従する
    ///
    /// 多体対応 (M3+):
    ///   現状 global なので 1 シーン 1 EGGYBOT 想定。複数キャラ時は GlobalVectorArray + arm material 側に
    ///   index プロパティを追加して per-renderer 切替する想定（将来拡張）。
    /// </summary>
    [ExecuteAlways]
    [DefaultExecutionOrder(15000)]
    public class EggybotShaderBinder : MonoBehaviour
    {
        [Tooltip("BodyLower の SkinnedMeshRenderer。未設定なら子から自動探索。bodyCenterOffsetLocal の base 位置として使用。")]
        public SkinnedMeshRenderer bodyRenderer;

        [Tooltip("Hand_L / Hand_R などの ArmClip マテリアル使用 renderer。空なら子から自動探索。")]
        public SkinnedMeshRenderer[] handRenderers;

        [Tooltip("体の球半径 (Blender BODY_OUTER_R に対応)")]
        public float bodyRadius = 0.336f;

        [Tooltip("Hips bone から sphere center までの local offset (Blender PARTING_Z 相当)。" +
                 "EGGYBOT の rest pose で Hips world Y + これ = body sphere center world Y。")]
        public Vector3 bodyCenterOffsetLocal = new Vector3(0f, 0.18f, 0f);

        [Tooltip("Body 上端 Y を sphere center から何 m 上に置くか。0 なら sphere center と同じ (= bowl rim 位置)。")]
        public float bodyTopOffsetFromCenter = 0f;

        [Tooltip("Clip 余白 mm。プラスで body より少し広く clip、マイナスで少し狭く。")]
        public float clipMarginMM = 0f;

        [Tooltip("Hips bone を Animator から取得して center の追従元にするか。" +
                 "false の場合は bodyRenderer.transform.position を center base として使用。")]
        public bool trackHipsBone = true;

        [Tooltip("v15.18: SDF center を直接示す Transform。設定すれば Hips offset 計算より優先される。" +
                 "Hips 配下に Empty を置いて Inspector で位置調整したいケース用。")]
        public Transform bodyCenterTransform;

        [Tooltip("デバッグ Gizmo を Scene view に描く。")]
        public bool drawDebugGizmo = false;

        static readonly int kBodyCenter = Shader.PropertyToID("_EGGYBOT_BodyCenter");
        static readonly int kBodyRadius = Shader.PropertyToID("_EGGYBOT_BodyRadius");
        static readonly int kBodyTopY   = Shader.PropertyToID("_EGGYBOT_BodyTopY");
        static readonly int kClipMargin = Shader.PropertyToID("_EGGYBOT_ClipMargin");

        Animator _animator;
        Transform _hipsBone;

        void OnEnable()
        {
            AutoFindRenderers();
            CacheHipsBone();
        }

        void OnValidate()
        {
            AutoFindRenderers();
            CacheHipsBone();
        }

        void LateUpdate()
        {
            UpdateShaderGlobals();
        }

        void AutoFindRenderers()
        {
            if (bodyRenderer == null)
            {
                foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.gameObject.name == "BodyLower")
                    {
                        bodyRenderer = smr;
                        break;
                    }
                }
            }
            if (handRenderers == null || handRenderers.Length == 0)
            {
                var list = new System.Collections.Generic.List<SkinnedMeshRenderer>();
                foreach (var smr in GetComponentsInChildren<SkinnedMeshRenderer>(true))
                {
                    if (smr.gameObject.name == "Hand_L" || smr.gameObject.name == "Hand_R")
                        list.Add(smr);
                }
                handRenderers = list.ToArray();
            }
        }

        void CacheHipsBone()
        {
            _animator = GetComponent<Animator>();
            if (_animator == null) _animator = GetComponentInChildren<Animator>();
            if (_animator != null && _animator.isHuman)
            {
                _hipsBone = _animator.GetBoneTransform(HumanBodyBones.Hips);
            }
        }

        Vector3 ComputeBodyCenter()
        {
            if (bodyCenterTransform != null)
            {
                return bodyCenterTransform.position;
            }
            if (trackHipsBone && _hipsBone != null)
            {
                return _hipsBone.position + _hipsBone.TransformDirection(bodyCenterOffsetLocal);
            }
            if (bodyRenderer != null)
            {
                return bodyRenderer.transform.position;
            }
            return transform.position;
        }

        void UpdateShaderGlobals()
        {
            Vector3 bodyCenter = ComputeBodyCenter();
            float bodyTopY = bodyCenter.y + bodyTopOffsetFromCenter;

            Shader.SetGlobalVector(kBodyCenter, new Vector4(bodyCenter.x, bodyCenter.y, bodyCenter.z, 1f));
            Shader.SetGlobalFloat(kBodyRadius, bodyRadius);
            Shader.SetGlobalFloat(kBodyTopY, bodyTopY);
            Shader.SetGlobalFloat(kClipMargin, clipMarginMM);
        }

        void OnDrawGizmosSelected()
        {
            if (!drawDebugGizmo) return;
            Vector3 c = ComputeBodyCenter();
            Gizmos.color = new Color(1f, 0.4f, 0.6f, 0.4f);
            Gizmos.DrawWireSphere(c, bodyRadius);
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(c + Vector3.left * bodyRadius, c + Vector3.right * bodyRadius);
            Gizmos.DrawLine(c + new Vector3(-bodyRadius, bodyTopOffsetFromCenter, 0f),
                            c + new Vector3( bodyRadius, bodyTopOffsetFromCenter, 0f));
        }
    }
}
