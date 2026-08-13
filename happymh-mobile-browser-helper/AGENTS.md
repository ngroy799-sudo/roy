# AGENTS — happymh-mobile-browser-helper

## Purpose

Local helper that launches the user's real browser with a mobile User-Agent against `m.happymh.com`. Ad blocking is delegated to uBlock Origin. No scraping, proxying, or content mirroring.

## Entry points

- `index.html` — human-readable setup guide
- `launch-chrome.bat` / `launch-edge.bat` — Windows launchers
- `launch-chrome.sh` — Unix launcher
- `config.env.example` — default URL template

## Out of scope

- Fetching/parsing manga images from happymh
- Ad-stripping reverse proxy
- Iframe embedding of the remote site (blocked by X-Frame-Options)
