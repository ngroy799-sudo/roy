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
`%AppData%\RevitTagAlign\AlignConfig.xml`
and restored the next time you open Configure.
Matches Bird Tools–style configure UI:
- **4 corner presets**: Upper-Left / Upper-Right / Lower-Left / Lower-Right
- **Pick Angle then Tag Position (Mouse)** — default ON:
  1. Two clicks define the **red arrow angle** (angled leader)
  2. One click sets the **tag text position** (stack origin)
- Yellow = horizontal landing; Red = parallel angled arrows
- Options: Switch Pick Point Side, Attached End Tags, Keep Selection, Turn Snaps Off
- TextNote justification, Constant Landing, Vertical / Intermittent spacing
- Help / Proceed / Cancel

### Annotation Dashboard (Modeless)
- **Scope**: Apply to Selected Tags, All Visible Tags, or auto-apply to New Tags
- **Leader Angle**: Slider 0°–90° to set leader direction
- **Landing Distance**: Control the horizontal landing line length
- **Leader Length**: Control overall leader length
- **Quick Presets**: 45° Standard, 60° Steep, 30° Shallow, Horizontal

## Project Structure

```
RevitTagAlign/
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
