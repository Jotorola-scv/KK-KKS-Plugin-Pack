# Single Side Clothes

Part of the [KK/KKS Plugin Pack](../README.md).

A BepInEx plugin for **Koikatsu (KK)** and **Koikatsu Sunshine (KKS)** that shows only the **left or the right piece** of gloves, pantyhose, legwear, shoes and paired accessories — set **per outfit** in the **character maker**, and kept in the character card so it also applies in **CharaStudio** and the main game.

Typical use: one glove on, one bare hand; a single sock; one shoe off; or hide one side of the main gloves and fill it with a different glove from an accessory slot. For shoes, the main use is mismatched colours (see [Limitations](#limitations)).

> **Based on an idea by Nil.** This plugin builds on **Nil**'s *KK_SingleShoe*, a CharaStudio plugin that hides the left or right shoe. Single Side Clothes takes that idea further: it works in the **character maker**, covers **gloves, pantyhose and legwear** as well as shoes, supports **accessories**, and saves the setting in the character card. It is an independent implementation and contains no code from KK_SingleShoe. Thanks to Nil for the original idea.

> [!IMPORTANT]
> **Sharing cards: the other person needs this plugin too.** The single-side setting is stored in the character card as plugin data. Anyone who opens the card, or a scene with the character, **without Single Side Clothes installed sees the clothes on both sides**. The setting is simply ignored; nothing breaks. When you share cards or scenes that use it, ask people to install this plugin.

[中文說明在下方](#中文說明) · [日本語の概要](#日本語の概要)

## Features

- **Character maker:** a *Single side* switch (Both / Left only / Right only) in the **Gloves, Pantyhose, Legwear (socks) and Shoes** tabs. In KK it appears in both the indoor and outdoor shoe tabs; they share one setting.
- **Accessories:** the same switch appears in the accessory window for
  - clothing used as an accessory through [ClothesToAccessories](https://github.com/IllusionMods/KK_Plugins) (types *Clothes Gloves / Pantyhose / Socks / Shoes*), and
  - regular **Arm** and **Leg** accessories that come as a pair.

  It is saved per outfit and per slot. Accessory transfer and copying accessories to another outfit carry the setting along. MoreAccessories slots are supported.
- **CharaStudio:** a *Single side* section in the character's state panel, with one dropdown per clothing part (applies to all selected characters).
- Settings are saved in the character card, per outfit. Left and right always mean the **character's own** left and right.
- The switches are placed next to the **Material Editor** button of each tab when Material Editor is installed (configurable), instead of at the bottom of the list.
- When a part is set to *Both* the plugin does not touch it at all.

## How it works

Nothing in the game files or mod files is changed. When a part is set to one side, the renderer gets a runtime copy of its mesh and only the triangle lists of that copy are filtered:

1. The mesh is split into connected pieces (islands).
2. **Clothing** (skinned to the body): an island belongs to a side when at least 90% of the skin weight it puts on left/right limb bones (`*_L` / `*_R`: hand, arm, finger, thigh, leg, foot…) is on that side.
3. **Rigid paired accessories** (Arm/Leg types): the attachment point says nothing about which piece is which, so each island is judged by its position: its centre relative to the body's middle, measured along the line between the left and right upper arms (arm accessories) or thighs (leg accessories).
4. Islands that cannot be assigned stay visible.

## Limitations

- **Items whose left and right halves are one connected mesh cannot be split.** They stay fully visible.
- **Pantyhose:** one-piece items (pantyhose joined at the waist, bodystockings and the like) are not hidden cleanly like socks and gloves. Setting the pantyhose to one side turns them into a plain left/right slice: the item is simply cut down the body's middle and one half is removed, waist or torso part included, instead of one leg being hidden.
- **Shoes:** every shoe model has its own heel height, and the height difference between two different high-heel models cannot be matched. For shoes the plugin is therefore mainly useful for **mismatched colours**: hide one side of the main shoes and wear the **same model** in another colour from an accessory slot. Mixing two different shoe models leaves one foot at the wrong height.
- Loading a clothing (coordinate) card does not carry these settings; they belong to the character card's outfit slots.
- CharaStudio has no switch for accessory slots yet. Set accessories in the character maker; the saved setting applies in Studio.
- Paired accessories are sorted into left/right when they load. If you move an accessory from one side to the other afterwards, it is re-sorted after the next reload (change the item or outfit, or reload the character).
- Meshes that are not CPU-readable are skipped and logged.
- Don't use it together with other plugins that replace the same clothing meshes at runtime, for example Nil's KK_SingleShoe. Both would swap the shoe mesh.

## Compatibility and requirements

- Koikatsu / Koikatsu Party and Koikatsu Sunshine, with BepInEx 5, KKAPI / KKSAPI and ExtensibleSaveFormat (all included in HF Patch / BetterRepack).
- Optional: Material Editor (button placement), ClothesToAccessories (clothing accessories), MoreAccessories.
- Tested in game by the maintainer: the clothing switches for gloves, pantyhose, legwear and shoes, the accessory switches (ClothesToAccessories clothing and paired Arm/Leg accessories), CharaStudio, and the placement next to Material Editor. No problems found so far. Wider use will tell more, so feedback is welcome (see [Reporting problems](#reporting-problems)).

## Installation

1. Download the dll for your game from [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases):
   - Koikatsu / Koikatsu Party: `KK_SingleSideClothes.dll`
   - Koikatsu Sunshine: `KKS_SingleSideClothes.dll`
2. Put it in `BepInEx/plugins` in your game folder.
3. Start the game. The switches appear in the clothing tabs of the character maker.

To uninstall, delete the dll. Cards keep their data; without the plugin, clothes are simply shown on both sides.

## Settings (`BepInEx/config/jotorola.singlesideclothes.cfg`)

| Setting | Default | |
|---|---|---|
| `[Maker] Control position` | `AboveMaterialEditor` | `AboveMaterialEditor`, `BelowMaterialEditor`, or `Bottom` (where KKAPI puts plugin controls by default) |

## Reporting problems

Please open an [issue](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) with the game (KK / KKS), where it happened (maker / Studio / main game), the clothing or accessory name, and the `Single Side Clothes` lines from `output_log.txt` / `LogOutput.log`. If left and right come out wrong for an item, a screenshot helps.

## Building

From the repository root (see the [pack README](../README.md#building)):

```sh
sh build/build.sh SingleSideClothes KK  "D:/Games/Koikatsu"
sh build/build.sh SingleSideClothes KKS "D:/Games/Koikatsu Sunshine"
```

The output goes to `bin/`.

## Credits

- **Code:** written by **Claude**, Anthropic's AI assistant.
- **Idea, direction and in-game testing:** [Jotorola-scv](https://github.com/Jotorola-scv), who also maintains this repository.
- The idea of hiding one side of a clothing item comes from **Nil**'s *KK_SingleShoe*, a CharaStudio-only plugin for shoes. This project is an independent implementation, extended to the character maker, other clothing parts and accessories. It contains no code from KK_SingleShoe.
- Works alongside [KKAPI](https://github.com/IllusionMods/IllusionModdingAPI), Material Editor and ClothesToAccessories ([KK_Plugins](https://github.com/IllusionMods/KK_Plugins)) and MoreAccessories. It is not affiliated with those projects.
- License: MIT

---

## 中文說明

**Single Side Clothes（單邊服裝）** 是戀活（KK）與戀活 Sunshine（KKS）的 BepInEx 插件：手套、褲襪、襪子、鞋子和成對的配件，可以設定**只顯示左邊或只顯示右邊**。在**創角**時依服裝分別設定，存在人物卡裡，**CharaStudio** 和本篇遊戲也會套用。

常見用法：只戴一隻手套；只穿一隻襪子；脫掉一隻鞋；或是把主服裝的手套隱藏一邊，再用配件欄的另一款手套補上。鞋子的主要用途是做異色鞋（見[限制](#限制)）。

> **構想來自 Nil。** 本插件以 **Nil** 的 *KK_SingleShoe*（在 CharaStudio 中隱藏左鞋或右鞋的插件）為基礎延伸：改為在**創角**時就能設定，範圍從鞋子擴充到**手套、褲襪、襪子**，並支援**配件**，設定會存進人物卡。本插件是獨立實作，不包含 KK_SingleShoe 的程式碼。感謝 Nil 提供最初的構想。

> [!IMPORTANT]
> **分享人物卡時，對方也需要安裝這個插件。** 單邊設定是以插件資料的形式存在人物卡裡。對方**沒有安裝 Single Side Clothes 時，打開這張卡（或含有這個角色的場景）會看到服裝左右兩邊都顯示**。設定只是不生效，不會出錯。分享有用到單邊設定的人物卡或場景時，請提醒對方安裝本插件。

### 功能

- **創角：** 手套、褲襪、襪子、鞋子分頁都有「單邊」切換（雙邊／僅左／僅右）。KK 的室內鞋和室外鞋分頁都有，兩者共用同一個設定。
- **配件：** 以下配件的配件視窗也會出現同樣的切換：
  - 透過 [ClothesToAccessories](https://github.com/IllusionMods/KK_Plugins) 把服裝當配件用的（類型 Clothes Gloves／Pantyhose／Socks／Shoes）；
  - 一般的**手臂**、**腳**類成對配件。

  設定依服裝、欄位分別保存。配件轉移、複製到其他服裝時會一起帶過去，也支援 MoreAccessories 的額外欄位。
- **CharaStudio：** 角色狀態欄有「單邊服裝」區塊，每個部位一個下拉選單，會套用到所有選取中的角色。
- 設定依服裝存在人物卡裡。左右一律以**角色自己的**左右為準。
- 有安裝 Material Editor 時，開關會放在各分頁的 Material Editor 按鈕旁，不會擠在最底下，位置可以在設定檔調整。
- 設為「雙邊」的部位，插件完全不會介入。

### 原理

不修改任何遊戲檔或模組檔。某個部位設成單邊時，插件會給該 renderer 一份執行期的網格副本，只過濾這份副本的三角面：

1. 把網格分成一塊塊相連的部分（網格島）。
2. **服裝**（蒙皮到身體）：一塊網格島對左右肢體骨（`*_L`／`*_R`：手、手臂、手指、大腿、腿、腳等）的權重中，有 90% 以上落在同一側，就屬於那一側。
3. **固定式的成對配件**（手臂、腳類型）：掛點看不出哪塊是左、哪塊是右，所以改看位置：以左右上臂（手臂配件）或左右大腿（腳配件）的連線為基準，判斷網格島中心偏向哪一側。
4. 無法判定的部分維持顯示。

### 限制

- **左右連成一整塊網格的物件無法分開**，會整件顯示。
- **褲襪：** 連身的物件（腰部相連的褲襪、連身襪等）不會像襪子和手套那樣正常隱藏單邊。褲襪設成單邊時，這類物件會變成單純的左右切片：沿著身體中線直接切掉一半，連腰部或軀幹部分也一起切掉，而不是只隱藏一條腿。
- **鞋子：** 每款鞋子的高度都不同，不同模型的高跟鞋之間的高低差無法互相適配。所以這個插件在鞋子上的主要作用是方便製作**異色鞋**：把主服裝的鞋子隱藏一邊，再用配件欄穿上**同一款**但不同顏色的鞋子。混搭兩款不同的鞋子，會有一隻腳的高度不對。
- 讀取服裝卡不會帶上這些設定，設定屬於人物卡的各套服裝。
- CharaStudio 目前沒有配件欄的切換，請在創角時設定，存檔後 Studio 會套用。
- 成對配件的左右是在載入時判定的。之後若把配件從一側移到另一側，要等下次重新載入（換配件、換服裝或重新讀取人物）才會重新判定。
- 無法由 CPU 讀取的網格會被略過並記錄在 log。
- 不要和其他會在執行期替換同一件服裝網格的插件一起使用，例如 Nil 的 KK_SingleShoe，兩者會同時替換鞋子網格。

### 安裝

1. 從 [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) 下載對應遊戲的 dll：戀活／Koikatsu Party 用 `KK_SingleSideClothes.dll`，戀活 Sunshine 用 `KKS_SingleSideClothes.dll`。
2. 放進遊戲資料夾的 `BepInEx/plugins`。
3. 啟動遊戲，創角的服裝分頁就會出現開關。

需要 BepInEx 5、KKAPI／KKSAPI、ExtensibleSaveFormat（HF Patch／BetterRepack 都已內含）。解除安裝時刪除 dll 即可。

維護者已在遊戲內測試過：手套、褲襪、襪子、鞋子的服裝開關，配件開關（ClothesToAccessories 服裝配件和手臂／腳成對配件），CharaStudio，以及放在 Material Editor 旁的位置。目前沒有發現問題，但還需要更多玩家實際使用才能知道更多，歡迎回報使用心得（見[回報問題](#回報問題)）。

### 設定

`BepInEx/config/jotorola.singlesideclothes.cfg` 的 `[Maker] Control position`：`AboveMaterialEditor`（預設，放在 Material Editor 按鈕上方）、`BelowMaterialEditor`（下方）、`Bottom`（最底下）。

### 回報問題

歡迎在 [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) 回報：遊戲（KK／KKS）、發生的地方（創角／Studio／本篇）、服裝或配件名稱，以及 log 裡 `Single Side Clothes` 開頭的訊息。左右判斷錯誤時，附上截圖會很有幫助。

### 製作

- **程式碼：** 由 Anthropic 的 AI 助理 **Claude** 撰寫。
- **構想、方向與遊戲內測試：** [Jotorola-scv](https://github.com/Jotorola-scv)，也是這個專案的維護者。
- 「隱藏服裝其中一邊」的構想來自 **Nil** 的 *KK_SingleShoe*（僅限 CharaStudio 的鞋子插件）。本專案是獨立實作，並擴充到創角、其他服裝部位和配件，不包含 KK_SingleShoe 的程式碼。
- 授權：MIT

---

## 日本語の概要

**Single Side Clothes** は、コイカツ（KK）／コイカツ・サンシャイン（KKS）用の BepInEx プラグインです。手袋・パンスト・靴下・靴、そして左右ペアのアクセサリーを、**左だけ／右だけ**表示にできます。

**Nil** さんの *KK_SingleShoe*（スタジオで左右の靴を片方だけ表示するプラグイン）のアイデアを元に、キャラメイク・手袋／パンスト／靴下・アクセサリーへ拡張した独立実装です（KK_SingleShoe のコードは含みません）。

> [!IMPORTANT]
> **カードを配布する場合は、相手にもこのプラグインが必要です。** 片側の設定はプラグインのデータとしてキャラカードに保存されます。**Single Side Clothes が入っていない環境でカード（またはそのキャラを含むシーン）を開くと、服は左右とも表示されます。** 設定が無視されるだけで、エラーにはなりません。

- **キャラメイク**の各服装タブ（手袋・パンスト・靴下・靴）とアクセサリー画面に「片側」スイッチを追加します。設定は服装ごとにキャラカードへ保存され、**スタジオ**や本編にも反映されます。
- アクセサリーは ClothesToAccessories の服装タイプ（手袋・パンスト・靴下・靴）と、通常の「腕」「脚」ペアアクセサリーに対応しています。
- ゲームやMODのファイルは変更しません。実行時にメッシュのコピーを作り、片側の三角形だけを非表示にします。左右がつながった一枚のメッシュは分けられません。また、パンストでは一体型のアイテム（腰でつながったパンストや全身タイツなど）は靴下や手袋のようにきれいに片側だけ消えず、体の中心線で単純に左右半分に切られます（腰や胴の部分も含む）。
- 靴はモデルごとにヒールの高さが違い、異なるハイヒール同士の高低差は合わせられません。靴での主な用途は**左右色違い**です（メインの靴を片側だけ消し、アクセサリー枠で**同じモデル**の別カラーを履かせる）。
- インストール：[Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) から `KK_SingleSideClothes.dll`（コイカツ）または `KKS_SingleSideClothes.dll`（サンシャイン）をダウンロードし、`BepInEx/plugins` に入れてください。
- 作者がゲーム内で全機能（服装・アクセサリーのスイッチ、スタジオ、Material Editor 横の配置）をテスト済みで、今のところ問題は見つかっていません。不具合や感想は [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) へどうぞ。
