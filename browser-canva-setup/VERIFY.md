# 驗證 checklist：Browser / Canva MCP

- [ ] `node -v` ≥ 18
- [ ] Cursor Settings → Tools & MCP 見到 `playwright` 且為 ON
- [ ] Agent 能打開 `https://example.com` 並回報 title
- [ ] Agent 能截圖並存到本機／Artifacts
- [ ] （可選）貼 Canva **view/share** link，至少截到一頁
- [ ] 若 Canva 登入牆出現：改用手動 PNG/PDF → `F:\For Cursor\<專案>\canva-export\`
- [ ] Skill `canva-browser-workflow` 被 agent 採用（問 Canva 時會跟 pipeline）

## 本機快速指令

```bash
node -v
npx -y @playwright/mcp@latest --help
```

Cloud / CI 無顯示時必須保留 `--headless`。
