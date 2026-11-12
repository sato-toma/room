---
description: "Use when editing Unity scenes or prefabs; preserve GameObjects marked as approved."
applyTo:
  - "Assets/**/*.unity"
  - "Assets/**/*.prefab"
---
# Approved Unity content

- Treat a GameObject whose name starts with `LOCKED_` as approved and read-only. Its children and referenced components/assets are included.
- Do not rename, move, reparent, delete, or change the Transform, components, children, or asset references of approved content. Do not add children beneath an approved GameObject.
- Add or revise content beside approved objects, or in a separate preview scene or prefab variant. Do not move existing objects under a new marker parent just to protect them; that can change hierarchy-dependent behavior.
- If a requested change requires editing approved content, stop before making that edit and ask for explicit permission. Once authorized, keep the change limited to the requested object and report what changed.
- A name marker is an agent convention, not a Unity-enforced lock. Unity's Static setting is for engine behavior, not edit protection. Use version-control commits for recoverable milestones and review scene/prefab diffs before accepting changes.

## Neon Geometry Room preview

- In `Assets/Generated/Scenes/NeonGeometryRoomPreview.unity`, preserve `LOCKED_Baseline` and its descendants. It contains the room/base, `System/DespawnHeight`, `Maze Entrance Spawn Point`, and `Box Boundary Stops`.
- Keep the seven `Start ...` movement triggers, passage `Stop 01 Entry` through `Stop 06 Skywalk`, and maze presentation objects outside `LOCKED_Baseline` so the movement gimmick remains editable.
- The preview scene's maze was unpacked to separate the locked boundary stops and spawn point from editable movement objects. Do not reconnect or replace it with the shared maze prefab unless explicitly requested.
