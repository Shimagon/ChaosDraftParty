using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT の Hand 用 ArmClip shader に「体の world 位置・半径・上端 Y」を毎フレーム配信する。
    /// SDF クリッピングで「体の中の手」が確実に discard される。
    ///
    /// 仕組み:
    ///   1. BodyLower transform から世界中心を読む
    ///   2. _BodyCenter / _BodyRadius / _BodyTopY を Hand_L/Hand_R material にセット
    ///   3. ArmClip shader fragment で「pixel が body 球内なら discard」判定
    ///
    /// 注意: material instance を作って独立化するので、複数 EGGYBOT が同シーンにいても OK。
    /// </summary>
    [ExecuteAlways]
    public class EggybotShaderBinder : MonoBehaviour
    {
        [Tooltip("BodyLower の SkinnedMeshRenderer。未設定なら子から自動探索。")]
        public SkinnedMeshRenderer bodyRenderer;

        [Tooltip("Hand_L / Hand_R などの ArmClip マテリアル使用 renderer。空なら子から自動探索。")]
        public SkinnedMeshRenderer[] handRenderers;

        [Tooltip("体の球半径（Blender BODY_OUTER_R に対応）")]
        public float bodyRadius = 0.336f;

        [Tooltip("体の上端 Y オフセット（PARTING_Z 相当、BodyLower transform 基準で 0）")]
        public float bodyTopOffset = 0f;

        [Tooltip("Clip 余白 mm。プラスで body より少し広く clip、マイナスで少し狭く。")]
        public float clipMarginMM = 0f;

        static readonly int kBodyCenter = Shader.PropertyToID("_BodyCenter");
        static readonly int kBodyRadius = Shader.PropertyToID("_BodyRadius");
        static readonly int kBodyTopY   = Shader.PropertyToID("_BodyTopY");
        static readonly int kClipMargin = Shader.PropertyToID("_ClipMargin");

        MaterialPropertyBlock _mpb;

        void OnEnable()
        {
            AutoFindRenderers();
            _mpb = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            UpdateShaderProps();
        }

        void OnValidate()
        {
            AutoFindRenderers();
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

        void UpdateShaderProps()
        {
            if (bodyRenderer == null || handRenderers == null) return;
            if (_mpb == null) _mpb = new MaterialPropertyBlock();

            Vector3 bodyCenter = bodyRenderer.transform.position;
            float bodyTopY = bodyCenter.y + bodyTopOffset;

            foreach (var smr in handRenderers)
            {
                if (smr == null) continue;
                smr.GetPropertyBlock(_mpb);
                _mpb.SetVector(kBodyCenter, new Vector4(bodyCenter.x, bodyCenter.y, bodyCenter.z, 1f));
                _mpb.SetFloat(kBodyRadius, bodyRadius);
                _mpb.SetFloat(kBodyTopY, bodyTopY);
                _mpb.SetFloat(kClipMargin, clipMarginMM);
                smr.SetPropertyBlock(_mpb);
            }
        }
    }
}
