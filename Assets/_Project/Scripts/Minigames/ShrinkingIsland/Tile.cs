using UnityEngine;
using UnityEngine.Serialization;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// 縮む島の 1 マス。NetworkObject ではなく単純な MonoBehaviour。
    /// 状態同期は ShrinkingIslandManager (ClientRpc) か TileManager (NetworkList) のどちらでも駆動可能。
    ///
    /// 設計:
    /// - state は Healthy → Warning → Fading → Gone へ単方向遷移
    /// - 視覚は MaterialPropertyBlock 経由で _BaseColor / _Color を補間 (instance 化せず batching を守る)
    /// - SE / Particle hook は SerializeField (null 許容)
    /// - Network 同期は不要 (manager が ClientRpc で StartFade をブロードキャスト)
    ///
    /// 後方互換:
    /// - 既存 TileManager (NetworkList&lt;byte&gt; 駆動) も SetState(byte) で呼び出し可能
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class Tile : MonoBehaviour
    {
        public enum TileState
        {
            Healthy = 0,
            Warning = 1,
            Fading  = 2,
            Gone    = 3,
        }

        [Header("Index")]
        [Tooltip("Grid 上の (x, z) index。ShrinkingIslandManager が起動時に set。")]
        [FormerlySerializedAs("tileIndex")]
        [SerializeField] private int _tileIndex = -1;

        [SerializeField] private int _gridX = -1;
        [SerializeField] private int _gridZ = -1;

        [Header("Visual")]
        [Tooltip("活性時の MeshRenderer (Healthy = enabled、Gone = disabled)。")]
        [FormerlySerializedAs("visualRenderer")]
        [SerializeField] private MeshRenderer _visualRenderer;

        [Tooltip("足場の Collider (踏める時 enabled、Gone で disabled)。")]
        [FormerlySerializedAs("standCollider")]
        [SerializeField] private Collider _standCollider;

        [Tooltip("Warning 中の点滅速度 (Hz)。視覚フィードバック用。")]
        [FormerlySerializedAs("fadingBlinkRate")]
        [SerializeField] private float _warningBlinkRate = 4f;

        [Header("Effects (optional)")]
        [Tooltip("Fading 突入時に Play する Particle (null OK)。")]
        [SerializeField] private ParticleSystem _fadeParticle;

        [Tooltip("Fading 突入時に Play する SE (null OK)。")]
        [SerializeField] private AudioSource _fadeSe;

        // Runtime state
        private TileState _state = TileState.Healthy;
        private float _stateTime;
        private float _warningDuration = 1f;
        private float _fadeDuration = 0.5f;
        private Color _baseColor = Color.white;
        private MaterialPropertyBlock _propBlock;
        private bool _initialized;

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        public int TileIndex => _tileIndex;
        public int GridX => _gridX;
        public int GridZ => _gridZ;
        public TileState State => _state;

        /// <summary>初期化。grid index を埋め、Healthy 状態に揃える。</summary>
        public void Initialize(int index, int gridX, int gridZ)
        {
            _tileIndex = index;
            _gridX = gridX;
            _gridZ = gridZ;
            EnsureInitialized();
            ResetTile();
        }

        /// <summary>後方互換: 旧 TileManager 用の (mgr, index) 形式。</summary>
        public void Initialize(TileManager manager, int index)
        {
            EnsureInitialized();
            _tileIndex = index;
            ResetTile();
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void EnsureInitialized()
        {
            if (_initialized)
            {
                return;
            }
            _propBlock = new MaterialPropertyBlock();
            if (_visualRenderer == null)
            {
                _visualRenderer = GetComponentInChildren<MeshRenderer>();
            }
            if (_standCollider == null)
            {
                _standCollider = GetComponentInChildren<Collider>();
            }
            if (_visualRenderer != null)
            {
                Material sharedMat = _visualRenderer.sharedMaterial;
                if (sharedMat != null)
                {
                    if (sharedMat.HasProperty(BaseColorId))
                    {
                        _baseColor = sharedMat.GetColor(BaseColorId);
                    }
                    else if (sharedMat.HasProperty(ColorId))
                    {
                        _baseColor = sharedMat.GetColor(ColorId);
                    }
                }
            }
            _initialized = true;
        }

        /// <summary>
        /// host から ClientRpc 経由で呼ばれる。Warning → Fading → Gone へ自走。
        /// </summary>
        public void StartFade(float warningDuration = 1f, float fadeDuration = 0.5f)
        {
            EnsureInitialized();
            if (_state == TileState.Fading || _state == TileState.Gone)
            {
                return;
            }
            _warningDuration = Mathf.Max(0f, warningDuration);
            _fadeDuration = Mathf.Max(0f, fadeDuration);
            EnterState(TileState.Warning);
        }

        /// <summary>ラウンド開始時に Healthy へ戻す。</summary>
        public void ResetTile()
        {
            EnsureInitialized();
            EnterState(TileState.Healthy);
        }

        /// <summary>後方互換: 既存 TileManager 経由の byte state を受ける。</summary>
        public void SetState(byte rawState)
        {
            // 旧 TileManager の規約: 0=Active, 1=Fading, 2=Gone
            switch (rawState)
            {
                case 0:
                    ResetTile();
                    break;
                case 1:
                    StartFade(_warningDuration, _fadeDuration);
                    break;
                case 2:
                    EnterState(TileState.Gone);
                    break;
            }
        }

        private void EnterState(TileState next)
        {
            _state = next;
            _stateTime = 0f;

            switch (next)
            {
                case TileState.Healthy:
                    if (_visualRenderer != null)
                    {
                        _visualRenderer.enabled = true;
                        ApplyColor(_baseColor);
                    }
                    if (_standCollider != null)
                    {
                        _standCollider.enabled = true;
                    }
                    if (gameObject.activeSelf == false)
                    {
                        gameObject.SetActive(true);
                    }
                    break;

                case TileState.Warning:
                    if (_fadeParticle != null)
                    {
                        _fadeParticle.Play(true);
                    }
                    if (_fadeSe != null)
                    {
                        _fadeSe.Play();
                    }
                    break;

                case TileState.Fading:
                    if (_standCollider != null)
                    {
                        _standCollider.enabled = false;
                    }
                    break;

                case TileState.Gone:
                    if (_visualRenderer != null)
                    {
                        _visualRenderer.enabled = false;
                    }
                    if (_standCollider != null)
                    {
                        _standCollider.enabled = false;
                    }
                    gameObject.SetActive(false);
                    break;
            }
        }

        private void Update()
        {
            if (_state == TileState.Healthy || _state == TileState.Gone)
            {
                return;
            }

            _stateTime += Time.deltaTime;

            if (_state == TileState.Warning)
            {
                // 赤点滅
                if (_visualRenderer != null)
                {
                    float k = 0.5f + 0.5f * Mathf.Sin(_stateTime * _warningBlinkRate * 2f * Mathf.PI);
                    ApplyColor(Color.Lerp(_baseColor, Color.red, k));
                }
                if (_stateTime >= _warningDuration)
                {
                    EnterState(TileState.Fading);
                }
                return;
            }

            if (_state == TileState.Fading)
            {
                // alpha を 1 → 0 へ補間 (collider は EnterState で OFF 済)
                float t = _fadeDuration > 0f ? Mathf.Clamp01(_stateTime / _fadeDuration) : 1f;
                if (_visualRenderer != null)
                {
                    Color c = Color.Lerp(Color.red, new Color(_baseColor.r, _baseColor.g, _baseColor.b, 0f), t);
                    ApplyColor(c);
                }
                if (_stateTime >= _fadeDuration)
                {
                    EnterState(TileState.Gone);
                }
            }
        }

        private void ApplyColor(Color c)
        {
            if (_visualRenderer == null)
            {
                return;
            }
            _visualRenderer.GetPropertyBlock(_propBlock);
            if (_visualRenderer.sharedMaterial != null && _visualRenderer.sharedMaterial.HasProperty(BaseColorId))
            {
                _propBlock.SetColor(BaseColorId, c);
            }
            else
            {
                _propBlock.SetColor(ColorId, c);
            }
            _visualRenderer.SetPropertyBlock(_propBlock);
        }
    }
}
