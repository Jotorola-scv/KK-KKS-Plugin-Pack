# Pseudo Maker Extras

Part of the [KK/KKS Plugin Pack](../README.md). **Beta: testers wanted** (see [Reporting problems](#reporting-problems)).

A BepInEx plugin for **CharaStudio** in **Koikatsu (KK)** and **Koikatsu Sunshine (KKS)**. It adds rows to **Studio Pseudo Maker** (by rikkibalboa) for settings that the character maker has but Pseudo Maker does not, so you can change them on a character in a scene without going back to the maker.

[中文說明在下方](#中文說明) · [日本語の概要](#日本語の概要)

## Features

Each feature switches itself on when the plugin it works with is installed. Values are written through that plugin's own controller, so they are saved with the scene or character exactly as if they had been set in the maker.

- **ABMX bone sliders.** The maker's yellow ABMX sliders, added at the bottom of the matching Pseudo Maker panels under an *ABMX* header:
  - Face: General, Ears, Jaw, Cheeks, Eyebrows, Eyes, Nose, Mouth
  - Body: General, Chest, Upper Body, Lower Body, Arms, Legs, Pubic Hair
  - Clothing: Bottom (skirt)

  Each panel has a **Split XYZ scale sliders** switch (the same setting as ABMX's own switch in the maker). Bones that come in left/right pairs have **Side to edit** (Both / Left / Right). Bones that ABMX keeps per outfit follow the current outfit. The maker's finger sliders are not included.
- **Accessory Move.** A *Move* button next to *Copy* in the accessory *Transfer* tab. It copies the accessory the same way Pseudo Maker's Copy does, so Material Editor and other plugins copy their data along, and then empties the source slot.
- **Fang.** A *Fang* (double tooth) switch in Face > Mouth. With *EditFangs* (by Njaecha) installed, four more sliders follow it: left/right fang length and spacing, with the same ranges and defaults as in the maker.
- **Single Side Clothes.** The *Single side* switch (Both / Left / Right) of [Single Side Clothes](../SingleSideClothes/README.md) in the Gloves, Pantyhose, Legwear and Shoes panels, and in the accessory panel for the accessory types it supports. Pseudo Maker's Copy and the Move button carry an accessory's setting along.
- **Clothing BlendShape.** The per-item sliders (UpL1, UpR1, …) of nakay's *ClothingBlendShape* in the clothing panels. Tops with several pieces get a set of sliders per piece. **Timeline:** click a slider's *label* to select it; Timeline then lists it as *Clothing BlendShape* under *Pseudo Maker*, so it can be keyframed.

## Requirements

- CharaStudio with BepInEx 5 and KKAPI / KKSAPI
- **Studio Pseudo Maker** by rikkibalboa (tested with 1.5.2.0)
- **ABMX** (Pseudo Maker already requires it)
- Optional: Single Side Clothes, EditFangs, ClothingBlendShape (tested with KK 1.0.0 and KKS 1.0.2), Timeline

Without an optional plugin, only its rows are missing. The log line `Features: ClothingBlendShape=…, SingleSideClothes=…, Fang=on, EditFangs=…, AccessoryMove=…` shows what is active.

## Status

This is a **beta**. The maintainer has tested every feature in KK and KKS CharaStudio, including saving and reloading scenes with changed ABMX values, and checked that Studio starts and runs normally when the optional plugins are missing. It has not been used on other people's setups yet.

## Installation

1. Download the dll for your game from [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases):
   - Koikatsu / Koikatsu Party: `KK_PseudoMakerExtras.dll`
   - Koikatsu Sunshine: `KKS_PseudoMakerExtras.dll`
2. Put it in `BepInEx/plugins` in your game folder.
3. Start CharaStudio and open Pseudo Maker on a character.

To uninstall, delete the dll. Nothing is stored by this plugin itself.

## Good to know

- If Pseudo Maker changes its internals in a later version, the log says `Unsupported Pseudo Maker version, missing member: …` and the plugin stays off, or `Accessory transfer panel members not found; the Move button is disabled.` and only Move is off.
- When an optional plugin is missing, some plugins and HarmonyX may log a `ReflectionTypeLoadException` warning that mentions `KK_PseudoMakerExtras` / `KKS_PseudoMakerExtras` while they scan all assemblies. That warning is harmless.

## Reporting problems

Please open an [issue](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues). Include the game (KK / KKS), which panel and row, what you expected and what happened, the `Features:` line from the log, and any errors that mention `Pseudo Maker Extras` in `output_log.txt` / `LogOutput.log`. Reports that everything works are welcome too.

## Credits

- **Code:** written by **Claude**, Anthropic's AI assistant.
- **Idea, direction and in-game testing:** [Jotorola-scv](https://github.com/Jotorola-scv).
- Built on **Studio Pseudo Maker** (rikkibalboa) and works with [ABMX](https://github.com/ManlyMarco/ABMX) (ManlyMarco), *ClothingBlendShape* (nakay), *EditFangs* (Njaecha), [KKAPI](https://github.com/IllusionMods/IllusionModdingAPI) and Timeline. It contains no code from those projects and is not affiliated with them.
- License: MIT

---

## 中文說明

**Pseudo Maker Extras** 是戀活（KK）與戀活 Sunshine（KKS）**CharaStudio** 用的 BepInEx 插件。它在 rikkibalboa 的 **Studio Pseudo Maker** 裡補上創角有、Pseudo Maker 沒有的項目，讓你在 Studio 場景裡直接調整角色，不必回到創角。**目前是測試版，歡迎協助測試**（見[回報問題](#回報問題)）。

### 功能

各功能在對應的插件有安裝時才會啟用。數值都寫進該插件自己的控制器，存場景或人物時和在創角裡設定的一樣。

- **ABMX 骨骼滑桿：** 創角裡黃色的 ABMX 滑桿，放在 Pseudo Maker 對應面板的最下方「ABMX」標題下：
  - 臉部：General、耳朵、下巴、臉頰、眉毛、眼睛、鼻子、嘴巴
  - 身體：General、胸部、上半身、下半身、手臂、腿、陰毛
  - 服裝：下身（裙子）

  每個面板都有「Split XYZ scale sliders」開關（和創角裡 ABMX 自己的開關是同一個設定）。左右成對的骨骼有「Side to edit」（雙邊／左／右）。ABMX 依服裝分開保存的骨骼，會跟著目前的服裝切換。創角的手指滑桿沒有加入。
- **配件 Move：** 配件 Transfer 分頁的 Copy 旁邊多一個 Move。先照 Pseudo Maker 的 Copy 複製（Material Editor 等插件的資料會一起轉移），再清空來源欄位。
- **八重齒：** 臉部 > 嘴巴 加上 Fang（八重齒）開關。有安裝 Njaecha 的 EditFangs 時，下方再多四個滑桿：左右虎牙的長度和間距，範圍與預設值和創角相同。
- **Single Side Clothes：** 手套、褲襪、襪子、鞋子面板，以及支援類型的配件面板，加上 [Single Side Clothes](../SingleSideClothes/README.md) 的「Single side」切換（雙邊／左／右）。Pseudo Maker 的 Copy 和 Move 會把配件的這個設定一起帶過去。
- **Clothing BlendShape：** 服裝面板加上 nakay 的 ClothingBlendShape 各件服裝的滑桿（UpL1、UpR1…）。有多個部件的上衣，每個部件各有一組。**Timeline：** 點滑桿的**名稱**選取它，Timeline 的 Pseudo Maker 底下會出現「Clothing BlendShape」，可以打關鍵影格。

### 需求

- CharaStudio、BepInEx 5、KKAPI／KKSAPI
- rikkibalboa 的 **Studio Pseudo Maker**（測試版本 1.5.2.0）
- **ABMX**（Pseudo Maker 本身就需要）
- 選用：Single Side Clothes、EditFangs、ClothingBlendShape（測試版本 KK 1.0.0／KKS 1.0.2）、Timeline

沒裝某個選用插件時，只是少了那部分的項目。log 裡的 `Features: …` 那一行會列出哪些功能有啟用。

### 狀態

這是**測試版**。維護者已在 KK 和 KKS 的 CharaStudio 測試過所有功能，包括改過 ABMX 數值後存檔再讀取場景，也確認過少了選用插件時 Studio 能正常啟動和運作。還沒有在其他玩家的環境上用過。

### 安裝

1. 從 [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) 下載對應遊戲的 dll：戀活／Koikatsu Party 用 `KK_PseudoMakerExtras.dll`，戀活 Sunshine 用 `KKS_PseudoMakerExtras.dll`。
2. 放進遊戲資料夾的 `BepInEx/plugins`。
3. 啟動 CharaStudio，對角色打開 Pseudo Maker。

解除安裝時刪除 dll 即可，這個插件本身不儲存任何資料。

### 注意事項

- 如果之後的 Pseudo Maker 版本改了內部結構，log 會出現 `Unsupported Pseudo Maker version, missing member: …`，插件不會啟用；或出現 `Accessory transfer panel members not found; the Move button is disabled.`，只有 Move 停用。
- 沒裝某個選用插件時，其他插件或 HarmonyX 掃描所有組件時，可能會在 log 留下提到 `KK_PseudoMakerExtras`／`KKS_PseudoMakerExtras` 的 `ReflectionTypeLoadException` 警告，這不影響使用。

### 回報問題

歡迎在 [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) 回報：遊戲（KK／KKS）、哪個面板哪一列、預期和實際的結果，log 裡的 `Features:` 那一行，以及 `output_log.txt`／`LogOutput.log` 中提到 `Pseudo Maker Extras` 的錯誤。一切正常的回報也很歡迎。

### 製作

- **程式碼：** 由 Anthropic 的 AI 助理 **Claude** 撰寫。
- **構想、方向與遊戲內測試：** [Jotorola-scv](https://github.com/Jotorola-scv)。
- 以 rikkibalboa 的 Studio Pseudo Maker 為基礎，搭配 ABMX（ManlyMarco）、ClothingBlendShape（nakay）、EditFangs（Njaecha）、KKAPI 與 Timeline 使用；不包含這些專案的程式碼，也與它們沒有關聯。
- 授權：MIT

---

## 日本語の概要

**Pseudo Maker Extras** は、コイカツ（KK）／コイカツ・サンシャイン（KKS）の**スタジオ**用 BepInEx プラグインです。rikkibalboa さんの **Studio Pseudo Maker** に、キャラメイクにはあって Pseudo Maker にはない項目を追加します。**現在ベータ版で、テストへの協力を募集しています。**

- **ABMX のボーンスライダー**（顔・体・ボトム）。「Split XYZ scale sliders」スイッチと、左右ペアのボーンの「Side to edit」付き。指のスライダーは含みません。
- アクセサリーの Transfer タブに、Copy の隣に **Move** ボタン（コピーしてから元の枠を空にします）。
- 口タブに**八重歯**スイッチ。EditFangs（Njaecha さん）があれば、左右の牙の長さと間隔のスライダーも追加。
- **Single Side Clothes** の「片側」スイッチ（手袋・パンスト・靴下・靴、対応するアクセサリー）。
- nakay さんの **ClothingBlendShape** のスライダー。スライダーの名前をクリックすると Timeline で使えます。
- 必要：Studio Pseudo Maker（1.5.2.0 でテスト）、ABMX。そのほかのプラグインは任意で、入っていない機能だけが非表示になります。
- インストール：[Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) から `KK_PseudoMakerExtras.dll`（コイカツ）または `KKS_PseudoMakerExtras.dll`（サンシャイン）をダウンロードし、`BepInEx/plugins` に入れてください。
- 不具合や感想は [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) へどうぞ。
