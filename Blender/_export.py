import bpy

objects = bpy.context.scene.objects

for obj in objects:
    obj.select_set(True)

bpy.ops.export_scene.fbx(
                     filepath=bpy.data.filepath[:-5] + 'fbx',
                     check_existing=True,
                     filter_glob='*.fbx',
                     use_selection=True,
                     use_visible=False,
                     use_active_collection=False,
                     collection='',
                     global_scale=1.0,
                     apply_unit_scale=True,
                     apply_scale_options='FBX_SCALE_NONE',
                     use_space_transform=True,
                     bake_space_transform=True,
                     object_types={'ARMATURE', 'MESH'},
                     use_mesh_modifiers=True,
                     use_mesh_modifiers_render=True,
                     mesh_smooth_type='OFF',
                     colors_type='SRGB',
                     prioritize_active_color=False,
                     use_subsurf=False,
                     use_mesh_edges=False,
                     use_tspace=False,
                     use_triangles=False,
                     use_custom_props=False,
                     add_leaf_bones=False,
                     primary_bone_axis='Y',
                     secondary_bone_axis='X',
                     use_armature_deform_only=False,
                     armature_nodetype='NULL',
                     bake_anim=False,
                     bake_anim_use_all_bones=True,
                     bake_anim_use_nla_strips=True,
                     bake_anim_use_all_actions=True,
                     bake_anim_force_startend_keying=True,
                     bake_anim_step=1.0,
                     bake_anim_simplify_factor=1.0,
                     path_mode='COPY',
                     embed_textures=False,
                     batch_mode='OFF',
                     use_batch_own_dir=True,
                     use_metadata=True,
                     axis_forward='-Z',
                     axis_up='Y'
                     )
