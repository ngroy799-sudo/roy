# Browser / Canva 流程設定

> AI Agent 搜尋關鍵字：`playwright mcp`、`canva export`、`browser automation`、`slide screenshot`、`F:\For Cursor`

Canva **冇官方 MCP**。呢個 folder 用 **Playwright Browser MCP** 做真實瀏覽器自動化，再配合 skill 處理 Canva 連結擷圖／匯出。

## 你會得到咩

| 項目 | 路徑 | 用途 |
|------|------|------|
| MCP 設定 | `.cursor/mcp.json` | 啟動 Playwright（Chromium headless） |
| Skill | `.cursor/skills/canva-browser-workflow/SKILL.md` | 教 agent 點開 Canva、截 slide、本地交付 |
| Desktop 安裝步驟 | `browser-canva-setup/DESKTOP-INSTALL.md` | Windows / Cursor Desktop 開 MCP |

## Desktop Cursor 最短安裝（Windows）

1. 用 Cursor 開呢個 repo（或把 `.cursor/mcp.json` 抄去你專案）
2. 開 **Cursor Settings → Tools & MCP**
3. 確認列表出現 **playwright**，Toggle **ON**
4. 第一次可能要等 `npx` 下載 `@playwright/mcp`（要有 Node.js 18+）
5. 新開一個 Agent chat，試：

```text
用 playwright 打開 https://example.com ，截一張圖
```

通過之後就可以貼 Canva view/share link。

詳細步驟見 [`DESKTOP-INSTALL.md`](./DESKTOP-INSTALL.md)。

## Canva 用咩講法

```text
用 browser 開呢個 Canva link，逐頁 screenshot，存去 F:\For Cursor\my-deck\canva-export\
```

如果私人檔／要登入：

```text
Canva 開唔到就叫我 export PNG，你用本地檔繼續
```

## 限制（睇清楚）

- 私人 / edit URL 經常 **403** 或登入牆 → 改用 Share view link，或你自己 Download PNG/PDF
- Cloud Agent 環境未必有顯示畫面 → 預設 **headless**
- Desktop 想睇瀏覽器視窗：喺 `mcp.json` 拎走 `"--headless"`，然後重啟 MCP

## 相關檔案索引

```
browser-canva-setup/
  README.md                 ← 本說明
  DESKTOP-INSTALL.md        ← Windows 安裝
  VERIFY.md                 ← 驗證 checklist
.cursor/
  mcp.json                  ← Playwright MCP
  skills/canva-browser-workflow/SKILL.md
```
