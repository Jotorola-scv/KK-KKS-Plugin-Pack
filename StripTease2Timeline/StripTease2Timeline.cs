// StripTease2 Timeline: animates StripTease2 garment deformations in Timeline.
// StripTease2 bakes its result into a working copy of the garment mesh (original vertices + bind-space deltas) and keeps
// the original mesh around. This plugin adds one extra shape key to that working mesh which moves every vertex back to
// its original position, so "Strip progress" 100 = the StripTease2 result and 0 = the untouched garment.
// StripTease2's re-skinned bone weights stay applied at every progress value (weights can't be blended).
// EXPERIMENTAL and unofficial: StripTease2 (ziglo) has announced official Timeline support in a future version; use that
// once it is released. This plugin only reaches StripTease2's internals through reflection and Harmony and contains none
// of its code, so a StripTease2 update can disable parts of it (the log then names the missing member).
using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using KKAPI;
using KKAPI.Studio;
using KKAPI.Utilities;
using Studio;
using UnityEngine;

namespace StripTease2Timeline
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("CharaStudio")]
    [BepInDependency(KoikatuAPI.GUID)]
    [BepInDependency(StripTeaseGuid)]
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.striptease2timeline";
        public const string PluginName = "StripTease2 Timeline";
        public const string Version = "0.5.0";
        internal const string StripTeaseGuid = "com.ziglo.striptease2";

        internal static ManualLogSource Log;
        internal static ConfigEntry<KeyboardShortcut> WindowKey;
        internal static ConfigEntry<float> UIScale;
        internal static ConfigEntry<bool> KeepSessions;
        internal static ConfigEntry<bool> FixCulling;
        internal static ConfigEntry<float> DropGravity;
        internal static ConfigEntry<float> DropInheritMotion;
        internal static ConfigEntry<float> DropAirDrag;
        internal static ConfigEntry<float> DropSpread;
        internal static ConfigEntry<float> DropMaxSpread;

        private void Awake()
        {
            Log = Logger;
            WindowKey = Config.Bind("General", "Toggle progress window", new KeyboardShortcut(KeyCode.F10, KeyCode.LeftShift),
                "Shows the Strip progress sliders for the selected character.");
            KeepSessions = Config.Bind("General", "Keep garment sessions when switching", true,
                "StripTease2 normally discards a garment's edit session (undo, pins, baked collision) when another mesh is selected. On: the session is kept and resumed when you select that mesh again.");
            FixCulling = Config.Bind("General", "Fix garment culling", true,
                "StripTease2-deformed garments can vanish at some camera angles because Unity still uses the original per-bone bounds. On: they get a fixed character-sized bounding box.");
            DropGravity = Config.Bind("Drop", "Gravity", 9.8f, new ConfigDescription("Fall acceleration of dropped garments (units per second squared).", new AcceptableValueRange<float>(0.5f, 40f)));
            DropInheritMotion = Config.Bind("Drop", "Inherit body motion", 1f, new ConfigDescription("How much of the body part's velocity at release the garment keeps (0 = falls straight down).", new AcceptableValueRange<float>(0f, 2f)));
            DropAirDrag = Config.Bind("Drop", "Air drag", 2f, new ConfigDescription("How quickly the inherited sideways motion dies down.", new AcceptableValueRange<float>(0.1f, 10f)));
            DropSpread = Config.Bind("Drop", "Landing spread", 0.35f, new ConfigDescription("How far landed cloth spreads out, relative to the fall it absorbs (0 = flattens in place).", new AcceptableValueRange<float>(0f, 1.5f)));
            DropMaxSpread = Config.Bind("Drop", "Max landing spread", 0.25f, new ConfigDescription("Upper limit of the landing spread per vertex (units).", new AcceptableValueRange<float>(0f, 1f)));
            UIScale = Config.Bind("General", "Window scale", 1.25f, new ConfigDescription("Size of the progress window.", new AcceptableValueRange<float>(0.75f, 2.5f)));
        }

        private void Start()
        {
            try
            {
                string missing = StripAccess.Init();
                if (missing != null)
                {
                    Log.LogError("Unsupported StripTease2 version, missing: " + missing);
                    return;
                }
                var harmony = new Harmony(GUID);
                StripAccess.PatchDirtyTracking(harmony);
                try
                {
                    SessionKeeper.Patch(harmony, StripAccess.BindingType.Assembly);
                }
                catch (Exception e)
                {
                    Log.LogWarning("Session keeping is unavailable: " + e.Message);
                }
                try
                {
                    CullingFix.Patch(harmony);
                }
                catch (Exception e)
                {
                    Log.LogWarning("Culling fix is unavailable: " + e.Message);
                }
                if (!TimelineCompatibility.IsTimelineAvailable())
                {
                    Log.LogWarning("Timeline is not available; only the progress window works.");
                }
                else
                {
                    ProgressTimeline.Register();
                }
                KKAPI.Studio.SaveLoad.StudioSaveLoadApi.SceneLoad += (sender, args) => Drop.SceneLoadedAt = Time.realtimeSinceStartup;
                gameObject.AddComponent<ProgressWindow>();
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        }
    }

    /// <summary>Reflection over StripTease2's internal registry and garment binding.</summary>
    internal static class StripAccess
    {
        private static MethodInfo _tryGetByRenderer;
        private static FieldInfo _entryBinding, _entrySession;
        private static PropertyInfo _originalMesh, _workingMesh;
        internal static Type BindingType;

        internal static string Init()
        {
            PluginInfo info;
            if (!Chainloader.PluginInfos.TryGetValue(Plugin.StripTeaseGuid, out info) || info.Instance == null) return "plugin";
            Assembly asm = info.Instance.GetType().Assembly;
            Type registry = asm.GetType("StripTease2.Serialization.DeformationRegistry");
            Type entry = asm.GetType("StripTease2.Serialization.GarmentEntry");
            BindingType = asm.GetType("StripTease2.Cloth.GarmentBinding");
            if (registry == null) return "DeformationRegistry";
            if (entry == null) return "GarmentEntry";
            if (BindingType == null) return "GarmentBinding";
            _tryGetByRenderer = AccessTools.Method(registry, "TryGetByRenderer");
            _entryBinding = AccessTools.Field(entry, "Binding");
            _entrySession = AccessTools.Field(entry, "Session");
            _originalMesh = AccessTools.Property(BindingType, "OriginalMesh");
            _workingMesh = AccessTools.Property(BindingType, "WorkingMesh");
            if (_tryGetByRenderer == null) return "TryGetByRenderer";
            if (_entryBinding == null || _entrySession == null) return "GarmentEntry fields";
            if (_originalMesh == null || _workingMesh == null) return "GarmentBinding meshes";
            return null;
        }

        /// <summary>Returns StripTease2's original (undeformed) mesh for a deformed renderer, and whether it is being edited.</summary>
        internal static Mesh OriginalMesh(ChaControl cha, SkinnedMeshRenderer renderer, out bool editing)
        {
            editing = false;
            var args = new object[] { cha, renderer, null, null };
            if (!(bool)_tryGetByRenderer.Invoke(null, args) || args[3] == null) return null;
            editing = _entrySession.GetValue(args[3]) != null;
            object binding = _entryBinding.GetValue(args[3]);
            return binding == null ? null : _originalMesh.GetValue(binding, null) as Mesh;
        }

        // StripTease2 rewrites the working mesh while editing and rebuilds it when toggling double-sided; the undo shape
        // has to be rebuilt afterwards.
        internal static void PatchDirtyTracking(Harmony harmony)
        {
            var postfix = new HarmonyMethod(typeof(StripAccess), nameof(MeshChangedPostfix));
            foreach (string name in new[] { "PushVerticesToMesh", "SetDoubleSided" })
            {
                MethodInfo method = AccessTools.Method(BindingType, name);
                if (method != null) harmony.Patch(method, postfix: postfix);
                else Plugin.Log.LogWarning("StripTease2 method not found, edits may need a scene reload to show: " + name);
            }
        }

        private static void MeshChangedPostfix(object __instance)
        {
            var mesh = _workingMesh.GetValue(__instance, null) as Mesh;
            if (mesh != null) UndoShapes.MarkDirty(mesh);
        }
    }


    /// <summary>
    /// Keeps each garment's StripTease2 edit session alive when another garment is selected. Stock StripTease2 disposes
    /// the session (undo history, pins/freeze state, baked collision) on every garment switch, so going back needs
    /// INIT MESH again, which re-applies the saved deformation. Card/scene saves already sync every live session.
    /// </summary>
    internal static class SessionKeeper
    {
        private class Parked
        {
            internal string Key;
            internal object Identity, Definition, Sdf, BodyQuery;
            internal int[] BoneRegion;
        }

        private static readonly Dictionary<object, Parked> ParkedEntries = new Dictionary<object, Parked>();
        private static bool _switching;
        private static FieldInfo _active, _activeKey, _identity, _definition, _sdf, _bodyQuery, _boneRegion, _character, _status;
        private static FieldInfo _candidateRenderer, _candidateLabel, _entrySession;
        private static MethodInfo _tryGetByRenderer, _releaseSession;

        internal static void Patch(Harmony harmony, Assembly asm)
        {
            Type plugin = asm.GetType("StripTease2.StripTease2Plugin");
            Type registry = asm.GetType("StripTease2.Serialization.DeformationRegistry");
            Type candidate = asm.GetType("StripTease2.StudioSupport.GarmentCandidate");
            Type entry = asm.GetType("StripTease2.Serialization.GarmentEntry");
            if (plugin == null || registry == null || candidate == null || entry == null) throw new Exception("StripTease2 types not found");
            _active = AccessTools.Field(plugin, "_active");
            _activeKey = AccessTools.Field(plugin, "_activeKey");
            _identity = AccessTools.Field(plugin, "_activeGarmentIdentity");
            _definition = AccessTools.Field(plugin, "_activeGarmentDefinition");
            _sdf = AccessTools.Field(plugin, "_sdf");
            _bodyQuery = AccessTools.Field(plugin, "_bodyQuery");
            _boneRegion = AccessTools.Field(plugin, "_garmentBoneToBodyRegion");
            _character = AccessTools.Field(plugin, "_character");
            _status = AccessTools.Field(plugin, "_status");
            _candidateRenderer = AccessTools.Field(candidate, "Renderer");
            _candidateLabel = AccessTools.Field(candidate, "Label");
            _entrySession = AccessTools.Field(entry, "Session");
            _tryGetByRenderer = AccessTools.Method(registry, "TryGetByRenderer");
            _releaseSession = AccessTools.Method(registry, "ReleaseSession");
            MethodInfo select = AccessTools.Method(plugin, "SelectGarment");
            MethodInfo loadCharacter = AccessTools.Method(plugin, "LoadSelectedCharacter");
            foreach (object f in new object[] { _active, _activeKey, _identity, _definition, _sdf, _bodyQuery, _boneRegion, _character, _status,
                _candidateRenderer, _candidateLabel, _entrySession, _tryGetByRenderer, _releaseSession, select, loadCharacter })
                if (f == null) throw new Exception("StripTease2 member not found");

            harmony.Patch(select, prefix: new HarmonyMethod(typeof(SessionKeeper), nameof(SelectPrefix)),
                postfix: new HarmonyMethod(typeof(SessionKeeper), nameof(SelectPostfix)));
            harmony.Patch(_releaseSession, prefix: new HarmonyMethod(typeof(SessionKeeper), nameof(ReleasePrefix)));
            harmony.Patch(loadCharacter, prefix: new HarmonyMethod(typeof(SessionKeeper), nameof(LoadCharacterPrefix)));
            Plugin.Log.LogInfo("Garment sessions are kept when switching meshes");
        }

        private static void SelectPrefix(object __instance)
        {
            if (!Plugin.KeepSessions.Value) return;
            object active = _active.GetValue(__instance);
            if (active == null || _entrySession.GetValue(active) == null) return;
            ParkedEntries[active] = new Parked
            {
                Key = _activeKey.GetValue(__instance) as string,
                Identity = _identity.GetValue(__instance),
                Definition = _definition.GetValue(__instance),
                Sdf = _sdf.GetValue(__instance),
                BodyQuery = _bodyQuery.GetValue(__instance),
                BoneRegion = _boneRegion.GetValue(__instance) as int[]
            };
            _switching = true;
        }

        // SelectGarment -> ReleaseActiveSession -> DeformationRegistry.ReleaseSession: skip disposing a parked session.
        private static bool ReleasePrefix(object entry)
        {
            return !(_switching && entry != null && ParkedEntries.ContainsKey(entry));
        }

        private static void SelectPostfix(object __instance, object candidate)
        {
            _switching = false;
            if (!Plugin.KeepSessions.Value || candidate == null) return;
            var renderer = _candidateRenderer.GetValue(candidate) as SkinnedMeshRenderer;
            var cha = _character.GetValue(__instance) as ChaControl;
            if (renderer == null || cha == null) return;
            var args = new object[] { cha, renderer, null, null };
            if (!(bool)_tryGetByRenderer.Invoke(null, args)) return;
            object entry = args[3];
            Parked parked;
            if (entry == null || _entrySession.GetValue(entry) == null || !ParkedEntries.TryGetValue(entry, out parked)) return;

            ParkedEntries.Remove(entry);
            _active.SetValue(__instance, entry);
            _activeKey.SetValue(__instance, parked.Key);
            _identity.SetValue(__instance, parked.Identity);
            _definition.SetValue(__instance, parked.Definition);
            _sdf.SetValue(__instance, parked.Sdf);
            _bodyQuery.SetValue(__instance, parked.BodyQuery);
            _boneRegion.SetValue(__instance, parked.BoneRegion);
            string label = _candidateLabel.GetValue(candidate) as string;
            _status.SetValue(__instance, "Resumed: " + label + " (previous session kept). Run INIT MESH again only if the pose changed.");
        }

        // Switching characters: finish the parked sessions normally (record captured, mesh compacted).
        private static void LoadCharacterPrefix()
        {
            foreach (object entry in new List<object>(ParkedEntries.Keys))
            {
                try
                {
                    if (_entrySession.GetValue(entry) != null) _releaseSession.Invoke(null, new[] { entry });
                }
                catch (Exception e)
                {
                    Plugin.Log.LogDebug(e);
                }
            }
            ParkedEntries.Clear();
        }
    }


    /// <summary>
    /// StripTease2 re-skins garments onto other bones (weight references / added bones), but Unity keeps computing the
    /// renderer bounds from the original per-bone bounding boxes (updateWhenOffscreen). Thin garments then get
    /// frustum-culled at certain camera angles. Deformed garments get a fixed character-sized box instead.
    /// </summary>
    internal static class CullingFix
    {
        private static PropertyInfo _renderer;

        internal static void Patch(Harmony harmony)
        {
            _renderer = AccessTools.Property(StripAccess.BindingType, "Renderer");
            MethodInfo bounds = AccessTools.Method(StripAccess.BindingType, "UpdateWorkingBounds");
            if (_renderer == null || bounds == null) throw new Exception("GarmentBinding.Renderer/UpdateWorkingBounds not found");
            harmony.Patch(bounds, postfix: new HarmonyMethod(typeof(CullingFix), nameof(BoundsPostfix)));
            Plugin.Log.LogInfo("StripTease2 garment culling fix enabled");
        }

        private static void BoundsPostfix(object __instance)
        {
            if (!Plugin.FixCulling.Value) return;
            var renderer = _renderer.GetValue(__instance, null) as SkinnedMeshRenderer;
            if (renderer != null) Apply(renderer);
        }

        internal static void Apply(SkinnedMeshRenderer renderer)
        {
            Transform root = renderer.rootBone != null ? renderer.rootBone : renderer.transform;
            Vector3 scale = root.lossyScale;
            float sx = Mathf.Max(Mathf.Abs(scale.x), 0.01f), sy = Mathf.Max(Mathf.Abs(scale.y), 0.01f), sz = Mathf.Max(Mathf.Abs(scale.z), 0.01f);
            // About 3 m around the character in world units, expressed in the root bone's space.
            renderer.updateWhenOffscreen = false;
            renderer.localBounds = new Bounds(new Vector3(0f, 0.8f / sy, 0f), new Vector3(3f / sx, 3.2f / sy, 3f / sz));
        }
    }

    /// <summary>Builds and drives the "undo" shape key on StripTease2 working meshes.</summary>
    internal static class UndoShapes
    {
        internal const string ShapeName = "ST2 Timeline Undo";
        private const string WorkingMeshMarker = " [ST2]";
        // Rebuilding is expensive; wait until StripTease2 stops rewriting the mesh.
        private const float RebuildDelay = 0.5f;

        private class State
        {
            internal int Index = -1;
            internal float DirtySince = -1f;
        }

        private static readonly Dictionary<Mesh, State> States = new Dictionary<Mesh, State>();
        private static readonly Dictionary<Mesh, bool> IsWorkingMesh = new Dictionary<Mesh, bool>();

        internal static void MarkDirty(Mesh mesh)
        {
            State state;
            if (!States.TryGetValue(mesh, out state))
            {
                state = new State();
                States[mesh] = state;
            }
            state.DirtySince = Time.realtimeSinceStartup;
        }

        internal static bool IsDeformed(SkinnedMeshRenderer renderer)
        {
            Mesh mesh = renderer != null ? renderer.sharedMesh : null;
            if (mesh == null) return false;
            bool result;
            if (!IsWorkingMesh.TryGetValue(mesh, out result))
            {
                if (IsWorkingMesh.Count > 256) Prune();
                result = mesh.name.EndsWith(WorkingMeshMarker, StringComparison.Ordinal);
                IsWorkingMesh[mesh] = result;
            }
            return result;
        }

        private static void Prune()
        {
            var dead = new List<Mesh>();
            foreach (Mesh m in IsWorkingMesh.Keys) if (m == null) dead.Add(m);
            foreach (Mesh m in dead) IsWorkingMesh.Remove(m);
            dead.Clear();
            foreach (Mesh m in States.Keys) if (m == null) dead.Add(m);
            foreach (Mesh m in dead) States.Remove(m);
        }

        /// <summary>Progress 100 = StripTease2 result, 0 = original garment.</summary>
        internal static float GetProgress(SkinnedMeshRenderer renderer)
        {
            State state;
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null || !States.TryGetValue(mesh, out state) || state.Index < 0 || state.Index >= mesh.blendShapeCount) return 100f;
            return 100f - renderer.GetBlendShapeWeight(state.Index);
        }

        internal static void SetProgress(ChaControl cha, SkinnedMeshRenderer renderer, float progress)
        {
            Mesh mesh = renderer.sharedMesh;
            if (mesh == null) return;
            State state;
            if (!States.TryGetValue(mesh, out state))
            {
                state = new State { DirtySince = 0f };
                States[mesh] = state;
            }
            float weight = 100f - Mathf.Clamp(progress, 0f, 100f);
            bool valid = state.Index >= 0 && state.Index < mesh.blendShapeCount && mesh.GetBlendShapeName(state.Index) == ShapeName;
            if (state.DirtySince >= 0f || !valid)
            {
                if (Time.realtimeSinceStartup - state.DirtySince < RebuildDelay && valid)
                {
                    // Still being edited: show StripTease2's live mesh untouched.
                    renderer.SetBlendShapeWeight(state.Index, 0f);
                    return;
                }
                if (weight <= 0f && !valid) return;   // nothing to undo yet, build lazily
                if (!Rebuild(cha, renderer, state)) return;
            }
            if (renderer.GetBlendShapeWeight(state.Index) != weight) renderer.SetBlendShapeWeight(state.Index, weight);
        }

        private static bool Rebuild(ChaControl cha, SkinnedMeshRenderer renderer, State state)
        {
            bool editing;
            Mesh working = renderer.sharedMesh;
            Mesh original = StripAccess.OriginalMesh(cha, renderer, out editing);
            if (original == null || working == null)
            {
                state.Index = -1;
                return false;
            }
            if (editing && state.DirtySince > 0f && Time.realtimeSinceStartup - state.DirtySince < RebuildDelay) return false;

            int n = original.vertexCount, total = working.vertexCount;
            if (total != n && total != n * 2)
            {
                Plugin.Log.LogWarning(renderer.name + ": vertex count " + total + " does not match StripTease2's original " + n + "; skipped.");
                state.Index = -1;
                state.DirtySince = -1f;
                return false;
            }
            Vector3[] orig = original.vertices, cur = working.vertices;
            Vector3[] origN = original.normals, curN = working.normals;
            bool normals = origN != null && origN.Length == n && curN != null && curN.Length == total;
            var dv = new Vector3[total];
            var dn = normals ? new Vector3[total] : null;
            for (int i = 0; i < n; i++)
            {
                dv[i] = orig[i] - cur[i];
                if (normals) dn[i] = origN[i] - curN[i];
            }
            // Double-sided copies: same movement, flipped normals.
            for (int i = n; i < total; i++)
            {
                dv[i] = dv[i - n];
                if (normals) dn[i] = -dn[i - n];
            }

            // Unity can't replace a single shape key: keep every other shape (and its renderer weight), re-add ours last.
            var keep = new List<KeyValuePair<string, List<KeyValuePair<float, Vector3[][]>>>>();
            var keepWeights = new List<float>();
            for (int s = 0; s < working.blendShapeCount; s++)
            {
                string name = working.GetBlendShapeName(s);
                if (name == ShapeName) continue;
                var frames = new List<KeyValuePair<float, Vector3[][]>>();
                for (int f = 0; f < working.GetBlendShapeFrameCount(s); f++)
                {
                    var v = new Vector3[total];
                    var nn = new Vector3[total];
                    var t = new Vector3[total];
                    working.GetBlendShapeFrameVertices(s, f, v, nn, t);
                    frames.Add(new KeyValuePair<float, Vector3[][]>(working.GetBlendShapeFrameWeight(s, f), new[] { v, nn, t }));
                }
                keep.Add(new KeyValuePair<string, List<KeyValuePair<float, Vector3[][]>>>(name, frames));
                keepWeights.Add(renderer.GetBlendShapeWeight(s));
            }
            working.ClearBlendShapes();
            for (int s = 0; s < keep.Count; s++)
            {
                foreach (var frame in keep[s].Value)
                    working.AddBlendShapeFrame(keep[s].Key, frame.Key, frame.Value[0], frame.Value[1], frame.Value[2]);
                renderer.SetBlendShapeWeight(s, keepWeights[s]);
            }
            working.AddBlendShapeFrame(ShapeName, 100f, dv, dn, null);
            state.Index = working.blendShapeCount - 1;
            state.DirtySince = -1f;
            return true;
        }
    }

    internal static class Garments
    {
        internal static readonly string[] SlotNames = { "Top", "Bottom", "Bra", "Underwear", "Gloves", "Pantyhose", "Legwear", "Indoor shoes", "Shoes" };

        private class Cache
        {
            internal GameObject Root;
            internal SkinnedMeshRenderer[] Renderers;
        }

        private static readonly Dictionary<ChaControl, Cache[]> Caches = new Dictionary<ChaControl, Cache[]>();

        /// <summary>Renderers of a clothing slot, cached until the clothing object is replaced.</summary>
        internal static SkinnedMeshRenderer[] Renderers(ChaControl cha, int slot)
        {
            if (cha == null || cha.objClothes == null || slot >= cha.objClothes.Length) return new SkinnedMeshRenderer[0];
            Cache[] caches;
            if (!Caches.TryGetValue(cha, out caches))
            {
                if (Caches.Count > 64) PruneCaches();
                caches = new Cache[SlotNames.Length];
                Caches[cha] = caches;
            }
            GameObject root = cha.objClothes[slot];
            Cache cache = caches[slot];
            if (cache == null || !ReferenceEquals(cache.Root, root) || (root != null && cache.Root == null))
            {
                cache = new Cache { Root = root, Renderers = root != null ? root.GetComponentsInChildren<SkinnedMeshRenderer>(true) : new SkinnedMeshRenderer[0] };
                caches[slot] = cache;
            }
            return cache.Renderers;
        }

        private static void PruneCaches()
        {
            var dead = new List<ChaControl>();
            foreach (ChaControl c in Caches.Keys) if (c == null) dead.Add(c);
            foreach (ChaControl c in dead) Caches.Remove(c);
        }

        internal static bool HasDeformation(ChaControl cha, int slot)
        {
            foreach (SkinnedMeshRenderer r in Renderers(cha, slot))
                if (UndoShapes.IsDeformed(r)) return true;
            return false;
        }

        internal static float GetProgress(ChaControl cha, int slot)
        {
            foreach (SkinnedMeshRenderer r in Renderers(cha, slot))
                if (UndoShapes.IsDeformed(r)) return UndoShapes.GetProgress(r);
            return 100f;
        }

        internal static void SetProgress(ChaControl cha, int slot, float progress)
        {
            foreach (SkinnedMeshRenderer r in Renderers(cha, slot))
            {
                if (!UndoShapes.IsDeformed(r)) continue;
                try
                {
                    UndoShapes.SetProgress(cha, r, progress);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogDebug(e);
                }
            }
        }
    }

    /// <summary>
    /// Body alpha mask (the skin hidden under clothes), switched exactly at Timeline keyframes. Same rule as StripTease2's
    /// own toggle: the bottom mask only applies while the top mask is on.
    /// </summary>
    internal static class BodyMask
    {
        private class Desired
        {
            internal bool? Top, Bottom;
        }

        private static readonly Dictionary<ChaControl, Desired> States = new Dictionary<ChaControl, Desired>();

        internal static bool Get(ChaControl cha, bool top)
        {
            if (cha == null || cha.customMatBody == null) return true;
            return cha.customMatBody.GetFloat(top ? ChaShader._alpha_a : ChaShader._alpha_b) > 0.5f;
        }

        internal static void Set(ChaControl cha, bool top, bool on)
        {
            if (cha == null || cha.customMatBody == null) return;
            Desired d;
            if (!States.TryGetValue(cha, out d))
            {
                if (States.Count > 64) Prune();
                d = new Desired();
                States[cha] = d;
            }
            if (top) d.Top = on;
            else d.Bottom = on;
            bool topOn = d.Top ?? Get(cha, true);
            bool bottomOn = d.Bottom ?? Get(cha, false);
            byte a = topOn ? (byte)1 : (byte)0;
            byte b = topOn && bottomOn ? (byte)1 : (byte)0;
            // The game can reset the mask when clothes change state, so compare with the material instead of a cache.
            if (cha.customMatBody.GetFloat(ChaShader._alpha_a) != a || cha.customMatBody.GetFloat(ChaShader._alpha_b) != b)
                cha.ChangeAlphaMask(a, b);
        }

        private static void Prune()
        {
            var dead = new List<ChaControl>();
            foreach (ChaControl c in States.Keys) if (c == null) dead.Add(c);
            foreach (ChaControl c in dead) States.Remove(c);
        }
    }


    /// <summary>
    /// "Drop": releases a clothing slot from the skeleton. The moment the drop time goes above 0 the garment's current
    /// shape (including Strip progress) is baked into a world-space mesh and the skinned garment is hidden. The copy
    /// keeps the body part's velocity at release (with air drag), falls with gravity and spreads out where it lands.
    /// The motion is a pure function of the drop time after capture, so Timeline scrubbing and seeking reproduce it;
    /// drop time 0 restores the worn garment.
    /// </summary>
    internal static class Drop
    {
        // StripTease2 restores saved deformations over up to ~2 s after a scene load; capturing before that would bake
        // (and hide) the undeformed garments.
        private const float SceneLoadGrace = 3f;
        private const float MaxInheritedSpeed = 4f;

        internal static float SceneLoadedAt = -100f;

        private class Piece
        {
            internal SkinnedMeshRenderer Source;
            internal Mesh SourceMesh;
            internal bool SourceWasEnabled;
            internal GameObject Object;
            internal Mesh Mesh;
            internal Vector3[] Start, Current;
            internal float[] Rest;
        }

        private class State
        {
            internal readonly List<Piece> Pieces = new List<Piece>();
            internal float Time = -1f;
            internal Vector3 Velocity, Center;
        }

        private static readonly Dictionary<ChaControl, State[]> States = new Dictionary<ChaControl, State[]>();

        internal static bool CanDrop(ChaControl cha, int slot)
        {
            return cha != null && cha.objClothes != null && slot < cha.objClothes.Length && cha.objClothes[slot] != null;
        }

        internal static float Get(ChaControl cha, int slot)
        {
            State state = Find(cha, slot);
            return state != null && state.Pieces.Count > 0 ? state.Time : 0f;
        }

        private static State Find(ChaControl cha, int slot)
        {
            State[] states;
            return cha != null && States.TryGetValue(cha, out states) ? states[slot] : null;
        }

        internal static void Set(ChaControl cha, int slot, float time)
        {
            if (cha == null) return;
            State state = Find(cha, slot);
            if (time <= 0f)
            {
                if (state != null) Restore(state);
                return;
            }
            if (state == null)
            {
                State[] states;
                if (!States.TryGetValue(cha, out states))
                {
                    states = new State[Garments.SlotNames.Length];
                    States[cha] = states;
                }
                state = states[slot] = new State();
            }
            // The garment was rebuilt under the copy (StripTease2 restore, re-edit): take a fresh snapshot.
            if (state.Pieces.Count > 0 && SourceChanged(state)) Restore(state);
            if (state.Pieces.Count == 0)
            {
                if (UnityEngine.Time.realtimeSinceStartup - SceneLoadedAt < SceneLoadGrace) return;
                if (!Capture(cha, slot, state)) return;
            }
            if (state.Time == time) return;
            state.Time = time;

            float gravity = Plugin.DropGravity.Value;
            float drag = Mathf.Max(Plugin.DropAirDrag.Value, 0.01f);
            // Horizontal: velocity decaying with air drag. Vertical: inherited velocity plus gravity.
            float travel = (1f - Mathf.Exp(-drag * time)) / drag;
            Vector3 drift = new Vector3(state.Velocity.x * travel, 0f, state.Velocity.z * travel);
            float lift = state.Velocity.y * travel - 0.5f * gravity * time * time;
            float spread = Plugin.DropSpread.Value, maxSpread = Plugin.DropMaxSpread.Value;
            Vector3 center = state.Center + drift;
            foreach (Piece piece in state.Pieces)
            {
                for (int i = 0; i < piece.Start.Length; i++)
                {
                    Vector3 p = piece.Start[i] + drift;
                    p.y += lift;
                    float below = piece.Rest[i] - p.y;
                    if (below > 0f)
                    {
                        // Landed: the fall that did not happen spreads the cloth outwards from its center.
                        p.y = piece.Rest[i];
                        float dx = p.x - center.x, dz = p.z - center.z;
                        float len = Mathf.Sqrt(dx * dx + dz * dz);
                        if (len > 1e-4f && spread > 0f)
                        {
                            float push = Mathf.Min(below * spread, maxSpread) / len;
                            p.x += dx * push;
                            p.z += dz * push;
                        }
                    }
                    piece.Current[i] = p;
                }
                piece.Mesh.vertices = piece.Current;
                piece.Mesh.RecalculateBounds();
            }
        }

        private static bool SourceChanged(State state)
        {
            foreach (Piece piece in state.Pieces)
                if (piece.Source == null || piece.Source.sharedMesh != piece.SourceMesh) return true;
            return false;
        }

        private static bool Capture(ChaControl cha, int slot, State state)
        {
            float floor = cha.transform.position.y;
            Vector3 sum = Vector3.zero;
            int count = 0;
            foreach (SkinnedMeshRenderer smr in Garments.Renderers(cha, slot))
            {
                if (smr == null || !smr.enabled || !smr.gameObject.activeInHierarchy || smr.sharedMesh == null) continue;
                var mesh = new Mesh { name = smr.name + " (ST2T drop)" };
                smr.BakeMesh(mesh);
                Vector3[] vertices = mesh.vertices;
                Vector3[] normals = mesh.normals;
                if (vertices == null || vertices.Length == 0)
                {
                    UnityEngine.Object.Destroy(mesh);
                    continue;
                }
                // BakeMesh output is already scaled; only position and rotation remain (same as StripTease2's snapshot).
                Matrix4x4 toWorld = Matrix4x4.TRS(smr.transform.position, smr.transform.rotation, Vector3.one);
                float minY = float.MaxValue;
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = toWorld.MultiplyPoint3x4(vertices[i]);
                    minY = Mathf.Min(minY, vertices[i].y);
                    sum += vertices[i];
                }
                count += vertices.Length;
                if (normals != null && normals.Length == vertices.Length)
                    for (int i = 0; i < normals.Length; i++) normals[i] = toWorld.MultiplyVector(normals[i]);
                mesh.vertices = vertices;
                if (normals != null && normals.Length == vertices.Length) mesh.normals = normals;
                mesh.MarkDynamic();
                float ground = Mathf.Min(floor, minY);
                // Settled cloth keeps a thin layering so it piles up instead of z-fighting on the floor.
                var rest = new float[vertices.Length];
                for (int i = 0; i < vertices.Length; i++) rest[i] = ground + 0.002f + (vertices[i].y - minY) * 0.03f;

                var go = new GameObject(mesh.name);
                go.layer = smr.gameObject.layer;
                go.AddComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = go.AddComponent<MeshRenderer>();
                renderer.sharedMaterials = smr.sharedMaterials;
                renderer.shadowCastingMode = smr.shadowCastingMode;
                renderer.receiveShadows = smr.receiveShadows;

                state.Pieces.Add(new Piece
                {
                    Source = smr,
                    SourceMesh = smr.sharedMesh,
                    SourceWasEnabled = smr.enabled,
                    Object = go,
                    Mesh = mesh,
                    Start = (Vector3[])vertices.Clone(),
                    Current = vertices,
                    Rest = rest
                });
                smr.enabled = false;
            }
            if (state.Pieces.Count == 0) return false;
            state.Center = sum / Mathf.Max(count, 1);
            Vector3 velocity = VelocityTracker.Get(cha, slot) * Plugin.DropInheritMotion.Value;
            state.Velocity = Vector3.ClampMagnitude(velocity, MaxInheritedSpeed);
            return true;
        }

        private static void Restore(State state)
        {
            foreach (Piece piece in state.Pieces)
            {
                if (piece.Source != null) piece.Source.enabled = piece.SourceWasEnabled;
                if (piece.Object != null) UnityEngine.Object.Destroy(piece.Object);
                if (piece.Mesh != null) UnityEngine.Object.Destroy(piece.Mesh);
            }
            state.Pieces.Clear();
            state.Time = -1f;
        }

        /// <summary>Drops copies whose character or garment is gone (scene reset, outfit change).</summary>
        internal static void Cleanup()
        {
            if (States.Count == 0) return;
            List<ChaControl> dead = null;
            foreach (KeyValuePair<ChaControl, State[]> pair in States)
            {
                bool alive = pair.Key != null;
                foreach (State state in pair.Value)
                {
                    if (state == null || state.Pieces.Count == 0) continue;
                    bool broken = !alive;
                    foreach (Piece piece in state.Pieces)
                        if (piece.Source == null || piece.Object == null) broken = true;
                    if (broken) Restore(state);
                }
                if (!alive) (dead ?? (dead = new List<ChaControl>())).Add(pair.Key);
            }
            if (dead != null) foreach (ChaControl c in dead) States.Remove(c);
        }
    }

    /// <summary>
    /// Velocity of the body part each clothing slot sits on, sampled every frame while Timeline plays (scrubbing jumps
    /// would give nonsense speeds, so it reads zero then).
    /// </summary>
    internal static class VelocityTracker
    {
        // 0 upper body, 1 hips, 2 hands, 3 feet
        private static readonly string[][] BoneNames =
        {
            new[] { "cf_j_spine03" },
            new[] { "cf_j_waist01" },
            new[] { "cf_j_hand_L", "cf_j_hand_R" },
            new[] { "cf_j_foot_L", "cf_j_foot_R" }
        };

        private static readonly int[] SlotGroup = { 0, 1, 0, 1, 2, 1, 3, 3, 3 };

        private class Tracked
        {
            internal Transform[][] Bones;
            internal Vector3[] Last = new Vector3[4];
            internal Vector3[] Velocity = new Vector3[4];
            internal bool HasLast;
        }

        private static readonly Dictionary<ChaControl, Tracked> Characters = new Dictionary<ChaControl, Tracked>();

        internal static Vector3 Get(ChaControl cha, int slot)
        {
            Tracked tracked;
            if (cha == null || slot < 0 || slot >= SlotGroup.Length || !Characters.TryGetValue(cha, out tracked)) return Vector3.zero;
            return tracked.Velocity[SlotGroup[slot]];
        }

        private static bool Playing()
        {
            try
            {
                return ProgressTimeline.Available && TimelineCompatibility.GetIsPlaying();
            }
            catch
            {
                return false;
            }
        }

        internal static void Sample()
        {
            if (!Playing())
            {
                foreach (Tracked t in Characters.Values)
                {
                    t.HasLast = false;
                    for (int g = 0; g < 4; g++) t.Velocity[g] = Vector3.zero;
                }
                return;
            }
            var studio = Singleton<global::Studio.Studio>.Instance;
            if (studio == null || studio.dicObjectCtrl == null) return;
            float dt = Time.deltaTime;
            foreach (ObjectCtrlInfo oci in studio.dicObjectCtrl.Values)
            {
                var chara = oci as OCIChar;
                ChaControl cha = chara != null ? chara.charInfo : null;
                if (cha == null) continue;
                Tracked tracked;
                if (!Characters.TryGetValue(cha, out tracked))
                {
                    tracked = new Tracked { Bones = FindBones(cha) };
                    Characters[cha] = tracked;
                }
                for (int g = 0; g < 4; g++)
                {
                    Vector3 pos = Average(tracked.Bones[g], cha.transform.position);
                    if (tracked.HasLast && dt > 0f)
                    {
                        // Light smoothing so a single hitchy frame doesn't fling the garment.
                        Vector3 v = (pos - tracked.Last[g]) / dt;
                        tracked.Velocity[g] = Vector3.Lerp(tracked.Velocity[g], v, 0.5f);
                    }
                    tracked.Last[g] = pos;
                }
                tracked.HasLast = true;
            }
            if (Characters.Count > 0)
            {
                List<ChaControl> dead = null;
                foreach (ChaControl c in Characters.Keys) if (c == null) (dead ?? (dead = new List<ChaControl>())).Add(c);
                if (dead != null) foreach (ChaControl c in dead) Characters.Remove(c);
            }
        }

        private static Transform[][] FindBones(ChaControl cha)
        {
            var byName = new Dictionary<string, Transform>();
            foreach (Transform t in cha.GetComponentsInChildren<Transform>(true))
                if (!byName.ContainsKey(t.name)) byName[t.name] = t;
            var result = new Transform[BoneNames.Length][];
            for (int g = 0; g < BoneNames.Length; g++)
            {
                var found = new List<Transform>();
                foreach (string n in BoneNames[g])
                {
                    Transform t;
                    if (byName.TryGetValue(n, out t)) found.Add(t);
                }
                result[g] = found.ToArray();
            }
            return result;
        }

        private static Vector3 Average(Transform[] bones, Vector3 fallback)
        {
            Vector3 sum = Vector3.zero;
            int n = 0;
            foreach (Transform t in bones)
            {
                if (t == null) continue;
                sum += t.position;
                n++;
            }
            return n > 0 ? sum / n : fallback;
        }
    }

    internal static class ProgressTimeline
    {
        internal static bool Available;

        internal static void Register()
        {
            for (int slot = 0; slot < Garments.SlotNames.Length; slot++)
            {
#if KKS
                if (slot == 7) continue;
#endif
                int s = slot;
                TimelineCompatibility.AddInterpolableModelStatic<float, int>(
                    owner: "StripTease2",
                    id: "StripProgress" + s,
                    parameter: s,
                    name: "Strip progress: " + Garments.SlotNames[s],
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        Garments.SetProgress(Cha(oci), parameter, Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: oci => oci is OCIChar && Garments.HasDeformation(Cha(oci), s),
                    getValue: (oci, parameter) => Garments.GetProgress(Cha(oci), parameter),
                    readValueFromXml: (parameter, node) => System.Xml.XmlConvert.ToSingle(node.Attributes["value"].Value),
                    writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", System.Xml.XmlConvert.ToString(value)),
                    readParameterFromXml: (oci, node) => s,
                    writeParameterToXml: (oci, writer, parameter) => { },
                    checkIntegrity: null,
                    useOciInHash: true);
            }
            for (int slot = 0; slot < Garments.SlotNames.Length; slot++)
            {
#if KKS
                if (slot == 7) continue;
#endif
                int s = slot;
                TimelineCompatibility.AddInterpolableModelStatic<float, int>(
                    owner: "StripTease2",
                    id: "Drop" + s,
                    parameter: s,
                    name: "Drop: " + Garments.SlotNames[s],
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        Drop.Set(Cha(oci), parameter, Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                    interpolateAfter: null,
                    isCompatibleWithTarget: oci => oci is OCIChar && Drop.CanDrop(Cha(oci), s),
                    getValue: (oci, parameter) => Drop.Get(Cha(oci), parameter),
                    readValueFromXml: (parameter, node) => System.Xml.XmlConvert.ToSingle(node.Attributes["value"].Value),
                    writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", System.Xml.XmlConvert.ToString(value)),
                    readParameterFromXml: (oci, node) => s,
                    writeParameterToXml: (oci, writer, parameter) => { },
                    checkIntegrity: null,
                    useOciInHash: true);
            }
            foreach (bool top in new[] { true, false })
            {
                bool t = top;
                TimelineCompatibility.AddInterpolableModelStatic<bool, int>(
                    owner: "StripTease2",
                    id: t ? "BodyMaskTop" : "BodyMaskBottom",
                    parameter: t ? 0 : 1,
                    name: t ? "Body mask: Top" : "Body mask: Bottom",
                    // Hold the left keyframe's value and switch exactly on the next keyframe.
                    interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                        BodyMask.Set(Cha(oci), t, factor >= 1f ? rightValue : leftValue),
                    interpolateAfter: null,
                    isCompatibleWithTarget: oci => oci is OCIChar,
                    getValue: (oci, parameter) => BodyMask.Get(Cha(oci), t),
                    readValueFromXml: (parameter, node) => System.Xml.XmlConvert.ToBoolean(node.Attributes["value"].Value),
                    writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", System.Xml.XmlConvert.ToString(value)),
                    readParameterFromXml: (oci, node) => t ? 0 : 1,
                    writeParameterToXml: (oci, writer, parameter) => { },
                    checkIntegrity: null,
                    useOciInHash: true);
            }
            Available = true;
            Plugin.Log.LogInfo("Timeline support enabled");
        }

        internal static ChaControl Cha(ObjectCtrlInfo oci)
        {
            var chara = oci as OCIChar;
            return chara != null ? chara.charInfo : null;
        }
    }

    /// <summary>Small window to set the current Strip progress, so Timeline keyframes can capture it.</summary>
    internal class ProgressWindow : MonoBehaviour
    {
        private const int WindowId = 0x53543254;
        private bool _open;
        private Rect _rect = new Rect(80, 120, 380, 10);

        private void Update()
        {
            if (Plugin.WindowKey.Value.IsDown()) _open = !_open;
            Drop.Cleanup();
        }

        private void LateUpdate()
        {
            VelocityTracker.Sample();
        }

        private void OnGUI()
        {
            if (!_open) return;
            float scale = Plugin.UIScale.Value;
            Matrix4x4 matrix = GUI.matrix;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            GUISkin skin = GUI.skin;
            GUI.skin = IMGUIUtils.SolidBackgroundGuiSkin;
            _rect = GUILayout.Window(WindowId, _rect, Draw, "StripTease2 Timeline");
            GUI.skin = skin;
            GUI.matrix = matrix;
            IMGUIUtils.EatInputInRect(new Rect(_rect.x * scale, _rect.y * scale, _rect.width * scale, _rect.height * scale));
        }

        private void Draw(int id)
        {
            ChaControl cha = null;
            foreach (OCIChar selected in StudioAPI.GetSelectedCharacters())
            {
                cha = selected.charInfo;
                break;
            }
            if (cha == null)
            {
                GUILayout.Label("Select a character in the workspace.");
            }
            else
            {
                bool any = false;
                for (int slot = 0; slot < Garments.SlotNames.Length; slot++)
                {
                    if (!Garments.HasDeformation(cha, slot)) continue;
                    any = true;
                    float progress = Garments.GetProgress(cha, slot);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Garments.SlotNames[slot], GUILayout.Width(90));
                    float next = GUILayout.HorizontalSlider(progress, 0f, 100f, GUILayout.Width(170));
                    GUILayout.Label(Mathf.RoundToInt(next).ToString(), GUILayout.Width(32));
                    if (GUILayout.Button("0", GUILayout.Width(26))) next = 0f;
                    if (GUILayout.Button("100", GUILayout.Width(36))) next = 100f;
                    GUILayout.EndHorizontal();
                    if (Math.Abs(next - progress) > 0.01f) Garments.SetProgress(cha, slot, next);
                }
                if (!any) GUILayout.Label("This character has no StripTease2 deformation.");
                else GUILayout.Label("100 = StripTease2 result, 0 = original.");
                GUILayout.BeginHorizontal();
                GUILayout.Label("Body mask", GUILayout.Width(90));
                bool maskTop = BodyMask.Get(cha, true), maskBottom = BodyMask.Get(cha, false);
                bool newTop = GUILayout.Toggle(maskTop, "Top", GUI.skin.button, GUILayout.Width(80));
                bool newBottom = GUILayout.Toggle(maskBottom, "Bottom", GUI.skin.button, GUILayout.Width(80));
                GUILayout.EndHorizontal();
                if (newTop != maskTop) BodyMask.Set(cha, true, newTop);
                if (newBottom != maskBottom) BodyMask.Set(cha, false, newBottom);
                GUILayout.Label("Drop (seconds after release, 0 = worn)");
                for (int slot = 0; slot < Garments.SlotNames.Length; slot++)
                {
                    if (!Drop.CanDrop(cha, slot)) continue;
                    float t = Drop.Get(cha, slot);
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(Garments.SlotNames[slot], GUILayout.Width(90));
                    float nt = GUILayout.HorizontalSlider(t, 0f, 3f, GUILayout.Width(170));
                    GUILayout.Label(nt.ToString("0.00"), GUILayout.Width(40));
                    if (GUILayout.Button("0", GUILayout.Width(26))) nt = 0f;
                    GUILayout.EndHorizontal();
                    if (Math.Abs(nt - t) > 0.001f) Drop.Set(cha, slot, nt);
                }
                GUILayout.Label("Set the values, then add keyframes in Timeline > StripTease2. Body mask tracks switch exactly on their keyframes.");
            }
            if (GUILayout.Button("Close")) _open = false;
            GUI.DragWindow();
        }
    }
}
