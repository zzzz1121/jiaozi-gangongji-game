"""Create the editable workshop revision and a Unity GLB from the shipped environment."""
import sys
import bpy

source, blend, target = sys.argv[sys.argv.index("--") + 1:]
bpy.ops.wm.read_factory_settings(use_empty=True)
bpy.ops.import_scene.gltf(filepath=source)

for obj in bpy.context.scene.objects:
    name = obj.name
    if obj.type != "MESH" or "__Collider_" in name:
        continue
    if name.startswith("station_") and any(part in name for part in (
        "tabletop__core", "counter__core", "__side_x", "__side_y",
        "__upright", "__crossbar", "__corner_post",
    )) and "rope" not in name:
        bevel = obj.modifiers.new("Soft paper edges", "BEVEL")
        bevel.width = .022
        bevel.segments = 3
        bevel.limit_method = "ANGLE"
        bevel.harden_normals = True
    if name.endswith(("tabletop__edge_shadow", "counter__edge_shadow")):
        # These meshes have baked origins. Offset vertices in world space below the upper edge.
        obj.data = obj.data.copy()
        world = obj.matrix_world
        inverse = world.inverted()
        ceiling = max((world @ vert.co).z for vert in obj.data.vertices)
        for vert in obj.data.vertices:
            point = world @ vert.co
            point.z = ceiling + (point.z - ceiling) * 1.38
            vert.co = inverse @ point
    if name.startswith(("eave_", "wall_front_")) and "tile" in name:
        bevel = obj.modifiers.new("Rounded tile edges", "BEVEL")
        bevel.width = .011
        bevel.segments = 2
        bevel.limit_method = "ANGLE"

for name, factor in (("v01_stamp", 1.38), ("v01_print_block", 1.13)):
    tool = bpy.data.objects.get(name)
    if tool is not None:
        tool.scale.x *= factor
        tool.scale.y *= factor

bpy.ops.file.pack_all()
bpy.ops.wm.save_as_mainfile(filepath=blend)
bpy.ops.export_scene.gltf(filepath=target, export_format="GLB", export_apply=True)
