# 實作分期（Roadmap）

## Phase 0 — 發現與標準
- 盤點公司 BIM 標準、常用 Family、命名規則
- 選定 Revit 目標版本與授權
- 定義 BIM Action Schema v1
- 建立樣本專案與 golden prompts

## Phase 1 — MVP
**目標**：對話 → 預覽 → 執行基礎建模

交付：
- C# Add-in Chat UI（最小）
- Python FastAPI Orchestrator + 1 個 LLM
- Actions：Wall / Floor / SetParameter / 基本 CreateView
- Transaction + Undo + 確認流程
- 基礎日誌

## Phase 2 — 生產可用
- RAG 接公司標準
- 出圖（Sheet + View Template）
- 品質檢查規則引擎
- 多 Revit 版本建置
- 審計後台（可簡陋）

## Phase 3 — 進階智能
- 草圖／PDF → 牆線
- 語音輸入
- 更完整 Family 放置與宿主邏輯
- 評估集自動回歸（防幻覺）

## Phase 4 — 雲端與協作
- APS Design Automation 批次
- ACC Issue 整合
- 跨專業協調建議

## 成功指標（示例）
- 常用指令成功率 ≥ 85%（人工確認後執行成功）
- 單次「建一層簡單隔間牆」較手動節省明顯時間
- 誤刪／錯誤大量修改事件 ≈ 0（靠確認與白名單）
