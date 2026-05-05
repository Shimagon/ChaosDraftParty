using UnityEngine;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// 縮む島の 1 マス。NetworkObject ではなく単純な MonoBehaviour。
    /// 状態同期は TileManager の NetworkList<byte> で集約管理。
    ///
    /// 設計 (host raycast 方式 / TileManager.Update に統一済):
    /// - TileManager が毎フレーム Physics.RaycastAll でプレイヤー位置を検査し、host 側で fade 開始
    /// - SetState で状態に応じた visual 切替 (active=不透明、fading=blink、gone=非表示+collider off)
    /// </summary>
    public class Tile : MonoBehaviour
    {
        [Tooltip("配列での index (TileManager から set)")]
        [SerializeField] int tileIndex = -1;

        [Tooltip("活性時の MeshRenderer (Active = enabled、Gone = disabled)")]
        [SerializeField] MeshRenderer visualRenderer;

        [Tooltip("足場の MeshCollider (踏める時 enabled、Gone で disabled)")]
        [SerializeField] Collider standCollider;

        [Tooltip("Fading 中の点滅速度 (Hz)。視覚フィードバック用。")]
        [SerializeField] float fadingBlinkRate = 4f;

        TileManager manager;
        Material runtimeMat;
        Color baseColor = Color.white;
        byte currentState; // 0=Active, 1=Fading, 2=Gone
        float fadingStartTime;

        public int TileIndex => tileIndex;
        public byte CurrentState => currentState;

        public void Initialize(TileManager mgr, int index)
        {
            manager = mgr;
            tileIndex = index;
            if (visualRenderer != null)
            {
                runtimeMat = visualRenderer.material; // instance
                if (runtimeMat.HasProperty("_BaseColor")) baseColor = runtimeMat.GetColor("_BaseColor");
            }
            SetState(0);
        }

        public void SetState(byte state)
        {
            currentState = state;
            if (state == 0)
            {
                if (visualRenderer != null) visualRenderer.enabled = true;
                if (standCollider != null) standCollider.enabled = true;
                if (runtimeMat != null && runtimeMat.HasProperty("_BaseColor")) runtimeMat.SetColor("_BaseColor", baseColor);
            }
            else if (state == 1)
            {
                fadingStartTime = Time.time;
            }
            else if (state == 2)
            {
                if (visualRenderer != null) visualRenderer.enabled = false;
                if (standCollider != null) standCollider.enabled = false;
            }
        }

        void Update()
        {
            // Fading 中の visual blink
            if (currentState == 1 && runtimeMat != null && runtimeMat.HasProperty("_BaseColor"))
            {
                float k = 0.5f + 0.5f * Mathf.Sin(Time.time * fadingBlinkRate * 2f * Mathf.PI);
                var c = Color.Lerp(baseColor, Color.red, k);
                runtimeMat.SetColor("_BaseColor", c);
            }
        }

    }
}
