# 快速清單：程式 × 語言 × 需求

> 給決策者／Kickoff 用的一頁摘要。詳細見各子資料夾。

## 1. 需要的程式（Programs）

| 類別 | 程式 |
|------|------|
| BIM 核心 | Autodesk Revit、Revit SDK、Visual Studio |
| 輔助工具 | Dynamo、Revit Lookup；（可選）pyRevit |
| AI 後端 | Python 3.11+、Docker、Git、VS Code/Cursor |
| AI 服務 | LLM API（OpenAI/Azure/Anthropic/本地）、向量庫（Qdrant/Chroma/pgvector） |
| 可選雲端 | Autodesk Platform Services (APS)、ACC/BIM 360 |
| 測試 | NUnit/xUnit、pytest、樣本 RVT、CI |

## 2. 需要的語言（Languages）

| 語言 | 用途 |
|------|------|
| **C#** | Revit Add-in、呼叫 Revit API、Transaction/Undo |
| **Python** | AI Orchestrator、RAG、Prompt→Action |
| **JSON Schema** | BIM Action 契約（機器可執行指令） |
| **TypeScript**（可選） | Web 管理後台 |
| **XAML**（可選） | WPF Chat UI |
| **Markdown** | BIM 標準與 Agent 可搜尋文件 |

## 3. 核心需求（Requirements）

### 功能
- Revit 內對話／指令面板
- 感知專案上下文（樓層、選取、類型、單位）
- 自然語言 → 驗證過的 BIM Actions → 預覽 → 執行
- MVP 建模：牆／樓板／參數／基本視圖
- 高風險操作確認 + 全程可 Undo
- （下一階段）出圖、規範檢查、RAG 標準問答

### 非功能
- API Key 不進 Add-in；Action 白名單
- 不上傳完整 RVT
- 多 Revit 版本策略
- 可測試的 Schema 與 golden prompts
- 繁中可用、錯誤訊息對 BIM 使用者友善

## 4. 建議架構一句話

**Revit C# Add-in（執行） + Python AI Orchestrator（理解） + LLM/RAG（知識） + JSON Action Schema（安全契約）**
