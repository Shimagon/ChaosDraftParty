using UnityEngine;

namespace ChaosDraftParty.UI
{
    [ExecuteAlways]
    public sealed class TurntableRotator : MonoBehaviour
    {
        [SerializeField] float degreesPerSecond = 12f;
        [SerializeField] bool rotateInEditMode = true;

        void Update()
        {
            if (!Application.isPlaying && !rotateInEditMode) return;
            transform.Rotate(0f, degreesPerSecond * Time.deltaTime, 0f, Space.World);
        }
    }
}
