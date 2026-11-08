---
description: "Use when creating or handling Blender models, FBX exports, previews, screenshots, or other temporary Unity project assets."
applyTo:
  - "Assets/Generated/Blender/**"
  - "Assets/Temp/**"
---
# Temporary Unity Assets

- Create new model work files and intermediate exports under `Assets/Temp/Models/`, grouped into a feature-specific subfolder when useful.
- Keep temporary renders, screenshots, and validation output under `Assets/Temp/`; do not place scratch files in `Assets/Generated/`.
- `Assets/Temp/` and its folder metadata are intentionally ignored by Git. Do not force-add temporary files.
- When a model is approved and referenced by a committed scene or prefab, promote its final `.blend` and `.fbx` files to `Assets/Generated/Blender/`, retain their Unity `.meta` files, and commit all required dependencies.
