---
name: canva-browser-workflow
description: >-
  Open Canva view/share links in a real browser via Playwright MCP, capture
  slides as screenshots, and export assets into a local project folder for
  portfolio or design-to-code work. Use when the user pastes a canva.com /
  canva.link URL, asks to scrape Canva, export slides, or rebuild a Canva
  deck as HTML/code.
---

# Canva + Browser workflow

## When to use

- User shares a Canva link (`canva.com`, `canva.link`, design/view URL)
- Need slide screenshots, layout references, or exportable images
- Rebuilding a Canva deck as local HTML / portfolio pages

## Prerequisites

1. Playwright MCP server must be enabled (`playwright` in `.cursor/mcp.json`).
2. Prefer a **view/share** link the user can open without editing rights.
3. If the design is private and the browser hits login / 403, stop scraping and ask the user to export manually (see Fallback).

## Preferred pipeline

1. **Confirm the URL type**
   - View / share / present → try browser capture
   - Edit URL → expect 403; ask for share link or manual export

2. **Open with Playwright MCP**
   - Navigate to the Canva URL
   - Wait for the canvas / slide deck to settle
   - Dismiss cookie / login walls if they block the design preview
   - If a login wall appears and the design is not visible → go to Fallback

3. **Capture slides**
   - Screenshot each visible slide / page at a consistent viewport (e.g. 1440×900)
   - Name files `slide-01.png`, `slide-02.png`, …
   - Save under a **dedicated project folder**, e.g. `canva-export/<project-name>/slides/`
   - Also write `SOURCE.md` with the original URL, date, and notes

4. **Deliver for design work**
   - Prefer local folder delivery the user can open (Windows: under `F:\For Cursor\<project>\`)
   - One composition / one job per section when rebuilding as a site
   - Do not rely on hotlinking Canva CDN assets — they expire

5. **Rebuild (optional)**
   - Use screenshots as visual reference only
   - Recreate typography, spacing, and hierarchy in code
   - If images are needed in the final HTML, embed or copy into the project folder (avoid broken relative paths after unzip)

## Fallback (when browser cannot see the design)

Ask the user to do this in Canva Desktop/Web:

1. Share → Anyone with the link (view), **or**
2. File → Download → PDF / PNG (all pages)
3. Drop the export into `F:\For Cursor\<project>\canva-export\`

Then continue from the local files — do not keep retrying blocked Canva edit URLs.

## Hard rules from past failures

- Do **not** depend on raw `curl` of Canva HTML — often timeouts / empty shells
- Do **not** assume edit URLs are readable
- Do **not** leave final sites pointing at temporary Canva or GitHub raw image URLs if the user wants offline/local files
- Prefer **Artifacts zip** or a single self-contained HTML when cloud → Windows handoff is needed
- Keep new web pages in their **own folder** for easy search

## Example agent prompts (user can say)

- 「用 browser 開呢個 Canva link，逐頁 screenshot」
- 「由 Canva 匯出圖，改成本地 HTML portfolio」
- 「Canva 開唔到就叫我 export PNG，你繼續用本地檔」
