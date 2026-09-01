# TagAlign 對齊行為說明

## 一齊 TAG（Common Angle，Constant Landing OFF）

### Anchor tag（基準：最底 / 最頂）

- **Upper corner**：row 0 = stack **最底**（你 click 位置）
- **Lower corner**：row 0 = stack **最頂**
- Anchor 計出 **sharedLanding 基準**（fallback 用）

### 每一條 leader

- **Tag 頭**：垂直 stack，間距 = **Vertical Spacing（mm，可自行設定）**
- **角度**：全部 **平行**（Configure Angle，elbow 黃↔紅）
- **橫 landing**：全部 **等長**（由 anchor tag 決定 sharedLanding）
- **紅 leader**：平行，長度按 host 遠近 **加長或縮短**；end 釘喺 **原本 host 接觸點**（唔轉面）

### Vertical Spacing

Configure → **Vertical Spacing (mm)**：控制 tag 文字上下距離（唔係角度）。

## 直接下載

https://github.com/ngroy799-sudo/roy/raw/cursor/revit-tag-align-tool-845e/RevitTagAlign/dist/RevitTagAlign-2023.zip
