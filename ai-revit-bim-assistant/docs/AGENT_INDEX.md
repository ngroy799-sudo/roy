# AI Agent 搜尋索引

本檔供 AI Agent／開發者快速定位關鍵概念。

## Keywords → Files

| Keyword | File |
|---------|------|
| Revit Add-in, Ribbon, Transaction | `architecture/system-architecture.md`, `tech-stack/languages.md` |
| LLM, Orchestrator, FastAPI, RAG | `architecture/system-architecture.md`, `tech-stack/programs.md` |
| C#, Python, TypeScript, XAML | `tech-stack/languages.md` |
| Dynamo, pyRevit, APS, Forge | `tech-stack/programs.md` |
| BIM Action Schema, whitelist | `architecture/system-architecture.md`, `requirements/functional.md` |
| FR, functional requirements | `requirements/functional.md` |
| NFR, security, performance | `requirements/non-functional.md` |
| user story, acceptance | `requirements/user-stories.md` |
| MVP, roadmap, phase | `roadmap/phases.md` |
| vision, persona, use case | `docs/01-overview.md` |

## 決策摘要（Decisions）

1. **執行面用 C# Revit API**，不用讓 LLM 直接跑任意程式碼。  
2. **智能面用 Python Orchestrator**，產出驗證過的 JSON Actions。  
3. **MVP 先桌面 Add-in**，雲端 Design Automation 放後期。  
4. **知識用 RAG**，把公司 BIM 標準變成可查來源。  
5. **高風險操作必須人工確認 + Undo。**
