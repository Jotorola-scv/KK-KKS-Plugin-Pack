**Optional fix.** Only useful if you move outfits or characters that use the **KKUTS** toon shader between Koikatsu and Koikatsu Sunshine. Card data is never changed.

- `KKS_KKUTSClothesRedirect.dll` (Koikatsu Sunshine) 1.0.0: KK outfits made with KKUTS render solid in KKS, because KKSUTS's `KKUTS` is a body shader that ignores texture transparency. On anything but the body, this applies KKSUTS's clothing shader `KKUTSclothes(_Tess)` instead and sets KK's default values.
- `KK_KKUTSClothesCompat.dll` (Koikatsu / Koikatsu Party) 1.1.0: cards saved in KKS that ask for `KKUTSclothes(_Tess)` or `xukmi/MainClothesAlphaPlusTess` get KK's `KKUTS(_Tess)` / `xukmi/MainAlphaPlusTess` instead of falling back to the vanilla shader.

Requires Material Editor. Put the dll for your game in `BepInEx/plugins`. Details: [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/KKUTSClothesRedirect).

---

**選用修正。** 只有在 KK 和 KKS 之間搬移使用 KKUTS 的服裝時才用得到，不會修改卡片。KKS 版把套在身體以外的 `KKUTS` 換成服裝用的 `KKUTSclothes`；KK 版把 KKS 存的 `KKUTSclothes` 換回 KK 的 `KKUTS`。詳見 [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/KKUTSClothesRedirect#中文說明)。
