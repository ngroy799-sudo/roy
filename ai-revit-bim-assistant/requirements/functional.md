# 功能需求（Functional Requirements）

## FR-01 對話介面
- 在 Revit 內提供 Chat 面板
- 支援文字輸入；可選語音
- 顯示 AI 理解摘要與即將執行的 Action 清單

## FR-02 模型上下文感知
系統需自動收集並傳送：
- 專案名稱、單位（mm/ft）
- 目前樓層、視圖類型、選取元素 Id／類型
- 可用 WallType / Family 名稱清單（可快取）
- 軸網與 Level 名稱

## FR-03 結構化指令生成
- 將自然語言轉成 BIM Action Schema
- 每個 Action 必須通過 JSON Schema 驗證
- 不支援的意圖要明確回覆「無法執行」並建議替代做法

## FR-04 建模操作（MVP）
白名單至少支援：
- CreateWall / CreateFloor / CreateColumn / CreateBeam（或結構線）
- PlaceDoor / PlaceWindow（需宿主牆）
- CreateRoom / RoomSeparation
- SetParameter（名稱、值）
- CreateViewPlan / CreateSheet / PlaceViewOnSheet
- Align / Move / Delete（Delete 需確認）

## FR-05 預覽與確認
- 執行前顯示 Action Diff（將建立／修改／刪除什麼）
- 高風險操作強制確認
- 一律可透過 Revit Undo 還原

## FR-06 出圖輔助
- 依樓層批次建平面視圖
- 套用 View Template
- 套用 Titleblock 建立 Sheet
- 放置視圖與基本標註（後期可加深）

## FR-07 規範／品質檢查
- 規則引擎或 LLM+規則混合：
  - 命名是否合規
  - 走廊淨寬、門寬最小值
  - 未命名房間、未放置標籤
- 輸出可定位到元素的檢查報告

## FR-08 知識庫（RAG）
- 匯入公司 BIM 標準文件
- 查詢族庫與參數說明
- 回答「我們專案牆要用哪種類型」類問題

## FR-09 審計與回放
- 記錄：誰、何時、哪個專案、Prompt、Actions、結果
- 失敗原因可診斷

## FR-10 多版本 Revit
- 明確宣告支援的 Revit 年版本
- Add-in 建置矩陣（每版本一個產出或條件編譯）

## 後續功能（非 MVP）
- 草圖／PDF 平面圖 OCR → 牆線生成
- 跨專業協調（MEP vs Arch）
- Design Automation 雲端批次
- 與 ACC Issue 雙向同步
