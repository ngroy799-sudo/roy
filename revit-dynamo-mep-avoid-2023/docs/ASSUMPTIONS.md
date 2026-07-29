# 假設狀態

已確認決策見 `DECISIONS.md`。以下為仍可微調的工程假設：

1. Linked 障礙：第一版只處理 **當前模型** 已選元素；Link 暫忽略（未確認 Q5）。
2. 執行方式：先支援手動開 Dynamo；Dynamo Player 包裝列 Phase 4。
3. Elbow／Fitting：使用專案內預設 Pipe/Duct Elbow 類型；無合適類型則跳過並記錄。
4. Revit 執行檔預設：`C:\Program Files\Autodesk\Revit 2023\Revit.exe`。
5. 水平或近水平 MEP 為主；明顯坡度管第一版可跳過並記錄。
6. 多個相鄰障礙可合併為一個下沉區（細節見 TECH_DESIGN）。
7. 執行後使用者需目視檢查 Fitting 連接與系統完整性。
