using System.Collections;
using Unity.Netcode;
using UnityEngine;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// 縮む島ミニゲームの中央集権 NetworkBehaviour。
    /// 8x8 = 64 タイルを <see cref="NetworkList{T}"/> で管理 (host 権威)。
    ///
    /// 同期戦略 (CLAUDE.md M1 雛形向け):
    /// - 64 個のタイルを個別 NetworkObject 化しない (spawn message 64 回は重い)
    /// - <c>NetworkList&lt;byte&gt;</c> 1 本で状態 (0=Active / 1=Fading / 2=Gone) を broadcast
    /// - 各 client は <c>OnListChanged</c> で local visual 切替
    ///
    /// ホスト権威ロジック:
    /// - 踏み消し: <see cref="NotifyStepped"/> から 1 秒後に Active→Gone
    /// - 外周 shrink: <see cref="shrinkInterval"/> 秒ごとに最外周 1 列を Fading に
    /// - shrink 順は対称的に外側 → 内側
    /// </summary>
    public class TileManager : NetworkBehaviour
    {
        [Header("Grid")]
        [Tooltip("1 辺のタイル数。8 推奨 (8x8 = 64 タイル)。")]
        [SerializeField] int gridSize = 8;

        [Tooltip("タイル間の中心距離 (m)。EGGYBOT 身長 0.75m に対し 2m が「2 歩で渡れる」感覚。")]
        [SerializeField] float tileSpacing = 2f;

        [Tooltip("タイル prefab (Tile component 必須)。null 時は scene 配置済の child Tile を使う。")]
        [SerializeField] Tile tilePrefab;

        [Tooltip("Tile を spawn する親 Transform (任意、null=本 GO の child)。")]
        [SerializeField] Transform tileParent;

        [Header("Timing")]
        [Tooltip("踏み込み検知から実 Gone までの時間 (秒)。視覚 fade 期間。")]
        [SerializeField] float fadeToGoneDelay = 1.0f;

        [Tooltip("外周 1 列消失の間隔 (秒)。15 秒で 90 秒ラウンド中 5 列消える。")]
        [SerializeField] float shrinkInterval = 15f;

        [Tooltip("ゲーム開始から最初の shrink までの delay (秒)。")]
        [SerializeField] float shrinkInitialDelay = 5f;

        [Header("Step detection (host raycast)")]
        [Tooltip("ゲーム開始から step 検知が有効化するまでの delay (秒)。spawn 時の即触れ回避。")]
        [SerializeField] float stepDetectionDelay = 1.5f;

        [Tooltip("足元 raycast の距離 (m)。CC bottom から下方向に。")]
        [SerializeField] float stepRaycastDistance = 0.4f;

        [Tooltip("Player object の検出間隔 (秒)。0 で毎フレ、0.1 で帯域節約。")]
        [SerializeField] float stepCheckInterval = 0.0f;

        // 状態: 0=Active, 1=Fading, 2=Gone
        readonly NetworkList<byte> tileStates = new();

        Tile[] tileVisuals; // local visual 参照、index 一致
        Coroutine shrinkRoutine;
        int outerLayerNext; // 次に shrink する layer (0=最外周、1=その内側、...)
        float spawnTime;
        float lastStepCheckTime;

        public override void OnNetworkSpawn()
        {
            tileStates.OnListChanged += HandleTileStateChanged;
            spawnTime = Time.time;

            if (IsServer)
            {
                // 状態リスト初期化 + visual 生成 (host だけが Spawn 時に生成)
                int total = gridSize * gridSize;
                for (int i = 0; i < total; i++) tileStates.Add(0);
                EnsureTilesInScene();
                shrinkRoutine = StartCoroutine(OuterShrinkLoop());
            }
            else
            {
                // Client は host から NetworkList が同期されるのを待ってから visual 作る
                EnsureTilesInScene();
            }
            ApplyAllStates();
        }

        void Update()
        {
            // host のみ: 全 player の足元 raycast でタイル踏み検知
            // (Tile の trigger は spawn overlap で誤発火するため host-side raycast に統一)
            if (!IsServer || !IsSpawned) return;
            if (Time.time - spawnTime < stepDetectionDelay) return;
            if (stepCheckInterval > 0f && Time.time - lastStepCheckTime < stepCheckInterval) return;
            lastStepCheckTime = Time.time;

            var nm = Unity.Netcode.NetworkManager.Singleton;
            if (nm == null) return;
            foreach (var kv in nm.ConnectedClients)
            {
                var pob = kv.Value.PlayerObject;
                if (pob == null) continue;
                var origin = pob.transform.position + Vector3.up * 0.05f;
                if (UnityEngine.Physics.Raycast(origin, Vector3.down, out var hit, stepRaycastDistance, ~0, UnityEngine.QueryTriggerInteraction.Ignore))
                {
                    var t = hit.collider.GetComponentInParent<Tile>();
                    if (t != null) NotifyStepped(t.TileIndex);
                }
            }
        }

        public override void OnNetworkDespawn()
        {
            tileStates.OnListChanged -= HandleTileStateChanged;
            if (shrinkRoutine != null) StopCoroutine(shrinkRoutine);
        }

        void EnsureTilesInScene()
        {
            int total = gridSize * gridSize;
            tileVisuals = new Tile[total];
            var parent = tileParent != null ? tileParent : transform;

            // 既存 child から先に拾う (prefab 不要パス用)
            int found = 0;
            foreach (Transform child in parent)
            {
                var t = child.GetComponent<Tile>();
                if (t == null) continue;
                if (found >= total) break;
                tileVisuals[found] = t;
                found++;
            }

            // 不足分は prefab から spawn
            if (found < total && tilePrefab != null)
            {
                for (int i = found; i < total; i++)
                {
                    var pos = IndexToWorldPos(i);
                    var t = Instantiate(tilePrefab, pos, Quaternion.identity, parent);
                    t.name = $"Tile_{i}";
                    tileVisuals[i] = t;
                }
            }

            // 各 Tile に index と参照を渡す
            for (int i = 0; i < total; i++)
            {
                if (tileVisuals[i] != null) tileVisuals[i].Initialize(this, i);
            }
        }

        Vector3 IndexToWorldPos(int idx)
        {
            int x = idx % gridSize;
            int z = idx / gridSize;
            float origin = -(gridSize - 1) * tileSpacing * 0.5f;
            return transform.position + new Vector3(origin + x * tileSpacing, 0f, origin + z * tileSpacing);
        }

        void ApplyAllStates()
        {
            if (tileVisuals == null || tileStates.Count == 0) return;
            for (int i = 0; i < tileVisuals.Length && i < tileStates.Count; i++)
            {
                if (tileVisuals[i] != null) tileVisuals[i].SetState(tileStates[i]);
            }
        }

        void HandleTileStateChanged(NetworkListEvent<byte> ev)
        {
            if (tileVisuals == null) return;
            if (ev.Index < 0 || ev.Index >= tileVisuals.Length) return;
            var t = tileVisuals[ev.Index];
            if (t != null) t.SetState(ev.Value);
        }

        /// <summary>Tile の OnTriggerEnter または host raycast から呼ばれる。host だけが state 変更。</summary>
        public void NotifyStepped(int idx)
        {
            if (!IsServer) return; // 各 client の trigger 発火は無視 (host が唯一 truth)
            if (Time.time - spawnTime < stepDetectionDelay) return; // spawn 直後の overlap 誤発火を無視
            if (idx < 0 || idx >= tileStates.Count) return;
            if (tileStates[idx] != 0) return; // すでに Fading/Gone
            tileStates[idx] = 1; // Fading
            StartCoroutine(FadeThenGone(idx));
        }

        IEnumerator FadeThenGone(int idx)
        {
            yield return new WaitForSeconds(fadeToGoneDelay);
            if (idx < tileStates.Count && tileStates[idx] == 1)
                tileStates[idx] = 2; // Gone
        }

        IEnumerator OuterShrinkLoop()
        {
            yield return new WaitForSeconds(shrinkInitialDelay);
            int maxLayer = gridSize / 2;
            while (outerLayerNext < maxLayer)
            {
                ShrinkOuterLayer(outerLayerNext);
                outerLayerNext++;
                yield return new WaitForSeconds(shrinkInterval);
            }
        }

        void ShrinkOuterLayer(int layer)
        {
            // layer 0 = 最外周、layer 1 = 内側 1 列、...
            int min = layer;
            int max = gridSize - 1 - layer;
            for (int x = min; x <= max; x++)
            {
                for (int z = min; z <= max; z++)
                {
                    if (x == min || x == max || z == min || z == max)
                    {
                        int idx = z * gridSize + x;
                        if (idx >= 0 && idx < tileStates.Count && tileStates[idx] == 0)
                        {
                            tileStates[idx] = 1;
                            StartCoroutine(FadeThenGone(idx));
                        }
                    }
                }
            }
        }

        [ContextMenu("Force Shrink Next Layer")]
        public void DebugShrinkNext()
        {
            if (!IsServer) return;
            ShrinkOuterLayer(outerLayerNext);
            outerLayerNext++;
        }
    }
}
