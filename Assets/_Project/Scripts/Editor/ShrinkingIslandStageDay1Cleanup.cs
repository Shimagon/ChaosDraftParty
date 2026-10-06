#if UNITY_EDITOR
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChaosDraftParty.Editor
{
    /// <summary>
    /// #119 Stage Day 1: 中央装飾全削除 + 中央 2×2 タイル隆起フラット化。
    ///
    /// User feedback (2026-06-03): 「真ん中行けば勝ちに見える、つまらん」
    /// Synthesizer 確定: Codex が配置した中央誘導装飾 (GreenDisk / SafeRim / RouteLanguage /
    ///                  Approach / Lane / Inset / ChokeTick / Visual) が「中央 = ゴール」
    ///                  逆方向誘導の真因。fun_design.md の「中央 = small final arena
    ///                  (DANGER)」設計と矛盾。Day 1 で削除 + フラット化。
    ///
    /// 維持: Tile_X*_Z* (64), ShrinkingIsland_Stage, HalfWall (6 個、橋接続点で Day 2
    ///       に活用), BouncePadAaa instances (4), EggybotPlayer, CinemachineCamera,
    ///       Main Camera, Global Volume, Directional Light, ShrinkingIslandManager,
    ///       KillVolume, Meshy_CenterGoalBeacon (Day 5 で DANGER アイコンに置換予定、
    ///       現状維持で Day 2 ステップ判断)
    /// </summary>
    internal static class ShrinkingIslandStageDay1Cleanup
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox_ShrinkingIsland_v1.unity";

        // 削除対象 prefix (これらで始まる GameObject 名は全部削除)
        static readonly string[] DeletePrefixes = new[]
        {
            "CenterGoal",         // GreenDisk + Marker
            "FinalArena_SafeRim", // 12 child の SafeRim 系
            "FinalArena_Static",  // Codex 親、中央 2×2 装飾の wrapper
            "Approach_",          // Approach_Dash 等
            "Lane_",              // Lane_Cyan, Lane_*_Trim
            "RouteLanguage",      // 40 路標タイル wrapper
            "Inset_",             // 内側装飾
            "ChokeTick_",         // choke marker tick
            "Visual_",            // 装飾系 wrapper
        };

        // 削除しない GameObject 名 (上記 prefix とマッチしても除外)
        static readonly HashSet<string> KeepNames = new HashSet<string>
        {
            // Tile は別 prefix で start するので原則該当しない、ただし安全のため
        };

        [MenuItem("Tools/ChaosDraftParty/Stage Day 1: Remove Center Decor (#119)")]
        public static void Cleanup()
        {
            var log = new System.Text.StringBuilder();

            // Load scene
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid())
            {
                EditorUtility.DisplayDialog("Day 1 Cleanup", "Scene open failed", "OK");
                return;
            }

            // Collect deletion targets
            var toDelete = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                CollectDeletionTargets(root.transform, toDelete);
            }

            log.AppendLine($"Found {toDelete.Count} GameObject(s) to delete");

            // Delete
            int deleted = 0;
            foreach (var go in toDelete)
            {
                if (go == null) continue;
                log.AppendLine($"  - {go.name}");
                Object.DestroyImmediate(go);
                deleted++;
            }
            log.AppendLine($"Deleted: {deleted}");

            // Flatten center 2x2 tiles Y to match other tiles
            // 他のタイル Y を取得 (Tile_X0_Z0 想定)
            float refY = 0f;
            var refTile = GameObject.Find("Tile_X0_Z0");
            if (refTile != null) refY = refTile.transform.localPosition.y;
            log.AppendLine($"Reference tile Y (Tile_X0_Z0): {refY}");

            string[] centerNames = { "Tile_X3_Z3", "Tile_X3_Z4", "Tile_X4_Z3", "Tile_X4_Z4" };
            int flattened = 0;
            foreach (var name in centerNames)
            {
                var t = GameObject.Find(name);
                if (t == null) continue;
                var pos = t.transform.localPosition;
                if (Mathf.Abs(pos.y - refY) > 0.001f)
                {
                    log.AppendLine($"  Flatten {name}: Y {pos.y} -> {refY}");
                    pos.y = refY;
                    t.transform.localPosition = pos;
                    flattened++;
                }
            }
            log.AppendLine($"Flattened: {flattened} center tiles");

            // Save
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);

            string summary = $"Day 1 cleanup complete\nDeleted: {deleted}\nFlattened center tiles: {flattened}\n\n--- log ---\n{log}";
            EditorUtility.DisplayDialog("Stage Day 1 (#119)", summary, "OK");
            Debug.Log($"[#119] {summary}");
        }

        static void CollectDeletionTargets(Transform t, List<GameObject> targets)
        {
            // Recursively scan
            // Add this GameObject if matches a delete prefix
            string n = t.gameObject.name;
            if (!KeepNames.Contains(n))
            {
                foreach (var prefix in DeletePrefixes)
                {
                    if (n.StartsWith(prefix))
                    {
                        targets.Add(t.gameObject);
                        return; // don't recurse into children (they'll be deleted with parent)
                    }
                }
            }

            // Recurse children (using a snapshot since we may delete)
            int childCount = t.childCount;
            var snapshot = new Transform[childCount];
            for (int i = 0; i < childCount; i++) snapshot[i] = t.GetChild(i);
            foreach (var c in snapshot)
            {
                if (c == null) continue;
                CollectDeletionTargets(c, targets);
            }
        }
    }
}
#endif
