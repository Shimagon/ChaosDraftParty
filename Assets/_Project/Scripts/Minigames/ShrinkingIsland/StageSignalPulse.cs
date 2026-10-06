using UnityEngine;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// Lightweight stage signal for objects that must read as intentional from gameplay camera distance.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class StageSignalPulse : MonoBehaviour
    {
        [SerializeField] private float _pulseAmplitude = 0.04f;
        [SerializeField] private float _pulseSpeed = 2.6f;
        [SerializeField] private Vector3 _rotationDegreesPerSecond = new(0f, 35f, 0f);

        private Vector3 _baseScale;

        private void Awake()
        {
            _baseScale = transform.localScale;
        }

        private void OnEnable()
        {
            if (_baseScale == Vector3.zero)
            {
                _baseScale = transform.localScale;
            }
        }

        private void Update()
        {
            float pulse = 1f + Mathf.Sin(Time.time * _pulseSpeed) * _pulseAmplitude;
            transform.localScale = _baseScale * pulse;
            transform.Rotate(_rotationDegreesPerSecond * Time.deltaTime, Space.Self);
        }
    }
}
