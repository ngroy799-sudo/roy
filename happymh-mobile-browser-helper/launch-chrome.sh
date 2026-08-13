#!/usr/bin/env bash
set -euo pipefail
cd "$(dirname "$0")"

TARGET_URL="${1:-https://m.happymh.com/manga/quanzhiduzheshijiao}"
WINDOW_WIDTH=390
WINDOW_HEIGHT=844
MOBILE_UA='Mozilla/5.0 (iPhone; CPU iPhone OS 17_0 like Mac OS X) AppleWebKit/605.1.15 (KHTML, like Gecko) Version/17.0 Mobile/15E148 Safari/604.1'

if [[ -f ./config.env ]]; then
  # shellcheck disable=SC1091
  set -a
  source ./config.env
  set +a
fi

if [[ -n "${1:-}" ]]; then
  TARGET_URL="$1"
fi

pick_browser() {
  local c
  for c in google-chrome google-chrome-stable chromium chromium-browser "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome" "/Applications/Chromium.app/Contents/MacOS/Chromium"; do
    if [[ -x "$c" ]] || command -v "$c" >/dev/null 2>&1; then
      echo "$c"
      return 0
    fi
  done
  return 1
}

BROWSER="$(pick_browser || true)"
if [[ -z "${BROWSER}" ]]; then
  echo "[!] 搵唔到 Chrome / Chromium。請先安裝。" >&2
  exit 1
fi

echo "啟動瀏覽器（手機 UA）..."
echo "URL: ${TARGET_URL}"
echo
echo "提示：請先安裝 uBlock Origin 攔截廣告。"
echo "若排版怪：F12 → Ctrl+Shift+M → 選 iPhone → 重新整理"
echo

exec "$BROWSER" --new-window --user-agent="$MOBILE_UA" --window-size="${WINDOW_WIDTH},${WINDOW_HEIGHT}" "$TARGET_URL"
