"""Converts glTF/GLB files to FBX with Blender, for Unity projects that do not use the glTFast package.

Run it with Blender, not with plain Python:
  blender --background --python scripts/blender_gltf_to_fbx.py -- input.glb output.fbx
Runs on Blender 3.x and 4.x (uses bpy.ops.import_scene.gltf and bpy.ops.export_scene.fbx).
"""
import sys

import bpy  # noqa: F401  (available only inside Blender)


def main():
    argv = sys.argv
    if "--" not in argv or len(argv) < argv.index("--") + 3:
        raise SystemExit("usage: blender --background --python blender_gltf_to_fbx.py -- input.glb output.fbx")
    source, target = argv[argv.index("--") + 1], argv[argv.index("--") + 2]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bpy.ops.import_scene.gltf(filepath=source)
    bpy.ops.export_scene.fbx(filepath=target, path_mode="COPY", embed_textures=True, apply_scale_options="FBX_SCALE_ALL", bake_anim=True)
    print("exported " + target)


main()
