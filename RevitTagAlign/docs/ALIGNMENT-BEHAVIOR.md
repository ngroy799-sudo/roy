# TagAlign 對齊行為說明

## 一齊 TAG（Common Angle，Constant Landing OFF）

### Anchor tag（基準：最底 / 最頂）

- **Upper corner**：row 0 = stack **最底**（你 click 位置）
- **Lower corner**：row 0 = stack **最頂**
- Anchor 計出 **sharedLanding 基準**（fallback 用）

### 每一條 leader

- **Tag 頭**：垂直 stack，間距 = **Vertical Spacing（mm，可自行設定）**
- **角度**：全部 **平行**（Configure Angle，elbow 黃↔紅）
- **橫線 + 角度線**：由 anchor 基準出發，**按 row / host 位置自動加長或縮短**，接到 pinned host 接觸點
- 解唔到幾何時 → 用 anchor landing + face snap

### Vertical Spacing

Configure → **Vertical Spacing (mm)**：控制 tag 文字上下距離（唔係角度）。

## 直接下載

https://github.com/ngroy799-sudo/roy/raw/cursor/revit-tag-align-tool-845e/RevitTagAlign/dist/RevitTagAlign-2023.zip
