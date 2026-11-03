**Two optional fang fixes in one download.** Each one only matters if you run into the problem it describes.

**KKPE Canine Link 1.0.1** (CharaStudio, requires KKPE): KKPE overrides each mouth mesh on its own, so a Teeth override or Timeline track leaves the fangs on the game's mouth shape. While Teeth is overridden and Canines is not, this plugin copies the teeth weights onto the fangs. Nothing is saved in the scene, and a manual Canines override always wins. Fixed in 1.0.1: after resetting Teeth or turning the plugin off, the `bisyou_op1` and `i_s_cl` shapes could stay on the fangs, because the game never rewrites them. [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/KKPECanineLink)

**EditFangs Head Fix 1.0.2** (requires EditFangs): fangs look stretched, thin or lopsided on modded heads outside the maker. EditFangs remembers the fang vertices before the card's head loads and only re-reads them in the maker, so elsewhere it writes the stock head's vertices onto the modded head. This plugin makes it read the head that is actually loaded. Card settings are unchanged. KKS: confirmed on Head Mod2 with 1.0.1. 1.0.2 also handles heads whose two lowest fang vertices are not at exactly the same height, and runs in both games' CharaStudio without warnings. [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/EditFangsHeadFix)

**Download:** `KK_CanineLink.rar` (Koikatsu / Koikatsu Party) or `KKS_CanineLink.rar` (Koikatsu Sunshine). Extract it into `BepInEx/plugins`. If you do not have KKPE or EditFangs, the matching dll does not load and BepInEx notes that in its log. You can delete that dll.

---

**兩個選用的八重齒修正，一次下載。** 只有遇到各自說明的問題時才需要。

**KKPE Canine Link 1.0.1**（CharaStudio，需要 KKPE）：只有用 KKPE 的 blendshape 編輯嘴型或做動畫、而且角色開了八重齒時才需要。Teeth 被覆寫、Canines 沒有時，插件會把牙齒的權重複製給八重齒，不會寫入場景。1.0.1 修正：重設 Teeth 或關閉插件後，`bisyou_op1` 和 `i_s_cl` 可能殘留在八重齒上，因為遊戲不會重寫這兩個形狀。[說明](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/KKPECanineLink#中文說明)

**EditFangs Head Fix 1.0.2**（需要 EditFangs）：只有移植頭模的八重齒在 maker 以外又長又細或左右不一樣時才需要。插件會讓 EditFangs 改讀實際載入的頭，角色卡設定不變。KKS 已用 1.0.1 確認有效；1.0.2 另外處理最低兩個頂點高度不同的頭，在兩款遊戲的 CharaStudio 裡都沒有警告。[說明](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/EditFangsHeadFix#中文說明)

**下載：** `KK_CanineLink.rar`（戀活）或 `KKS_CanineLink.rar`（戀活 Sunshine），解壓縮到 `BepInEx/plugins`。沒有 KKPE 或 EditFangs 時，對應的 dll 不會載入，BepInEx 會在 log 裡註明，可以把那個 dll 刪掉。
