**Optional fix.** Only needed if you use Shader Swapper (KK_Plugins) and an outfit loses all its Material Editor textures after an accessory transfer, usually one with a `*Tess` shader.

Shader Swapper's `SetFloat` hook throws a `NullReferenceException` for objects that don't exist yet (with *Verbose logging* on, the default), which stops Material Editor in the middle of loading. This plugin skips the hook for missing objects; nothing else changes.

**Download:** `KK_ShaderSwapperNullFix.dll` (Koikatsu / Koikatsu Party) or `KKS_ShaderSwapperNullFix.dll` (Koikatsu Sunshine), into `BepInEx/plugins`. Details: [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/ShaderSwapperNullFix).

---

**選用修正。** 只有使用 Shader Swapper、且配件轉移後整套服裝的 Material Editor 貼圖消失時才需要。詳見 [README](https://github.com/Jotorola-scv/KK-KKS-Plugin-Pack/tree/main/ShaderSwapperNullFix#中文說明)。
