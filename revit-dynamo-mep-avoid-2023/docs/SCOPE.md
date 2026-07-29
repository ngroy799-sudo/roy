# SCOPE：Revit 2023 Dynamo — MEP 向下 Fitting 避開選定模型

**專案代碼**：`revit-dynamo-mep-avoid-2023`  
**狀態**：`SCOPE_LOCKED_PARTIAL`（核心規則已確認；少數細節待答）  
**Revit**：2023  
**本階段交付**：更新後 SCOPE + 決策紀錄（尚未實作 `.dyn`）

---

## 1. 一句話目標（已鎖定）

在 Revit 2023 用 Dynamo：對 **MEP 管線／風管等** 偵測與 **使用者已點選的所有障礙模型** 的衝突；**不移動障礙物及其他非目標部位**，只在 MEP 路徑上 **向下生成 Fitting（彎頭／配件）** 繞過相撞區，並維持 **上下表面垂直淨空 10 cm（100 mm）**。

---

## 2. 名詞定義

| 代號 | 含義 | 本專案用法 |
|------|------|------------|
| **Subject（MEP）** | 被繞行改線的機電曲線元素 | Pipe / Duct / Cable Tray / Conduit（類型優先順序見開放問題） |
| **Obstacle** | 障礙物 | **使用者已點選的所有模型元素**（可含 ARC / STC / 其他 MEP / Generic Model 等） |
| **向下 Fitting 避開** | 只允許向下折繞 | 在衝突區前下彎 → 水平段在障礙下方通過 → 再上彎接回原標高（典型 U 形下沉） |
| **上下表面淨空** | 垂直方向表面間距 | **≥ 100 mm（10 cm）**（MEP 外表面 ↔ 障礙相關表面） |

---

## 3. 已確認規則（來源：使用者 2026-07-29）

| # | 規則 |
|---|------|
| R1 | **不移動**障礙物及其他非 Subject 部位 |
| R2 | MEP **只向下**生成 Fitting 來避開相撞位置（不做側向／向上優先繞行） |
| R3 | 主要閃避對象 = **已點選的所有 Model**（手動選取為準） |
| R4 | 垂直方向：**上下表面相距 10 cm** |

---

## 4. In Scope（第一版）

1. **輸入**
   - 選取要改線的 MEP（Subject）
   - 選取所有要避開的障礙模型（Obstacles）— 手動點選
2. **衝突判斷**
   - 偵測 Subject 與 Obstacles 的重疊／距離不足
   - 垂直淨空檢查：目標 **≥ 100 mm**
3. **避開動作（鎖定方向）**
   - 僅改 Subject MEP 路徑
   - 在衝突區以 **向下 Fitting** 形成下沉繞行（drop-under）
   - 下沉後水平段頂面（或 MEP 上表面）與障礙下表面（或相關表面）維持 ≥ 100 mm
4. **安全**
   - Revit Transaction，可 Undo
   - 失敗案例記錄 Element Id，不默默略過無痕
5. **環境**
   - Revit 2023 + Dynamo for Revit
   - 現行檔元素為主（Link 見開放問題）

---

## 5. Out of Scope（第一版不做）

| 項目 | 原因 |
|------|------|
| 移動／刪改 Obstacle 或其他非 Subject | 已明確禁止 |
| 側向（左右）或向上優先繞行 | 規則鎖定「只向下」 |
| 完整自動全域 MEP 路由引擎 | 超出局部 Fitting 避開 |
| 水力／風量重算 | 非本工具範圍 |
| 寫回 Linked 模型 | 工作流複雜（除非後續要求） |
| C# 獨立外掛 | 先 Dynamo |

---

## 6. 目標幾何行為（概念）

```
原 MEP 標高 ────────●════════════●────────
                    │            │
                    │ Fitting    │ Fitting
                    ▼ 向下       ▲ 接回
              ══════●════════════●══════  ← 下沉水平段
                         ▲
                    ≥ 100 mm
                         ▼
              ████████████████████████    ← 已選障礙模型
```

- 障礙與原路徑相撞（或垂直淨空 < 100 mm）時觸發。
- 只插入／調整 Subject 上的 Fitting 與必要的中間 MEP 段；**Obstacle 幾何不變**。

---

## 7. 技術架構（暫定）

```
[Select MEP Subjects] + [Select Obstacle Models]
            │
            ▼
   Clash / vertical clearance check (< 100 mm)
            │
            ▼
   Compute drop elevation = obstacle_bottom - 100mm - MEP_half_height
            │
            ▼
   Split / rebuild MEP path with downward fittings
            │
            ▼
   Transaction commit + result report
```

| 層 | 建議技術 |
|----|----------|
| 圖 | Dynamo for Revit 2023 (`.dyn`) |
| 幾何 | BoundingBox / Solid；垂直淨空用表面或 bbox Z |
| API | Python：`MEPCurve`、Fitting 放置、連接（`Connector`） |
| 移動 API | **不**對 Obstacle 呼叫 Move；避免 `ElementTransformUtils` 用在非 Subject |

---

## 8. 驗收標準（草案）

1. 選 1 條水平 MEP + 1 個已選障礙（例如梁），原路徑相撞時，工具只改 MEP，障礙位置／幾何不變。
2. 改後 MEP 以向下 Fitting 繞至障礙下方，**上下相關表面垂直間距 ≥ 100 mm**。
3. 無衝突或已滿足淨空時，不寫入無謂修改。
4. 執行失敗有明確訊息與 Element Id；可用 Undo 還原整批。
5. 未選 Subject 或 Obstacle 時提示並中止。

---

## 9. 開放問題（剩餘）

已由本次指示關閉：Q1→向下 Fitting；Q2→只改 MEP、不移其他；Q3→已點選模型；Q4→10 cm。

仍請回覆：

### Q5. Linked Model
已點選的障礙若來自 Link，是否納入？（只讀避開 / 忽略 Link）

### Q6. MEP 類型第一版做哪些？
Pipe / Duct / Cable Tray / Conduit / 全部

### Q7. 避開失敗（無法連接到系統、無對應 Elbow 類型等）
跳過並記錄 / 整批中止

### Q8. 「上下表面」量測基準（請選最接近的）
- A) 障礙 **底面** ↔ 下沉 MEP **頂面** ≥ 100 mm（最符合「從下方閃過」）  
- B) 兩元素 BoundingBox 的 Z 間隙 ≥ 100 mm  
- C) 其他（請描述，例如含保溫 Insulation）

### Q9. 執行方式
手動 Dynamo / Dynamo Player

### Q10. 有無公司 Elbow／標準 Fitting 家族或範例模型？

### Q11. Revit.exe 是否為  
`C:\Program Files\Autodesk\Revit 2023\Revit.exe`？

### Q12. 下沉後是否必須回到原標高接回兩端？（預設：**是**）

### Q13. 若障礙下方空間不足 100 mm（例如碰到樓板），要怎樣？
報錯跳過 / 允許貼地但警告 / 其他

---

## 10. 交付階段（技術切分）

| 階段 | 內容 |
|------|------|
| **Phase 0** | SCOPE + 決策（進行中） |
| **Phase 1** | 衝突／100 mm 淨空偵測 + 報告 |
| **Phase 2** | 單段水平 MEP 向下 Fitting drop-under |
| **Phase 3** | 多障礙、連續衝突段、失敗處理強化 |
| **Phase 4** | Dynamo Player／報表包裝 |

---

## 11. 風險

- 自動放置 Fitting 依賴專案內 Elbow／Transition 類型是否齊全。
- 改 MEP 曲線可能暫時斷開系統連接，需在同一 Transaction 內重建連接。
- 「只向下」在下方空間不足時必然失敗，需明確失敗策略（Q13）。
- 保溫層、坡度管會影響「表面」定義（Q8）。
- 雲端無法開你本機 Revit；實作後需本機驗證。

---

## 12. 下一步

1. 你回覆 **Q5–Q13**（最少：**Q6、Q8、Q12、Q13**）。  
2. 我更新 `DECISIONS.md` 並產出 `TECH_DESIGN.md`。  
3. 再開始 Phase 1/2 的 Dynamo／Python 骨架。
