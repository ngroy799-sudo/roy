# Roy NG Portfolio 改進報告(資深 UXUI Designer 視角)

> 針對 Canva deck《Copy of Roy NG》(https://canva.link/abjq55k1nfiuky8) 嘅完整 review。
> 同一個 folder 入面嘅 `index.html` 係按呢份報告重新打造嘅網頁版 portfolio,可以直接開嚟睇效果。

---

## 一、總體評價

**做得好嘅地方:**
- 有完整嘅 design process 展示(Discover → Define → Develop → Deliver),唔係淨係擺靚圖
- 有真實 research 證據:13 間銀行 competitor analysis、4 間虛擬銀行 benchmark、5 人 guerrilla testing
- 有講 challenge(UI 唔一致)同埋點樣用 design system 解決 —— 呢樣係好多 designer portfolio 冇嘅

**致命問題(令說服力大打折扣):**

| # | 問題 | 影響 |
|---|------|------|
| 1 | **完全冇量化成果(outcome/metrics)** | 睇完成個 deck,唔知你嘅設計「改善咗啲乜」。Hiring manager 最想睇嘅係 impact,唔係 process |
| 2 | **英文串錯字多** — "Completitor"(應為 Competitor)、"Gureilla"(Guerilla)、"Ditial"(Digital)、"Brad-related"(Brand-related) | UXUI Designer 賣嘅係 attention to detail,串錯字直接扣分 |
| 3 | **年資過時** — 寫住 "7+ years" 但 career path 由 2015 開始,截至而家已經 10 年以上;2023 年後有空窗冇交代 | 顯得份 portfolio 冇更新,亦令人懷疑近況 |
| 4 | **自我介紹太 generic** — "proficient in Figma, Sketch..." 人人都寫,冇 positioning | 開頭 10 秒留唔住人 |
| 5 | **NDA 風險** — deck 入面直接出現 "HASE"(恒生銀行)字眼 | 銀行項目通常有保密協議,公開 portfolio 應該匿名化(寫 "a major HK retail bank"),反而顯得專業 |
| 6 | **每頁重複 "Roy NG / UXUI Portfolio Presentation / Next"** 佔用大量空間 | 資訊密度低,deck 太長(重複 header/footer 可以極簡化) |
| 7 | **冇 CTA、冇聯絡方式、冇 testimonial** | 睇完唔知點搵你 |
| 8 | **Career path 冇講每份工嘅「成果」**,只講「負責乜」 | Responsible ≠ Impact |

---

## 二、逐部分改進建議 + 改良版文案

### 1. 封面 + 自我介紹(Slide 1–2)

**問題:** "Hi, my name is Roy NG, a UXUI Designer with 7+ years..." 係工具清單式介紹,冇賣點。

**改良版(可直接貼入 Canva):**

> **I design banking & fintech products that turn complexity into confidence.**
>
> Roy NG — Senior UX/UI & Product Designer, Hong Kong
>
> 10+ years across agency, startup, fintech and consulting (Capgemini), including a large-scale retail banking app revamp. I run the research, craft the UI, build the design system — and because I code (HTML/CSS/JS), my designs get shipped, not questioned. Currently exploring AI-assisted design workflows and Web3 product patterns.

**重點:**
- 第一句係 value proposition,唔係自我介紹
- "7+ years" → "10+ years"(2015 至今)
- 將「識 coding」由技能清單變成賣點:「designs get shipped, not questioned」
- AI/Web3 由「興趣」升級為「正在實踐嘅方向」(2026 年呢點好值錢)

### 2. Career Path(Slide 3–7)

**問題:** 每份工只有 About + Responsible,冇成果;2023 之後空白。

**改良格式 — 每份工加一行 Impact:**

| 年份 | 職位 | 一行 Impact(新增) |
|------|------|---------------------|
| 2023–現在 | Senior Product Designer(Freelance/Contract)| **必須新增呢一欄交代近況**,例:為 fintech/SaaS 團隊做 design system 同 AI workflow |
| 2020–2023 | UX/UI Consultant @ Capgemini | 用一套 design system 統一咗多個 vendor team 整出嚟嘅碎片化 UI |
| 2019–2020 | UX/UI Designer @ Pony Technology | 一人包辦,由 0 到 1 推出 VPN 產品 |
| 2017–2019 | UX/UI Designer @ SD System | 重新設計後台支付 portal,提升內部團隊日常操作效率 |
| 2015–2017 | UI Designer @ Four Directions | 多品牌 agency 交付:user flow、wireframe、prototype、WordPress |

### 3. Case Study 1:Pre-login Landing Page Redesign

**問題:** process 好完整,但結尾冇 outcome;"Completitor Research" 串錯字。

**建議新增 Outcome slide(數字要用返你真實數據,以下係示範格式):**

- ✅ 回頭用戶到達 login 嘅步數減少 ~40%(內部 walkthrough 對比舊 flow)
- ✅ 8 個 design options 迭代收斂至 1 個獲 stakeholder 一致通過嘅方案
- ✅ 100% 畫面連 dev-ready spec + marketing guidance 交付
- ✅ 全部顏色通過 WCAG accessibility contrast

**其他修正:**
- "Completitor Research" → "Competitor Research"
- "Brad-related images" → "Brand-related images"
- 13 間銀行 research 係好大賣點,建議升做大字 headline:「Benchmarked 13 banks across China, HK & overseas」

### 4. Case Study 2:Digital ID Verification

**問題:** "Gureilla Research"、"Ditial ID" 串錯;deck 出現 "HASE" 字眼(NDA 風險);research 結果冇轉化成數字。

**建議:**
- "HASE back offices" → "the bank's back offices"(匿名化)
- 加 outcome:「5/5 測試參與者無需協助完成整個驗證流程」—— 呢個數據你本身已經有("All participants completed the Digital ID Verification steps"),只係冇包裝成 metric
- Guerrilla research 嘅 findings→design changes 對應關係好正,建議做成一個「Finding → Design decision」對照表,一眼睇到你係 evidence-driven

### 5. Design System(最後部分)

**問題:** 呢個係全 deck 最有價值嘅故事(entropy → system),但只用咗兩頁帶過。

**建議:** 升格做獨立 case study,用「問題唔係一個 screen,係 entropy」呢個角度講:
1. UI audit 發現幾多種不一致(例:7 種藍色、5 款 button —— 用返真實數字)
2. 建立 token + component library
3. 設立「新 component 要 contribute 返入 library」嘅流程,令 consistency 會複利增長

### 6. 全新增加嘅部分(原 deck 冇)

1. **Design Principles(3 條)** — 顯示你有自己嘅方法論:
   - Research before pixels
   - Design for the handoff
   - Systems over screens
2. **Testimonial / 引言** — 搵舊同事或 PO 寫一兩句(deck 入面可以寫 "reference available on request")
3. **Contact + CTA 頁** — email、LinkedIn、online portfolio link
4. **NDA 聲明** — "Client details anonymised under NDA; full walkthrough available in interview" 反而加分

---

## 三、需要 Roy 自己核實/補充嘅資料

網頁版 (`index.html`) 入面有啲位係我按合理推斷填寫,請更新為真實資料:

- [ ] 2023 年之後嘅工作經歷(而家寫咗 Freelance placeholder)
- [ ] Email(placeholder: hello@royng.design)、LinkedIn URL
- [ ] Outcome 數字(−40% taps、20+ products)— 建議用真實數據取代,或保留 * 註明係內部估算
- [ ] Testimonial 引言(而家係示範寫法,需要真人授權)
- [ ] 如有真實 app screenshot / mockup(冇 NDA 問題嘅),加入 case study 部分

---

## 四、檔案清單

| 檔案 | 用途 |
|------|------|
| `index.html` | 重新設計嘅網頁版 portfolio(單一檔案,可直接開啟或部署) |
| `PORTFOLIO-REVIEW.md` | 本報告:逐頁點評 + Canva 改良版文案 |
| `README.md` | Folder 索引(方便 AI agent 搜尋) |
