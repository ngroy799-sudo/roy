# How to set Revit keyboard shortcuts for Tag Align

Revit **does not** let add-ins hard-code global shortcuts via API.
You assign them once in Revit’s **Keyboard Shortcuts (KS)** dialog. After that they persist for your user profile.

## Method 1 — Revit UI (recommended)

1. Install the plugin and **restart Revit 2023**
2. Confirm ribbon tab **Tag Align** is visible
3. In Revit, type **`KS`** then Enter  
   (or: File → Options → User Interface → Keyboard Shortcuts)
4. In **Search**, type one of:
   - `Align Tags`
   - `Annotation Dashboard`
   - `Tag Align`
5. Select the command row
6. Click in **Press new keys**, type your shortcut (examples below), click **Assign**
7. Click **OK**

### Suggested shortcuts (examples — change if they conflict)

| Command | Suggested keys | Notes |
|---------|----------------|-------|
| Align Tags | `TA` | Tag Align |
| Annotation Dashboard | `AD` | Annotation Dashboard |

If Revit says the keys are already used, pick another combo (e.g. `TT`, `TG`, `AL`).

## Method 2 — Find command after first launch

Shortcuts only list commands that Revit has already loaded.
If search finds nothing:

1. Click the ribbon buttons once (or just open Revit with the add-in loaded)
2. Open **KS** again and search

## Method 3 — Optional XML path (advanced)

User shortcuts file:

```
%AppData%\Autodesk\Revit\Autodesk Revit 2023\KeyboardShortcuts.xml
```

Command ids for this add-in (typical pattern):

```
CustomCtrl_%CustomCtrl_%Tag Align%Alignment%Align Tags
CustomCtrl_%CustomCtrl_%Tag Align%Alignment%Annotation Dashboard
```

A starter snippet is in `KeyboardShortcuts-TagAlign.sample.xml`.
Prefer Method 1 — export your KS from Revit after assigning once if you need to copy to other PCs.

## Copy shortcuts to another PC

1. On the first PC: KS dialog → **Export**
2. On the other PC: KS dialog → **Import** (or merge carefully)

## Quick checklist

- [ ] Plugin DLL + `.addin` in `%AppData%\Autodesk\Revit\Addins\2023\`
- [ ] Revit restarted
- [ ] Tab **Tag Align** visible
- [ ] `KS` → search **Align Tags** → Assign
