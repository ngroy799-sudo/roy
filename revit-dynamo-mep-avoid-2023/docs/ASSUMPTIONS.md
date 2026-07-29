# 暫定假設（未確認）

以下假設用於起草 SCOPE，**確認前請勿當最終規格**。

1. 第一版以 **MEP 為 Subject**，**ARC + STC 為 Obstacle**。
2. 「避開」MVP = **衝突報告 + 簡易轴向偏移**，不做完整自動路由。
3. 只處理 **當前模型** 元素；Linked Model 暫不寫入。
4. Clearance 全域預設 **50 mm**，可於 Dynamo 輸入調整。
5. 使用 **Dynamo for Revit 2023** 內建節點 + 少量 Python，避免強制第三方套件。
6. 使用者接受執行後需 **目視檢查接頭／系統完整性**。
7. 本機 Revit 實際路徑為 `C:\Program Files\Autodesk\Revit 2023\Revit.exe`（捷徑資料夾僅作啟動入口）。
