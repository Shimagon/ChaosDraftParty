using UnityEngine;

namespace ChaosDraftParty.UI
{
    public sealed class CameraBreath : MonoBehaviour
    {
        [SerializeField] float amplitudeX = 0.35f;
        [SerializeField] float amplitudeY = 0.18f;
        [SerializeField] float speedX = 0.18f;
        [SerializeField] float speedY = 0.24f;
        [SerializeField] float lookAtY = 1.2f;
        [SerializeField] float lookAtZ = 8f;

        Vector3 basePos;

        void Awake()
        {
            basePos = transform.position;
        }

        void LateUpdate()
        {
            float t = Time.time;
            Vector3 offset = new Vector3(
                Mathf.Sin(t * speedX * Mathf.PI * 2f) * amplitudeX,
                Mathf.Sin(t * speedY * Mathf.PI * 2f) * amplitudeY,
                0f);
            transform.position = basePos + offset;
            transform.LookAt(new Vector3(basePos.x, lookAtY, lookAtZ));
        }
    }
}
