# happymh-mobile-browser-helper

用**本機瀏覽器**以手機模式直接開嗨皮漫畫原站，並用擴充功能攔截廣告。  
**唔會抓取、代理或轉載**網站內容，只係啟動 Chrome / Edge。

## 快速開始（Windows）

1. 安裝 [uBlock Origin](https://chromewebstore.google.com/detail/ublock-origin/cjpalhdlnbpafiamejdnhcphjbkeiagm)（Chrome）或 Edge 版。
2. 雙擊 `launch-chrome.bat`（或 `launch-edge.bat`）。
3. 會以 iPhone User-Agent、窄視窗開原站。

改預設連結：用記事本開 `.bat`，改 `TARGET_URL=` 嗰行。

## 快速開始（macOS / Linux）

```bash
chmod +x launch-chrome.sh
./launch-chrome.sh
# 或指定網址：
./launch-chrome.sh "https://m.happymh.com/manga/quanzhiduzheshijiao"
```

## 本機說明頁

用瀏覽器開 `index.html`，入面有步驟同「一鍵開原站」連結。

## 若排版仍然怪

1. 按 `F12` 開開發者工具  
2. 撳裝置圖示（Toggle device toolbar）或 `Ctrl+Shift+M`  
3. 選 iPhone / 自訂闊度約 `390`  
4. 重新整理頁面  

## 檔案一覽（方便搜尋）

| 檔案 | 用途 |
|------|------|
| `index.html` | 本機設定說明頁 |
| `launch-chrome.bat` | Windows：Chrome 手機 UA 啟動 |
| `launch-edge.bat` | Windows：Edge 手機 UA 啟動 |
| `launch-chrome.sh` | macOS/Linux：Chrome/Chromium 啟動 |
| `config.env.example` | 預設網址範例 |
| `AGENTS.md` | 俾 AI agent 快速理解呢個專案 |

## 注意

- 需要已安裝 Chrome 或 Edge。
- 廣告攔截靠 uBlock Origin，唔係本工具改寫網頁。
- 本工具只開原站，唔下載、唔鏡像、唔去殼轉載內容。
