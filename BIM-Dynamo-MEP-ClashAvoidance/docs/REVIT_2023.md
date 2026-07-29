# Target Environment — Revit 2023

本專案鎖定：**Autodesk Revit 2023** + 內建 **Dynamo for Revit**。

## 建議組合

| 項目 | 版本 |
|------|------|
| Revit | **2023**（含累積更新） |
| Dynamo for Revit | Revit 2023 內建（約 Dynamo 2.13–2.16，視更新而定） |
| Python 引擎 | **IronPython 2.7**（預設；本專案腳本已相容） |
| 可選 Package | Clockwork / Rhythm / spring nodes（非必須） |

## Dynamo 設定（Revit 2023）

1. 開啟 Dynamo → **Settings** → **Python**  
2. Engine 選 **IronPython2**（或 IronPython 2.7）  
3. 若你改用 CPython3，多數腳本仍可跑，但 `print_function` / 字串格式已寫成雙引擎友善  

## API 注意（2023）

- Element Id 用 `ElementId.IntegerValue`（本專案已優先用此）  
- Link 文件：`RevitLinkInstance.GetLinkDocument()` + `GetTotalTransform()`  
- 寫入：`TransactionManager`（Dynamo）包住 `LocationCurve.Move`  
- 單位：Revit 內部係 feet；腳本邊界轉 **mm**

## 已知差異（唔好當 2024/2025）

- 唔依賴 Revit 2024+ 先有嘅 API 變更  
- `.dyn` 用較通用節點型別；若 Dynamo 開檔提示缺節點，按 `docs/DYNAMO_ASSEMBLY.md` 手動組裝即可（最穩）

## 快速驗證

1. Revit 2023 開 MEP 模型 + ARC/STC Link  
2. Dynamo 開 `dynamo/MEP_AutoAvoid_ARC_STC.dyn`  
3. 按組裝清單貼 `python/00`–`06`  
4. `RunMode = DetectOnly` 跑一次，確認有 clash 報告
