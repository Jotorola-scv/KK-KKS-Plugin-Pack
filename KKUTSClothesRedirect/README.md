# KKUTS Clothes Redirect / KKUTSclothes Compat

Part of the [KK/KKS Plugin Pack](../README.md). **Optional:** only useful if you move outfits or characters that use the **KKUTS** toon shader between Koikatsu and Koikatsu Sunshine.

Two small BepInEx plugins, one per game, that make KKUTS outfits look the same in both games without editing the cards. Neither plugin changes card data. They only change which shader is applied when the card loads, so the same card stays correct in both games.

| dll | Game | Direction |
|---|---|---|
| `KKS_KKUTSClothesRedirect.dll` | Koikatsu Sunshine (game, maker and CharaStudio) | KK outfits in KKS |
| `KK_KKUTSClothesCompat.dll` | Koikatsu / Koikatsu Party (game, maker and CharaStudio) | KKS outfits in KK |

Both require BepInEx 5, KKAPI / KKSAPI and Material Editor.

## KKUTS Clothes Redirect (KKS)

**The problem.** In KKS, the KKSUTS versions of `KKUTS` / `KKUTS_Tess` are meant for **bodies**. They ignore the transparency of the main texture. KK outfits that use KKUTS and rely on transparent parts of their textures (holes, lace, cut-outs and so on) therefore render solid in KKS. KKSUTS has separate clothing shaders for this, `KKUTSclothes` / `KKUTSclothes_Tess`, which do respect the texture's transparency.

**What it does:**
- When Material Editor applies `KKUTS` or `KKUTS_Tess` to anything other than a character's body (clothes, accessories, hair, studio items), the plugin applies `KKUTSclothes` / `KKUTSclothes_Tess` instead. Bodies keep KKUTS.
- It then sets the properties whose defaults differ between KK's KKUTS and KKSUTS (highlight colour, shade colours and feathers, and a few more) to the KK values. Values saved in the card still take priority.
- With Material Editor's *Shader Optimization* on, it also swaps KKUTS materials that come built into mod files (except the body models).
- It needs the KKSUTS shader pack that provides `KKUTSclothes`; without it, nothing is redirected.

**Settings** (`BepInEx/config/jotorola.kkutsclothesredirect.cfg`):

| Setting | Default | |
|---|---|---|
| `[General] Enabled` | `true` | Turn the redirect on or off. Takes effect when characters or outfits are reloaded. |
| `[General] Apply KK KKUTS defaults` | `true` | Set the KK default values after redirecting. |

## KKUTSclothes Compat (KK)

**The problem.** Cards and coordinates saved in KKS can ask for `KKUTSclothes` / `KKUTSclothes_Tess`, which only exist in KKS. KK's KKUTS does not have them. Material Editor then logs `Could not load shader: KKUTSclothes` and the item keeps its vanilla shader. The same happens with `xukmi/MainClothesAlphaPlusTess` from the KKS-only *VanillaPlus Tess Extra* pack.

**What it does:** it maps these shaders to their KK equivalents while loading:

| Saved in KKS | Applied in KK |
|---|---|
| `KKUTSclothes` | `KKUTS` |
| `KKUTSclothes_Tess` | `KKUTS_Tess` |
| `xukmi/MainClothesAlphaPlusTess` | `xukmi/MainAlphaPlusTess` |

A shader is only mapped when its KK equivalent is installed.

**Settings** (`BepInEx/config/jotorola.kkutsclothescompat.cfg`): `[General] Enabled` (default `true`).

## Installation

Download the dll for your game from [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) and put it in `BepInEx/plugins`. Delete the dll to uninstall. Cards are never changed, so nothing needs to be undone.

## Credits

- **Code:** written by **Claude**, Anthropic's AI assistant. **Idea, direction and in-game testing:** [Jotorola-scv](https://github.com/Jotorola-scv).
- Works with KKUTS / KKSUTS, xukmi's VanillaPlus shaders and Material Editor ([KK_Plugins](https://github.com/IllusionMods/KK_Plugins)). It is not affiliated with those projects.
- License: MIT

---

## 中文說明

**選用**：只有在 KK 和 KKS 之間搬移使用 **KKUTS** 卡通著色器的服裝或人物時才用得到。兩個小插件各對應一個遊戲，都不修改卡片資料，只改變讀取時實際套用的著色器，所以同一張卡在兩邊都正確。需要 BepInEx 5、KKAPI／KKSAPI、Material Editor。

**KKUTS Clothes Redirect（KKS 用，`KKS_KKUTSClothesRedirect.dll`）**
- **問題：** KKS 的 KKSUTS 把 `KKUTS`／`KKUTS_Tess` 做成**身體**用的著色器，不理會主貼圖的透明度。KK 裡用 KKUTS、靠貼圖透明部分（鏤空、蕾絲、開口等）的服裝，到 KKS 會變成實心。KKSUTS 另外有服裝用的 `KKUTSclothes`／`KKUTSclothes_Tess`，會正確處理透明度。
- **做法：** Material Editor 把 `KKUTS`／`KKUTS_Tess` 套到角色身體以外的東西（服裝、配件、頭髮、Studio 物件）時，改用 `KKUTSclothes`／`KKUTSclothes_Tess`，身體維持 KKUTS。接著把 KK 版和 KKS 版預設值不同的屬性（高光色、陰影色與羽化等）設成 KK 的值，卡片裡存的數值仍然優先。開啟 Material Editor 的 Shader Optimization 時，也會替換模組檔內建的 KKUTS 材質（身體模型除外）。需要有提供 `KKUTSclothes` 的 KKSUTS 著色器包。
- **設定：** `jotorola.kkutsclothesredirect.cfg` 的 `Enabled`（開關）和 `Apply KK KKUTS defaults`（套用 KK 預設值），預設都開啟。

**KKUTSclothes Compat（KK 用，`KK_KKUTSClothesCompat.dll`）**
- **問題：** 在 KKS 存的卡片或服裝卡可能要求 `KKUTSclothes`／`KKUTSclothes_Tess`，但 KK 的 KKUTS 沒有這些著色器，Material Editor 會記錄 `Could not load shader: KKUTSclothes`，物件維持原版著色器。KKS 專用的 VanillaPlus Tess Extra 裡的 `xukmi/MainClothesAlphaPlusTess` 也一樣。
- **做法：** 讀取時對應到 KK 的同等著色器：`KKUTSclothes` → `KKUTS`、`KKUTSclothes_Tess` → `KKUTS_Tess`、`xukmi/MainClothesAlphaPlusTess` → `xukmi/MainAlphaPlusTess`。只有 KK 那邊有安裝對應的著色器時才會替換。
- **設定：** `jotorola.kkutsclothescompat.cfg` 的 `Enabled`，預設開啟。

**安裝：** 從 [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) 下載對應遊戲的 dll，放進 `BepInEx/plugins`。刪除 dll 即可解除安裝，卡片沒有被修改過，不需要還原。

---

## 日本語の概要

**任意**：**KKUTS** トゥーンシェーダーを使った服やキャラを、コイカツとサンシャインの間で移す場合だけ役に立ちます。カードのデータは変更せず、読み込み時に使うシェーダーだけを切り替えます。

- `KKS_KKUTSClothesRedirect.dll`（サンシャイン）：KKSUTS の `KKUTS` は体用で、テクスチャの透明部分を無視するため、KK で作った KKUTS の服が穴のない状態で表示されます。体以外に `KKUTS` が使われたとき、服用の `KKUTSclothes` に切り替え、KK と既定値が違うプロパティを KK の値にします。
- `KK_KKUTSClothesCompat.dll`（コイカツ）：KKS で保存されたカードの `KKUTSclothes(_Tess)` を `KKUTS(_Tess)` に、`xukmi/MainClothesAlphaPlusTess` を `xukmi/MainAlphaPlusTess` に置き換えます。
- [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) から dll をダウンロードし、`BepInEx/plugins` に入れてください。Material Editor が必要です。
