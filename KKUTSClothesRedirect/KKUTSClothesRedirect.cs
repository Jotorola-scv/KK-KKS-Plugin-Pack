// KKS only. KKSUTS turned the "KKUTS" / "KKUTS_Tess" shaders into body shaders: their discard ignores _MainTex alpha,
// so KK outfits that rely on texture holes render solid. The KKSUTS clothing equivalents of KK's KKUTS are
// "KKUTSclothes" / "KKUTSclothes_Tess" (same blend state, discard reads _MainTex.a). This plugin applies those instead
// whenever a KKUTS shader is set on anything that is not a character body, without touching the saved card data, so
// the same cards stay correct in KK and outfits moved from KK work without edits.
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KK_Plugins.MaterialEditor;
using MaterialEditorAPI;
using UnityEngine;
using XUnity.ResourceRedirector;

namespace KKUTSClothesRedirect
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("KoikatsuSunshine")]
    [BepInProcess("CharaStudio")]
    [BepInDependency("com.deathweasel.bepinex.materialeditor")]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.kkutsclothesredirect";
        public const string PluginName = "KKUTS Clothes Redirect";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        internal static ConfigEntry<bool> Enabled;
        internal static ConfigEntry<bool> ApplyKKDefaults;

        private void Awake()
        {
            Log = Logger;
            Enabled = Config.Bind("General", "Enabled", true,
                "Use KKUTSclothes / KKUTSclothes_Tess when KKUTS / KKUTS_Tess is applied to clothes, accessories, hair or studio items. Character bodies keep KKUTS. Takes effect when characters/outfits are reloaded.");
            ApplyKKDefaults = Config.Bind("General", "Apply KK KKUTS defaults", true,
                "After redirecting, set the properties whose defaults differ between KK KKUTS 2.5 and KKSUTS (HighColor, shade colors, etc.) to the KK values. Values saved in the card still override them.");
            Harmony harmony = new Harmony(GUID);
            harmony.PatchAll(typeof(Hooks));
            var assetHook = AccessTools.Method(typeof(MaterialEditorPlugin), "AssetLoadedHook");
            if (assetHook != null)
                harmony.Patch(assetHook, postfix: new HarmonyMethod(typeof(Hooks), nameof(Hooks.AssetLoadedHookPostfix)));
            else
                Log.LogWarning("MaterialEditorPlugin.AssetLoadedHook not found; shaders embedded in mod bundles will not be redirected.");
        }
    }

    internal class Rule
    {
        internal string From, To;
        internal KeyValuePair<string, float>[] Floats;
        internal KeyValuePair<string, Color>[] Colors;
    }

    internal static class Rules
    {
        // Defaults that differ between KKUTS 2.5 (KK) and KKSUTS 2.2.1 (KKS) manifests for properties both shaders expose.
        private static readonly KeyValuePair<string, float>[] CommonFloats =
        {
            new KeyValuePair<string, float>("1st_ShadeColor_Feather", 0.01f),
            new KeyValuePair<string, float>("2nd_ShadeColor_Step", 0f),
            new KeyValuePair<string, float>("2nd_ShadeColor_Feather", 0.01f),
            new KeyValuePair<string, float>("LightDirection_MaskOn", 0f),
            new KeyValuePair<string, float>("Is_OutlineTex", 1f),
        };

        private static readonly KeyValuePair<string, Color>[] CommonColors =
        {
            new KeyValuePair<string, Color>("1st_ShadeColor", new Color(0.85f, 0.85f, 0.85f, 1f)),
            new KeyValuePair<string, Color>("2nd_ShadeColor", new Color(0.75f, 0.75f, 0.75f, 1f)),
            new KeyValuePair<string, Color>("HighColor", new Color(0.2f, 0.2f, 0.2f, 1f)),
        };

        internal static readonly Dictionary<string, Rule> ByName = new Dictionary<string, Rule>
        {
            { "KKUTS", new Rule { From = "KKUTS", To = "KKUTSclothes", Floats = CommonFloats, Colors = CommonColors } },
            { "KKUTS_Tess", new Rule { From = "KKUTS_Tess", To = "KKUTSclothes_Tess", Colors = CommonColors,
                Floats = Concat(CommonFloats, new KeyValuePair<string, float>("HighColor_Power", 0.1f)) } },
        };

        private static KeyValuePair<string, float>[] Concat(KeyValuePair<string, float>[] a, KeyValuePair<string, float> b)
        {
            var list = new List<KeyValuePair<string, float>>(a) { b };
            return list.ToArray();
        }

        internal static Shader Loaded(string name)
        {
            MaterialEditorPluginBase.ShaderData data;
            return MaterialEditorPluginBase.LoadedShaders.TryGetValue(name, out data) && data != null ? data.Shader : null;
        }
    }

    internal static class Hooks
    {
        private static int _logged;

        private static bool IsCharacterBody(GameObject go)
        {
            // MaterialEditor targets the ChaControl object itself for body/face materials; clothes, accessories and hair are child objects.
            return go.GetComponent<ChaControl>() != null;
        }

        [HarmonyPrefix]
        [HarmonyPatch(typeof(MaterialAPI), nameof(MaterialAPI.SetShader), new[] { typeof(GameObject), typeof(string), typeof(string), typeof(bool) })]
        private static void SetShaderPrefix(GameObject gameObject, string materialName, ref string shaderName, out Rule __state)
        {
            __state = null;
            try
            {
                Rule rule;
                if (!Plugin.Enabled.Value || gameObject == null || shaderName == null || !Rules.ByName.TryGetValue(shaderName, out rule))
                    return;
                if (IsCharacterBody(gameObject) || Rules.Loaded(rule.To) == null)
                    return;
                if (_logged++ < 30)
                    Plugin.Log.LogDebug(string.Format("{0} -> {1} on {2} / {3}", shaderName, rule.To, gameObject.name, materialName));
                shaderName = rule.To;
                __state = rule;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(MaterialAPI), nameof(MaterialAPI.SetShader), new[] { typeof(GameObject), typeof(string), typeof(string), typeof(bool) })]
        private static void SetShaderPostfix(GameObject gameObject, string materialName, bool __result, Rule __state)
        {
            if (__state == null || !__result || !Plugin.ApplyKKDefaults.Value)
                return;
            try
            {
                foreach (var f in __state.Floats)
                    MaterialAPI.SetFloat(gameObject, materialName, f.Key, f.Value);
                foreach (var c in __state.Colors)
                    MaterialAPI.SetColor(gameObject, materialName, c.Key, c.Value);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        // Shader Optimization swaps shaders embedded in mod bundles for MaterialEditor's copy by name; a bundle material
        // authored with KK's KKUTS would end up on the KKS body variant. Runs after MaterialEditor's own hook.
        internal static void AssetLoadedHookPostfix(AssetLoadedContext context)
        {
            try
            {
                if (!Plugin.Enabled.Value || !MaterialEditorPluginBase.ShaderOptimization.Value)
                    return;
                GameObject go = context.Asset as GameObject;
                if (go == null || go.name.StartsWith("p_cf_body") || go.name.StartsWith("p_cm_body"))
                    return;
                Dictionary<Shader, Shader> swap = null;
                foreach (Rule rule in Rules.ByName.Values)
                {
                    Shader from = Rules.Loaded(rule.From), to = Rules.Loaded(rule.To);
                    if (from == null || to == null)
                        continue;
                    if (swap == null)
                        swap = new Dictionary<Shader, Shader>();
                    swap[from] = to;
                }
                if (swap == null)
                    return;
                foreach (Renderer renderer in go.GetComponentsInChildren<Renderer>(true))
                {
                    foreach (Material m in renderer.sharedMaterials)
                    {
                        Shader to;
                        if (m == null || !swap.TryGetValue(m.shader, out to))
                            continue;
                        int queue = m.renderQueue;
                        m.shader = to;
                        m.renderQueue = queue;
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }
    }
}
