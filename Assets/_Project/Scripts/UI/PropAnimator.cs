using UnityEngine;

namespace ChaosDraftParty.UI
{
    public sealed class PropAnimator : MonoBehaviour
    {
        [SerializeField] float bobAmplitude = 0.2f;
        [SerializeField] float bobFrequency = 0.5f;
        [SerializeField] float swayAmplitudeDeg = 4f;
        [SerializeField] float swayFrequency = 0.3f;
        [SerializeField] Vector3 swayAxis = Vector3.forward;
        [SerializeField] Vector3 rotationSpeedDeg = Vector3.zero;
        [SerializeField] float phaseOffset = 0f;
        [SerializeField] float scalePulseAmplitude = 0f;
        [SerializeField] float scalePulseFrequency = 0.4f;

        Vector3 basePos;
        Quaternion baseRot;
        Vector3 baseScale;

        void Awake()
        {
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            baseScale = transform.localScale;
        }

        void Update()
        {
            float t = Time.time + phaseOffset;
            float twoPi = Mathf.PI * 2f;

            Vector3 posOffset = Vector3.up * (Mathf.Sin(t * bobFrequency * twoPi) * bobAmplitude);
            transform.localPosition = basePos + posOffset;

            float swayAngle = Mathf.Sin(t * swayFrequency * twoPi) * swayAmplitudeDeg;
            Quaternion sway = Quaternion.AngleAxis(swayAngle, swayAxis.sqrMagnitude > 0 ? swayAxis.normalized : Vector3.forward);
            Quaternion spin = Quaternion.Euler(rotationSpeedDeg * Time.time);
            transform.localRotation = baseRot * sway * spin;

            if (scalePulseAmplitude > 0.0001f)
            {
                float s = 1f + Mathf.Sin(t * scalePulseFrequency * twoPi) * scalePulseAmplitude;
                transform.localScale = baseScale * s;
            }
        }
    }
}
