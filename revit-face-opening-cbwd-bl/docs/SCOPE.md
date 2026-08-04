# SCOPE — Face Opening Elevation → CBWD B.L.

## In scope

- 目前文件（active document）內的 **Generic Models**
- Family / Type 名稱含可設定關鍵字（預設 `Face Opening`）
- 以 BoundingBox **Min.Z** 作為底面 Elevation（Revit 內部長度單位）
- 寫入實例參數 **CBWD B.L.**（Dimensions 群組之長度參數）
- 選取模式：`SelectedOnly` | `AllOpenings`
- Dynamo Player 一鍵執行 + JSON 報告

## Out of scope

- Linked model 內的 Opening（本版只處理主文件）
- 兩個不同元件之間的配對寫入
- 自動建立缺失的 `CBWD B.L.` 參數（參數必須已存在於 family）
- 修改 Opening 幾何位置／host
- C# Revit add-in / pyRevit（本專案為 Dynamo + Python）

## Assumptions

- Face Opening 為 Generic Model family
- `CBWD B.L.` 為可寫入的 instance Double（長度）參數
- 目標環境：**Revit 2023** + Dynamo IronPython 2.7
