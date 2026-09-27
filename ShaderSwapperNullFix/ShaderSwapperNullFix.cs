// Shader Swapper 1.8.1 hooks MaterialAPI.SetFloat to clamp TessMin; with "Verbose logging" on it logs gameObject.name
// without a null check. MaterialEditor's LoadData calls SetFloat with a null GameObject whenever an object is not
// present yet (e.g. right after an accessory transfer reloads the character, for slots MoreAccessories has not rebuilt),
// so the NullReferenceException aborts LoadData before textures are applied and every accessory/clothing item on the
// outfit loses its MaterialEditor textures. Transferring an accessory that uses a *Tess shader triggers it.
// This skips the hook for null GameObjects; MaterialAPI.SetFloat itself already returns false for them.
using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ShaderSwapperNullFix
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInDependency("com.deathweasel.bepinex.shaderswapper", BepInDependency.DependencyFlags.SoftDependency)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.shaderswappernullfix";
        public const string PluginName = "Shader Swapper Null Fix";
        public const string Version = "1.0.0";

        internal static ManualLogSource Log;
        private static int _skipped;

        private void Awake()
        {
            Log = Logger;
            Type swapper = AccessTools.TypeByName("KK_Plugins.ShaderSwapper");
            MethodInfo hook = swapper == null ? null : AccessTools.Method(swapper, "SetFloatHook");
            if (hook == null)
            {
                Log.LogWarning("KK_Plugins.ShaderSwapper.SetFloatHook not found - nothing to fix");
                return;
            }
            new Harmony(GUID).Patch(hook, prefix: new HarmonyMethod(typeof(Plugin), nameof(SetFloatHookPrefix)));
        }

        private static bool SetFloatHookPrefix(GameObject gameObject, string materialName, string propertyName)
        {
            if (gameObject != null)
                return true;
            if (_skipped++ < 10)
                Log.LogDebug(string.Format("Skipped Shader Swapper SetFloat hook for missing object ({0} / {1})", materialName, propertyName));
            return false;
        }
    }
}
