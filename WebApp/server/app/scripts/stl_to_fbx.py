"""
Blender headless script – import STL files and export a single FBX.

Usage:
    blender --background --python stl_to_fbx.py -- \
        --stl-dir /path/to/stl \
        --output  /path/to/model.fbx \
        --colors  /path/to/organ_colors.json

Each *.stl file in --stl-dir becomes a separate mesh object named after
the organ (filename stem).  A distinct material with the colour and alpha
from the config is assigned to every object.  Unknown organs fall back to
the "default" entry in the config.
"""

import sys
import json
import argparse
from pathlib import Path

import bpy  # type: ignore


# ── CLI arguments (after the "--" separator) ─────────────────────────────

def parse_args() -> argparse.Namespace:
    """Parse arguments that come after Blender's own ``--`` separator."""
    argv = sys.argv[sys.argv.index("--") + 1:] if "--" in sys.argv else []
    parser = argparse.ArgumentParser(description="STL → FBX converter")
    parser.add_argument("--stl-dir", required=True, help="Directory with .stl files")
    parser.add_argument("--output", required=True, help="Output .fbx path")
    parser.add_argument("--colors", required=True, help="organ_colors.json path")
    return parser.parse_args(argv)


# ── Helpers ──────────────────────────────────────────────────────────────

def clear_scene() -> None:
    """Remove every object, mesh, material and collection from the scene."""
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    for block_type in (bpy.data.meshes, bpy.data.materials, bpy.data.cameras, bpy.data.lights):
        for block in block_type:
            block_type.remove(block)


def load_color_config(path: str) -> dict:
    """Load the organ colour config JSON."""
    with open(path, "r") as f:
        return json.load(f)


def create_material(name: str, rgba: list[float]) -> bpy.types.Material:
    """
    Create a Principled BSDF material with the given RGBA colour.
    Alpha < 1 enables transparent blending.
    """
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True

    nodes = mat.node_tree.nodes
    bsdf = nodes.get("Principled BSDF")
    if bsdf is None:
        bsdf = nodes.new("ShaderNodeBsdfPrincipled")

    r, g, b, a = rgba
    bsdf.inputs["Base Color"].default_value = (r, g, b, 1.0)
    bsdf.inputs["Alpha"].default_value = a

    if a < 1.0:
        mat.blend_method = "BLEND"
        mat.shadow_method = "CLIP"
        mat.use_backface_culling = False

    return mat


def import_stl(filepath: str) -> bpy.types.Object | None:
    """Import an STL and return the resulting object, or None if empty."""
    file_size = Path(filepath).stat().st_size
    if file_size <= 84:
        return None

    bpy.ops.wm.stl_import(filepath=filepath)
    obj = bpy.context.active_object

    if obj is None or obj.type != "MESH" or len(obj.data.vertices) == 0:
        if obj:
            bpy.data.objects.remove(obj)
        return None

    return obj


def fix_normals(obj: bpy.types.Object) -> None:
    """Recalculate normals to point outward."""
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.mode_set(mode="EDIT")
    bpy.ops.mesh.select_all(action="SELECT")
    bpy.ops.mesh.normals_make_consistent(inside=False)
    bpy.ops.object.mode_set(mode="OBJECT")


def remesh_and_smooth(obj: bpy.types.Object) -> None:
    """Remesh to reduce vertices and smooth out pixelated voxel geometry, then shade smooth."""
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)

    bpy.ops.object.make_single_user(object=True, obdata=True)

    mod = obj.modifiers.new(name="Remesh", type="REMESH")
    mod.mode = "SMOOTH"
    mod.octree_depth = 6
    mod.scale = 0.9

    bpy.ops.object.modifier_apply(modifier=mod.name)
    bpy.ops.object.shade_smooth()


def center_all_objects() -> None:
    """
    Move all objects so their combined bounding box is centred at the
    world origin.  This corrects for medical-imaging coordinates that
    are far from (0, 0, 0).
    """
    import mathutils

    objs = [o for o in bpy.data.objects if o.type == "MESH"]
    if not objs:
        return

    all_coords = []
    for obj in objs:
        for corner in obj.bound_box:
            all_coords.append(obj.matrix_world @ mathutils.Vector(corner))

    bbox_min = mathutils.Vector((
        min(v.x for v in all_coords),
        min(v.y for v in all_coords),
        min(v.z for v in all_coords),
    ))
    bbox_max = mathutils.Vector((
        max(v.x for v in all_coords),
        max(v.y for v in all_coords),
        max(v.z for v in all_coords),
    ))
    centre = (bbox_min + bbox_max) / 2

    for obj in objs:
        obj.location -= centre


# ── Main ─────────────────────────────────────────────────────────────────

def main() -> None:
    args = parse_args()

    stl_dir = Path(args.stl_dir)
    output_path = Path(args.output)
    color_config = load_color_config(args.colors)

    default_rgba = color_config.get("default", {}).get("color", [0.8, 0.8, 0.8, 0.4])
    organ_colors = color_config.get("organs", {})

    stl_files = sorted(stl_dir.glob("*.stl"))
    if not stl_files:
        sys.exit(1)

    clear_scene()

    imported_count = 0
    for stl_path in stl_files:
        organ = stl_path.stem

        obj = import_stl(str(stl_path))
        if obj is None:
            continue

        obj.name = organ
        fix_normals(obj)
        remesh_and_smooth(obj)

        organ_cfg = organ_colors.get(organ, {})
        rgba = organ_cfg.get("color", default_rgba)
        display_name = organ_cfg.get("name", organ)

        alpha_tag = f"__a{int(rgba[3] * 100):03d}"
        mat = create_material(f"M_{display_name}{alpha_tag}", rgba)
        obj.data.materials.clear()
        obj.data.materials.append(mat)

        obj.select_set(False)
        imported_count += 1

    if imported_count == 0:
        sys.exit(1)

    for obj in bpy.data.objects:
        if obj.type == "MESH":
            obj.scale *= 0.001
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)

    center_all_objects()

    bpy.ops.object.select_all(action="SELECT")
    output_path.parent.mkdir(parents=True, exist_ok=True)

    bpy.ops.export_scene.fbx(
        filepath=str(output_path),
        use_selection=True,
        apply_scale_options="FBX_SCALE_NONE",
        path_mode="COPY",
        embed_textures=False,
        mesh_smooth_type="OFF",
    )


if __name__ == "__main__":
    main()
