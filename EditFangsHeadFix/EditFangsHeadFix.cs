using System;
using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EditFangsHeadFix
{
    // EditFangs caches the fang vertices in Start, before the card's head is loaded, and only re-reads them in the maker.
    // Outside the maker it then writes the stock head's vertices onto a modded head's fang mesh. In KKS the modded meshes share
    // the positions but only 5 of 42 sit on the same index, so skin weights, triangles and blendshapes land on the wrong vertices.
    // This re-reads the cache from the real head right before EditFangs clones and adjusts the mesh.
    [BepInPlugin(GUID, "EditFangs Head Fix", Version)]
    [BepInDependency(EditFangsGuid)]
    public class EditFangsHeadFixPlugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.editfangsheadfix";
        public const string Version = "1.0.2";
        private const string EditFangsGuid = "org.njaecha.plugins.editfangs";
        private const string CaninePath = "N_tonn_face/N_cf_haed/cf_O_canine";
        private const int FangVertexCount = 42;

        private static ManualLogSource _logger;
        private static FieldInfo _headId;
        private static FieldInfo _rotationPivots;
        private static FieldInfo _tips;
        private static FieldInfo _registered;
        private static MethodInfo _registerFangs;

        private void Awake()
        {
            _logger = Logger;
            var controller = AccessTools.TypeByName("EditFangs.EditFangsController");
            if (controller == null)
            {
                Logger.LogError("EditFangsController not found, plugin disabled");
                return;
            }
            _headId = AccessTools.Field(controller, "headID");
            _rotationPivots = AccessTools.Field(controller, "rotp");
            _tips = AccessTools.Field(controller, "tips");
            _registered = AccessTools.Field(controller, "registered");
            _registerFangs = AccessTools.Method(controller, "registerFangs");
            // KKAPI's base class has two OnReload overloads; EditFangs overrides only one.
            var onReload = AccessTools.DeclaredMethod(controller, "OnReload");
            if (_headId == null || _rotationPivots == null || _tips == null || _registered == null || _registerFangs == null || onReload == null)
            {
                Logger.LogError("EditFangsController members not found, plugin disabled");
                return;
            }
            new Harmony(GUID).Patch(onReload, new HarmonyMethod(typeof(EditFangsHeadFixPlugin), nameof(OnReloadPrefix)));
        }

        private static void OnReloadPrefix(Component __instance)
        {
            try
            {
                var chara = __instance.GetComponent<ChaControl>();
                if (chara == null || chara.objHead == null)
                    return;
                var canine = chara.objHead.transform.Find(CaninePath);
                var renderer = canine == null ? null : canine.GetComponentInChildren<SkinnedMeshRenderer>();
                // A clone is this head's mesh already adjusted by EditFangs, so the cache is still correct.
                if (renderer == null || renderer.sharedMesh == null || renderer.sharedMesh.name.EndsWith("(Clone)"))
                    return;
                // EditFangs ignores other fang meshes, and its cache only holds this many vertices.
                var mesh = renderer.sharedMesh;
                if (mesh.vertexCount != FangVertexCount)
                    return;

                Array.Clear((int[])_rotationPivots.GetValue(__instance), 0, 2);
                _headId.SetValue(__instance, int.MinValue);
                try
                {
                    _registerFangs.Invoke(__instance, null);
                }
                catch (TargetInvocationException ex) when (ex.InnerException is ArgumentOutOfRangeException)
                {
                    // registerFangs needs two vertices at exactly the same lowest height. It throws without that, but only
                    // after filling the cache and the pivots. The tips are worked out below either way.
                }
                FindTips(mesh, (int[])_tips.GetValue(__instance));
                _registered.SetValue(__instance, true);
                _headId.SetValue(__instance, chara.fileFace.headId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Could not re-read the fang vertices: " + ex);
            }
        }

        // EditFangs treats the first half of the vertices as one fang and the second half as the other.
        private static void FindTips(Mesh mesh, int[] tips)
        {
            var vertices = mesh.vertices;
            var half = vertices.Length / 2;
            tips[0] = 0;
            tips[1] = half;
            for (var i = 1; i < vertices.Length; i++)
            {
                var side = i < half ? 0 : 1;
                if (vertices[i].y < vertices[tips[side]].y)
                    tips[side] = i;
            }
        }
    }
}
