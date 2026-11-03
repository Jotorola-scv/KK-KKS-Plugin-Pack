# EditFangs Head Fix

Part of the [KK/KKS Plugin Pack](../README.md). **Optional:** only needed if you use EditFangs and fangs look wrong outside the maker on modded heads.

A small BepInEx fix for **Koikatsu (KK)** and **Koikatsu Sunshine (KKS)**.

## The problem

EditFangs adds the *Fang Length* and *Fang Spacing* sliders. It remembers the fang's vertices when the character is created, before the card's head has loaded, and only reads them again in the maker. Outside the maker it then writes the stock head's vertices onto the modded head's fang.

In KKS, heads such as *Head Mod*, *Head Mod2* and *Modified headmod* have fangs in the same positions as the stock head, but only 5 of the 42 vertices share the same index. Skin weights, triangles and expression shapes all land on the wrong vertices. **The fangs look stretched, thin and different on each side in CharaStudio, while the same card looks fine in the maker.** Saving and reloading does not help.

## The fix

Right before EditFangs adjusts the fang, the plugin makes it read the vertices from the head that is actually loaded. The card's fang settings are unchanged. Fang meshes that do not have the 42 vertices EditFangs expects are left alone.

**Long fangs are not this bug.** Fang Length moves only the tip vertex, so a high value gives a long, narrow spike. It should still look the same on both sides.

## Installation

Download `KK_EditFangsHeadFix.dll` (Koikatsu / Koikatsu Party) or `KKS_EditFangsHeadFix.dll` (Koikatsu Sunshine) from [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) and put it in `BepInEx/plugins`. Requires BepInEx 5 and EditFangs (tested with KKS_EditFangs 1.1.1 and KK_EditFangs 1.1.2). Delete the dll to uninstall. It can be removed once EditFangs fixes this itself.

## Tested in game

- **KKS:** the fix was confirmed on Head Mod2 in CharaStudio with 1.0.1.
- **KK:** the three heads above have the same vertex order as the stock head, so the visible bug was not reproduced there. Version 1.0.2 fixes a warning seen in the KK log (see below) and has not been run in either game yet.

1.0.2 also handles heads whose two lowest fang vertices are not at exactly the same height. EditFangs throws on those, which showed up as `Could not re-read the fang vertices` in the log.

## Credits

- **Code:** written by **Claude**, Anthropic's AI assistant. **Problem report, direction and in-game testing:** [Jotorola-scv](https://github.com/Jotorola-scv).
- Works around a bug in EditFangs by [Njaecha](https://github.com/Njaecha). It is not affiliated with that project.
- License: MIT

---

## 中文說明

**選用**：只有使用 EditFangs、而且移植頭模的八重齒在 maker 以外看起來不對時才需要。適用於 KK 和 KKS。

**問題：** EditFangs 提供 *Fang Length* 和 *Fang Spacing* 滑桿。它在角色建立時就記下八重齒的頂點，那時角色卡的頭還沒載入，之後也只有 maker 會重新讀取。所以在 maker 以外，它會把原版頭的頂點寫到移植頭模的八重齒上。

在 KKS，*Head Mod*、*Head Mod2*、*Modified headmod* 這些頭的八重齒位置和原版相同，但 42 個頂點只有 5 個順序一致，權重、三角面和表情形狀都會對到錯的頂點。**結果是八重齒在 CharaStudio 裡又長又細、左右不一樣，同一張卡在 maker 卻正常。** 存檔重載也修不好。

**修正：** 在 EditFangs 調整八重齒之前，讓它改讀目前實際載入的頭。角色卡的八重齒設定不會改變。頂點數不是 EditFangs 所需 42 個的八重齒網格不會處理。

**八重齒很長不一定是這個問題。** Fang Length 只移動尖端那一個頂點，數值高時本來就會變成又長又尖，但左右應該一樣。

**安裝：** 從 [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) 下載 `KK_EditFangsHeadFix.dll`（戀活）或 `KKS_EditFangsHeadFix.dll`（戀活 Sunshine），放進 `BepInEx/plugins`。需要 BepInEx 5 和 EditFangs（測試版本 KKS_EditFangs 1.1.1、KK_EditFangs 1.1.2）。刪除 dll 即可解除安裝；等 EditFangs 本身修好後就可以移除。

**遊戲內測試：** KKS 已用 1.0.1 在 CharaStudio 的 Head Mod2 上確認修正有效。KK 的這三顆頭頂點順序和原版相同，所以沒有重現這個外觀問題。1.0.2 修正了 KK log 裡的警告（有些頭最低的兩個八重齒頂點高度不完全相同，EditFangs 會出錯），還沒有在兩款遊戲裡實際跑過。

---

## 日本語の概要

**任意**：EditFangs を使っていて、MOD の頭の八重歯がメーカー以外で崩れる場合だけ必要です。EditFangs はカードの頭が読み込まれる前に八重歯の頂点を記憶し、メーカーでしか読み直しません。そのため CharaStudio などでは、純正の頭の頂点が MOD の頭に書き込まれ、八重歯が細長く左右非対称になります。このプラグインは、調整の直前に実際の頭から頂点を読み直させます。カードの設定は変わりません。[Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) から dll をダウンロードし、`BepInEx/plugins` に入れてください。
