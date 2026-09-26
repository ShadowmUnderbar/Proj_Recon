import bpy,json
mats={m for o in bpy.data.collections['ElseIf_Game_Optimized'].objects if o.type=='MESH' for m in o.data.materials if m};print(json.dumps({m.name:{'nodes':[(n.name,n.type) for n in m.node_tree.nodes],'links':[(l.from_node.name,l.from_socket.name,l.to_node.name,l.to_socket.name) for l in m.node_tree.links]} for m in mats if m.use_nodes}))
