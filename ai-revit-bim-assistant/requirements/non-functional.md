# 非功能需求（Non-Functional Requirements）

## NFR-01 安全性
- LLM API Key 僅存在後端／密鑰庫
- Action 白名單；禁止任意程式碼執行
- 刪除與大量修改需人工確認
- 不上傳完整 `.rvt`；最小化上下文
- 符合公司資料外傳政策（可選私有化 LLM／Azure 區域）

## NFR-02 可靠性
- Action 執行包在 Transaction；失敗整批回滾或明確部分成功策略
- Orchestrator 超時與重試策略
- Revit 崩潰不得因 Add-in 未處理例外而失控（全域 exception handler）

## NFR-03 效能
- 一般對話回覆：目標 < 5–15 秒（視 LLM）
- 本地執行 100 個簡單建牆 Action：目標可接受互動時間內完成並顯示進度
- 上下文快取（類型清單）避免每次全專案掃描

## NFR-04 可用性
- 繁中／英文 UI（至少繁中文件與 Prompt）
- 錯誤訊息對 BIM 使用者可讀（不要只丟 Exception）
- 一鍵 Undo 說明

## NFR-05 可維護性
- Action Schema 版本化（v1、v2）
- Add-in 與 Orchestrator 鬆耦合（只靠 JSON 契約）
- 文件放獨立 folder，方便 AI Agent 搜尋

## NFR-06 可測試性
- 每個 Action op 有單元／整合測試樣本專案
- Prompt → Action golden set（回歸防幻覺）
- CI 至少跑 Orchestrator 與 Schema 驗證

## NFR-07 相容性
- Windows 10/11（Revit 桌面）
- 支援公司指定 Revit 年版本
- 單位：公制為主，英制可配置

## NFR-08 觀測性
- 結構化日誌（correlation id：session / command）
- 成功率、確認取消率、平均 Actions/指令 指標
