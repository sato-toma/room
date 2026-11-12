---
description: "Use when editing Unity scenes, prefabs, scripts, or assets, or when checking Unity Console and compilation errors."
applyTo:
  - "Assets/**/*.unity"
  - "Assets/**/*.prefab"
  - "Assets/**/*.cs"
---
# Unity Console validation

- Check the Unity Console and script-compilation status before making changes so existing errors are distinguishable from new ones.
- After editing, wait for asset import and script compilation to finish, then inspect the Console again.
- A Unity task is complete only when the Console has no unresolved errors and script compilation has succeeded. Investigate existing errors too; do not label the task complete while errors remain.
- Fix error causes rather than hiding them by disabling diagnostics, suppressing logs, or merely clearing the Console. Clear stale entries only after their causes are resolved, then recheck.
- If Unity Editor or its Console is unavailable, report validation as unverified rather than claiming that errors are resolved.
