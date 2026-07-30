# 使用者故事與驗收條件

## US-01 對話建牆
**As a** BIM Engineer  
**I want** 用中文描述牆的位置與類型  
**So that** 不用手動一條條畫牆  

**Acceptance Criteria**
- Given 專案有 Level 1 與牆類型 `200mm Concrete`
- When 輸入「在 Level 1 從 (0,0) 到 (8000,0) 建 200mm 混凝土牆」
- Then 系統顯示 CreateWall Action 預覽
- And 確認後模型出現正確牆，且可 Undo

## US-02 批次改參數
**As a** BIM Engineer  
**I want** 批次修改所選門的寬度  
**So that** 節省重複點選時間  

**Acceptance Criteria**
- 選取多扇門後下指令「寬度改 900」
- 僅修改選取元素
- 無法寫入的參數要列出失敗清單

## US-03 出圖
**As a** Drafter  
**I want** 一鍵為各樓層建平面圖紙  
**So that** 加速出圖準備  

**Acceptance Criteria**
- 指定 titleblock 與 view template 後產生 Sheets
- 每層至少一張平面圖
- 命名符合專案規則（由 RAG／設定提供）

## US-04 標準問答
**As a** Junior BIM Engineer  
**I want** 問「走廊牆用哪種類型」  
**So that** 符合公司標準  

**Acceptance Criteria**
- 回答引用知識庫條目
- 可選「套用此類型並繼續建模」

## US-05 高風險確認
**As a** BIM Coordinator  
**I want** 刪除操作必須確認  
**So that** 避免誤刪模型  

**Acceptance Criteria**
- Delete Action 預設 `requires_confirmation=true`
- 未確認不得執行
