# Shader Swapper Null Fix

Part of the [KK/KKS Plugin Pack](../README.md). **Optional:** only needed if you use Shader Swapper and run into the problem below.

A small BepInEx fix for **Koikatsu (KK)** and **Koikatsu Sunshine (KKS)**, for the maker, CharaStudio and the main game.

## The problem

Shader Swapper (from [KK_Plugins](https://github.com/IllusionMods/KK_Plugins)) hooks Material Editor's `SetFloat` to clamp `TessMin` on xukmi's Tess shaders. When it clamps a value and *Verbose logging* is on, it writes the target object's name to the log without checking that the object exists. Both *Verbose logging* and *TessMin Clamping* are on by default. Material Editor sometimes restores values for objects that are not there yet. That happens, for example, right after an accessory transfer reloads the character, for slots that MoreAccessories has not rebuilt yet.

The hook then throws a `NullReferenceException`, which stops Material Editor in the middle of loading. **Every accessory and clothing item on the outfit loses its Material Editor textures and settings.** Transferring an accessory that uses a `*Tess` shader is the usual trigger. In the log it looks like a `NullReferenceException` from `ShaderSwapper.SetFloatHook` inside Material Editor's `LoadData`.

## The fix

The plugin skips Shader Swapper's hook when the object is missing. Material Editor's own `SetFloat` already ignores missing objects, so nothing else changes. Everything else Shader Swapper does is unchanged.

If Shader Swapper is not installed, the plugin does nothing and logs `KK_Plugins.ShaderSwapper.SetFloatHook not found - nothing to fix`.

Without this plugin, turning off Shader Swapper's `[General] Verbose logging` also avoids the problem.

## Installation

Download `KK_ShaderSwapperNullFix.dll` (Koikatsu / Koikatsu Party) or `KKS_ShaderSwapperNullFix.dll` (Koikatsu Sunshine) from [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) and put it in `BepInEx/plugins`. Requires BepInEx 5. Delete the dll to uninstall. It can be removed once Shader Swapper fixes this itself.

## Credits

- **Code:** written by **Claude**, Anthropic's AI assistant. **Diagnosis, direction and in-game testing:** [Jotorola-scv](https://github.com/Jotorola-scv).
- Works around a bug in Shader Swapper ([KK_Plugins](https://github.com/IllusionMods/KK_Plugins)). It is not affiliated with that project.
- License: MIT

---

## 中文說明

**選用**：只有使用 Shader Swapper、並遇到下面這個問題時才需要。KK 和 KKS 的創角、CharaStudio、本篇都適用。

**問題：** Shader Swapper（KK_Plugins）會攔截 Material Editor 的 `SetFloat` 來限制 xukmi Tess 著色器的 `TessMin`。限制數值且開著 Verbose logging 時，它會把目標物件的名稱寫進 log，卻沒有檢查物件是否存在（Verbose logging 和 TessMin Clamping 預設都開啟）。Material Editor 有時會替還沒建立的物件還原數值，例如配件轉移後重新載入角色、MoreAccessories 還沒重建的欄位。這時 Shader Swapper 會丟出 `NullReferenceException`，讓 Material Editor 中途停止讀取，**整套服裝的配件和衣服都會失去 Material Editor 的貼圖與設定**。最常見的情況是轉移使用 `*Tess` 著色器的配件。

**修正：** 物件不存在時跳過 Shader Swapper 的攔截。Material Editor 自己的 `SetFloat` 本來就會忽略不存在的物件，所以其他行為都不變。沒有安裝 Shader Swapper 時，這個插件什麼也不做。不裝這個插件的話，把 Shader Swapper 的 `[General] Verbose logging` 關掉也能避開這個問題。

**安裝：** 從 [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) 下載 `KK_ShaderSwapperNullFix.dll`（戀活）或 `KKS_ShaderSwapperNullFix.dll`（戀活 Sunshine），放進 `BepInEx/plugins`。刪除 dll 即可解除安裝；等 Shader Swapper 本身修好後就可以移除。

---

## 日本語の概要

**任意**：Shader Swapper を使っていて、アクセサリーの転送（特に `*Tess` シェーダーのもの）の後に、そのコーデ全体の Material Editor のテクスチャや設定が消える場合だけ必要です。原因は、存在しないオブジェクトに対して Shader Swapper のフックが（Verbose logging がオンのとき）`NullReferenceException` を出し、Material Editor の読み込みが途中で止まることです。Shader Swapper の Verbose logging をオフにしても回避できます。このプラグインはオブジェクトがない場合だけフックをスキップします。[Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) から dll をダウンロードし、`BepInEx/plugins` に入れてください。
