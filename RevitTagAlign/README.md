# RevitTagAlign - Tag Alignment Tool for Revit 2023

A Revit 2023 add-in that aligns Tags and Text Notes with leaders, making landing lines straight and leaders are parallel. Includes an Annotation Dashboard for dynamic control of leader geometry.

## Get the DLL

Prebuilt files are already in the repo:

```
RevitTagAlign/dist/RevitTagAlign-2023/
├── RevitTagAlign.dll
├── RevitTagAlign.addin
└── INSTALL.txt
```

### Install into Revit 2023

Copy **both** `RevitTagAlign.dll` and `RevitTagAlign.addin` into:

```
%AppData%\Autodesk\Revit\Addins\2023\
```

Restart Revit 2023 → ribbon tab **Tag Align**.

### Rebuild (optional)

- Windows: run `Build.bat` → output in `bin\Release\`
- CI: Actions → **Build RevitTagAlign DLL** → artifact `RevitTagAlign-2023`

## Features

### Settings persistence
Configure values are saved on **Proceed** to:
`%AppData%\Roaming\RevitTagAlign\AlignConfig.xml`
and restored the next time you open Configure.

### Align Tags Command (Configure dialog)
Matches Bird Tools official Help + Configure:
- **4 corner presets**: Upper-Left / Upper-Right / Lower-Left / Lower-Right
- **Angle slider**: angle at the **elbow** only (horizontal landing ↔ red leader) — **not** the gap between stacked tags (use Vertical Spacing)
- **1 click**: position of the **closest tag to the tagged elements** (taghead)
  - Upper: that tag at stack bottom; others grow up
  - Lower: that tag at stack top; others grow down
- Repeat clicks until ESC
- **Vertical Spacing**: user-set gap (mm) between stacked tag texts
- **Anchor tag** (row 0 at click): sets **equal horizontal landing** for whole stack; **red leader** length varies per host; all red segments **parallel** (common-angle)
- **Vertical Spacing**: freely set (mm); enforces a no-overlap minimum from tag height
- **Leader contact face**: snapped to the original host face (left stays left). Ends stay on the element (no fly-away)
- **Constant Landing**: ON = fixed landing rather than common angle
- **Force Attached End Tags**: ON = Revit Attached (may re-pick face); OFF = Free snapped to original face
- Options: Switch Pick Point Side, Keep Selection, Turn Snaps Off
- TextNote justification, Vertical / Intermittent spacing
- May open Configure with empty selection, then pick tags after Proceed

**Shortcut:** Optional preselect → KS (e.g. TA) → Configure → click closest-tag position.

### Annotation Dashboard (Modeless)
- **Scope**: Apply to Selected Tags, All Visible Tags, or auto-apply to New Tags
- **Leader Angle**: Slider 0°–90° to set leader direction
- **Landing Distance**: Control the horizontal landing line length
- **Leader Length**: Control overall leader length
- **Quick Presets**: 45° Standard, 60° Steep, 30° Shallow, Horizontal

## Project Structure

```
RevitTagAlign/
├── docs/
│   └── ALIGNMENT-BEHAVIOR.md         # 對齊行為說明（60° elbow、host 場景）
├── Build.bat                         # One-click Windows build → DLL
├── RevitTagAlign.csproj
├── RevitTagAlign.addin
├── App.cs
├── AlignTagsCommand.cs
├── AlignOptionsWindow.xaml/.cs
├── AnnotationDashboardCommand.cs
├── AnnotationDashboardWindow.xaml/.cs
├── bin/Release/                      # After build: DLL lives here
└── dist/                             # Packaged install folder (CI)
```
