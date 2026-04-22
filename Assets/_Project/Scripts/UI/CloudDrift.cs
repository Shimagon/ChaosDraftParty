using UnityEngine;

namespace ChaosDraftParty.UI
{
    public sealed class CloudDrift : MonoBehaviour
    {
        [SerializeField] Vector3 driftVelocity = new Vector3(0.35f, 0f, 0f);
        [SerializeField] float wrapRangeX = 18f;
        [SerializeField] float bobAmplitude = 0.25f;
        [SerializeField] float bobFrequency = 0.08f;

        Vector3 basePos;

        void Awake()
        {
            basePos = transform.position;
        }

        void Update()
        {
            Vector3 pos = transform.position + driftVelocity * Time.deltaTime;
            if (wrapRangeX > 0f)
            {
                float dx = pos.x - basePos.x;
                if (dx > wrapRangeX) pos.x -= wrapRangeX * 2f;
                else if (dx < -wrapRangeX) pos.x += wrapRangeX * 2f;
            }
            float bob = Mathf.Sin(Time.time * bobFrequency * Mathf.PI * 2f) * bobAmplitude;
            pos.y = basePos.y + bob;
            transform.position = pos;
        }
    }
}
