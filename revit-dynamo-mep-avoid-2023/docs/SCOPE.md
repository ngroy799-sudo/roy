# SCOPE：Revit 2023 Dynamo — MEP 向下 Fitting 避開選定模型

**專案代碼**：`revit-dynamo-mep-avoid-2023`  
**狀態**：`SCOPE_LOCKED`（核心規格已確認；可進 Phase 1）  
**Revit**：2023  
**本階段交付**：SCOPE + 決策 + `TECH_DESIGN.md`（尚未實作 `.dyn`）

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
| **上下表面淨空** | 垂直方向表面間距（含保溫） | 障礙外底面 ↔ 下沉 MEP 外頂面 **≥ 100 mm** |

---

## 3. 已確認規則（來源：使用者 2026-07-29）

| # | 規則 |
|---|------|
| R1 | **不移動**障礙物及其他非 Subject 部位 |
| R2 | MEP **只向下**生成 Fitting 來避開相撞位置（不做側向／向上優先繞行） |
| R3 | 主要閃避對象 = **已點選的所有 Model**（手動選取為準） |
| R4 | 垂直方向：**上下表面相距 10 cm**，**含保溫**外表面 |
| R5 | 下沉後兩端 **必須** 接回原標高 |
| R6 | 下方空間不足 → **跳過並記錄**，不改該段 |
| R7 | 第一版 Subject 類型：**Pipe + Duct** |

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

## 9. 開放問題（不擋 Phase 1）

已關閉：Q1–Q4、Q6–Q8、Q12–Q13（見 `DECISIONS.md`）。

可之後再答：

### Q5. Linked Model
已點選障礙若來自 Link：只讀避開 / 忽略 Link？

### Q9. 執行方式
手動 Dynamo / Dynamo Player

### Q10. 公司標準 Elbow／Fitting 家族或範例模型？

### Q11. Revit.exe 是否為 `C:\Program Files\Autodesk\Revit 2023\Revit.exe`？

---

## 10. 交付階段（技術切分）

| 階段 | 內容 |
|------|------|
| **Phase 0** | SCOPE + 決策 + TECH_DESIGN（完成） |
| **Phase 1** | 含保溫淨空偵測 + ReportOnly |
| **Phase 2** | Pipe／Duct 向下 Fitting drop-under |
| **Phase 3** | 多障礙合併、Tray/Conduit、失敗強化 |
| **Phase 4** | Dynamo Player／報表包裝 |

詳見 `docs/TECH_DESIGN.md`。

---

## 11. 風險

- 自動放置 Fitting 依賴專案內 Elbow 類型是否齊全 → 不足則跳過並記錄。
- 改 MEP 需在 Transaction 內重建連接。
- 下方空間不足 → 跳過（已定）。
- 保溫外尺寸取法因家族而異，需本機樣板驗證。
- 雲端無法開本機 Revit；實作後需本機驗證。

---

## 12. 下一步

規格已鎖定。你回覆 **「開始 Phase 1」**（或一次做 Phase 1+2）後，即建立 Python 骨架與 ReportOnly Dynamo 說明／腳本。
