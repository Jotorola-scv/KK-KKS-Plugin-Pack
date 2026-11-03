**Optional fix.** Only needed if you use EditFangs and fangs look stretched, thin or lopsided on modded heads outside the maker.

EditFangs remembers the fang vertices before the card's head loads and only re-reads them in the maker, so elsewhere it writes the stock head's vertices onto the modded head. This plugin makes it read the head that is actually loaded. Card settings are unchanged.

KKS: confirmed on Head Mod2 with 1.0.1. 1.0.2 also handles heads whose two lowest fang vertices are not at exactly the same height, and has not been run in game yet.

**Download:** `KK_EditFangsHeadFix.dll` (Koikatsu / Koikatsu Party) or `KKS_EditFangsHeadFix.dll` (Koikatsu Sunshine), into `BepInEx/plugins`. Details: [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/EditFangsHeadFix).

---

**選用修正。** 只有使用 EditFangs、而且移植頭模的八重齒在 maker 以外又長又細或左右不一樣時才需要。插件會讓 EditFangs 改讀實際載入的頭，角色卡設定不變。KKS 已用 1.0.1 確認有效；1.0.2 另外處理最低兩個頂點高度不同的頭，還沒有實際跑過。詳見 [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/EditFangsHeadFix#中文說明)。
