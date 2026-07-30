# How to set Revit keyboard shortcuts for TagAlign Tool

Revit **does not** let add-ins hard-code global shortcuts via API.
You assign them once in Revit’s **Keyboard Shortcuts (KS)** dialog.

## Method 1 — Revit UI (recommended)

1. Install the plugin and **restart Revit 2023**
2. Confirm ribbon tab **TagAlign Tool** is visible
3. In Revit, type **`KS`** then Enter  
   (or: File → Options → User Interface → Keyboard Shortcuts)
4. In **Search**, type: **`TagAlign`**  
   (unique prefix — avoids colliding with Revit’s own “Align” commands)
5. You should see:
   - `TagAlign Align Selected Tags`
   - `TagAlign Leader Dashboard`
   - `TagAlign Settings Folder`
6. Select the command → **Press new keys** → type shortcut → **Assign** → **OK**

### Suggested shortcuts

| Command (KS search) | Suggested keys |
|---------------------|----------------|
| TagAlign Align Selected Tags | `TA` |
| TagAlign Leader Dashboard | `TD` |
| TagAlign Settings Folder | `TS` |

If keys conflict, try `TAA`, `TAL`, `RT`.

## If search finds nothing

1. Click ribbon **TagAlign Align Selected Tags** once (loads the command into KS)
2. Open **KS** again → search **`TagAlign`**

## Advanced — XML

```
%AppData%\Autodesk\Revit\Autodesk Revit 2023\KeyboardShortcuts.xml
```

Typical CommandIds after rename:

```
CustomCtrl_%CustomCtrl_%TagAlign Tool%TagAlign Commands%TagAlign Align Selected Tags
CustomCtrl_%CustomCtrl_%TagAlign Tool%TagAlign Commands%TagAlign Leader Dashboard
```

See `KeyboardShortcuts-TagAlign.sample.xml`.
