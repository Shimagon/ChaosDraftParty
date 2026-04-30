using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT のカプセル開閉動作: BodyUpperClear (透明上半球) を回転 + リフトで「パカっと」開ける。
    ///
    /// 前提: BodyUpperClear が SkinnedMR から MeshFilter+MeshRenderer に変換されてること。
    /// (SkinnedMR のままだと transform 動かしても描画位置に反映されない)
    ///
    /// 開閉モーション:
    /// - localEulerAngles X を 0 (閉) ↔ openTiltX (開) で Lerp
    /// - localPosition Y を 0 (閉) ↔ openLiftY (開) で Lerp
    ///
    /// プロト用: Inspector の bool isOpen を toggle で確認。Input 不要。
    /// 後でゲームロジックから OpenCapsule() / CloseCapsule() を呼ぶ。
    /// </summary>
    [DefaultExecutionOrder(110)]
    public class EggybotCapsule : MonoBehaviour
    {
        [Tooltip("透明上半球のオブジェクト。BodyUpperClear。MeshRenderer 化済みであること。")]
        [SerializeField] Transform upperLid;

        [Tooltip("開時の傾き (X 軸回転、度)。プラスで前にパカッ。45 推奨。")]
        [SerializeField] float openTiltX = 45f;

        [Tooltip("開時の上昇量 (Y、世界単位)。0.05 で 5cm 浮く。")]
        [SerializeField] float openLiftY = 0.05f;

        [Tooltip("開閉 Lerp 速度。値が大きいほど速い。8 推奨。")]
        [SerializeField] float lerpSpeed = 6f;

        [Tooltip("Inspector toggle でテスト。Play 中に切替可。")]
        [SerializeField] public bool isOpen;

        Vector3 closedLocalPos;
        Quaternion closedLocalRot;
        bool initialized;

        void Awake()
        {
            if (upperLid == null)
            {
                Debug.LogWarning("[EggybotCapsule] upperLid が未設定。BodyUpperClear を割り当てて");
                return;
            }
            closedLocalPos = upperLid.localPosition;
            closedLocalRot = upperLid.localRotation;
            initialized = true;
        }

        void LateUpdate()
        {
            if (!initialized) return;

            // 目標 transform
            var openLocalPos = closedLocalPos + new Vector3(0f, openLiftY, 0f);
            var openLocalRot = closedLocalRot * Quaternion.Euler(openTiltX, 0f, 0f);

            var targetPos = isOpen ? openLocalPos : closedLocalPos;
            var targetRot = isOpen ? openLocalRot : closedLocalRot;

            float t = Time.deltaTime * lerpSpeed;
            upperLid.localPosition = Vector3.Lerp(upperLid.localPosition, targetPos, t);
            upperLid.localRotation = Quaternion.Slerp(upperLid.localRotation, targetRot, t);
        }

        public void OpenCapsule() => isOpen = true;
        public void CloseCapsule() => isOpen = false;
        public void Toggle() => isOpen = !isOpen;
    }
}
