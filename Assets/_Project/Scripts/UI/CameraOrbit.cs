using UnityEngine;

namespace ChaosDraftParty.UI
{
    public sealed class CameraOrbit : MonoBehaviour
    {
        [SerializeField] Vector3 pivot = new Vector3(0f, 1f, 8f);
        [SerializeField] float radius = 11f;
        [SerializeField] float height = 2.2f;
        [SerializeField] float angularSpeedDeg = 3.2f;
        [SerializeField] float startAngleDeg = -12f;
        [SerializeField] float bobAmplitude = 0.35f;
        [SerializeField] float bobFrequency = 0.15f;
        [SerializeField] Vector3 lookAtOffset = new Vector3(0f, 0.6f, 0f);

        float angle;

        void OnEnable()
        {
            angle = startAngleDeg;
        }

        void LateUpdate()
        {
            angle += angularSpeedDeg * Time.deltaTime;
            float rad = angle * Mathf.Deg2Rad;
            float y = height + Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            Vector3 offset = new Vector3(Mathf.Sin(rad) * radius, y - pivot.y, -Mathf.Cos(rad) * radius);
            transform.position = pivot + offset;
            transform.LookAt(pivot + lookAtOffset);
        }
    }
}
