# TagAlign 對齊行為說明

Bird Tools Tag Alignment Tool v1.4 對齊邏輯摘要（RevitTagAlign 實作對照）。

## 最重要規則：60° 只喺 elbow（黃 ↔ 紅）

**Angle (°)** 係每條 leader **elbow** 位：水平 **landing（黃）** 同 **紅 leader** 之間嘅角。

| 係 Angle | 唔係 Angle |
|----------|------------|
| 每條 leader 黃 ↔ 紅 交角 | Tag 1 同 Tag 2 垂直距離 |
| Configure Angle slider | Vertical Spacing |

```
Tag ──── 黃 landing（水平）──── elbow ╲
                                      ╲  ← Angle 只喺呢度
                                       ╲
                                        → Host（連續貼邊）
```

## Workflow

1. Configure：Corner、Angle、Vertical Spacing、Constant Landing
2. 揀 Tags / Text Notes（可 Configure 後再揀）
3. **一 click** = 最接近 host 嘅 **taghead**；mouse 唔改 angle
4. **Upper** corner：最近 tag 喺 stack **底**，向上堆
5. **Lower** corner：最近 tag 喺 stack **頂**，向下堆
6. 重複 click 移成疊；**ESC** 結束

## 多 tag（Common Angle，Constant Landing OFF）

- 黃 landing + 紅 leader：**逐條自動調整長度**（common angle）；紅段平行；end pin 原本 face
- End：**SnapEndToOriginalFace** — pin 原本 face，唔轉面
- Tag → 黃 → 紅 → Host：**連續線**

## 場景

### Host 向右移（更遠）

- 黃 landing、elbow angle、平行方向：**唔變**
- **紅段長度**：越遠越長
- Tag column：**唔變**

### Host 1 / Host 2 垂直對調

- Tag ↔ Host 配對：**唔變**
- Stack 次序：**跟 host 高度重新排**（`CompareHostStackOrder`）
- Upper：最低 host 嘅 tag → row 0（click 位置）
- 對調後 click 要放 **而家最底 host 嘅 tag**

## Constant Landing

| 模式 | 紅 leader |
|------|-----------|
| OFF（預設） | 全部平行同一 Angle |
| ON | 每條各自指向 host |

## 設定檔

`%AppData%\Roaming\RevitTagAlign\AlignConfig.xml`

## 直接下載

https://github.com/ngroy799-sudo/roy/raw/cursor/revit-tag-align-tool-845e/RevitTagAlign/dist/RevitTagAlign-2023.zip

## 相關 code

- `LeaderGeometry.cs` — 純數學（有 unit tests）
- `AlignmentEngine.cs` — Revit 幾何應用
- `RevitTagAlign.Tests/LeaderGeometryTests.cs` — 60° elbow、host 距離、stack 排序
