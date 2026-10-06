#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ChaosDraftParty.Editor
{
    /// <summary>
    /// #116 VFX Scene Wiring (Sandbox_ShrinkingIsland_v1)。
    /// 前提: Materials (7) + Prefabs (BouncePadAaa, SI_TileDustVfx) 生成済。
    /// </summary>
    internal static class ShrinkingIslandVfxSceneWiring
    {
        const string ScenePath = "Assets/_Project/Scenes/Sandbox_ShrinkingIsland_v1.unity";
        const string BouncePadAaaPath = "Assets/_Project/Prefabs/Minigames/ShrinkingIsland/BouncePadAaa.prefab";
        const string TileDustVfxPath = "Assets/_Project/Prefabs/Minigames/ShrinkingIsland/SI_TileDustVfx.prefab";
        const string MatTileWarningPath = "Assets/_Project/Art/Materials/ShrinkingIsland/M_TileWarning.mat";
        const string MatTileDissolvePath = "Assets/_Project/Art/Materials/ShrinkingIsland/M_TileDissolve.mat";
        const string ObsoleteTileFadeBurst = "Assets/_Project/Prefabs/Minigames/ShrinkingIsland/SI_TileFadeBurstVfx.prefab";
        const string ObsoleteTileWarningVfx = "Assets/_Project/Prefabs/Minigames/ShrinkingIsland/SI_TileWarningVfx.prefab";

        [MenuItem("Tools/ChaosDraftParty/Wire VFX Scene (#116)")]
        public static void Wire()
        {
            var log = new System.Text.StringBuilder();
            int replacedBouncePads = 0;
            int tilePrefabsUpdated = 0;
            int obsoleteDeleted = 0;

            // 1. Load asset refs
            var bouncePadPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BouncePadAaaPath);
            var tileDustPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(TileDustVfxPath);
            var matWarning = AssetDatabase.LoadAssetAtPath<Material>(MatTileWarningPath);
            var matDissolve = AssetDatabase.LoadAssetAtPath<Material>(MatTileDissolvePath);

            if (bouncePadPrefab == null) { log.AppendLine("ABORT: BouncePadAaa.prefab missing"); Done(log, false); return; }
            if (tileDustPrefab == null) { log.AppendLine("ABORT: SI_TileDustVfx.prefab missing"); Done(log, false); return; }
            if (matWarning == null) { log.AppendLine("WARN: M_TileWarning.mat missing"); }
            if (matDissolve == null) { log.AppendLine("WARN: M_TileDissolve.mat missing"); }

            // 2. Open scene
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            if (!scene.IsValid()) { log.AppendLine("ABORT: scene open failed"); Done(log, false); return; }

            // 3. Find & replace BouncePad GameObjects
            var bpType = System.Type.GetType("ChaosDraftParty.Minigames.ShrinkingIsland.BouncePad, ChaosDraftParty.Minigames");
            if (bpType == null) { log.AppendLine("ABORT: BouncePad type not found"); Done(log, false); return; }

            // Collect old BouncePad instances (any Component of type BouncePad on scene roots/children)
            var oldPads = new List<GameObject>();
            foreach (var root in scene.GetRootGameObjects())
            {
                var comps = root.GetComponentsInChildren(bpType, true);
                foreach (var c in comps) oldPads.Add(((Component)c).gameObject);
            }
            log.AppendLine($"Found {oldPads.Count} existing BouncePad instances");

            // Record transforms and replace
            foreach (var oldPad in oldPads)
            {
                if (oldPad == null) continue;
                // Skip if already a BouncePadAaa prefab instance (idempotency)
                var src = PrefabUtility.GetCorrespondingObjectFromSource(oldPad);
                if (src == bouncePadPrefab)
                {
                    log.AppendLine($"Skip (already BouncePadAaa): {oldPad.name}");
                    continue;
                }

                Vector3 pos = oldPad.transform.position;
                Quaternion rot = oldPad.transform.rotation;
                Transform parent = oldPad.transform.parent;

                Object.DestroyImmediate(oldPad);
                var newInstance = (GameObject)PrefabUtility.InstantiatePrefab(bouncePadPrefab);
                newInstance.transform.position = pos;
                newInstance.transform.rotation = rot;
                if (parent != null) newInstance.transform.SetParent(parent, true);
                replacedBouncePads++;
            }
            log.AppendLine($"Replaced {replacedBouncePads} BouncePad(s) with BouncePadAaa.prefab");

            // 4. Tile prefab Material + _fadeParticlePrefab wiring
            // Tile prefab paths: search in ShrinkingIsland folder
            var tileGuids = AssetDatabase.FindAssets("t:Prefab Tile", new[] { "Assets/_Project/Prefabs/Minigames/ShrinkingIsland" });
            foreach (var guid in tileGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!path.EndsWith(".prefab")) continue;
                var fname = System.IO.Path.GetFileName(path);
                // Only target actual Tile prefabs (avoid SI_Tile* misc)
                if (!fname.StartsWith("Tile") && !fname.StartsWith("SI_Tile")) continue;
                // Exclude obsolete VFX-only prefabs
                if (fname.Contains("FadeBurst") || fname.Contains("WarningVfx") || fname.Contains("DustVfx")) continue;

                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                if (prefab == null) continue;
                bool changed = WireTilePrefab(prefab, matWarning, tileDustPrefab, log);
                if (changed) tilePrefabsUpdated++;
            }
            log.AppendLine($"Updated {tilePrefabsUpdated} Tile prefab(s)");

            // 4c. Scene 内の Tile instance (prefab 化されてないやつ) も wiring
            // #107 で Tile 64 個は scene 直接配置 (prefab なし)。MeshRenderer.sharedMaterial と
            // Tile.cs._fadeParticlePrefab を SerializedObject 経由で書換える。
            int sceneTilesUpdated = 0;
            var tileTypeForScene = System.Type.GetType("ChaosDraftParty.Minigames.ShrinkingIsland.Tile, ChaosDraftParty.Minigames");
            if (tileTypeForScene != null)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var tilesInScene = root.GetComponentsInChildren(tileTypeForScene, true);
                    foreach (var tc in tilesInScene)
                    {
                        var tileGo = ((Component)tc).gameObject;

                        // Material 切替 (MeshRenderer の sharedMaterial)
                        if (matWarning != null)
                        {
                            var renderers = tileGo.GetComponentsInChildren<MeshRenderer>(true);
                            bool matChanged = false;
                            foreach (var r in renderers)
                            {
                                if (r.sharedMaterial != matWarning)
                                {
                                    Undo.RecordObject(r, "Wire Tile Material");
                                    r.sharedMaterial = matWarning;
                                    EditorUtility.SetDirty(r);
                                    matChanged = true;
                                }
                            }
                            if (matChanged) sceneTilesUpdated++;
                        }

                        // _fadeParticlePrefab wiring
                        if (tileDustPrefab != null)
                        {
                            var so = new SerializedObject((Object)tc);
                            var p = so.FindProperty("_fadeParticlePrefab");
                            if (p != null && p.objectReferenceValue != tileDustPrefab)
                            {
                                p.objectReferenceValue = tileDustPrefab;
                                so.ApplyModifiedProperties();
                            }
                        }
                    }
                }
            }
            log.AppendLine($"Updated {sceneTilesUpdated} scene Tile instance(s) (material + _fadeParticlePrefab)");

            // 5. ShrinkingIslandManager Inspector
            var mgrType = System.Type.GetType("ChaosDraftParty.Minigames.ShrinkingIsland.ShrinkingIslandManager, ChaosDraftParty.Minigames");
            if (mgrType != null)
            {
                foreach (var root in scene.GetRootGameObjects())
                {
                    var mgr = root.GetComponentInChildren(mgrType, true);
                    if (mgr != null)
                    {
                        var so = new SerializedObject(mgr);
                        // StaggerPattern enum: 0=Simultaneous, 1=RowByRow, 2=Random
                        var spProp = so.FindProperty("_staggerPattern");
                        if (spProp != null) spProp.enumValueIndex = 1;
                        var rsProp = so.FindProperty("_rowStaggerDelay");
                        if (rsProp != null && rsProp.propertyType == SerializedPropertyType.Float) rsProp.floatValue = 0.08f;
                        so.ApplyModifiedProperties();
                        log.AppendLine($"ShrinkingIslandManager: stagger=RowByRow, delay=0.08");
                        break;
                    }
                }
            }

            // 6. Save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("Scene saved");

            // 7. Delete obsolete prefabs (defensive: only if no scene/asset references remain)
            // To be safe, leave them on disk and only log. User can delete manually if confirmed.
            foreach (var path in new[] { ObsoleteTileFadeBurst, ObsoleteTileWarningVfx })
            {
                if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null)
                {
                    log.AppendLine($"NOTE: obsolete prefab still present (not deleted, manual review): {path}");
                }
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Done(log, true, $"BouncePadAaa replaced: {replacedBouncePads}\nTile prefabs updated: {tilePrefabsUpdated}\nObsolete prefabs flagged for review: see log");
        }

        static bool WireTilePrefab(GameObject prefab, Material matWarning, GameObject tileDustPrefab, System.Text.StringBuilder log)
        {
            bool changed = false;
            var path = AssetDatabase.GetAssetPath(prefab);
            var content = PrefabUtility.LoadPrefabContents(path);
            try
            {
                // 4a. MeshRenderer.sharedMaterial = M_TileWarning (default state for Healthy/Warning)
                if (matWarning != null)
                {
                    var renderers = content.GetComponentsInChildren<MeshRenderer>(true);
                    foreach (var r in renderers)
                    {
                        if (r.sharedMaterial != matWarning)
                        {
                            r.sharedMaterial = matWarning;
                            changed = true;
                        }
                    }
                }

                // 4b. Tile.cs _fadeParticlePrefab field -> SI_TileDustVfx
                var tileType = System.Type.GetType("ChaosDraftParty.Minigames.ShrinkingIsland.Tile, ChaosDraftParty.Minigames");
                if (tileType != null && tileDustPrefab != null)
                {
                    var tileComps = content.GetComponentsInChildren(tileType, true);
                    foreach (var c in tileComps)
                    {
                        var so = new SerializedObject((Object)c);
                        var p = so.FindProperty("_fadeParticlePrefab");
                        if (p != null && p.objectReferenceValue != tileDustPrefab)
                        {
                            p.objectReferenceValue = tileDustPrefab;
                            so.ApplyModifiedProperties();
                            changed = true;
                        }
                    }
                }

                if (changed)
                {
                    PrefabUtility.SaveAsPrefabAsset(content, path);
                    log.AppendLine($"Tile prefab updated: {path}");
                }
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(content);
            }
            return changed;
        }

        static void Done(System.Text.StringBuilder log, bool ok, string summary = null)
        {
            string title = "VFX Scene Wiring (#116)";
            string body = $"{(ok ? "OK" : "FAILED")}\n\n{summary}\n\n--- log ---\n{log}";
            EditorUtility.DisplayDialog(title, body, "OK");
            Debug.Log($"[#116] {body}");
        }
    }
}
#endif
