using UnityEngine;

#if UNITY_EDITOR
namespace ChaosDraftParty.Core
{
    /// <summary>
    /// Run チューニング専用 (Sandbox_v20)。Animator.Speed = forceSpeed を毎フレーム書込み、
    /// BlendTree を Run 100% で再生継続する。本番 scene には付けない。
    /// Editor ビルドのみ有効 (#if UNITY_EDITOR)。
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class RunTuningSpeedForcer : MonoBehaviour
    {
        [Tooltip("Animator parameter 'Speed' に書き込む値。8 で Run 100%、1.5 で Walking。")]
        [SerializeField] float forceSpeed = 8f;

        [SerializeField] bool active = true;

        Animator anim;
        static readonly int SpeedHash = Animator.StringToHash("Speed");

        void Awake() { anim = GetComponent<Animator>(); }
        void Update()
        {
            if (!active || anim == null) return;
            anim.SetFloat(SpeedHash, forceSpeed);
        }
    }
}
#endif
