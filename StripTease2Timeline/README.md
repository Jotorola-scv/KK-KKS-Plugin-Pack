# StripTease2 Timeline

Part of the [KK/KKS Plugin Pack](../README.md).

> [!WARNING]
> **Experimental and unofficial.** **ziglo**, the author of StripTease2, has announced that a **future StripTease2 version will support Timeline officially**. Once that version is out, use the official support and remove this plugin. Until then, this plugin is only meant for experimenting.
>
> This plugin is **not made by ziglo** and is not affiliated with StripTease2. Please **don't report problems with it to ziglo**; use this repository's [issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) instead.

A companion plugin for **CharaStudio** in **Koikatsu (KK)** and **Koikatsu Sunshine (KKS)** that lets you animate **StripTease2** garment deformations with **Timeline**, plus a few fixes for working with StripTease2 in Studio.

[中文說明在下方](#中文說明) · [日本語の概要](#日本語の概要)

## Features

All Timeline tracks are under **Timeline > StripTease2**. Press **Shift+F10** to open a window with sliders and toggles that set the current values, so you can add keyframes.

- **Strip progress: _slot_** (0–100): blends a garment between its original shape (0) and the StripTease2 result (100). A shape key is added to StripTease2's working mesh; the saved deformation itself is not changed.
- **Body mask: Top / Bottom** (on/off): switches the body mask of the top and bottom at exact keyframes. StripTease2's static mask either shows holes while cloth falls or skin poking through while it is worn; these tracks let you change it during the animation.
- **Drop: _slot_** (seconds after release): detaches a garment from the skeleton and lets it fall to the ground. It keeps some of the body's motion at release, then piles up and spreads a little on landing. It is a simple deterministic fall, not cloth physics, so scrubbing Timeline shows the same result every time. 0 = worn normally.
- **Keep garment sessions** (setting): StripTease2 normally discards a garment's edit session (undo, pins, baked collision) whenever another garment is selected. With this on, the session is kept and resumed when you select that garment again.
- **Fix garment culling** (setting): deformed garments, especially thin ones, could vanish at some camera angles. With this on, they get a character-sized bounding box.

## Limitations

- Bone weights that StripTease2 changes stay applied at every progress value; only vertex positions are blended.
- Drop looks natural only with carefully timed keyframes. It is not a physics simulation.
- The plugin reaches StripTease2's internals through reflection and Harmony. A StripTease2 update can turn parts of it off. The log then says `Unsupported StripTease2 version, missing: …` or names the feature that is unavailable.
- Checked against StripTease2 **1.3.0** and **1.4.1**.

## Requirements

- CharaStudio with BepInEx 5 and KKAPI / KKSAPI
- **StripTease2** by ziglo
- **Timeline** (without it, only the Shift+F10 window works)

## Installation

1. Download the dll for your game from [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases):
   - Koikatsu / Koikatsu Party: `KK_StripTease2Timeline.dll`
   - Koikatsu Sunshine: `KKS_StripTease2Timeline.dll`
2. Put it in `BepInEx/plugins` in your game folder.

To uninstall, delete the dll. Scenes keep their StripTease2 deformations; only the Timeline tracks of this plugin stop working.

## Settings (`BepInEx/config/jotorola.striptease2timeline.cfg`)

| Setting | Default | |
|---|---|---|
| `[General] Toggle progress window` | `LeftShift + F10` | Opens the window |
| `[General] Window scale` | `1.25` | Size of the window |
| `[General] Keep garment sessions when switching` | `true` | See *Features* |
| `[General] Fix garment culling` | `true` | See *Features* |
| `[Drop] Gravity` | `9.8` | Fall acceleration |
| `[Drop] Inherit body motion` | `1` | How much of the body's motion a dropped garment keeps (0 = falls straight down) |
| `[Drop] Air drag` | `2` | How quickly that sideways motion dies down |
| `[Drop] Landing spread` / `Max landing spread` | `0.35` / `0.25` | How far landed cloth spreads out |

## Credits

- **Code:** written by **Claude**, Anthropic's AI assistant. **Idea, direction and in-game testing:** [Jotorola-scv](https://github.com/Jotorola-scv).
- **StripTease2** is made by **ziglo**. This plugin contains no StripTease2 code.
- License: MIT

---

## 中文說明

> [!WARNING]
> **實驗性、非官方插件。** StripTease2 的作者 **ziglo** 已宣布**未來的 StripTease2 版本會正式支援 Timeline**。正式版推出後，請改用官方支援並移除本插件。在那之前，本插件僅供實驗性使用。
>
> 本插件**不是 ziglo 製作的**，與 StripTease2 沒有關聯。使用上有問題**請不要回報給 ziglo**，請到本 repo 的 [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) 回報。

戀活（KK）與戀活 Sunshine（KKS）**CharaStudio** 用的輔助插件，讓 **StripTease2** 的服裝變形可以用 **Timeline** 做動畫，另外附帶幾個在 Studio 使用 StripTease2 時的修正。

### 功能

Timeline 軌道都在 **Timeline > StripTease2** 底下。按 **Shift+F10** 會開啟一個視窗，可以用滑桿和開關設定目前的數值，方便打關鍵影格。

- **Strip progress：部位**（0–100）：讓服裝在原本形狀（0）和 StripTease2 變形結果（100）之間漸變。做法是在 StripTease2 的工作網格上加一個形狀鍵，不會改動存下來的變形。
- **Body mask：Top／Bottom**（開／關）：在指定的關鍵影格切換上身、下身的身體遮罩。StripTease2 的遮罩是固定的：開著時衣服掉落會露出破洞，關著時穿著的衣服會被皮膚穿模，用這個軌道可以在動畫中途切換。
- **Drop：部位**（放開後的秒數）：讓服裝脫離骨架掉到地上，會帶一點放開當下的身體動作，落地後堆疊並稍微攤開。這是簡單、固定的掉落計算，不是布料物理，所以在 Timeline 來回拖動時結果都一樣。0 = 正常穿著。
- **保留編輯狀態**（設定）：StripTease2 原本每次選別的服裝，就會丟掉目前服裝的編輯狀態（復原紀錄、固定點、烘焙的碰撞）。開啟後會保留，切回那件服裝時接著編輯。
- **修正服裝消失**（設定）：變形後的服裝（尤其是很細的）在某些鏡頭角度會消失。開啟後會給它們一個角色大小的包圍盒。

### 限制

- StripTease2 改過的骨骼權重在任何進度下都維持套用，只有頂點位置會漸變。
- Drop 需要仔細調整關鍵影格的時間才會自然，並不是物理模擬。
- 本插件透過反射和 Harmony 存取 StripTease2 的內部，StripTease2 更新後部分功能可能失效，log 會出現 `Unsupported StripTease2 version, missing: …` 或寫出無法使用的功能。
- 已對 StripTease2 **1.3.0** 和 **1.4.1** 確認過。

### 需求與安裝

- CharaStudio、BepInEx 5、KKAPI／KKSAPI、ziglo 的 **StripTease2**、**Timeline**（沒有 Timeline 時只有 Shift+F10 視窗可用）。
- 從 [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) 下載 `KK_StripTease2Timeline.dll`（戀活）或 `KKS_StripTease2Timeline.dll`（戀活 Sunshine），放進 `BepInEx/plugins`。
- 刪除 dll 即可解除安裝。場景裡的 StripTease2 變形會保留，只有本插件的 Timeline 軌道不再作用。
- 設定檔：`BepInEx/config/jotorola.striptease2timeline.cfg`。

### 製作

- **程式碼：** 由 Anthropic 的 AI 助理 **Claude** 撰寫；**構想、方向與遊戲內測試：** [Jotorola-scv](https://github.com/Jotorola-scv)。
- **StripTease2** 由 **ziglo** 製作，本插件不包含 StripTease2 的程式碼。
- 授權：MIT

---

## 日本語の概要

> [!WARNING]
> **実験的・非公式のプラグインです。** StripTease2 の作者 **ziglo** さんが、**今後の StripTease2 で Timeline に正式対応する**と発表しています。正式版が出たらそちらを使い、このプラグインは削除してください。それまでの実験用です。ziglo さんとは無関係なので、このプラグインの不具合は ziglo さんではなく、このリポジトリの [Issues](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/issues) へお願いします。

- スタジオ用。**StripTease2** の服の変形を **Timeline** でアニメーションできます（Strip progress 0–100、Body mask の切り替え、Drop で服を落とす）。Shift+F10 のウィンドウで値を設定してキーフレームを打てます。
- 服を切り替えても編集セッションを保持する設定と、変形した服が特定のカメラ角度で消える問題の修正も含みます。
- 必要：StripTease2（ziglo さん）、Timeline、KKAPI／KKSAPI。StripTease2 1.3.0 と 1.4.1 で確認済み。
- [Releases](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/releases) から dll をダウンロードし、`BepInEx/plugins` に入れてください。
