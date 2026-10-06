using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

namespace ChaosDraftParty.Minigames.ShrinkingIsland
{
    /// <summary>
    /// 縮む島ミニゲームの中央管理 (NetworkBehaviour、ホスト権威)。
    ///
    /// 設計:
    /// - host のみが shrink scheduling を持ち、ClientRpc で全 client に StartFade を broadcast
    /// - 各 client は Tile.StartFade で warning → fade → gone を local で再生
    /// - KO 判定は持たない (PlayerController が Y &lt; koThresholdY で自動 KO)
    /// - Network 同期対象は「どの tile を消し始めるか」のみ (低帯域)
    ///
    /// Grid 解析:
    /// - _stageRoot 配下の Tile 名 "Tile_X{i}_Z{j}" から index 解析 → grid[gridSize, gridSize]
    /// - 名前解析失敗時は Tile.GridX / GridZ をフォールバック参照
    ///
    /// Shrink 順:
    /// - layer 0 = 最外周 (8x8 では 28 tile)
    /// - layer 1 = 内側 1 列 (6x6 の外周 = 20 tile)
    /// - layer 2 = (4x4 の外周 = 12 tile)
    /// - layer 3 = (2x2 = 4 tile)
    /// → 全 64 tile が 4 layer で消える (合計 28+20+12+4 = 64)
    ///
    /// TODO: UniTask 未導入のため coroutine 暫定。manifest に com.cysharp.unitask 追加後に置換。
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class ShrinkingIslandManager : NetworkBehaviour
    {
        [Header("Stage")]
        [Tooltip("Tile を集める親 Transform。配下の Tile を grid 解析する。")]
        [SerializeField] private Transform _stageRoot;

        [Tooltip("Grid 1 辺のタイル数 (8x8 = 64 推奨)。")]
        [SerializeField] private int _gridSize = 8;

        [Header("Timing")]
        [Tooltip("外周 1 列消失の間隔 (秒)。Synthesizer 推奨 10 秒。")]
        [SerializeField] private float _shrinkInterval = 10f;

        [Tooltip("ゲーム開始から最初の shrink までの delay (秒)。")]
        [SerializeField] private float _initialDelay = 5f;

        [Tooltip("各 tile の Warning 期間 (秒、点滅 = 危険サイン)。")]
        [SerializeField] private float _warningDuration = 1f;

        [Tooltip("各 tile の Fading 期間 (秒、alpha 落ち + collider off)。")]
        [SerializeField] private float _fadeDuration = 0.5f;

        [Header("KO")]
        [Tooltip("Y 座標 がこの値より下に落ちたら KO (参考値、実 KO は PlayerController で実施)。")]
        [SerializeField] private float _koThresholdY = -5f;

        [Header("Config (優先、null なら個別 SerializeField fallback)")]
        [Tooltip("ShrinkingIslandConfig SO。設定されている場合は SO 値を優先、null なら個別 SerializeField を使う。")]
        [SerializeField] private ShrinkingIslandConfig _config;

        [Header("Stagger (Fall Guys defacto、64 tile 同時崩壊の知覚閾値最適化)")]
        [Tooltip("Row 単位 stagger delay (秒)。Fall Guys Hex-a-gone defacto: 0.05-0.10s。同時崩壊体感を維持しつつ CPU spike を分散。")]
        [SerializeField] private float _rowStaggerDelay = 0.08f;

        [Tooltip("Stagger pattern (RowByRow=外周から順、Random=ランダム、Simultaneous=stagger 無)。")]
        [SerializeField] private StaggerPattern _staggerPattern = StaggerPattern.RowByRow;

        private enum StaggerPattern { Simultaneous, RowByRow, Random }

        // 各値の effective getter (config 優先、null なら fallback)
        // koThresholdY は PlayerController 側持ち、Manager は読まない
        private float EffectiveShrinkInterval => _config != null ? _config.ShrinkInterval : _shrinkInterval;
        private float EffectiveInitialDelay => _config != null ? _config.InitialDelay : _initialDelay;
        private float EffectiveWarningDuration => _config != null ? _config.WarningDuration : _warningDuration;
        private float EffectiveFadeDuration => _config != null ? _config.FadeDuration : _fadeDuration;

        // grid: [x, z] index で Tile を保持 (x,z = 0..gridSize-1)
        private Tile[,] _grid;
        // layer 順アクセス用 (リスト化済)
        private readonly List<Tile> _allTiles = new();
        private int _nextLayer;
        private Coroutine _shrinkRoutine;
        private bool _completedLogged;
        private int _roundVersion;
        private System.Random _rng;

        // Stagger 用 row buffer (毎 layer 使い回し、heap alloc 回避)
        private readonly List<Tile> _layerTilesBuffer = new();
        private readonly List<List<Tile>> _rowGroupsBuffer = new();
        private readonly Dictionary<int, List<Tile>> _rowGroupDict = new();

        public float KoThresholdY => _koThresholdY;
        public int GridSize => _gridSize;

        private bool HasShrinkAuthority => !IsSpawned || IsServer;
        private bool ShouldRunSandboxShrink => !IsSpawned && (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsListening);

        private void Awake()
        {
            // System.Random で seed (UnityEngine.Random は規約禁止)
            _rng = new System.Random();
        }

        private void OnEnable()
        {
            if (_rng == null)
            {
                _rng = new System.Random();
            }
            if (Application.isPlaying && ShouldRunSandboxShrink)
            {
                BeginSandboxShrink();
            }
        }

        private void OnDisable()
        {
            StopShrinkRoutine();
        }

        private void Start()
        {
            // Sandbox 直接 Play では NetworkManager を経由しないため OnNetworkSpawn が呼ばれない。
            // その状態でも「縮む島」を試遊できるよう、single-player fallback として shrink を開始する。
            if (!ShouldRunSandboxShrink)
            {
                return;
            }

            BeginSandboxShrink();
        }

        public override void OnNetworkSpawn()
        {
            StopShrinkRoutine();
            CollectTiles();
            ResetRoundState();
            if (IsServer)
            {
                _shrinkRoutine = StartCoroutine(ShrinkLoop(_roundVersion));
            }
        }

        public override void OnNetworkDespawn()
        {
            StopShrinkRoutine();
        }

        private void BeginSandboxShrink()
        {
            if (!Application.isPlaying)
            {
                return;
            }

            StopShrinkRoutine();
            CollectTiles();
            ResetRoundState();
            _shrinkRoutine = StartCoroutine(ShrinkLoop(_roundVersion));
        }

        private void StopShrinkRoutine()
        {
            StopAllCoroutines();
            _shrinkRoutine = null;
        }

        private void ResetRoundState()
        {
            _roundVersion++;
            _nextLayer = 0;
            _completedLogged = false;
        }

        /// <summary>
        /// 全 Tile を Healthy に戻し、shrink coroutine を再起動する。
        /// host 側のラウンド開始フェーズから呼ばれる想定。
        /// </summary>
        public void ResetIsland()
        {
            if (_grid == null)
            {
                CollectTiles();
            }

            for (int i = 0; i < _allTiles.Count; i++)
            {
                Tile tile = _allTiles[i];
                if (tile == null)
                {
                    continue;
                }
                tile.ResetTile();
            }
            ResetBouncePads();

            if (!HasShrinkAuthority)
            {
                ResetRoundState();
                return;
            }
            StopShrinkRoutine();
            ResetRoundState();
            _shrinkRoutine = StartCoroutine(ShrinkLoop(_roundVersion));

            // client 側にも Reset を broadcast。Sandbox 直接 Play では NetworkManager が無いので呼ばない。
            if (IsSpawned && IsServer)
            {
                ResetAllClientRpc();
            }
        }

        private static void ResetBouncePads()
        {
            BouncePad[] pads = FindObjectsByType<BouncePad>(FindObjectsInactive.Include);
            for (int i = 0; i < pads.Length; i++)
            {
                if (pads[i] != null)
                {
                    pads[i].ResetPad();
                }
            }
        }

        private void CollectTiles()
        {
            if (_stageRoot == null)
            {
                _stageRoot = transform;
            }

            _grid = new Tile[_gridSize, _gridSize];
            _allTiles.Clear();

            // 子孫を再帰で集める (深さ任意)
            Tile[] found = _stageRoot.GetComponentsInChildren<Tile>(true);

            for (int i = 0; i < found.Length; i++)
            {
                Tile tile = found[i];
                if (tile == null)
                {
                    continue;
                }

                int gx;
                int gz;
                if (!TryParseGridIndex(tile, out gx, out gz))
                {
                    Debug.LogWarning($"[ShrinkingIslandManager] Tile '{tile.name}' grid index 解析失敗 (name 規約 'Tile_X{{i}}_Z{{j}}' / Tile.GridX,GridZ 設定推奨)。skip。");
                    continue;
                }
                if (gx < 0 || gx >= _gridSize || gz < 0 || gz >= _gridSize)
                {
                    Debug.LogWarning($"[ShrinkingIslandManager] Tile '{tile.name}' index ({gx},{gz}) が grid サイズ {_gridSize} 範囲外。skip。");
                    continue;
                }

                _grid[gx, gz] = tile;
                int linearIndex = gz * _gridSize + gx;
                tile.Initialize(linearIndex, gx, gz);
                _allTiles.Add(tile);
            }
        }

        private static bool TryParseGridIndex(Tile tile, out int gx, out int gz)
        {
            // 優先 1: 既存 Tile.GridX/GridZ が有効ならそれを使う
            if (tile.GridX >= 0 && tile.GridZ >= 0)
            {
                gx = tile.GridX;
                gz = tile.GridZ;
                return true;
            }

            // 優先 2: 名前 "Tile_X{i}_Z{j}" 解析
            string name = tile.name;
            int xTag = name.IndexOf("_X", StringComparison.Ordinal);
            int zTag = name.IndexOf("_Z", StringComparison.Ordinal);
            if (xTag >= 0 && zTag > xTag)
            {
                string xStr = name.Substring(xTag + 2, zTag - (xTag + 2));
                string zStr = name.Substring(zTag + 2);
                if (int.TryParse(xStr, out gx) && int.TryParse(zStr, out gz))
                {
                    return true;
                }
            }

            gx = -1;
            gz = -1;
            return false;
        }

        private IEnumerator ShrinkLoop(int roundVersion)
        {
            yield return new WaitForSeconds(EffectiveInitialDelay);
            if (roundVersion != _roundVersion)
            {
                yield break;
            }

            int maxLayer = _gridSize / 2;
            while (_nextLayer < maxLayer)
            {
                if (roundVersion != _roundVersion)
                {
                    yield break;
                }
                yield return StartCoroutine(ShrinkLayerWithStagger(_nextLayer, roundVersion));
                _nextLayer++;
                if (_nextLayer >= maxLayer)
                {
                    break;
                }
                yield return new WaitForSeconds(EffectiveShrinkInterval);
            }

            // 全消失確認 (Fading + Gone まで含めて最後の tile が落ちるまで余韻待ち)
            yield return new WaitForSeconds(EffectiveWarningDuration + EffectiveFadeDuration);
            if (roundVersion != _roundVersion)
            {
                yield break;
            }
            if (!_completedLogged)
            {
                _completedLogged = true;
                Debug.Log("[ShrinkingIslandManager] Stage Cleared");
            }
        }

        private void ShrinkLayer(int layer)
        {
            // 旧 API (Simultaneous 互換): 内部で全 tile を同時 fade
            CollectLayerTiles(layer, _layerTilesBuffer);
            for (int i = 0; i < _layerTilesBuffer.Count; i++)
            {
                FadeSingleTile(_layerTilesBuffer[i]);
            }
        }

        /// <summary>
        /// Layer 内 tile を stagger pattern に従って順次 fade する coroutine。
        /// - Simultaneous: 既存挙動 (全 tile を 1 frame で fade)
        /// - RowByRow: 同 row (gridZ) ごとに同時 fade、row 間で _rowStaggerDelay 待機
        /// - Random: shuffle した順で row group 化 (擬似ランダム連鎖)
        /// </summary>
        private IEnumerator ShrinkLayerWithStagger(int layerIndex, int roundVersion)
        {
            CollectLayerTiles(layerIndex, _layerTilesBuffer);

            if (_staggerPattern == StaggerPattern.Simultaneous || _rowStaggerDelay <= 0f)
            {
                // 旧挙動: 全 tile 同時 fade
                for (int i = 0; i < _layerTilesBuffer.Count; i++)
                {
                    FadeSingleTile(_layerTilesBuffer[i]);
                }
                yield break;
            }

            if (_staggerPattern == StaggerPattern.Random)
            {
                ShuffleTilesInPlace(_layerTilesBuffer);
            }
            else
            {
                // RowByRow: gridZ 昇順で sort (外周 layer なら自然に N→S 順)
                SortTilesByRow(_layerTilesBuffer);
            }

            BuildRowGroups(_layerTilesBuffer, _rowGroupsBuffer, _rowGroupDict);

            WaitForSeconds wait = new WaitForSeconds(_rowStaggerDelay);
            for (int g = 0; g < _rowGroupsBuffer.Count; g++)
            {
                if (roundVersion != _roundVersion)
                {
                    yield break;
                }
                List<Tile> row = _rowGroupsBuffer[g];
                for (int i = 0; i < row.Count; i++)
                {
                    FadeSingleTile(row[i]);
                }
                if (g < _rowGroupsBuffer.Count - 1)
                {
                    yield return wait;
                }
            }
        }

        /// <summary>
        /// 指定 layer の外周 tile を buffer に収集する (heap alloc 抑制のため呼び側が buffer 渡し)。
        /// </summary>
        private void CollectLayerTiles(int layer, List<Tile> buffer)
        {
            buffer.Clear();
            int min = layer;
            int max = _gridSize - 1 - layer;

            for (int x = min; x <= max; x++)
            {
                for (int z = min; z <= max; z++)
                {
                    // layer 内側 cell はスキップ (外周 1 列のみ消す)
                    if (x != min && x != max && z != min && z != max)
                    {
                        continue;
                    }

                    Tile tile = _grid[x, z];
                    if (tile == null)
                    {
                        continue;
                    }
                    buffer.Add(tile);
                }
            }
        }

        private void FadeSingleTile(Tile tile)
        {
            if (tile == null)
            {
                return;
            }
            int linearIndex = tile.GridZ * _gridSize + tile.GridX;
            if (IsSpawned)
            {
                StartFadeClientRpc(linearIndex, EffectiveWarningDuration, EffectiveFadeDuration);
            }
            else
            {
                tile.StartFade(EffectiveWarningDuration, EffectiveFadeDuration);
            }
        }

        private static void SortTilesByRow(List<Tile> tiles)
        {
            // gridZ 昇順 (LINQ 禁止規約のため Sort comparer 使用)
            tiles.Sort(CompareTileByGridZ);
        }

        private static int CompareTileByGridZ(Tile a, Tile b)
        {
            if (a == null && b == null)
            {
                return 0;
            }
            if (a == null)
            {
                return 1;
            }
            if (b == null)
            {
                return -1;
            }
            return a.GridZ.CompareTo(b.GridZ);
        }

        private void ShuffleTilesInPlace(List<Tile> tiles)
        {
            // Fisher-Yates (System.Random、UnityEngine.Random は規約禁止)
            for (int i = tiles.Count - 1; i > 0; i--)
            {
                int j = _rng.Next(i + 1);
                Tile tmp = tiles[i];
                tiles[i] = tiles[j];
                tiles[j] = tmp;
            }
        }

        /// <summary>
        /// tile を gridZ で row 化。buffer 渡しで heap alloc 抑制。
        /// </summary>
        private static void BuildRowGroups(List<Tile> source, List<List<Tile>> groups, Dictionary<int, List<Tile>> dict)
        {
            // 既存 group list の中身 clear (List 自体は使い回し)
            for (int i = 0; i < groups.Count; i++)
            {
                if (groups[i] != null)
                {
                    groups[i].Clear();
                }
            }
            dict.Clear();

            int groupCount = 0;
            for (int i = 0; i < source.Count; i++)
            {
                Tile tile = source[i];
                if (tile == null)
                {
                    continue;
                }
                int row = tile.GridZ;
                if (!dict.TryGetValue(row, out List<Tile> bucket))
                {
                    if (groupCount < groups.Count)
                    {
                        bucket = groups[groupCount];
                    }
                    else
                    {
                        bucket = new List<Tile>();
                        groups.Add(bucket);
                    }
                    groupCount++;
                    dict[row] = bucket;
                }
                bucket.Add(tile);
            }

            // groups 末尾の余剰 list は size 0 のまま使い回されるが、
            // ループ側は groupCount を見ない (foreach 互換) ので余剰を切り詰める。
            while (groups.Count > groupCount)
            {
                groups.RemoveAt(groups.Count - 1);
            }
        }

        [ClientRpc]
        private void StartFadeClientRpc(int linearIndex, float warningDuration, float fadeDuration)
        {
            if (_grid == null)
            {
                CollectTiles();
            }

            int gx = linearIndex % _gridSize;
            int gz = linearIndex / _gridSize;
            if (gx < 0 || gx >= _gridSize || gz < 0 || gz >= _gridSize)
            {
                return;
            }

            Tile tile = _grid[gx, gz];
            if (tile == null)
            {
                return;
            }
            tile.StartFade(warningDuration, fadeDuration);
        }

        [ClientRpc]
        private void ResetAllClientRpc()
        {
            if (_grid == null)
            {
                CollectTiles();
                ResetBouncePads();
                return; // CollectTiles 内で Initialize → ResetTile 済
            }
            for (int i = 0; i < _allTiles.Count; i++)
            {
                Tile tile = _allTiles[i];
                if (tile == null)
                {
                    continue;
                }
                tile.ResetTile();
            }
            ResetBouncePads();
        }

        [ContextMenu("Force Shrink Next Layer")]
        private void DebugShrinkNext()
        {
            if (!HasShrinkAuthority)
            {
                return;
            }
            if (_nextLayer >= _gridSize / 2)
            {
                return;
            }
            ShrinkLayer(_nextLayer);
            _nextLayer++;
        }
    }
}
