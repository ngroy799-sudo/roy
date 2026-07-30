# 需要的程式與平台（Programs）

> 依角色分類：開發、執行、可選雲端、協作。

## A. BIM / CAD 核心（必須）

| 程式 | 用途 | 備註 |
|------|------|------|
| **Autodesk Revit** | 主建模平台 | 建議支援近 2–3 個年版本（如 2024–2026） |
| **Revit SDK** | Add-in 開發與 API 文件 | 隨 Revit 安裝或 Autodesk 下載 |
| **Visual Studio**（Windows） | 開發／除錯 C# Add-in | Community 以上即可 |
| **Autodesk Desktop App** | 安裝與更新 Revit | 企業授權管理 |

## B. 視覺化腳本／自動化（強烈建議）

| 程式 | 用途 | 備註 |
|------|------|------|
| **Dynamo for Revit** | 節點式自動化、原型驗證 AI 輸出 | 隨 Revit 或獨立 |
| **pyRevit**（可選） | 快速 Python 工具列、腳本測試 | 開源；適合 MVP 原型 |
| **Revit Lookup** | 檢查元素參數／Id | 開發除錯必備 |

## C. AI / 後端開發

| 程式 | 用途 |
|------|------|
| **Python 3.11+** | Orchestrator、RAG、評估腳本 |
| **Node.js 20+**（可選） | 若選 TypeScript 後端／Web UI |
| **Docker / Docker Compose** | 本地跑 API、向量庫、Redis |
| **Git + GitHub/GitLab** | 版本控制 |
| **Postman / Insomnia** | API 測試 |
| **VS Code / Cursor** | Python、文件、前端開發 |

## D. LLM 與 AI 服務

| 服務／程式 | 用途 |
|------------|------|
| **OpenAI / Azure OpenAI / Anthropic / 本地 LLM** | 意圖理解與 Action 生成 |
| **Embedding 模型** | RAG 向量化 |
| **向量資料庫**：Qdrant / Chroma / pgvector / Azure AI Search | 標準與族庫檢索 |
| **語音**（可選）：Whisper / Azure Speech | 語音下指令 |

## E. Autodesk 雲端（可選進階）

| 程式／服務 | 用途 |
|------------|------|
| **Autodesk Platform Services (APS)** | OAuth、Model Derivative、Design Automation |
| **ACC / BIM 360** | 雲端協作模型 |
| **Navisworks**（後期） | 碰撞檢查整合 |

## F. 文件與標準管理

| 程式 | 用途 |
|------|------|
| **Notion / Confluence / Markdown repo** | BIM 標準知識庫來源 |
| **Excel / CSV** | 族參數表、命名規則匯出 |
| **PDF 解析工具**（如 unstructured） | 標準文件進 RAG |

## G. 測試與品質

| 程式 | 用途 |
|------|------|
| **NUnit / xUnit** | C# Add-in 單元測試 |
| **pytest** | Orchestrator 測試 |
| **樣本 RVT 專案庫** | 回歸測試場景 |
| **CI**（GitHub Actions） | Build Add-in、跑 API 測試 |

## 最小可跑 MVP 清單（精簡）

1. Revit（目標版本）+ SDK  
2. Visual Studio  
3. Python 3.11 + FastAPI  
4. 一個 LLM API Key  
5. Docker（跑向量庫，可後期再加）  
6. Git  

## 授權注意

- Revit 需合法授權（商業／教育）
- Autodesk API Terms、Design Automation 計費
- LLM 供應商資料處理協議（專案機密／圖面不可外洩政策）
