First public release.

Show only the left or the right piece of gloves, pantyhose, legwear, shoes and paired accessories — per outfit, set in the character maker and saved in the character card (also applies in CharaStudio and the main game).

Based on an idea by **Nil** (*KK_SingleShoe*, a CharaStudio shoe plugin), extended to the character maker, more clothing parts and accessories. Independent implementation; thanks to Nil for the original idea.

**Download**
- Koikatsu / Koikatsu Party: `KK_SingleSideClothes.dll`
- Koikatsu Sunshine: `KKS_SingleSideClothes.dll`

Put the dll in `BepInEx/plugins`. Requires BepInEx 5, KKAPI / KKSAPI and ExtensibleSaveFormat.

**What's in it**
- *Single side* switch (Both / Left only / Right only) in the Gloves, Pantyhose, Legwear and Shoes tabs of the character maker, placed next to the Material Editor button
- The same switch for ClothesToAccessories clothing accessories (gloves / pantyhose / socks / shoes) and for paired Arm / Leg accessories
- CharaStudio: *Single side* dropdowns in the character state panel
- Game files and mod files are never modified

**Good to know**
- **Sharing cards:** the setting is saved in the character card, so people you share cards or scenes with need this plugin too. Without it the setting is ignored and both sides are shown.
- Items whose left and right halves are one connected mesh stay fully visible.
- One-piece pantyhose (joined at the waist, bodystockings) don't hide one leg: set to one side, they are cut down the body's middle instead, waist or torso part included.
- Shoes: different shoe models have different heel heights, so the main use is **mismatched colours** — hide one side of the main shoes and wear the **same model** in another colour from an accessory slot.

See the [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/SingleSideClothes#limitations) for details and all limitations.

---

首次公開版本。構想來自 **Nil** 的 *KK_SingleShoe*，延伸到創角、更多服裝部位與配件。手套、褲襪、襪子、鞋子與成對配件可以只顯示左邊或右邊，在創角時依服裝設定並存在人物卡裡（CharaStudio 與本篇遊戲同樣套用）。連身款褲襪設成單邊時會沿身體中線切成一半，而不是只隱藏一條腿；鞋子因為各款高度不同，主要用來做同款異色鞋。詳見 [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/SingleSideClothes#限制)。

**分享人物卡時，對方也需要安裝這個插件**：設定存在人物卡裡，對方沒有安裝時設定不會生效，服裝會左右兩邊都顯示。
