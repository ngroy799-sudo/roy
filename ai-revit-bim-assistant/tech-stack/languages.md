# 程式語言與技術棧（Languages）

## 語言總覽

| 語言 | 主要用途 | 優先級 |
|------|----------|--------|
| **C#** | Revit Add-in、Revit API 呼叫、Transaction、UI | 必須（核心） |
| **Python** | AI Orchestrator、RAG、評估、原型腳本、pyRevit | 必須（AI 層） |
| **TypeScript / JavaScript** | Web Dashboard、設定後台（可選） | 建議 |
| **JSON / JSON Schema** | BIM Action 契約、API 通訊 | 必須 |
| **XAML** | WPF 對話面板 UI（可選） | 建議 |
| **SQL** | 審計日誌、專案設定（可選） | 建議 |
| **Markdown** | 標準文件、Prompt 庫、Agent 可搜尋文件 | 必須（知識） |

## 詳細說明

### 1. C# / .NET（Revit Add-in）

- **Target**：`.NET Framework 4.8` 或 Revit 版本對應的 .NET（依年版本查 SDK）
- **關鍵套件／概念**：
  - `RevitAPI.dll` / `RevitAPIUI.dll`
  - External Application / External Command
  - `Transaction` / `TransactionGroup`
  - FilteredElementCollector
  - FamilySymbol / WallType / ViewSheet
- **UI**：WPF + XAML 或 WinForms；可用 WebView2 嵌 Chat UI

### 2. Python（AI Orchestrator）

- **框架**：FastAPI + Uvicorn
- **函式庫建議**：
  - `openai` / `anthropic` / `langchain` 或輕量自管 tool-calling
  - `pydantic`（Action Schema 驗證）
  - `chromadb` / `qdrant-client`
  - `httpx`、`pytest`
- **為何 Python**：AI 生態最成熟、RAG／評估腳本快、與工程師腳本文化接近

### 3. TypeScript（可選 Web）

- **框架**：Next.js 或 Vite + React
- **用途**：Prompt 模板管理、標準庫上傳、使用統計、管理員審核

### 4. Dynamo / DesignScript（輔助）

- 非獨立語言產品，但常用於：
  - 驗證 Action 可行性
  - 給非程式 BIM 人員擴充
  - 與 AI 輸出對接的「安全沙箱」節點

### 5. 契約與通訊格式

```text
User Prompt + Project Context (JSON)
        ↓
LLM Tool Calling
        ↓
BIM Action Schema (JSON Schema validated)
        ↓
C# Executor (whitelist ops)
```

## 語言分工原則

| 不要用 | 原因 |
|--------|------|
| 在 LLM 端直接產出 C# 再 Compile | 不安全、難驗證 |
| 純 Python 操控正式 Revit 生產流程（無 Add-in） | 穩定性／部署差；pyRevit 僅適合原型 |
| 把完整業務邏輯全塞進 Prompt | 難測、易幻覺；應用 Schema + 白名單 |

## 建議團隊技能組合

1. **BIM + Revit API（C#）** 工程師 1 人  
2. **Backend / AI（Python）** 工程師 1 人  
3. **BIM 標準擁有者**（定義規則與驗收）  
4. （可選）前端 0.5 人做 Dashboard  
