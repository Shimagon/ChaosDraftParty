using System.Collections;
using UnityEngine;

namespace ChaosDraftParty.Core
{
    /// <summary>
    /// EGGYBOT のしゃがみ sequence: 頭と蓋を並列制御し、cute かつリアルな動きを再現。
    ///
    /// Enter (stand→crouch):
    ///   1. 頭がぴょんッと上昇 + 蓋が開く (並列)
    ///   2. 頂点で hold
    ///   3. 頭がスポンと bowl 内に落下 + 蓋が同時に閉じる (並列、ラグなし)
    ///   4. 蓋が反動で軽くバウンス (slam → 戻り → 閉) + 頭の bowl 内 bounce
    ///
    /// Exit (crouch→stand):
    ///   1. 頭が bowl から高く飛び出す + 蓋が開く (並列)
    ///   2. 頂点で hold
    ///   3. 頭が body 上に降下 + 蓋が同時に閉じる (並列)
    ///   4. 蓋が反動 bounce
    /// </summary>
    [RequireComponent(typeof(EggybotCapsule))]
    [RequireComponent(typeof(EggybotCrouch))]
    public class EggybotCrouchSequence : MonoBehaviour
    {
        [Tooltip("「ぴょんッ」と上に跳ねる量 (m)。Enter 用。")]
        [SerializeField] float popUpHeight = 0.10f;

        [Tooltip("Exit 時の頭頂点高さ (m)。Enter より高く跳ねる。")]
        [SerializeField] float exitPopUpHeight = 0.18f;

        [Tooltip("ジャンプ時間 (秒)。短いほどキビキビ。")]
        [SerializeField] float popUpDuration = 0.15f;

        [Tooltip("頂点 hold (秒)。「溜め」感。")]
        [SerializeField] float holdAtPeakDuration = 0.05f;

        [Tooltip("落下時間 (秒)。短いほど勢い。")]
        [SerializeField] float dropDuration = 0.18f;

        [Tooltip("着地後の頭 bounce 戻り量 (m)。")]
        [SerializeField] float headBounceBack = 0.06f;

        [Tooltip("蓋 slam 時のオーバーシュート角度 (deg、負値で閉じ過ぎる方向)。")]
        [SerializeField] float lidSlamOvershoot = -8f;

        [Tooltip("蓋 slam 後の戻りバウンス角度 (deg、正で開く方向)。")]
        [SerializeField] float lidBounceOpen = 5f;

        [Tooltip("蓋 bounce 1 サイクル時間 (秒)。")]
        [SerializeField] float lidBounceDuration = 0.18f;

        [Tooltip("Inspector で ON にすると現状反転 sequence 1 サイクル再生。")]
        [SerializeField] bool triggerSequence;

        public bool IsRunning => sequenceRunning;

        EggybotCapsule capsule;
        EggybotCrouch crouch;
        bool sequenceRunning;

        void Awake()
        {
            capsule = GetComponent<EggybotCapsule>();
            crouch = GetComponent<EggybotCrouch>();
        }

        void Update()
        {
            // TODO(M2): スキル/ノックアウト演出から直接 PlayEnter/Exit を呼ぶ設計に置換、Inspector toggle は撤去
            if (triggerSequence && !sequenceRunning)
            {
                triggerSequence = false;
                StartCoroutine(ToggleCrouchSequence());
            }
        }

        public void PlayEnterCrouch()
        {
            if (sequenceRunning || crouch.IsCrouching) return;
            StartCoroutine(EnterCrouchSequence());
        }

        public void PlayExitCrouch()
        {
            if (sequenceRunning || !crouch.IsCrouching) return;
            StartCoroutine(ExitCrouchSequence());
        }

        IEnumerator ToggleCrouchSequence()
        {
            yield return crouch.IsCrouching ? ExitCrouchSequence() : EnterCrouchSequence();
        }

        IEnumerator EnterCrouchSequence()
        {
            sequenceRunning = true;
            var face = crouch.FaceVisual;
            var lid = capsule.UpperLid;
            if (face == null || lid == null) { sequenceRunning = false; yield break; }
            float crouchY = -crouch.GetCrouchDepth();
            var closedRot = capsule.GetClosedLocalRot();
            var openRot = closedRot * Quaternion.Euler(capsule.GetOpenTiltX(), 0f, 0f);
            var slamRot = closedRot * Quaternion.Euler(lidSlamOvershoot, 0f, 0f);
            var bounceRot = closedRot * Quaternion.Euler(lidBounceOpen, 0f, 0f);

            crouch.ExternalControl = true;
            capsule.ExternalControl = true;

            // Phase 1: 頭ぴょんッ上昇 + 蓋開 (並列)
            yield return AnimateBoth(face, lid, 0f, popUpHeight, closedRot, openRot, popUpDuration);
            yield return new WaitForSeconds(holdAtPeakDuration);

            // Phase 2: 頭スポン落下 + 蓋同時閉じ (並列、ラグなし)
            yield return AnimateBoth(face, lid, popUpHeight, crouchY, openRot, slamRot, dropDuration);

            // Phase 3: 着地 - 蓋反動 bounce (slam → 戻り → 閉)
            yield return AnimateBoth(face, lid, crouchY, crouchY + headBounceBack, slamRot, bounceRot, lidBounceDuration * 0.4f);
            yield return AnimateBoth(face, lid, crouchY + headBounceBack, crouchY, bounceRot, closedRot, lidBounceDuration * 0.6f);

            // 最終 snap
            face.localPosition = new Vector3(0f, crouchY, 0f);
            lid.localRotation = closedRot;
            crouch.ExternalControl = false;
            capsule.ExternalControl = false;
            crouch.Crouch();
            capsule.CloseCapsule();
            sequenceRunning = false;
        }

        IEnumerator ExitCrouchSequence()
        {
            sequenceRunning = true;
            var face = crouch.FaceVisual;
            var lid = capsule.UpperLid;
            if (face == null || lid == null) { sequenceRunning = false; yield break; }
            float crouchY = -crouch.GetCrouchDepth();
            var closedRot = capsule.GetClosedLocalRot();
            var openRot = closedRot * Quaternion.Euler(capsule.GetOpenTiltX(), 0f, 0f);
            var slamRot = closedRot * Quaternion.Euler(lidSlamOvershoot, 0f, 0f);
            var bounceRot = closedRot * Quaternion.Euler(lidBounceOpen, 0f, 0f);

            crouch.ExternalControl = true;
            capsule.ExternalControl = true;

            // Phase 1: 頭が bowl から高く跳ぶ + 蓋開 (並列、頭は exitPopUpHeight まで)
            yield return AnimateBoth(face, lid, crouchY, exitPopUpHeight, closedRot, openRot, popUpDuration * 1.4f);
            yield return new WaitForSeconds(holdAtPeakDuration);

            // Phase 2: 頭が body 上に降下 + 蓋が同時に閉じる (並列、頭は高い位置から、蓋は下で勢いよく)
            yield return AnimateBoth(face, lid, exitPopUpHeight, 0f, openRot, slamRot, dropDuration);

            // Phase 3: 蓋反動 bounce + 頭の軽い bounce
            yield return AnimateBoth(face, lid, 0f, -headBounceBack * 0.4f, slamRot, bounceRot, lidBounceDuration * 0.4f);
            yield return AnimateBoth(face, lid, -headBounceBack * 0.4f, 0f, bounceRot, closedRot, lidBounceDuration * 0.6f);

            // 最終 snap
            face.localPosition = Vector3.zero;
            lid.localRotation = closedRot;
            crouch.ExternalControl = false;
            capsule.ExternalControl = false;
            crouch.StandUp();
            capsule.CloseCapsule();
            sequenceRunning = false;
        }

        // 頭 Y と蓋 rotation を smoothstep ease で同時アニメ
        IEnumerator AnimateBoth(Transform face, Transform lid, float fromFaceY, float toFaceY, Quaternion fromLidRot, Quaternion toLidRot, float duration)
        {
            if (duration <= 0f)
            {
                var pi = face.localPosition; pi.y = toFaceY; face.localPosition = pi;
                lid.localRotation = toLidRot;
                yield break;
            }
            float t = 0f;
            while (t < duration)
            {
                float u = t / duration;
                float eased = u * u * (3f - 2f * u);
                var p = face.localPosition;
                p.y = Mathf.Lerp(fromFaceY, toFaceY, eased);
                face.localPosition = p;
                lid.localRotation = Quaternion.Slerp(fromLidRot, toLidRot, eased);
                t += Time.deltaTime;
                yield return null;
            }
            var pf = face.localPosition; pf.y = toFaceY; face.localPosition = pf;
            lid.localRotation = toLidRot;
        }
    }
}
