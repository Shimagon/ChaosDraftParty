using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT のしゃがみ動作: Head bone を bowl の中に Lerp で収納する。
    ///
    /// コンセプト: EGGYBOT 自身がカプセルなので、しゃがみ = 顔のついた頭が bowl 内部に沈み込む動き。
    /// SDF clip shader (EggybotShaderBinder, 半径 0.336 の判定球) が pixel 単位で頭を消すので、
    /// 物理的には Head bone Y を下げるだけで「カプセルに頭が呑まれる」演出が成立する。
    /// bowl (BodyLower / BodyUpperClear) や Hips は一切触らない。
    ///
    /// 実行順: PoseLock (default 0) → EggybotCrouch (100) で Head を後勝ちで上書き。
    /// </summary>
    [DefaultExecutionOrder(100)]
    [RequireComponent(typeof(Animator))]
    public class EggybotCrouch : MonoBehaviour
    {
        [Tooltip("立ち姿勢の Head world Y。EGGYBOT v16 は 0.91 前後。")]
        // TODO(M2): EggybotConfig SO に移管
        [SerializeField] float standingHeadY = 0.91f;

        [Tooltip("しゃがみ姿勢の Head world Y。bowl 中心 Y=0.50 より下に置くと SDF で完全に消える。0.30 推奨。")]
        // TODO(M2): EggybotConfig SO に移管
        [SerializeField] float crouchingHeadY = 0.30f;

        [Tooltip("立ち↔しゃがみの Lerp 速度。値が大きいほど速く切り替わる。5 推奨。")]
        [SerializeField] float lerpSpeed = 8f;

        [Tooltip("Inspector toggle でテスト。Play 中に切替可。M2 でスキル / ノックアウトに繋ぐ。")]
        [SerializeField] public bool isCrouching;

        public bool IsCrouching => isCrouching;

        Animator anim;
        Transform head;
        float currentY;

        void Awake()
        {
            anim = GetComponent<Animator>();
            head = anim.GetBoneTransform(HumanBodyBones.Head);
            if (head == null)
            {
                Debug.LogWarning("[EggybotCrouch] Head bone not found. Avatar が Humanoid か確認");
                return;
            }
            currentY = standingHeadY;
        }

        void LateUpdate()
        {
            if (head == null) return;
            float targetY = isCrouching ? crouchingHeadY : standingHeadY;
            currentY = Mathf.Lerp(currentY, targetY, Time.deltaTime * lerpSpeed);
            var p = head.position;
            p.y = currentY;
            head.position = p;
        }

        public void Crouch() => isCrouching = true;
        public void StandUp() => isCrouching = false;
        public void Toggle() => isCrouching = !isCrouching;
    }
}
