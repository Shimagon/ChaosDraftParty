import json
import os
import traceback

import unreal


SOURCE_FBX = r"C:\Users\taise\Documents\GitHub\ChaosDraftParty\Assets\ThirdParty\Meshy\Characters\EGGYBOT\eggybot_v16.fbx"
DESTINATION_PATH = "/Game/CDP/Characters/Eggybot"
LOG_PATH = r"C:\Users\taise\Documents\GitHub\ChaosDraftParty\Unreal\ChaosDraftPartyUE\Saved\import_eggybot_result.json"


def make_import_options():
    options = unreal.FbxImportUI()
    options.import_mesh = True
    options.import_as_skeletal = True
    options.import_materials = True
    options.import_textures = True
    options.mesh_type_to_import = unreal.FBXImportType.FBXIT_SKELETAL_MESH
    options.automated_import_should_detect_type = False

    return options


def main():
    result = {
        "source": SOURCE_FBX,
        "destination": DESTINATION_PATH,
        "imported": [],
        "success": False,
        "error": None,
    }

    try:
        if not os.path.exists(SOURCE_FBX):
            raise FileNotFoundError(SOURCE_FBX)

        unreal.EditorAssetLibrary.make_directory(DESTINATION_PATH)

        task = unreal.AssetImportTask()
        task.filename = SOURCE_FBX
        task.destination_path = DESTINATION_PATH
        task.destination_name = "SK_EggybotV16"
        task.automated = True
        task.replace_existing = True
        task.save = True
        task.options = make_import_options()

        unreal.AssetToolsHelpers.get_asset_tools().import_asset_tasks([task])

        imported = list(task.imported_object_paths)
        result["imported"] = imported
        result["success"] = len(imported) > 0

        for path in imported:
            unreal.EditorAssetLibrary.save_asset(path, only_if_is_dirty=False)
    except Exception as exc:
        result["error"] = "".join(traceback.format_exception_only(type(exc), exc)).strip()
        result["traceback"] = traceback.format_exc()

    os.makedirs(os.path.dirname(LOG_PATH), exist_ok=True)
    with open(LOG_PATH, "w", encoding="utf-8") as f:
        json.dump(result, f, ensure_ascii=False, indent=2)

    if not result["success"]:
        raise RuntimeError(result["error"] or "Eggybot import failed")


main()
