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

        public bool IsCrouching => isCrouching;
        public Transform FaceVisual => faceVisual;
        public float GetCrouchDepth() => crouchDepth;

        // Sequence script からの外部制御中は lerp を停止 (ぴょんッ→スポン アニメ用)
        public bool ExternalControl { get; set; }

        float currentY;
        bool synced;

        void Awake()
        {
#if UNITY_EDITOR
            // Editor の Game view 非フォーカス時でも Editor playerLoop を動かす
            // (ビルド後に副作用が残らないよう Editor only)
            Application.runInBackground = true;
#endif
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
        }

        public void Crouch() => isCrouching = true;
        public void StandUp() => isCrouching = false;
        public void Toggle() => isCrouching = !isCrouching;
    }
}
