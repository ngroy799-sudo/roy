# SCOPE：Revit 2023 Dynamo 自動避開選定 MEP / ARC / STC

**專案代碼**：`revit-dynamo-mep-avoid-2023`  
**狀態**：草稿（等待你決策）  
**Revit**：2023  
**交付物（本階段）**：本 SCOPE，不含 Dynamo 實作

---

## 1. 一句話目標（暫定）

在 Revit 2023 用 Dynamo，針對使用者選定（或規則篩選）的 **MEP / ARC / STC** 元素，偵測空間衝突／淨空不足，並以可控制方式 **自動調整路徑或位置以避開障礙**。

> 「避開」具體行為尚未鎖定（見第 5 節選項與第 8 節問題）。

---

## 2. 名詞定義（本專案）

| 代號 | 含義（暫定） | Revit 常見對應 |
|------|--------------|----------------|
| **MEP** | 機電 | Ducts、Pipes、Cable Trays、Conduits、Fixtures、Equipment |
| **ARC** | 建築 | Walls、Floors、Ceilings、Doors、Windows、Generic Models（建築） |
| **STC** | 結構 | Structural Framing、Columns、Foundations、Structural Floors |

可改為你公司內部分類／Workset／參數規則。

---

## 3. In Scope（建議納入第一版）

1. **輸入**
   - 手動選取「要移動／繞行的元素」（Subject）
   - 手動選取或分類篩選「障礙物」（Obstacles：MEP / ARC / STC）
2. **衝突判斷**
   - 以 Bounding Box 或 Solid Intersection 偵測重疊
   - 可設定 **淨空 Clearance**（例如四周預留 mm）
3. **避開動作（擇一實作，見第 5 節）**
   - 水平偏移、垂直偏移、或簡易折線繞行
4. **安全與回饋**
   - 預覽衝突清單（Element Id、類別、距離）
   - 可選：只報告不修改 / 修改前 Transaction 可 Undo
5. **環境**
   - 現行模型元素（非 Link 優先；Link 為選配）
   - Dynamo Player 或手動跑圖均可

---

## 4. Out of Scope（建議第一版不做）

| 項目 | 原因 |
|------|------|
| 完整自動 MEP 路由引擎（類似專業 pipe routing） | 複雜度高、需大量規則 |
| 自動重算系統壓力／水力／風量 | 屬分析軟體範疇 |
| 跨 Link 模型寫回 Linked 檔 | 權限與工作流複雜 |
| 與 Navisworks / BIM 360 Clash 雙向同步 | 可列 Phase 2 |
| UI 獨立外掛（C# Revit Add-in） | 除非你要求；本階段以 Dynamo 為主 |
| 非 2023 版本相容保證 | 先鎖 2023 |

---

## 5. 方案選項（請你選方向）

### 選項 A — Clash Report Only（只偵測）
- 輸出衝突表、3D 標記、Excel/CSV
- **不移動**模型
- 風險最低，適合先驗證規則

### 選項 B — Simple Offset Avoid（簡易偏移避開）【建議 MVP】
- Subject 與 Obstacle 衝突時，沿指定軸（X/Y/Z 或垂直於障礙）偏移固定距離或直到無衝突
- 適合：管／風管／橋架被梁、牆、其他管擋住的局部修正
- 限制：可能打斷系統連續性、接頭需後續手動修

### 選項 C — Polyline Reroute（折線繞行）
- 在衝突區前後插入彎頭／fitting，繞過障礙外包盒
- 較接近「自動避開」，但 Fitting 類型、坡度、連接規則較多

### 選項 D — Hybrid
- 先 Report → 使用者勾選要處理的衝突 → 再 Offset 或 Reroute

**建議預設**：`D` 流程外殼 + 第一版只做 `A` 再加 `B`。

---

## 6. 技術架構（暫定）

```
[User Selection / Category Filter]
            │
            ▼
   Collect Subjects + Obstacles
            │
            ▼
   Expand solids by Clearance
            │
            ▼
   Clash / Distance Check (Dynamo + Python)
            │
            ├─ Mode: Report → List / Markers
            └─ Mode: Avoid  → Move / Reroute (Transaction)
```

| 層 | 建議技術 |
|----|----------|
| 圖形邏輯 | Dynamo for Revit 2023 (`.dyn`) |
| 幾何／衝突 | Dynamo Geometry + `Element.Solids` / BoundingBox |
| 進階 API | Python 節點呼叫 Revit API（`ElementTransformUtils.MoveElement`、`MEPCurve` 等） |
| 套件 | 盡量少依賴；若需 Data-Shapes / Rhythm 會先徵求同意 |

---

## 7. 驗收標準（草案，確認後鎖定）

1. 在測試模型中選 1 條 MEP 管線與 1 個 STC 梁，能列出衝突。
2. Clearance = 50mm 時，擴張後衝突結果與肉眼檢查一致。
3. 若啟用 Offset 模式：執行後衝突消除或距離 ≥ Clearance；可用 Revit Undo 還原。
4. 未選元素時有明確提示，不寫入模型。
5. 執行紀錄可追溯（時間、數量、失敗 Id）。

---

## 8. 開放問題（請你回答，決定下一步）

請直接回覆編號答案（可簡短）：

### Q1. 「避開」要做到哪一級？
- A) 只報衝突  
- B) 自動平移偏移  
- C) 自動折線繞行（加彎頭）  
- D) A→B 分階段  

### Q2. 誰是 Subject（被移動的）？誰是 Obstacle（不動的）？
例如：
- Subject = MEP；Obstacle = ARC + STC  
- 或三者皆可互選  

### Q3. 元素如何指定？
- 手動選取  
- 依 Category / Workset / 參數自動篩  
- 兩者都要  

### Q4. 淨空 Clearance 預設多少？是否分專業不同值？
例如：管對梁 50mm、電橋架對結構 100mm  

### Q5. 是否包含 Linked Model（建築／結構連結檔）當障礙？
- 只要現行檔  
- 要讀 Link（只讀障礙）  
- 要寫回 Link（通常不建議）  

### Q6. MEP 類型優先順序？
Pipe / Duct / Cable Tray / Conduit / 全部  

### Q7. 避開失敗時策略？
跳過並記錄 / 整批中止 / 產生建議位置給人手調  

### Q8. 輸出形式？
Dynamo 觀察列表 / Excel / 模型內 Generic Model 標記 / 顏色覆寫  

### Q9. Dynamo 執行方式？
手動開 Dynamo / Dynamo Player（給現場同事用）  

### Q10. 有沒有公司標準或現有 `.dyn`／範例模型可參考？
有的話請提供路徑或上傳  

### Q11. 本機實際 Revit 安裝路徑是否為標準 Program Files？
你提供的是開始功能表捷徑資料夾：  
`C:\ProgramData\Microsoft\Windows\Start Menu\Programs\Autodesk\Revit 2023`  
請確認可執行檔是否類似：  
`C:\Program Files\Autodesk\Revit 2023\Revit.exe`  

---

## 9. 建議交付階段（技術切分，非日曆排期）

| 階段 | 內容 | 依賴 |
|------|------|------|
| **Phase 0** | 本 SCOPE + 問題確認 | 你的 Q1–Q11 答案 |
| **Phase 1** | Clash Report `.dyn` + 測試說明 | Q1 含 A；Q2–Q6 |
| **Phase 2** | Offset Avoid + Undo 安全 | Q1 = B 或 D |
| **Phase 3** | Reroute / Fitting 規則 | Q1 = C；需更多標準 |
| **Phase 4** | Dynamo Player 包裝、Excel、標記 | Q8–Q9 |

---

## 10. 風險與限制

- Dynamo 移動 `MEPCurve` 可能破壞連接或系統；需限制「短段／末端」或事後連接檢查。
- Bounding Box 會有假陽性；Solid 較準但較慢。
- 坡度管、保溫層、配件家族行為因專案範本而異。
- 雲端環境無法直接開啟你本機 Revit；實作後需你在本機 Revit 2023 驗證。

---

## 11. 你確認後我下一步會做什麼

收到 Q1–Q11（可部分回答）後：
1. 把答案寫入 `docs/DECISIONS.md`
2. 產出 `docs/TECH_DESIGN.md`（節點圖邏輯 + API）
3. 再開始做 Phase 1 的 `.dyn` / Python 骨架

若你現在只想先定方向，最少請回：**Q1、Q2、Q3、Q5**。
