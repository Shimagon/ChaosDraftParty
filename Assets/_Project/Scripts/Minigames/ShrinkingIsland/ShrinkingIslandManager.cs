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

        public float KoThresholdY => _koThresholdY;
        public int GridSize => _gridSize;

        public override void OnNetworkSpawn()
        {
            CollectTiles();
            if (IsServer)
            {
                _shrinkRoutine = StartCoroutine(ShrinkLoop());
            }
        }

        public override void OnNetworkDespawn()
        {
            if (_shrinkRoutine != null)
            {
                StopCoroutine(_shrinkRoutine);
                _shrinkRoutine = null;
            }
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

            _nextLayer = 0;
            _completedLogged = false;

            if (!IsServer)
            {
                return;
            }
            if (_shrinkRoutine != null)
            {
                StopCoroutine(_shrinkRoutine);
            }
            _shrinkRoutine = StartCoroutine(ShrinkLoop());

            // client 側にも Reset を broadcast
            if (IsSpawned)
            {
                ResetAllClientRpc();
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

        private IEnumerator ShrinkLoop()
        {
            yield return new WaitForSeconds(EffectiveInitialDelay);

            int maxLayer = _gridSize / 2;
            while (_nextLayer < maxLayer)
            {
                ShrinkLayer(_nextLayer);
                _nextLayer++;
                if (_nextLayer >= maxLayer)
                {
                    break;
                }
                yield return new WaitForSeconds(EffectiveShrinkInterval);
            }

            // 全消失確認 (Fading + Gone まで含めて最後の tile が落ちるまで余韻待ち)
            yield return new WaitForSeconds(EffectiveWarningDuration + EffectiveFadeDuration);
            if (!_completedLogged)
            {
                _completedLogged = true;
                Debug.Log("[ShrinkingIslandManager] Stage Cleared");
            }
        }

        private void ShrinkLayer(int layer)
        {
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

                    int linearIndex = z * _gridSize + x;
                    StartFadeClientRpc(linearIndex, EffectiveWarningDuration, EffectiveFadeDuration);
                }
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
        }

        [ContextMenu("Force Shrink Next Layer")]
        private void DebugShrinkNext()
        {
            if (!IsServer)
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
