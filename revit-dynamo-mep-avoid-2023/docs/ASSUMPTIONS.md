# 假設狀態

核心規則已由使用者確認，見 `DECISIONS.md` ADR-001～003。  
以下為 **仍待確認** 的工作假設（可被推翻）：

1. 「上下表面」= 障礙 **底面** ↔ 下沉 MEP **頂面** ≥ 100 mm（ADR-004）。
2. 下沉後 **必須** 兩端接回原標高（ADR-005）。
3. 第一版先做 **Pipe 與 Duct**；Cable Tray / Conduit 其後。
4. 只處理 **當前模型** 內已選元素；Link 內障礙暫不納入，除非 Q5 要求。
5. 下方空間不足 100 mm 時：**跳過該段並記錄**，不強制寫入。
6. 使用 Dynamo + Python；盡量不強制第三方套件。
7. Revit 執行檔預設：`C:\Program Files\Autodesk\Revit 2023\Revit.exe`。
8. 執行後使用者需目視檢查 Fitting 連接與系統完整性。
