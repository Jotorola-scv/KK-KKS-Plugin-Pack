using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

namespace KKPECanineLink
{
    // KKPE overrides each mouth renderer on its own: a Teeth override or Timeline track leaves Canines under the game's
    // face animation, so the fangs stay on the old mouth pattern. While Teeth is overridden and Canines is not, this mirrors the
    // teeth weights onto the canines. Nothing is stored in the scene, and a manual Canines override always wins.
    [BepInPlugin(GUID, "KKPE Canine Link", Version)]
    [BepInDependency(KKPEGuid)]
    [BepInProcess("CharaStudio")]
    public class KKPECanineLinkPlugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.kkpecaninelink";
        public const string Version = "1.0.1";
        private const string KKPEGuid = "com.joan6694.kkplugins.kkpe";
        private const int RefreshInterval = 60;

        private static ConfigEntry<bool> _enabled;
        private static FieldInfo _instances;
        private static FieldInfo _blendRenderers;
        private static FieldInfo _renderer;
        private static FieldInfo _dirtyBlends;
        private static readonly Dictionary<FaceBlendShape, Link> _links = new Dictionary<FaceBlendShape, Link>();

        private class Link
        {
            public SkinnedMeshRenderer Tooth;
            public SkinnedMeshRenderer Canine;
            public Mesh ToothMesh;
            public Mesh CanineMesh;
            public int[] ToothIndexByCanine;
            public string[] CanineNames;
            public bool[] GameDriven;
            public float[] WeightsBeforeMirroring;
            public bool Mirroring;
            public object ToothBlend;
            public object CanineBlend;
            public int NextRefresh;
        }

        private void Awake()
        {
            _enabled = Config.Bind("General", "Enabled", true, "Make the fangs follow the teeth while KKPE overrides the teeth blendshapes.");

            var editor = AccessTools.TypeByName("HSPE.AMModules.BlendShapesEditor");
            var blendRenderer = editor == null ? null : AccessTools.Inner(editor, "BlendRenderer");
            if (blendRenderer == null)
            {
                Logger.LogError("KKPE BlendShapesEditor not found, plugin disabled");
                return;
            }
            _instances = AccessTools.Field(editor, "_instanceByFaceBlendShape");
            _blendRenderers = AccessTools.Field(editor, "_blendRenderers");
            _renderer = AccessTools.Field(blendRenderer, "_renderer");
            _dirtyBlends = AccessTools.Field(blendRenderer, "_dirtyBlends");
            if (_instances == null || _blendRenderers == null || _renderer == null || _dirtyBlends == null)
            {
                Logger.LogError("KKPE BlendShapesEditor fields not found, plugin disabled");
                return;
            }
            Harmony.CreateAndPatchAll(typeof(KKPECanineLinkPlugin), GUID);
        }

        [HarmonyPostfix]
        [HarmonyPriority(Priority.Last)]
        [HarmonyAfter(KKPEGuid)]
        [HarmonyPatch(typeof(FaceBlendShape), "LateUpdate")]
        private static void FaceBlendShapeLateUpdatePostfix(FaceBlendShape __instance)
        {
            Link link;
            var known = _links.TryGetValue(__instance, out link);
            if (!_enabled.Value && !(known && link.Mirroring))
                return;
            var editors = _instances.GetValue(null) as IDictionary;
            if (editors == null || !editors.Contains(__instance))
                return;

            if (!known)
            {
                link = new Link();
                _links[__instance] = link;
                PruneDestroyedLinks();
            }

            if (Time.frameCount >= link.NextRefresh || !IsValid(link))
            {
                link.NextRefresh = Time.frameCount + RefreshInterval;
                Refresh(link, __instance, editors[__instance]);
            }
            if (link.ToothIndexByCanine == null)
                return;

            var canineOverrides = Overrides(link.CanineBlend, link.Canine);
            if (_enabled.Value && Overrides(link.ToothBlend, link.Tooth) != null && canineOverrides == null)
                Mirror(link);
            else if (link.Mirroring)
                StopMirroring(link, canineOverrides);
        }

        private static void Mirror(Link link)
        {
            if (!link.Mirroring)
            {
                for (var i = 0; i < link.WeightsBeforeMirroring.Length; i++)
                    link.WeightsBeforeMirroring[i] = link.Canine.GetBlendShapeWeight(i);
                link.Mirroring = true;
            }
            var map = link.ToothIndexByCanine;
            for (var i = 0; i < map.Length; i++)
            {
                if (map[i] < 0)
                    continue;
                var weight = link.Tooth.GetBlendShapeWeight(map[i]);
                if (link.Canine.GetBlendShapeWeight(i) != weight)
                    link.Canine.SetBlendShapeWeight(i, weight);
            }
        }

        // The game rewrites the shapes its mouth patterns use every frame, but never touches the others, so a mirrored
        // value would stay on them for good. While Canines is overridden KKPE owns its own shapes, and can still restore
        // a weight it recorded during mirroring, so this keeps running until that override is gone.
        private static void StopMirroring(Link link, IDictionary canineOverrides)
        {
            for (var i = 0; i < link.GameDriven.Length; i++)
            {
                if (link.GameDriven[i] || (canineOverrides != null && canineOverrides.Contains(link.CanineNames[i])))
                    continue;
                if (link.Canine.GetBlendShapeWeight(i) != link.WeightsBeforeMirroring[i])
                    link.Canine.SetBlendShapeWeight(i, link.WeightsBeforeMirroring[i]);
            }
            if (canineOverrides == null)
                link.Mirroring = false;
        }

        private static bool IsValid(Link link)
        {
            return link.Tooth != null && link.Canine != null && link.Tooth.sharedMesh == link.ToothMesh && link.Canine.sharedMesh == link.CanineMesh
                && link.ToothBlend != null && ReferenceEquals(_renderer.GetValue(link.ToothBlend), link.Tooth);
        }

        // The renderer's KKPE overrides, or null when it has none.
        private static IDictionary Overrides(object blend, SkinnedMeshRenderer renderer)
        {
            if (blend == null || !ReferenceEquals(_renderer.GetValue(blend), renderer))
                return null;
            var dirty = _dirtyBlends.GetValue(blend) as IDictionary;
            return dirty != null && dirty.Count > 0 ? dirty : null;
        }

        private static void Refresh(Link link, FaceBlendShape face, object editor)
        {
            var previousCanine = link.Canine;
            link.Tooth = null;
            link.Canine = null;
            link.ToothBlend = null;
            link.CanineBlend = null;
            FBSTargetInfo canineTarget = null;
            foreach (var target in face.MouthCtrl.FBSTarget)
            {
                var renderer = target.GetSkinnedMeshRenderer();
                if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.blendShapeCount == 0)
                    continue;
                var firstShape = renderer.sharedMesh.GetBlendShapeName(0);
                if (renderer.name == "cf_O_tooth" || firstShape.StartsWith("kuti_ha."))
                    link.Tooth = renderer;
                else if (renderer.name == "cf_O_canine" || firstShape.StartsWith("kuti_yaeba."))
                {
                    link.Canine = renderer;
                    canineTarget = target;
                }
            }
            if (link.Tooth == null || link.Canine == null)
            {
                link.ToothIndexByCanine = null;
                link.Mirroring = false;
                return;
            }
            // A new renderer means a newly loaded head, which has no mirrored weights on it.
            if (link.Canine != previousCanine)
                link.Mirroring = false;

            if (link.Tooth.sharedMesh != link.ToothMesh || link.Canine.sharedMesh != link.CanineMesh || link.ToothIndexByCanine == null)
            {
                link.ToothMesh = link.Tooth.sharedMesh;
                link.CanineMesh = link.Canine.sharedMesh;
                link.ToothIndexByCanine = BuildMap(link.ToothMesh, link.CanineMesh);
                var count = link.CanineMesh.blendShapeCount;
                link.CanineNames = new string[count];
                for (var i = 0; i < count; i++)
                    link.CanineNames[i] = link.CanineMesh.GetBlendShapeName(i);
                link.GameDriven = BuildGameDriven(canineTarget, count);
                if (link.WeightsBeforeMirroring == null || link.WeightsBeforeMirroring.Length != count)
                {
                    link.WeightsBeforeMirroring = new float[count];
                    link.Mirroring = false;
                }
            }

            var blends = _blendRenderers.GetValue(editor) as IDictionary;
            if (blends == null)
                return;
            foreach (var blend in blends.Values)
            {
                var renderer = _renderer.GetValue(blend);
                if (ReferenceEquals(renderer, link.Tooth))
                    link.ToothBlend = blend;
                else if (ReferenceEquals(renderer, link.Canine))
                    link.CanineBlend = blend;
            }
        }

        private static bool[] BuildGameDriven(FBSTargetInfo target, int count)
        {
            var driven = new bool[count];
            foreach (var pattern in target.PtnSet)
            {
                if (pattern.Close >= 0 && pattern.Close < count)
                    driven[pattern.Close] = true;
                if (pattern.Open >= 0 && pattern.Open < count)
                    driven[pattern.Open] = true;
            }
            return driven;
        }

        // Stock meshes pair by pattern name; the one stock mismatch (ha00_e_s_op / y00_e_l_op) sits at the same index.
        private static int[] BuildMap(Mesh tooth, Mesh canine)
        {
            var toothByPattern = new Dictionary<string, int>();
            for (var i = 0; i < tooth.blendShapeCount; i++)
                toothByPattern[PatternName(tooth.GetBlendShapeName(i))] = i;

            var sameCount = tooth.blendShapeCount == canine.blendShapeCount;
            var map = new int[canine.blendShapeCount];
            for (var i = 0; i < map.Length; i++)
            {
                int index;
                if (toothByPattern.TryGetValue(PatternName(canine.GetBlendShapeName(i)), out index))
                    map[i] = index;
                else
                    map[i] = sameCount ? i : -1;
            }
            return map;
        }

        // "kuti_yaeba.y00_uresi_op" -> "uresi_op"
        private static string PatternName(string shapeName)
        {
            var name = shapeName.Substring(shapeName.LastIndexOf('.') + 1);
            return name.Substring(name.IndexOf('_') + 1);
        }

        private static void PruneDestroyedLinks()
        {
            var destroyed = new List<FaceBlendShape>();
            foreach (var key in _links.Keys)
            {
                if (key == null)
                    destroyed.Add(key);
            }
            foreach (var key in destroyed)
                _links.Remove(key);
        }
    }
}
