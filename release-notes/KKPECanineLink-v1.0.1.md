**Optional fix.** Only needed if you edit or animate the mouth with KKPE blendshapes on characters with fangs.

KKPE overrides each mouth mesh on its own, so a Teeth override or Timeline track leaves the fangs on the game's mouth shape. While Teeth is overridden and Canines is not, this plugin copies the teeth weights onto the fangs. Nothing is saved in the scene, and a manual Canines override always wins.

**Fixed in 1.0.1:** after resetting Teeth or turning the plugin off, the `bisyou_op1` and `i_s_cl` shapes could stay on the fangs, because the game never rewrites them.

**Download:** `KK_KKPECanineLink.dll` (Koikatsu / Koikatsu Party) or `KKS_KKPECanineLink.dll` (Koikatsu Sunshine), into `BepInEx/plugins`. CharaStudio only, requires KKPE. Details: [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/KKPECanineLink).

---

**選用修正。** 只有用 KKPE 的 blendshape 編輯嘴型或做動畫、而且角色開了八重齒時才需要。Teeth 被覆寫、Canines 沒有時，插件會把牙齒的權重複製給八重齒，不會寫入場景。

**1.0.1 修正：** 重設 Teeth 或關閉插件後，`bisyou_op1` 和 `i_s_cl` 可能殘留在八重齒上，因為遊戲不會重寫這兩個形狀。詳見 [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/KKPECanineLink#中文說明)。
