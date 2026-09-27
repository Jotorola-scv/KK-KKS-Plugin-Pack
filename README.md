# KK/KKS Plugin Pack

BepInEx plugins for **Koikatsu (KK)** and **Koikatsu Sunshine (KKS)**. Every plugin is a separate dll: install only the ones you want.

[中文說明在下方](#中文說明) · [日本語の概要](#日本語の概要)

## Plugins

| Plugin | Games | Where | What it does | Status |
|---|---|---|---|---|
| [**Single Side Clothes**](SingleSideClothes/README.md) | KK, KKS | Maker, CharaStudio, main game | Show only the left or right piece of gloves, pantyhose, legwear, shoes and paired accessories, per outfit, saved in the character card. **People you share cards with need the plugin too**, otherwise both sides show. | 1.4.0 |
| [**Pseudo Maker Extras**](PseudoMakerExtras/README.md) | KK, KKS | CharaStudio | Adds maker settings that Studio Pseudo Maker lacks: ABMX bone sliders with *Split XYZ*, an accessory *Move* button, Fang / EditFangs sliders, Single Side Clothes switches and ClothingBlendShape sliders (with Timeline). | 1.3.0 **beta, testers wanted** |
| [**Shader Swapper Null Fix**](ShaderSwapperNullFix/README.md) | KK, KKS | Everywhere | *Optional fix.* Stops Shader Swapper from wiping an outfit's Material Editor textures after an accessory transfer. | 1.0.0 |
| [**KKUTS Clothes Redirect / KKUTSclothes Compat**](KKUTSClothesRedirect/README.md) | KKS / KK | Everywhere | *Optional fix.* Makes KKUTS outfits look the same when they move between KK and KKS, without editing cards. | 1.0.0 / 1.1.0 |

The two *optional fixes* only matter if you run into the specific problem each one describes. The other plugins do not need them.

## Downloads

Get the dlls from [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases). Each plugin has its own release, tagged `<Plugin>-v<version>`. Single Side Clothes 1.4.0 is tagged `v1.4.0`. Files starting with `KK_` are for Koikatsu / Koikatsu Party, files starting with `KKS_` are for Koikatsu Sunshine. Put them in `BepInEx/plugins`.

## Reporting problems

Please open an [issue](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) and say which plugin and game it is about. Each plugin's README lists what helps to include.

## Building

`build/build.sh` compiles one plugin against your own game install with the .NET SDK's Roslyn compiler (no NuGet). The plugins that a project works with must be installed in that game folder.

```sh
sh build/build.sh SingleSideClothes    KK  "D:/Games/Koikatsu"
sh build/build.sh PseudoMakerExtras    KKS "D:/Games/Koikatsu Sunshine"
sh build/build.sh ShaderSwapperNullFix KK  "D:/Games/Koikatsu"
sh build/build.sh KKUTSClothesRedirect KKS "D:/Games/Koikatsu Sunshine"   # KK builds KKUTSclothes Compat
```

The output goes to `bin/`. `sh build/package.sh <KK folder> <KKS folder> [KK CharaStudio folder]` builds everything and copies the release dlls to `dist/`.

## Credits

- **Code:** written by **Claude**, Anthropic's AI assistant.
- **Ideas, direction and in-game testing:** [Jotorola-scv](https://github.com/Jotorola-scv), who maintains this repository.
- Single Side Clothes is based on an idea by **Nil** (*KK_SingleShoe*). The other projects these plugins work with are credited in each plugin's README.
- License: [MIT](LICENSE)

---

## 中文說明

**KK/KKS Plugin Pack** 是戀活（KK）與戀活 Sunshine（KKS）的 BepInEx 插件合集。每個插件都是獨立的 dll，只裝需要的就好。

| 插件 | 遊戲 | 使用場合 | 功能 | 狀態 |
|---|---|---|---|---|
| [**Single Side Clothes**（單邊服裝）](SingleSideClothes/README.md#中文說明) | KK、KKS | 創角、CharaStudio、本篇 | 手套、褲襪、襪子、鞋子和成對配件可以只顯示左邊或右邊，依服裝設定並存在人物卡裡。**分享人物卡時對方也要安裝**，否則會顯示雙邊。 | 1.4.0 |
| [**Pseudo Maker Extras**](PseudoMakerExtras/README.md#中文說明) | KK、KKS | CharaStudio | 在 Studio Pseudo Maker 補上創角有的項目：ABMX 骨骼滑桿（含 Split XYZ）、配件 Move 按鈕、八重齒／EditFangs 滑桿、Single Side Clothes 開關、ClothingBlendShape 滑桿（支援 Timeline）。 | 1.3.0 **測試版，歡迎協助測試** |
| [**Shader Swapper Null Fix**](ShaderSwapperNullFix/README.md#中文說明) | KK、KKS | 全部 | **選用修正。** 避免 Shader Swapper 在配件轉移後讓整套服裝的 Material Editor 貼圖消失。 | 1.0.0 |
| [**KKUTS Clothes Redirect／KKUTSclothes Compat**](KKUTSClothesRedirect/README.md#中文說明) | KKS／KK | 全部 | **選用修正。** 讓使用 KKUTS 的服裝在 KK 和 KKS 之間搬移後看起來一樣，不必修改卡片。 | 1.0.0／1.1.0 |

兩個**選用修正**只有在遇到它們各自說明的問題時才需要，其他插件不依賴它們。

**下載：** 到 [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) 下載。每個插件各有自己的 release，標籤為 `插件名-v版本`（Single Side Clothes 1.4.0 的標籤是 `v1.4.0`）。`KK_` 開頭的給戀活／Koikatsu Party，`KKS_` 開頭的給戀活 Sunshine，放進 `BepInEx/plugins`。

**回報問題：** 請到 [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues)，註明是哪個插件、哪個遊戲。各插件的說明裡有列出需要附上的資訊。

**製作：** 程式碼由 Anthropic 的 AI 助理 **Claude** 撰寫；構想、方向與遊戲內測試為 [Jotorola-scv](https://github.com/Jotorola-scv)。Single Side Clothes 的構想來自 **Nil** 的 *KK_SingleShoe*。授權：MIT。

---

## 日本語の概要

**KK/KKS Plugin Pack** は、コイカツ（KK）／コイカツ・サンシャイン（KKS）用 BepInEx プラグイン集です。プラグインはそれぞれ独立した dll なので、必要なものだけ入れてください。

- [**Single Side Clothes**](SingleSideClothes/README.md#日本語の概要)：手袋・パンスト・靴下・靴・ペアアクセサリーを左右片側だけ表示（キャラメイクで設定、カードに保存）。カードを受け取る側にもプラグインが必要です。
- [**Pseudo Maker Extras**](PseudoMakerExtras/README.md#日本語の概要)（**ベータ版・テスター募集中**）：スタジオの Pseudo Maker に ABMX スライダー、アクセサリーの Move、八重歯／EditFangs、Single Side Clothes、ClothingBlendShape を追加。
- [**Shader Swapper Null Fix**](ShaderSwapperNullFix/README.md#日本語の概要)（任意の修正）：アクセサリー転送後に Material Editor のテクスチャが消える Shader Swapper の不具合を回避。
- [**KKUTS Clothes Redirect／KKUTSclothes Compat**](KKUTSClothesRedirect/README.md#日本語の概要)（任意の修正）：KKUTS の服を KK と KKS の間で移しても同じ見た目にする。

ダウンロードは [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) から。不具合や感想は [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) へどうぞ。
