# Desktop 安裝：Playwright Browser MCP（Canva 流程）

適用：Windows Cursor Desktop，產物習慣放 `F:\For Cursor\`。

## 1. 先決條件

- [Cursor](https://cursor.com) Desktop 已安裝
- [Node.js 18+](https://nodejs.org/)（Terminal 跑 `node -v` 有版本號）
- 網絡可下載 npm 套件

## 2. 用 repo 設定（建議）

1. 用 Cursor 打開已包含呢個設定嘅專案（有 `.cursor/mcp.json`）
2. **Settings → Tools & MCP**
3. 見到 `playwright` → 打開
4. 等 status 變 ready（首次會跑 `npx -y @playwright/mcp@latest`）

### `mcp.json` 內容（已放喺 repo）

```json
{
  "mcpServers": {
    "playwright": {
      "command": "npx",
      "args": [
        "-y",
        "@playwright/mcp@latest",
        "--headless",
        "--browser=chromium",
        "--viewport-size=1440,900"
      ]
    }
  }
}
```

### 想睇瀏覽器視窗（headed）

刪除 `"--headless"` 嗰行，儲存後喺 MCP panel 重啟 `playwright`。

## 3. 全域安裝（所有專案都用）

若唔想綁單一 repo，喺 Windows 開：

`%USERPROFILE%\.cursor\mcp.json`

貼上同上面一樣嘅 JSON（或合併入現有 `mcpServers`）。

## 4. 手動喺 UI 加（無檔案時）

1. Settings → Tools & MCP → **Add new MCP Server**
2. Type: **command**
3. Command: `npx`
4. Args: `-y` `@playwright/mcp@latest` `--headless` `--browser=chromium` `--viewport-size=1440,900`

## 5. 驗證

Agent 輸入：

```text
用 playwright 打開 https://example.com ，回報 page title，並截圖
```

成功 = MCP 正常。之後先試 Canva view link。

## 6. Canva 建議做法

| 情況 | 做法 |
|------|------|
| 有 share / view link | 叫 agent 用 playwright 開同截 slide |
| Edit link / 要登入 | 你自己 Download → PNG/PDF，放去 `F:\For Cursor\<專案>\canva-export\` |
| 只要改 portfolio | 匯出圖後叫 agent 跟 skill `canva-browser-workflow` 重建 HTML |

## 7. 故障排除

| 現象 | 處理 |
|------|------|
| MCP 唔出現 | 重開 Cursor；檢查 `mcp.json` JSON 係咪合法 |
| `npx` / node not found | 安裝 Node，重開 Terminal／Cursor |
| Browser 啟動失敗 | 第一次跑完後再試；或本機執行 `npx playwright install chromium` |
| Canva 空白／登入 | 換 view link，或手動 export |
| Cloud Agent 截唔到私人 Canva | 預期行為；改用本機 Desktop + 你已登入嘅 browser profile（進階） |
