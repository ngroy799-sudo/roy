---
name: direct-download-artifacts
description: >-
  Always give the user a direct download URL for artifacts the agent created
  (installers, zips, DLLs, images, videos, exports). Never send them to an
  intermediate page first. Use when packaging installs, sharing build outputs,
  or linking screenshots/videos/files for download.
---

# Direct Download Artifacts

## Rule (mandatory)

When sharing **anything the agent (or this project) produced** for the user to download — programs, install packages, zips, DLLs, images, videos, PDFs, exports, etc.:

1. **Prefer a direct download link** that starts the file download (or opens the raw file) in one click.
2. **Do not** send the user to an intermediate page (repo homepage, Actions UI, release notes page, drive folder listing, blog post) and expect them to hunt for the file.
3. If a direct link is **impossible** and they must visit another site/page first, **tell them explicitly before** giving that link (what site, why, and what to click).

## What counts as a direct link

Good (use these):

- GitHub/GitLab **raw** or **blob download** URLs that hit the file bytes  
  Example: `https://github.com/<owner>/<repo>/raw/<branch>/<path/to/file.zip>`
- Artifact / CDN URLs that respond with the file (`Content-Disposition: attachment` or raw bytes)
- Absolute file paths only when the user can open them locally (and state that clearly)

Bad (avoid as the primary link):

- Repo root / folder browse pages
- “Go to Actions → download artifact”
- App Store / product marketing pages that are not the file itself
- Short links that land on a landing page instead of the file

## Response format

When handing over a download:

```text
直接下載：
https://…/path/to/YourPackage.zip

（如需 Unblock / 安裝步驟，用一句跟住講）
```

If only an intermediate page exists:

```text
注意：無法提供一鍵直接下載，必須先到以下頁面再撳 Download：
https://example.com/…
原因：…
```

## Scope

- Applies to **agent-created / project-built** artifacts (install packs, renders, recordings, exports).
- Does **not** forbid linking to docs for *reading*; it forbids using docs/pages as the only way to get the file.
- Prefer keeping install packages in-repo under a stable path (e.g. `Something/dist/…zip`) so a permanent raw URL can be given.
