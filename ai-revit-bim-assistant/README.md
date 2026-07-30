# AI Revit BIM Assistant

> AI 輔助 BIM Engineer 在 Autodesk Revit 內畫圖／建模的系統設計藍圖。

## 專案目的

讓 BIM Engineer 用自然語言、語音或草圖指令，驅動 Revit 自動建立／修改模型元素、產生圖紙、檢查規範與加速重複性繪圖工作。

## 文件索引（方便 AI Agent 搜尋）

| 路徑 | 說明 | 關鍵字 |
|------|------|--------|
| [`docs/01-overview.md`](docs/01-overview.md) | 產品願景、使用情境、角色 | vision, persona, use-case |
| [`architecture/system-architecture.md`](architecture/system-architecture.md) | 系統架構、資料流、模組邊界 | architecture, Revit API, LLM |
| [`tech-stack/programs.md`](tech-stack/programs.md) | 需要安裝／使用的程式與平台 | Revit, Dynamo, Forge, Docker |
| [`tech-stack/languages.md`](tech-stack/languages.md) | 程式語言、框架、SDK | C#, Python, TypeScript |
| [`requirements/functional.md`](requirements/functional.md) | 功能需求 | FR, modeling, drafting |
| [`requirements/non-functional.md`](requirements/non-functional.md) | 非功能需求 | NFR, security, performance |
| [`requirements/user-stories.md`](requirements/user-stories.md) | 使用者故事與驗收條件 | user-story, AC |
| [`roadmap/phases.md`](roadmap/phases.md) | 分期實作路線 | MVP, phase |
| [`docs/QUICK_CHEATSHEET.md`](docs/QUICK_CHEATSHEET.md) | 一頁程式／語言／需求摘要 | cheatsheet, kickoff |
| [`docs/AGENT_INDEX.md`](docs/AGENT_INDEX.md) | AI Agent 關鍵字索引 | agent, search, keywords |

## 一句話總結

**Revit Add-in（C#）+ AI Orchestrator（Python）+ LLM + Revit API/Dynamo**，讓工程師用對話驅動 BIM 建模與出圖。
