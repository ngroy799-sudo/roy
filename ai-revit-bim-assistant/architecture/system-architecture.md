# 系統架構（System Architecture）

## 高層架構圖

```text
┌─────────────────────────────────────────────────────────────┐
│                     Autodesk Revit (Desktop)                │
│  ┌──────────────┐  ┌──────────────┐  ┌───────────────────┐ │
│  │ Ribbon UI    │  │ Command      │  │ Preview / Diff    │ │
│  │ Chat Panel   │→ │ Executor     │→ │ Transaction Mgr   │ │
│  └──────┬───────┘  └──────▲───────┘  └───────────────────┘ │
│         │                 │  Revit API (C#)                │
└─────────┼─────────────────┼─────────────────────────────────┘
          │ HTTPS / gRPC    │
          ▼                 │
┌──────────────────┐        │
│ AI Orchestrator  │────────┘  Structured BIM Actions JSON
│ (Python FastAPI) │
│  - Intent parse  │
│  - Tool calling  │
│  - RAG (standards│
│  - Safety guard  │
└────────┬─────────┘
         │
    ┌────┴────┬────────────┬──────────────┐
    ▼         ▼            ▼              ▼
 ┌─────┐  ┌───────┐  ┌──────────┐  ┌────────────┐
 │ LLM │  │ Vector│  │ Project  │  │ Autodesk   │
 │ API │  │  DB   │  │ Context  │  │ Platform   │
 └─────┘  └───────┘  └──────────┘  │ (optional) │
                                   └────────────┘
```

## 模組說明

### 1. Revit Add-in（Client）
- 語言：C# / .NET
- 職責：UI、收集模型上下文、執行 API 指令、Transaction／Undo、顯示預覽
- 關鍵 API：`Autodesk.Revit.DB`、`UI`、`ExternalCommand`、`IExternalApplication`

### 2. AI Orchestrator（Server）
- 語言：Python（FastAPI）或可選 Node.js
- 職責：
  - 解析使用者意圖
  - 呼叫 LLM + tools
  - RAG 查公司 BIM 標準／族庫說明
  - 輸出結構化 **BIM Action Schema**（JSON）
  - 風險評分（破壞性操作需人工確認）

### 3. BIM Action Schema（契約層）
機器可執行的指令格式，例如：

```json
{
  "actions": [
    {
      "op": "CreateWall",
      "level": "Level 2",
      "wallType": "Basic Wall: 200mm Concrete",
      "path": [[0, 0], [10000, 0]],
      "height_mm": 3000
    }
  ],
  "requires_confirmation": false,
  "rationale": "Create wall along grid A from (0,0) to (10000,0)"
}
```

### 4. Knowledge / RAG
- 公司建模標準（PDF/MD）
- Family 目錄與參數說明
- 專案 Naming Convention
- 過去成功 Prompt → Action 範例

### 5. Optional：Autodesk Platform Services (APS / Forge)
- 雲端模型存取、Viewer、Design Automation（無桌面 Revit 批次任務）

## 資料流（典型對話建模）

1. 工程師在 Chat 輸入指令
2. Add-in 打包上下文：當前專案、樓層、選取元素、可見視圖、單位
3. Orchestrator + LLM 產出 Action JSON
4. Add-in 驗證（類型是否存在、單位、權限）
5. 顯示預覽 → 使用者確認
6. 在單一 `TransactionGroup` 執行 → 可 Undo
7. 回傳結果摘要給 LLM 做後續追問

## 安全邊界

- **永不**直接讓 LLM 執行任意程式碼
- 只允許白名單 Action ops
- 刪除／大量修改必須二次確認
- API Key 不放在 Add-in；走後端
- 專案檔不上傳完整 RVT；只傳結構化上下文與必要截圖
