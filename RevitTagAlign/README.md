# RevitTagAlign - Tag Alignment Tool for Revit 2023

A Revit 2023 add-in that aligns Tags and Text Notes with leaders, making landing lines straight and leaders parallel. Includes an Annotation Dashboard for dynamic control of leader geometry.

## Features

### Align Tags Command
- Select 2+ tags/text notes and align them (Left, Right, Top, Bottom, Middle)
- Adjusts leaders to a specified angle so all are parallel
- Supports both IndependentTag and TextNote elements

### Annotation Dashboard (Modeless)
- **Scope**: Apply to Selected Tags, All Visible Tags, or auto-apply to New Tags
- **Leader Angle**: Slider 0°–90° to set leader direction
- **Landing Distance**: Control the horizontal landing line length
- **Leader Length**: Control overall leader length
- **Quick Presets**: 45° Standard, 60° Steep, 30° Shallow, Horizontal

## Installation

1. Build the project targeting .NET Framework 4.8
2. Copy `RevitTagAlign.dll` to a Revit add-ins folder:
   - `%AppData%\Autodesk\Revit\Addins\2023\`
3. Copy `RevitTagAlign.addin` to the same folder
4. Restart Revit 2023

## Build Requirements

- Visual Studio 2022
- .NET Framework 4.8 SDK
- Revit 2023 SDK (or NuGet package `Autodesk.Revit.SDK` 2023.0.0)

## Usage

1. Open Revit 2023 and navigate to the **Tag Align** ribbon tab
2. **Align Tags**: Select tags/text notes → click "Align Tags" → choose alignment mode and angle → OK
3. **Annotation Dashboard**: Click to open the modeless dashboard, adjust sliders, and click "Apply Now"

## Project Structure

```
RevitTagAlign/
├── RevitTagAlign.csproj          # Project file (net48, Revit 2023 SDK)
├── RevitTagAlign.addin           # Revit manifest file
├── App.cs                        # IExternalApplication - ribbon setup
├── AlignTagsCommand.cs           # Main alignment command
├── AlignOptionsWindow.xaml/.cs   # Alignment options dialog
├── AnnotationDashboardCommand.cs # Opens the dashboard
├── AnnotationDashboardWindow.xaml/.cs # Modeless dashboard UI
└── README.md
```
