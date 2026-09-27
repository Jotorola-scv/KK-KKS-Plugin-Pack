// KK only. Mirror of the KKS "KKUTS Clothes Redirect" plugin. Cards and coordinates saved in KKS can ask MaterialEditor
// for "KKUTSclothes" / "KKUTSclothes_Tess", which only exist in KKSUTS; KK's KKUTS 2.5 has no such shaders, so
// MaterialEditor logs "Could not load shader:KKUTSclothes" and the part keeps its vanilla shader. KKSUTS KKUTSclothes is
// the KKS equivalent of KK KKUTS (same blend state, discard reads _MainTex.a), so apply KKUTS / KKUTS_Tess instead.
// Only the shader actually applied changes; the card data still says KKUTSclothes, so it stays correct back in KKS.
// 1.1.0: also "xukmi/MainClothesAlphaPlusTess" from the KKS-only pack "[KKS] VanillaPlus_Tess_Extra" (xukmi's V+ port of
// KKS Koikano/main_clothes_alpha) -> KK VanillaPlus "xukmi/MainAlphaPlusTess" (KK's clothes alpha shader; 66 of its 70
// properties are shared, the rest are the clothing color slots, which the game sets itself).
using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Configuration;
using HarmonyLib;
using KK_Plugins.MaterialEditor;
using MaterialEditorAPI;
using UnityEngine;

[assembly: AssemblyTitle("KK_KKUTSClothesCompat")]
[assembly: AssemblyVersion("1.1.0.0")]

namespace KKUTSClothesCompat
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("Koikatu")]
    [BepInProcess("Koikatsu Party")]
    [BepInProcess("CharaStudio")]
    [BepInDependency("com.deathweasel.bepinex.materialeditor")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.kkutsclothescompat";
        public const string PluginName = "KKUTSclothes Compat";
        public const string Version = "1.1.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Apply KK equivalents when a card saved in KKS asks for KKS-only shaders (KKUTSclothes(_Tess) -> KKUTS(_Tess), xukmi/MainClothesAlphaPlusTess -> xukmi/MainAlphaPlusTess). Card data is not changed. Takes effect when characters/outfits are reloaded.");
            Harmony.CreateAndPatchAll(typeof(Hooks), GUID);
        }
    }

    internal static class Hooks
    {
        private static readonly Dictionary<string, string> Map = new Dictionary<string, string>
        {
            { "KKUTSclothes", "KKUTS" },
            { "KKUTSclothes_Tess", "KKUTS_Tess" },
            { "xukmi/MainClothesAlphaPlusTess", "xukmi/MainAlphaPlusTess" },
        };

        private static int _logged;

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MaterialAPI), nameof(MaterialAPI.SetShader), new[] { typeof(GameObject), typeof(string), typeof(string), typeof(bool) })]
        private static void SetShaderPrefix(GameObject gameObject, string materialName, ref string shaderName)
        {
            try
            {
                string to;
                if (!Plugin.Enabled.Value || shaderName == null || !Map.TryGetValue(shaderName, out to))
                    return;
                MaterialEditorPluginBase.ShaderData data;
                if (!MaterialEditorPluginBase.LoadedShaders.TryGetValue(to, out data) || data == null || data.Shader == null)
                    return;
                if (_logged++ < 30)
                    Plugin.Log.LogDebug(string.Format("{0} -> {1} on {2} / {3}", shaderName, to, gameObject != null ? gameObject.name : "null", materialName));
                shaderName = to;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }
    }
}
