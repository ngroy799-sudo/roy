# TagAlign — Keyboard Shortcuts (KS)

## Why you might not see names in KS

Custom ribbon tabs are **sometimes missing** from Keyboard Shortcuts.
This build registers commands in **three** places so KS can find them:

1. **Add-Ins → External Tools** (most reliable for KS)
2. **Add-Ins → TagAlign** panel
3. **TagAlign Tool** custom tab

## Set shortcut (do this)

1. Restart Revit after installing (fully close Revit, open again)
2. Confirm you can run it:  
   **Add-Ins** tab → **External Tools** → **TagAlign Align Selected Tags**
3. Type **`KS`** → Enter
4. In Search type: **`TagAlign`**
5. Filter / list should show **All** (not only Architecture etc.)
6. Select **TagAlign Align Selected Tags** → press keys e.g. **`TA`** → **Assign** → OK

### Names to search

| KS / External Tools name | Suggested key |
|--------------------------|---------------|
| TagAlign Align Selected Tags | `TA` |
| TagAlign Leader Dashboard | `TD` |
| TagAlign Settings Folder | `TS` |

## Still empty?

1. FileLoadException / Unblock DLL first (`Install.bat` or Properties → Unblock)
2. Check `%AppData%\Autodesk\Revit\Addins\2023\` has both `.dll` and `.addin`
3. Open Revit → **Add-Ins → External Tools** — if TagAlign is not there, add-in did not load
4. In KS, clear search, set filter to **All**, then search `TagAlign` again
5. Click External Tools → TagAlign Align Selected Tags once, then reopen KS
