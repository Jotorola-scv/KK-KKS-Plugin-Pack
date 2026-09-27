// Pseudo Maker Extras - rows for Studio Pseudo Maker (rikkibalboa) that the character maker has but Pseudo Maker lacks.
// Each feature only turns on when the plugin it talks to is installed:
//   * Clothing BlendShape (nakay):   per-item mesh sliders (UpL1/UpR1...) on the clothing panels, with Timeline support
//                                     (merged from the former "Pseudo Maker Clothing BlendShape" 1.1.0).
//   * Single Side Clothes:            Both / Left / Right on the Gloves, Pantyhose, Legwear and Shoes panels and on
//                                     accessory slots of the supported types.
//   * Face > Mouth:                   Fang (double tooth) toggle (vanilla), plus EditFangs' (Njaecha) left/right fang
//                                     length and spacing sliders when EditFangs is installed.
//   * ABMX:                           the maker's yellow bone sliders (face, body, bottom) with "Split XYZ scale
//                                     sliders" and "Side to edit" (Pseudo Maker itself requires ABMX).
//   * Accessory transfer:             a "Move" button next to Pseudo Maker's "Copy".
// Values are always written through the owning plugin's own controller, so they are saved with the character/scene
// exactly as if they had been edited in the maker. Rows are added inside the panels' Initialize, while Pseudo Maker's
// row templates still exist.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using KK_ClothingBlendShape;
using KKAPI.Utilities;
using PseudoMaker;
using PseudoMaker.UI;
using Studio;
using UnityEngine;
using UnityEngine.UI;

namespace PseudoMakerExtras
{
    [BepInPlugin(GUID, PluginName, Version)]
    [BepInProcess("CharaStudio")]
    [BepInDependency("com.rikkibalboa.bepinex.studioPseudoMaker")]
    [BepInDependency(ClothingBlendShapeGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(SingleSideGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(EditFangsGuid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInIncompatibility("local.pseudomaker.clothingblendshape")]   // merged into this plugin; remove the old dll
    public class Plugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.pseudomakerextras";
        public const string PluginName = "Pseudo Maker Extras";
        public const string Version = "1.3.0";
        internal const string ClothingBlendShapeGuid = "nakay.kk.ClothingBlendShape";
        internal const string SingleSideGuid = "jotorola.singlesideclothes";
        internal const string EditFangsGuid = "org.njaecha.plugins.editfangs";

        internal static ManualLogSource Log;
        internal static bool SingleSide, EditFangsInstalled;

        private void Awake()
        {
            Log = Logger;
            string missing = ExtrasAccess.Init();
            if (missing != null)
            {
                Log.LogError("Unsupported Pseudo Maker version, missing member: " + missing);
                return;
            }
            SingleSide = Chainloader.PluginInfos.ContainsKey(SingleSideGuid);
            EditFangsInstalled = Chainloader.PluginInfos.ContainsKey(EditFangsGuid);
            var harmony = Harmony.CreateAndPatchAll(typeof(ExtrasHooks), GUID);
            if (Chainloader.PluginInfos.ContainsKey(ClothingBlendShapeGuid)) BlendShapeModule.Enable(harmony);
            if (SingleSide) SingleSideRows.HookTransfers();
            Log.LogInfo("Features: ClothingBlendShape=" + BlendShapeModule.Ready + ", SingleSideClothes=" + SingleSide
                        + ", Fang=on, EditFangs=" + EditFangsInstalled + ", AccessoryMove=" + ExtrasAccess.MoveAvailable);
        }

        private void Start()
        {
            // Timeline can only be queried once every plugin has finished loading.
            if (BlendShapeModule.Ready) BlendShapeModule.RegisterTimeline();
        }

        internal static ChaControl SelectedCharacter()
        {
            PseudoMakerSceneController scene = PseudoMakerSceneController.Instance;
            return scene != null ? scene.SelectedCharacter : null;
        }
    }

    /// <summary>Clothing BlendShape part; only called when that plugin is installed (its types resolve lazily).</summary>
    internal static class BlendShapeModule
    {
        internal static bool Ready;

        internal static void Enable(Harmony harmony)
        {
            string missing = BSAccess.Init();
            if (missing != null)
            {
                Plugin.Log.LogError("Unsupported Clothing BlendShape version, missing member: " + missing);
                return;
            }
            harmony.PatchAll(typeof(BSHooks));
            BSHooks.PatchPseudoMakerSelection(harmony);
            Ready = true;
        }

        internal static void RegisterTimeline()
        {
            try { BlendShapeTimeline.Register(); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }
    }

    internal static class ExtrasAccess
    {
        internal static FieldInfo ClothingMESplitter, AccessoryMESplitter, CurrentAccessoryNr;

        // Accessory transfer panel (for Move); optional - Move is skipped if any is missing.
        internal static FieldInfo TransferFrom, TransferTo, TransferRows;
        internal static MethodInfo EditTransferRow, OnChangeAcs, SetAccessory;
        internal static Type CharaControllerType;   // PseudoMaker.PseudoMakerCharaController is internal
        internal static bool MoveAvailable;

        internal static string Init()
        {
            ClothingMESplitter = AccessTools.Field(typeof(ClothingEditorPanel), "MaterialEditorSplitter");
            AccessoryMESplitter = AccessTools.Field(typeof(AccessoryEditorPanel), "MaterialEditorSplitter");
            CurrentAccessoryNr = AccessTools.Field(typeof(AccessoryEditorPanel), "currentAccessoryNr");

            TransferFrom = AccessTools.Field(typeof(AccessoryTransferPanel), "fromSlotNr");
            TransferTo = AccessTools.Field(typeof(AccessoryTransferPanel), "toSlotNr");
            TransferRows = AccessTools.Field(typeof(AccessoryTransferPanel), "transferComponents");
            EditTransferRow = AccessTools.Method(typeof(AccessoryTransferPanel), "EditTransferRow");
            OnChangeAcs = AccessTools.Method(typeof(KKAPI.Maker.AccessoriesApi), "OnChangeAcs");
            CharaControllerType = typeof(PseudoMakerSceneController).Assembly.GetType("PseudoMaker.PseudoMakerCharaController");
            SetAccessory = CharaControllerType == null ? null
                : AccessTools.Method(CharaControllerType, "SetAccessory", new[] { typeof(int), typeof(int), typeof(int), typeof(bool) });
            MoveAvailable = TransferFrom != null && TransferTo != null && TransferRows != null && EditTransferRow != null
                            && OnChangeAcs != null && SetAccessory != null;
            if (!MoveAvailable) Plugin.Log.LogWarning("Accessory transfer panel members not found; the Move button is disabled.");

            if (CurrentAccessoryNr == null) return "AccessoryEditorPanel.currentAccessoryNr";
            return null;
        }

        internal static int CurrentAccessory()
        {
            return CurrentAccessoryNr == null ? -1 : (int)CurrentAccessoryNr.GetValue(null);
        }

        // Put a new row right above the given Material Editor splitter (else leave it at the end).
        internal static void PlaceAbove(GameObject row, GameObject anchor)
        {
            if (row == null || anchor == null || row.transform.parent != anchor.transform.parent) return;
            row.transform.SetSiblingIndex(anchor.transform.GetSiblingIndex());
        }
    }

    internal static class ExtrasHooks
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClothingEditorPanel), "InitializeClothing")]
        private static void ClothingPostfix(ClothingEditorPanel __instance)
        {
            if (Plugin.SingleSide)
            {
                try { SingleSideRows.AddClothing(__instance); }
                catch (Exception e) { Plugin.Log.LogError(e); }
            }
            try { AbmxRows.Add(__instance, __instance.SubCategory); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(BodyEditorPanel), "Initialize")]
        private static void BodyPostfix(BodyEditorPanel __instance)
        {
            try { AbmxRows.Add(__instance, __instance.SubCategory); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AccessoryEditorPanel), "Initialize")]
        private static void AccessoryPostfix(AccessoryEditorPanel __instance)
        {
            if (!Plugin.SingleSide) return;
            try { SingleSideRows.AddAccessory(__instance); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(AccessoryTransferPanel), "Initialize")]
        private static void TransferPostfix(AccessoryTransferPanel __instance)
        {
            if (!ExtrasAccess.MoveAvailable) return;
            try { AccessoryMove.AddButton(__instance); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(FaceEditorPanel), "Initialize")]
        private static void FacePostfix(FaceEditorPanel __instance)
        {
            if (__instance.SubCategory == SubCategory.FaceMouth)
            {
                try { FangRows.Add(__instance); }
                catch (Exception e) { Plugin.Log.LogError(e); }
            }
            try { AbmxRows.Add(__instance, __instance.SubCategory); }
            catch (Exception e) { Plugin.Log.LogError(e); }
        }
    }

    /// <summary>
    /// Accessory transfer panel: adds "Move" next to Pseudo Maker's own "Copy". Move = Pseudo Maker's Copy (same steps,
    /// including A12 and KKAPI's AccessoryTransferred event so Material Editor & co. copy their data), then the source
    /// slot is emptied the way Pseudo Maker removes an accessory (type None + AccessoryKindChanged, so plugins drop the
    /// source slot's data).
    /// </summary>
    internal static class AccessoryMove
    {
        private const int TypeNone = 120;   // ChaListDefine.CategoryNo.ao_none

        internal static void AddButton(AccessoryTransferPanel panel)
        {
            // Pseudo Maker adds its "Copy" row in Initialize; its buttons are only created in Start, so extend its map.
            foreach (ButtonGroupComponent group in panel.GetComponentsInChildren<ButtonGroupComponent>(true))
            {
                if (group.ButtonsMap == null || !group.ButtonsMap.ContainsKey("Copy") || group.ButtonsMap.ContainsKey("Move")) continue;
                group.ButtonsMap.Add("Move", () =>
                {
                    try { Move(panel); }
                    catch (Exception e) { Plugin.Log.LogError(e); }
                });
                return;
            }
            Plugin.Log.LogWarning("Accessory transfer panel: Copy row not found; Move not added.");
        }

        private static void Move(AccessoryTransferPanel panel)
        {
            ChaControl cha = Plugin.SelectedCharacter();
            if (cha == null) return;
            int from = (int)ExtrasAccess.TransferFrom.GetValue(panel);
            int to = (int)ExtrasAccess.TransferTo.GetValue(panel);
            ChaFileAccessory.PartsInfo[] parts = cha.nowCoordinate.accessory.parts;
            if (from == to || from < 0 || to < 0 || from >= parts.Length || to >= parts.Length) return;
            if (parts[from].type == TypeNone) return;   // nothing to move

            // 1. Same as Pseudo Maker's Copy button.
            Compatibility.A12.TransferAccessoryBefore(to);
            byte[] bytes = MessagePack.MessagePackSerializer.Serialize(parts[from]);
            parts[to] = MessagePack.MessagePackSerializer.Deserialize<ChaFileAccessory.PartsInfo>(bytes);
            cha.AssignCoordinate((ChaFileDefine.CoordinateType)cha.fileStatus.coordinateType);
            cha.Reload(false, true, true, true);
            ExtrasAccess.OnChangeAcs.Invoke(null, new object[] { panel, from, to });
            Compatibility.A12.TransferAccessoryAfter();

            // 2. Empty the source slot the way Pseudo Maker's own "Type: None" does.
            Component controller = cha.GetComponent(ExtrasAccess.CharaControllerType);
            if (controller != null) ExtrasAccess.SetAccessory.Invoke(controller, new object[] { from, TypeNone, 0, false });
            if (Plugin.SingleSide) SingleSideRows.ClearAccessoryMode(cha, from);

            // 3. Refresh the transfer list and the slot names.
            var rows = ExtrasAccess.TransferRows.GetValue(panel) as IList;
            if (rows != null)
            {
                if (from < rows.Count) ExtrasAccess.EditTransferRow.Invoke(panel, new object[] { rows[from], from });
                if (to < rows.Count) ExtrasAccess.EditTransferRow.Invoke(panel, new object[] { rows[to], to });
            }
            AccessoryPanel.UpdateSlotName(from);
            AccessoryPanel.UpdateSlotName(to);
        }
    }

    /// <summary>Face > Mouth: vanilla fang toggle, plus EditFangs' four sliders when that plugin is installed.</summary>
    internal static class FangRows
    {
        internal static void Add(FaceEditorPanel panel)
        {
            ToggleComponent toggle = panel.AddToggleRow("Fang", SetFang, GetFang);
            // Mouth panel ends with: ..., Lip Gloss, splitter, Material Editor button. Go before that splitter.
            Transform content = toggle.transform.parent;
            int index = content.childCount >= 3 ? content.childCount - 3 : content.childCount - 1;
            toggle.transform.SetSiblingIndex(index);
            toggle.gameObject.AddComponent<RowUpdater>().Refresh = () => toggle.UpdateValue(GetFang());
            if (Plugin.EditFangsInstalled) EditFangsRows.Add(panel, index + 1);
        }

        private static bool GetFang()
        {
            ChaControl cha = Plugin.SelectedCharacter();
            return cha != null && cha.fileFace.doubleTooth;
        }

        private static void SetFang(bool value)
        {
            ChaControl cha = Plugin.SelectedCharacter();
            if (cha == null || cha.fileFace.doubleTooth == value) return;
            cha.fileFace.doubleTooth = value;
            cha.VisibleDoubleTooth();
        }
    }

    /// <summary>EditFangs (Njaecha): same four sliders, ranges and defaults as its maker controls.</summary>
    internal static class EditFangsRows
    {
        // 0 left length, 1 left spacing, 2 right length, 3 right spacing
        private static readonly string[] Names = { "Left Fang Length", "Left Fang Spacing", "Right Fang Length", "Right Fang Spacing" };
        private static readonly float[] Max = { 1f, 1.3f, 1f, 1.3f };
        private static readonly float[] Default = { 0.1f, 1f, 0.1f, 1f };

        private static EditFangs.EditFangsController Controller()
        {
            ChaControl cha = Plugin.SelectedCharacter();
            return cha == null ? null : cha.GetComponent<EditFangs.EditFangsController>();
        }

        private static float Get(int which)
        {
            var c = Controller();
            if (c == null || c.fangData == null) return Default[which];
            switch (which)
            {
                case 0: return c.fangData.scaleL;
                case 1: return c.fangData.spacingL;
                case 2: return c.fangData.scaleR;
                default: return c.fangData.spacingR;
            }
        }

        private static void Set(int which, float value)
        {
            var c = Controller();
            if (c == null || c.fangData == null || Mathf.Approximately(Get(which), value)) return;
            float scaleL = c.fangData.scaleL, spacingL = c.fangData.spacingL, scaleR = c.fangData.scaleR, spacingR = c.fangData.spacingR;
            switch (which)
            {
                case 0: scaleL = value; break;
                case 1: spacingL = value; break;
                case 2: scaleR = value; break;
                default: spacingR = value; break;
            }
            // The maker re-applies after spacing changes (readjust), not after length changes; do the same.
            c.adjustFang(scaleL, spacingL, scaleR, spacingR, which == 1 || which == 3);
        }

        internal static void Add(FaceEditorPanel panel, int index)
        {
            for (int i = 0; i < Names.Length; i++)
            {
                int which = i;
                SliderComponent slider = panel.AddSliderRow(Names[which], () => Get(which), () => Default[which],
                    v => Set(which, v), () => Set(which, Default[which]), 0f, Max[which]);
                slider.transform.SetSiblingIndex(index + i);
                slider.gameObject.AddComponent<RowUpdater>().Refresh = () => slider.UpdateValue(Get(which));
            }
        }
    }

    /// <summary>Single Side Clothes rows. Only touched when the plugin is installed (types resolve lazily).</summary>
    internal static class SingleSideRows
    {
        private static readonly List<string> Labels = new List<string> { "Both", "Left", "Right" };

        // Single Side Clothes parts: 0 gloves, 1 pantyhose, 2 legwear, 3 shoes (indoor + outdoor).
        private static int PartOf(SubCategory sub)
        {
            switch (sub.ToString())
            {
                case "ClothingGloves": return 0;
                case "ClothingPantyhose": return 1;
                case "ClothingLegwear": return 2;
                case "ClothingShoesInDoors":
                case "ClothingShoes":
                case "ClothingShoesOutdoors": return 3;
                default: return -1;
            }
        }

        // Accessory types Single Side Clothes handles: ClothesToAccessories gloves..shoes (109-112), Leg (127), Arm (128).
        private static bool IsSideType(int type)
        {
            return (type >= 109 && type <= 112) || type == 127 || type == 128;
        }

        private static SingleSideClothes.SingleSideController Controller()
        {
            ChaControl cha = Plugin.SelectedCharacter();
            return cha == null ? null : cha.GetComponent<SingleSideClothes.SingleSideController>();
        }

        // Single Side Clothes keeps its accessory mode per slot; in the maker it follows transfers itself, but in Studio
        // nothing does. Pseudo Maker's Copy and our Move both raise AccessoryTransferred with the transfer panel as sender.
        internal static void HookTransfers()
        {
            KKAPI.Maker.AccessoriesApi.AccessoryTransferred += (sender, e) =>
            {
                if (!(sender is AccessoryTransferPanel)) return;
                try
                {
                    ChaControl cha = Plugin.SelectedCharacter();
                    var c = cha == null ? null : cha.GetComponent<SingleSideClothes.SingleSideController>();
                    if (c != null) c.CopyAccMode(c.OutfitIndex, e.SourceSlotIndex, c.OutfitIndex, e.DestinationSlotIndex);
                }
                catch (Exception ex) { Plugin.Log.LogError(ex); }
            };
        }

        internal static void ClearAccessoryMode(ChaControl cha, int slot)
        {
            var c = cha.GetComponent<SingleSideClothes.SingleSideController>();
            if (c != null) c.SetAccMode(slot, SingleSideClothes.SideMode.Both);
        }

        internal static void AddClothing(ClothingEditorPanel panel)
        {
            int part = PartOf(panel.SubCategory);
            if (part < 0) return;
            Func<int> get = () =>
            {
                var c = Controller();
                return c == null ? 0 : (int)c.GetCurrentMode(part);
            };
            ToggleGroupComponent row = panel.AddToggleGroupRow("Single side", Labels, v =>
            {
                var c = Controller();
                if (c != null) c.SetCurrentMode(part, (SingleSideClothes.SideMode)v);
            }, get, () => Labels.Count);
            ExtrasAccess.PlaceAbove(row.gameObject, ExtrasAccess.ClothingMESplitter == null ? null : ExtrasAccess.ClothingMESplitter.GetValue(panel) as GameObject);
            row.gameObject.AddComponent<RowUpdater>().Refresh = () => row.UpdateValue(get());
        }

        internal static void AddAccessory(AccessoryEditorPanel panel)
        {
            Func<int> get = () =>
            {
                var c = Controller();
                int slot = ExtrasAccess.CurrentAccessory();
                return c == null || slot < 0 ? 0 : (int)c.GetAccMode(slot);
            };
            ToggleGroupComponent row = panel.AddToggleGroupRow("Single side", Labels, v =>
            {
                var c = Controller();
                int slot = ExtrasAccess.CurrentAccessory();
                if (c != null && slot >= 0) c.SetAccMode(slot, (SingleSideClothes.SideMode)v);
            }, get, () => Labels.Count);
            ExtrasAccess.PlaceAbove(row.gameObject, ExtrasAccess.AccessoryMESplitter == null ? null : ExtrasAccess.AccessoryMESplitter.GetValue(panel) as GameObject);
            var updater = panel.gameObject.AddComponent<RowUpdater>();   // on the panel: the row itself may be hidden
            updater.Refresh = () =>
            {
                ChaControl cha = Plugin.SelectedCharacter();
                int slot = ExtrasAccess.CurrentAccessory();
                var parts = cha == null || cha.nowCoordinate == null ? null : cha.nowCoordinate.accessory.parts;
                bool show = parts != null && slot >= 0 && slot < parts.Length && IsSideType(parts[slot].type);
                if (row.gameObject.activeSelf != show) row.gameObject.SetActive(show);
                if (show) row.UpdateValue(get());
            };
            updater.WatchAccessory = true;
        }
    }

    /// <summary>
    /// ABMX "yellow" bone sliders (InterfaceData.BoneControls) that the maker shows but Pseudo Maker does not, with ABMX's
    /// "Split XYZ scale sliders" switch. Values are read/written on the character's ABMX BoneController exactly like
    /// ABMX's own maker sliders (per-outfit bones, left/right pairs with "Side to edit"). Pseudo Maker hard-depends on
    /// ABMX, so this is always available. The maker's finger control (with its finger selector) is not included.
    /// </summary>
    internal static class AbmxRows
    {
        private static readonly List<AbmxBoneRow> Rows = new List<AbmxBoneRow>();

        // ABMX maker category -> Pseudo Maker panel ("category/subcategory" -> panel, label for built-in categories).
        private static readonly Dictionary<string, SubCategory> PanelOf = new Dictionary<string, SubCategory>
        {
            { "00_FaceTop/tglAll", SubCategory.FaceGeneral }, { "00_FaceTop/tglHeadABM", SubCategory.FaceGeneral },
            { "00_FaceTop/tglEar", SubCategory.FaceEars }, { "00_FaceTop/tglChin", SubCategory.FaceJaw },
            { "00_FaceTop/tglCheek", SubCategory.FaceCheeks }, { "00_FaceTop/tglEyebrow", SubCategory.FaceEyebrows },
            { "00_FaceTop/tglEye01", SubCategory.FaceEyes }, { "00_FaceTop/tglEye02ABM", SubCategory.FaceEyes },
            { "00_FaceTop/tglEyelashUpABM", SubCategory.FaceEyes }, { "00_FaceTop/tglEyelashLoABM", SubCategory.FaceEyes },
            { "00_FaceTop/tglNose", SubCategory.FaceNose },
            { "00_FaceTop/tglMouth", SubCategory.FaceMouth }, { "00_FaceTop/tglMouth2ABM", SubCategory.FaceMouth },
            { "01_BodyTop/tglAll", SubCategory.BodyGeneral },
            { "01_BodyTop/tglBreast", SubCategory.BodyChest }, { "01_BodyTop/tglBreast2ABM", SubCategory.BodyChest },
            { "01_BodyTop/tglNipplesABM", SubCategory.BodyChest },
            { "01_BodyTop/tglUpper", SubCategory.BodyUpper }, { "01_BodyTop/tglUpper2ABM", SubCategory.BodyUpper },
            { "01_BodyTop/tglLower", SubCategory.BodyLower }, { "01_BodyTop/tglLower2ABM", SubCategory.BodyLower },
            { "01_BodyTop/tglArm", SubCategory.BodyArms }, { "01_BodyTop/tglArm2ABM", SubCategory.BodyArms },
            { "01_BodyTop/tglForearmsABM", SubCategory.BodyArms }, { "01_BodyTop/tglHandsABM", SubCategory.BodyArms },
            { "01_BodyTop/tglLeg", SubCategory.BodyLegs }, { "01_BodyTop/tglThighsABM", SubCategory.BodyLegs },
            { "01_BodyTop/tglFeetABM", SubCategory.BodyLegs },
            { "01_BodyTop/tglUnderhair", SubCategory.BodyPubicHair }, { "01_BodyTop/tglGenitalsABM", SubCategory.BodyPubicHair },
            { "03_ClothesTop/tglBot", SubCategory.ClothingBottom }, { "03_ClothesTop/tglSkirtSclABM", SubCategory.ClothingBottom },
        };

        private static readonly Dictionary<string, string> BuiltInLabel = new Dictionary<string, string>
        {
            { "00_FaceTop/tglAll", "Face" }, { "00_FaceTop/tglEar", "Ears" }, { "00_FaceTop/tglChin", "Chin" },
            { "00_FaceTop/tglCheek", "Cheeks" }, { "00_FaceTop/tglEyebrow", "Eyebrows" }, { "00_FaceTop/tglEye01", "Eyes" },
            { "00_FaceTop/tglNose", "Nose" }, { "00_FaceTop/tglMouth", "Mouth" }, { "01_BodyTop/tglAll", "Body" },
            { "01_BodyTop/tglBreast", "Chest" }, { "01_BodyTop/tglUpper", "Upper Body" }, { "01_BodyTop/tglLower", "Lower Body" },
            { "01_BodyTop/tglArm", "Arms" }, { "01_BodyTop/tglLeg", "Legs" }, { "01_BodyTop/tglUnderhair", "Pubic Hair" },
            { "03_ClothesTop/tglBot", "Bottom" },
        };

        private static string Key(KKAPI.Maker.MakerCategory c)
        {
            return c == null ? "" : c.CategoryName + "/" + c.SubCategoryName;
        }

        internal static void Add(BaseEditorPanel panel, SubCategory sub)
        {
            var metas = KKABMX.GUI.InterfaceData.BoneControls
                .Where(m => { SubCategory s; return PanelOf.TryGetValue(Key(m.Category), out s) && s == sub; }).ToList();
            if (metas.Count == 0) return;

            panel.AddSplitter();
            panel.AddHeader("ABMX");
            ToggleComponent xyz = panel.AddToggleRow("Split XYZ scale sliders", v =>
            {
                if (KKABMX.GUI.KKABMX_GUI.XyzMode == v) return;
                KKABMX.GUI.KKABMX_GUI.XyzMode = v;   // ABMX's own setting, shared with the maker
                RefreshAll(false);
            }, () => KKABMX.GUI.KKABMX_GUI.XyzMode);
            xyz.gameObject.AddComponent<RowUpdater>().Refresh = () => xyz.UpdateValue(KKABMX.GUI.KKABMX_GUI.XyzMode);

            string lastKey = null;
            foreach (var meta in metas)
            {
                string key = Key(meta.Category);
                if (key != lastKey)
                {
                    string label;
                    if (!BuiltInLabel.TryGetValue(key, out label)) label = meta.Category.DisplayName ?? meta.Category.SubCategoryName;
                    panel.AddHeader(label);
                    lastKey = key;
                }
                if (meta.IsSeparator) { panel.AddSplitter(); continue; }
                Rows.Add(new AbmxBoneRow(panel, meta));
            }
        }

        internal static void RefreshAll(bool characterChanged)
        {
            Rows.RemoveAll(r => !r.Alive);
            foreach (var r in Rows) r.Refresh(characterChanged);
        }
    }

    internal sealed class AbmxBoneRow
    {
        private static readonly List<string> SideLabels = new List<string> { "Both", "Left", "Right" };

        private readonly KKABMX.GUI.BoneMeta _meta;
        private readonly ToggleGroupComponent _sideRow;
        private readonly SliderComponent _x, _y, _z, _v, _l;
        private int _side;          // 0 both, 1 left (BoneName), 2 right (RightBoneName)
        private bool _refreshing;
        private readonly GameObject _anchor;   // Unity null check: rows die with their panel

        internal bool Alive { get { return _anchor != null; } }

        internal AbmxBoneRow(BaseEditorPanel panel, KKABMX.GUI.BoneMeta meta)
        {
            _meta = meta;
            float max = KKABMX.GUI.KKABMX_GUI.RaiseLimits ? meta.Max * 2f : meta.Max;
            float lMax = KKABMX.GUI.KKABMX_GUI.RaiseLimits ? meta.LMax * 2f : meta.LMax;
            if (!string.IsNullOrEmpty(meta.RightBoneName))
                _sideRow = panel.AddToggleGroupRow("Side to edit", SideLabels, v =>
                {
                    if (_refreshing || _side == v) return;
                    _side = v;
                    Refresh(false);
                }, () => _side, () => SideLabels.Count);
            if (meta.X) _x = panel.AddSliderRow(meta.XDisplayName, () => GetScale(0), () => 1f, v => SetScale(0, v), () => SetScale(0, 1f), meta.Min, max);
            if (meta.Y) _y = panel.AddSliderRow(meta.YDisplayName, () => GetScale(1), () => 1f, v => SetScale(1, v), () => SetScale(1, 1f), meta.Min, max);
            if (meta.Z) _z = panel.AddSliderRow(meta.ZDisplayName, () => GetScale(2), () => 1f, v => SetScale(2, v), () => SetScale(2, 1f), meta.Min, max);
            if (meta.X && meta.Y && meta.Z)
                _v = panel.AddSliderRow(meta.DisplayName + meta.XYZPostfix, () => GetScale(3), () => 1f, v => SetScale(3, v), () => SetScale(3, 1f), meta.Min, max);
            if (meta.L) _l = panel.AddSliderRow(meta.LDisplayName, GetLength, () => 1f, SetLength, () => SetLength(1f), meta.LMin, lMax);

            _anchor = _sideRow != null ? _sideRow.gameObject : (_x ?? _v ?? _l ?? _y ?? _z).gameObject;
            _anchor.AddComponent<RowUpdater>().Refresh = () => Refresh(true);
            UpdateVisibility();
        }

        private static KKABMX.Core.BoneController Controller()
        {
            ChaControl cha = Plugin.SelectedCharacter();
            return cha == null ? null : cha.GetComponent<KKABMX.Core.BoneController>();
        }

        // Same lookup as ABMX's maker GUI (GetBoneModifier), optionally creating the modifier.
        private KKABMX.Core.BoneModifierData Data(KKABMX.Core.BoneController c, string bone, bool create)
        {
            var mod = c.GetModifier(bone, KKABMX.Core.BoneLocation.BodyTop);
            if (mod == null)
            {
                if (!create) return null;
                c.AddModifier(new KKABMX.Core.BoneModifier(bone, KKABMX.Core.BoneLocation.BodyTop));
                mod = c.GetModifier(bone, KKABMX.Core.BoneLocation.BodyTop);
                if (mod == null) return null;
            }
            if (_meta.UniquePerCoordinate) mod.MakeCoordinateSpecific(c.ChaFileControl.coordinate.Length);
            return mod.GetModifier(c.CurrentCoordinate.Value);
        }

        private string DisplayBone { get { return _side == 2 && !string.IsNullOrEmpty(_meta.RightBoneName) ? _meta.RightBoneName : _meta.BoneName; } }

        private IEnumerable<string> Targets()
        {
            if (string.IsNullOrEmpty(_meta.RightBoneName) || _side == 1) { yield return _meta.BoneName; yield break; }
            if (_side == 2) { yield return _meta.RightBoneName; yield break; }
            yield return _meta.BoneName;
            yield return _meta.RightBoneName;
        }

        private Vector3 Scale()
        {
            var c = Controller();
            var d = c == null ? null : Data(c, DisplayBone, false);
            return d != null ? d.ScaleModifier : Vector3.one;
        }

        private float GetScale(int axis)
        {
            Vector3 s = Scale();
            return axis == 0 ? s.x : axis == 1 ? s.y : axis == 2 ? s.z : (s.x + s.y + s.z) / 3f;
        }

        private void SetScale(int axis, float value)
        {
            var c = Controller();
            if (c == null || _refreshing) return;
            var src = Data(c, DisplayBone, true);
            if (src == null) return;
            Vector3 s = src.ScaleModifier;
            if (axis == 0) s.x = value;
            else if (axis == 1) s.y = value;
            else if (axis == 2) s.z = value;
            else s = new Vector3(value, value, value);
            foreach (string bone in Targets())
            {
                var d = Data(c, bone, true);
                if (d != null) d.ScaleModifier = s;
            }
            Refresh(false);
        }

        private float GetLength()
        {
            var c = Controller();
            var d = c == null ? null : Data(c, DisplayBone, false);
            return d != null ? d.LengthModifier : 1f;
        }

        private void SetLength(float value)
        {
            var c = Controller();
            if (c == null || _refreshing) return;
            foreach (string bone in Targets())
            {
                var d = Data(c, bone, true);
                if (d != null) d.LengthModifier = value;
            }
        }

        internal void Refresh(bool characterChanged)
        {
            if (!Alive || _refreshing) return;
            _refreshing = true;
            try
            {
                // Like ABMX: a pair whose sides differ opens on "Left"; an even pair opens on "Both".
                if (characterChanged && _sideRow != null)
                {
                    var c = Controller();
                    var l = c == null ? null : Data(c, _meta.BoneName, false);
                    var r = c == null ? null : Data(c, _meta.RightBoneName, false);
                    Vector3 ls = l != null ? l.ScaleModifier : Vector3.one, rs = r != null ? r.ScaleModifier : Vector3.one;
                    if (ls != rs) { if (_side == 0) _side = 1; }
                    else _side = 0;
                }
                if (_sideRow != null) _sideRow.UpdateValue(_side);
                if (_x != null) _x.UpdateValue(GetScale(0));
                if (_y != null) _y.UpdateValue(GetScale(1));
                if (_z != null) _z.UpdateValue(GetScale(2));
                if (_v != null) _v.UpdateValue(GetScale(3));
                if (_l != null) _l.UpdateValue(GetLength());
            }
            finally { _refreshing = false; }
            UpdateVisibility();
        }

        // ABMX shows X/Y/Z when "split" is on, when the bone lacks an all-axes slider, or when the axes differ.
        private void UpdateVisibility()
        {
            Vector3 s = Scale();
            bool split = KKABMX.GUI.KKABMX_GUI.XyzMode || _v == null
                         || !(Mathf.Approximately(s.x, s.y) && Mathf.Approximately(s.x, s.z));
            SetActive(_x, split);
            SetActive(_y, split);
            SetActive(_z, split);
            SetActive(_v, !split);
        }

        private static void SetActive(SliderComponent slider, bool active)
        {
            if (slider != null && slider.gameObject.activeSelf != active) slider.gameObject.SetActive(active);
        }
    }

    /// <summary>Re-reads a row when the selected character, outfit or accessory slot/type changes.</summary>
    internal class RowUpdater : MonoBehaviour
    {
        internal Action Refresh;
        internal bool WatchAccessory;

        private ChaControl _cha;
        private int _coordinate = -2, _slot = -2, _type = -2;
        private bool _dirty = true;

        private void OnEnable() { _dirty = true; }

        private void LateUpdate()
        {
            ChaControl cha = Plugin.SelectedCharacter();
            int coordinate = cha != null ? cha.fileStatus.coordinateType : -1;
            int slot = -1, type = -1;
            if (WatchAccessory)
            {
                slot = ExtrasAccess.CurrentAccessory();
                var parts = cha == null || cha.nowCoordinate == null ? null : cha.nowCoordinate.accessory.parts;
                type = parts != null && slot >= 0 && slot < parts.Length ? parts[slot].type : -1;
            }
            if (!_dirty && ReferenceEquals(cha, _cha) && coordinate == _coordinate && slot == _slot && type == _type) return;
            _dirty = false;
            _cha = cha;
            _coordinate = coordinate;
            _slot = slot;
            _type = type;
            try { if (Refresh != null) Refresh(); }
            catch (Exception e) { Plugin.Log.LogError(e); Refresh = null; }
        }
    }

    // ===================== Clothing BlendShape (from Pseudo Maker Clothing BlendShape 1.1.0) =====================

    internal static class BSHooks
    {
        // Runs inside BaseEditorPanel.Initialize, while the row templates still exist.
        [HarmonyPostfix]
        [HarmonyPatch(typeof(ClothingEditorPanel), "InitializeClothing")]
        private static void InitializeClothingPostfix(ClothingEditorPanel __instance)
        {
            try
            {
                BlendShapeRows.Build(__instance);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        // Pseudo Maker shows one selected slider at a time in Timeline; selecting one of its own sliders deselects ours.
        internal static void PatchPseudoMakerSelection(Harmony harmony)
        {
            if (BSAccess.PseudoMakerTimeline == null) return;
            var postfix = new HarmonyMethod(typeof(BSHooks), nameof(PseudoMakerSelectionPostfix));
            foreach (string property in new[] { "SelectedFloatType", "SelectedColorType", "SelectedBodyShape", "SelectedFaceShape", "SelectedAccessory" })
            {
                MethodInfo setter = AccessTools.PropertySetter(BSAccess.PseudoMakerTimeline, property);
                if (setter != null) harmony.Patch(setter, postfix: postfix);
            }
        }

        private static void PseudoMakerSelectionPostfix(object[] __args)
        {
            if (__args != null && __args.Length > 0 && __args[0] != null) BlendShapeTimeline.Deselect();
        }
    }

    /// <summary>Reflection over ClothingBlendShape's and Pseudo Maker's internal members.</summary>
    internal static class BSAccess
    {
        // .NET 3.5 (KK) has no five-argument Action.
        internal delegate void SetWeightDelegate(ClothingBlendShapeController ctrl, ListInfoBase listInfo, int clothesKind, int infoIndex, float weight);

        internal static FieldInfo ShapesComp, ShapesCompSub, ClothesDataSet, DataName, MaterialEditorSplitter, DisplayTemplate;
        internal static Func<ClothingBlendShapeController, bool> IsSubParts;
        internal static Func<ChaClothesBlendShape, Array> DataSet;
        internal static Func<ClothingBlendShapeController, ListInfoBase, int, BlendShapeData> GetClothesData;
        internal static Action<ClothingBlendShapeController, ListInfoBase, int> AddClothesData;
        internal static SetWeightDelegate SetClothesDataWeight;
        internal static Action<ClothingBlendShapeController, int> UpdateClothes;

        // Optional: Pseudo Maker's Timeline selection, cleared when one of our sliders is selected.
        internal static Type PseudoMakerTimeline;
        internal static FieldInfo[] PseudoMakerSelections;

        internal static string Init()
        {
            Type ctrl = typeof(ClothingBlendShapeController);
            Type comp = typeof(ChaClothesBlendShape);
            Type data = comp.GetNestedType("Data", BindingFlags.NonPublic | BindingFlags.Public);
            ShapesComp = AccessTools.Field(ctrl, "ShapesComp");
            ShapesCompSub = AccessTools.Field(ctrl, "ShapesCompSub");
            ClothesDataSet = AccessTools.Field(ctrl, "ClothesDataSet");
            IsSubParts = Getter<ClothingBlendShapeController, bool>(AccessTools.Property(ctrl, "IsSubParts"));
            DataSet = Getter<ChaClothesBlendShape, Array>(AccessTools.Property(comp, "DataSet"));
            GetClothesData = Bind<Func<ClothingBlendShapeController, ListInfoBase, int, BlendShapeData>>(AccessTools.Method(ctrl, "GetClothesData"));
            AddClothesData = Bind<Action<ClothingBlendShapeController, ListInfoBase, int>>(AccessTools.Method(ctrl, "AddClothesData"));
            SetClothesDataWeight = Bind<SetWeightDelegate>(AccessTools.Method(ctrl, "SetClothesDataWeight"));
            UpdateClothes = Bind<Action<ClothingBlendShapeController, int>>(AccessTools.Method(ctrl, "UpdateClothes"));
            DataName = data == null ? null : AccessTools.Field(data, "name");
            MaterialEditorSplitter = AccessTools.Field(typeof(ClothingEditorPanel), "MaterialEditorSplitter");
            DisplayTemplate = AccessTools.Field(typeof(SliderComponent), "displayTemplate");

            PseudoMakerTimeline = AccessTools.TypeByName("PseudoMaker.TimelineCompatibilityHelper");
            if (PseudoMakerTimeline != null)
                PseudoMakerSelections = new[] { "_selectedFloatType", "_selectedColorType", "_selectedBodyShape", "_selectedFaceShape" }
                    .Select(n => AccessTools.Field(PseudoMakerTimeline, n)).Where(f => f != null).ToArray();

            if (ShapesComp == null) return "ShapesComp";
            if (ShapesCompSub == null) return "ShapesCompSub";
            if (ClothesDataSet == null) return "ClothesDataSet";
            if (IsSubParts == null) return "IsSubParts";
            if (GetClothesData == null) return "GetClothesData";
            if (AddClothesData == null) return "AddClothesData";
            if (SetClothesDataWeight == null) return "SetClothesDataWeight";
            if (UpdateClothes == null) return "UpdateClothes";
            if (DataSet == null) return "DataSet";
            if (DataName == null) return "Data.name";
            return null;
        }

        private static T Bind<T>(MethodInfo method) where T : class
        {
            if (method == null) return null;
            try
            {
                return Delegate.CreateDelegate(typeof(T), method) as T;
            }
            catch
            {
                return null;
            }
        }

        private static Func<TOwner, TValue> Getter<TOwner, TValue>(PropertyInfo property)
        {
            return property == null ? null : Bind<Func<TOwner, TValue>>(property.GetGetMethod(true));
        }
    }

    /// <summary>Read/write of one Clothing BlendShape slider on any character.</summary>
    internal static class BlendShapes
    {
        internal const float DefaultWeight = 20f;

        private static readonly string[] KindNames = { "Top", "Bottom", "Bra", "Underwear", "Gloves", "Pantyhose", "Legwear", "Indoor Shoes", "Shoes" };

        internal static ClothingBlendShapeController Controller(ChaControl cha)
        {
            return cha != null ? cha.GetComponent<ClothingBlendShapeController>() : null;
        }

        // Tops made of jacket/sailor pieces carry one blendshape component per piece; "group" selects the piece.
        private static bool SubParts(ClothingBlendShapeController ctrl, int kind)
        {
            return kind == 0 && BSAccess.IsSubParts(ctrl);
        }

        internal static ChaClothesBlendShape Component(ChaControl cha, int kind, int group)
        {
            ClothingBlendShapeController ctrl = Controller(cha);
            if (ctrl == null) return null;
            try
            {
                ChaClothesBlendShape[] arr;
                int index;
                if (SubParts(ctrl, kind))
                {
                    arr = BSAccess.ShapesCompSub.GetValue(ctrl) as ChaClothesBlendShape[];
                    index = group;
                }
                else
                {
                    if (group != 0) return null;
                    arr = BSAccess.ShapesComp.GetValue(ctrl) as ChaClothesBlendShape[];
                    index = kind;
                }
                if (arr == null || index >= arr.Length) return null;
                ChaClothesBlendShape comp = arr[index];
                return comp != null ? comp : null;   // Unity null check for destroyed clothes
            }
            catch
            {
                return null;
            }
        }

        internal static string[] Names(ChaClothesBlendShape comp)
        {
            if (comp == null) return null;
            Array dataSet = BSAccess.DataSet(comp);
            if (dataSet == null) return null;
            var names = new string[4];
            for (int i = 0; i < 4 && i < dataSet.Length; i++)
            {
                object d = dataSet.GetValue(i);
                names[i] = d == null ? null : BSAccess.DataName.GetValue(d) as string;
            }
            return names;
        }

        private static ListInfoBase ListInfo(ChaControl cha, ClothingBlendShapeController ctrl, int kind, int group)
        {
            if (SubParts(ctrl, kind)) return group < cha.infoParts.Length ? cha.infoParts[group] : null;
            return kind < cha.infoClothes.Length ? cha.infoClothes[kind] : null;
        }

        internal static float GetWeight(ChaControl cha, int kind, int group, int index)
        {
            try
            {
                ClothingBlendShapeController ctrl = Controller(cha);
                if (ctrl == null) return DefaultWeight;
                ListInfoBase info = ListInfo(cha, ctrl, kind, group);
                if (info == null) return DefaultWeight;
                BlendShapeData data = BSAccess.GetClothesData(ctrl, info, kind);
                return data != null && data.ShapeWeight != null && index < data.ShapeWeight.Length ? data.ShapeWeight[index] : DefaultWeight;
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug(e);
                return DefaultWeight;
            }
        }

        /// <summary>Slider edit from the Pseudo Maker panel.</summary>
        internal static void SetWeight(ChaControl cha, int kind, int group, int index, float value)
        {
            try
            {
                ClothingBlendShapeController ctrl = Controller(cha);
                if (ctrl == null || Component(cha, kind, group) == null) return;
                ListInfoBase info = ListInfo(cha, ctrl, kind, group);
                if (info == null) return;
                // Same bookkeeping the maker does when an item with blendshape sliders is selected.
                BSAccess.AddClothesData(ctrl, info, kind);
                BSAccess.SetClothesDataWeight(ctrl, info, kind, index, value);
                BSAccess.UpdateClothes(ctrl, kind);
            }
            catch (Exception e)
            {
                Plugin.Log.LogError(e);
            }
        }

        /// <summary>Per-frame write from Timeline: only touches the data and meshes when the value actually changes.</summary>
        internal static void ApplyWeight(ChaControl cha, int kind, int group, int index, float value)
        {
            try
            {
                ApplyWeightCore(cha, kind, group, index, value);
            }
            catch (Exception e)
            {
                Plugin.Log.LogDebug(e);
            }
        }

        private static void ApplyWeightCore(ChaControl cha, int kind, int group, int index, float value)
        {
            ClothingBlendShapeController ctrl = Controller(cha);
            if (ctrl == null || index < 0 || index > 3 || Component(cha, kind, group) == null) return;
            ListInfoBase info = ListInfo(cha, ctrl, kind, group);
            if (info == null) return;
            BlendShapeData data = BSAccess.GetClothesData(ctrl, info, kind);
            // GetClothesData hands out a detached default entry when the character has no saved data yet.
            var stored = BSAccess.ClothesDataSet.GetValue(ctrl) as IList;
            if (data == null || stored == null || !stored.Contains(data))
            {
                BSAccess.AddClothesData(ctrl, info, kind);
                data = BSAccess.GetClothesData(ctrl, info, kind);
                if (data == null) return;
            }
            if (data.ShapeWeight[index] == value) return;
            data.ShapeWeight[index] = value;
            BSAccess.UpdateClothes(ctrl, kind);
        }

        internal static string DisplayName(ChaControl cha, int kind, int group, int index)
        {
            string kindName = kind >= 0 && kind < KindNames.Length ? KindNames[kind] : "Clothes";
            ClothingBlendShapeController ctrl = Controller(cha);
            if (ctrl != null && SubParts(ctrl, kind)) kindName += " Part " + (group + 1);
            string[] names = Names(Component(cha, kind, group));
            string slider = names != null && index < names.Length && !string.IsNullOrEmpty(names[index]) ? names[index] : "BlendShape " + (index + 1);
            return kindName + " " + slider;
        }
    }

    /// <summary>Timeline parameter: which clothing slot, which top piece and which of its four sliders.</summary>
    internal sealed class BlendShapeParameter
    {
        internal readonly int Kind, Group, Index;
        internal string Label;

        internal BlendShapeParameter(int kind, int group, int index, string label)
        {
            Kind = kind;
            Group = group;
            Index = index;
            Label = label;
        }

        public override int GetHashCode()
        {
            return ((17 * 31 + Kind) * 31 + Group) * 31 + Index;
        }

        public override bool Equals(object obj)
        {
            var other = obj as BlendShapeParameter;
            return other != null && other.Kind == Kind && other.Group == Group && other.Index == Index;
        }
    }

    internal static class BlendShapeTimeline
    {
        private static bool _available;
        private static BlendShapeParameter _selected;

        internal static void Register()
        {
            if (!TimelineCompatibility.IsTimelineAvailable()) return;
            TimelineCompatibility.AddInterpolableModelDynamic<float, BlendShapeParameter>(
                owner: "Pseudo Maker",
                id: "ClothingBlendShape",
                name: "Clothing BlendShape",
                interpolateBefore: (oci, parameter, leftValue, rightValue, factor) =>
                    BlendShapes.ApplyWeight(Cha(oci), parameter.Kind, parameter.Group, parameter.Index, Mathf.LerpUnclamped(leftValue, rightValue, factor)),
                interpolateAfter: null,
                isCompatibleWithTarget: oci => oci is OCIChar && _selected != null,
                getValue: (oci, parameter) => BlendShapes.GetWeight(Cha(oci), parameter.Kind, parameter.Group, parameter.Index),
                readValueFromXml: (parameter, node) => XmlConvert.ToSingle(node.Attributes["value"].Value),
                writeValueToXml: (parameter, writer, value) => writer.WriteAttributeString("value", XmlConvert.ToString(value)),
                getParameter: oci => new BlendShapeParameter(_selected.Kind, _selected.Group, _selected.Index,
                    BlendShapes.DisplayName(Cha(oci), _selected.Kind, _selected.Group, _selected.Index)),
                readParameterFromXml: (oci, node) => new BlendShapeParameter(
                    XmlConvert.ToInt32(node.Attributes["kind"].Value),
                    XmlConvert.ToInt32(node.Attributes["group"].Value),
                    XmlConvert.ToInt32(node.Attributes["index"].Value),
                    node.Attributes["label"] != null ? node.Attributes["label"].Value : null),
                writeParameterToXml: (oci, writer, parameter) =>
                {
                    writer.WriteAttributeString("kind", XmlConvert.ToString(parameter.Kind));
                    writer.WriteAttributeString("group", XmlConvert.ToString(parameter.Group));
                    writer.WriteAttributeString("index", XmlConvert.ToString(parameter.Index));
                    if (parameter.Label != null) writer.WriteAttributeString("label", parameter.Label);
                },
                checkIntegrity: null,
                useOciInHash: true,
                getFinalName: (currentName, oci, parameter) =>
                    parameter.Label ?? BlendShapes.DisplayName(Cha(oci), parameter.Kind, parameter.Group, parameter.Index));
            _available = true;
            Plugin.Log.LogInfo("Timeline support enabled");
        }

        private static ChaControl Cha(ObjectCtrlInfo oci)
        {
            var chara = oci as OCIChar;
            return chara != null ? chara.charInfo : null;
        }

        internal static void Select(int kind, int group, int index)
        {
            if (!_available) return;
            _selected = new BlendShapeParameter(kind, group, index, null);
            if (BSAccess.PseudoMakerSelections != null)
                foreach (FieldInfo field in BSAccess.PseudoMakerSelections) field.SetValue(null, null);
            TimelineCompatibility.RefreshInterpolablesList();
        }

        internal static void Deselect(int kind = -1)
        {
            if (!_available || _selected == null || (kind >= 0 && _selected.Kind != kind)) return;
            _selected = null;
            TimelineCompatibility.RefreshInterpolablesList();
        }
    }

    internal class Group
    {
        internal GameObject Splitter;
        internal SliderComponent[] Sliders = new SliderComponent[4];
        internal Text[] Labels = new Text[4];
    }

    internal static class BlendShapeRows
    {
        internal static int KindOf(SubCategory sub)
        {
            switch (sub.ToString())
            {
                case "ClothingTop": return 0;
                case "ClothingBottom": return 1;
                case "ClothingBra": return 2;
                case "ClothingUnderwear": return 3;
                case "ClothingGloves": return 4;
                case "ClothingPantyhose": return 5;
                case "ClothingLegwear": return 6;
                case "ClothingShoesInDoors": return 7;
                case "ClothingShoes":
                case "ClothingShoesOutdoors": return 8;
                default: return -1;
            }
        }

        internal static void Build(ClothingEditorPanel panel)
        {
            int kind = KindOf(panel.SubCategory);
            if (kind < 0) return;

            var meSplitter = BSAccess.MaterialEditorSplitter == null ? null : BSAccess.MaterialEditorSplitter.GetValue(panel) as GameObject;
            int insertAt = meSplitter != null ? meSplitter.transform.GetSiblingIndex() : -1;

            var updater = panel.gameObject.AddComponent<BlendShapeRowsUpdater>();
            updater.Kind = kind;
            // Tops made of jacket/sailor pieces carry one blendshape component per piece (maker shows 8 sliders).
            updater.Groups = new Group[kind == 0 ? 2 : 1];
            for (int g = 0; g < updater.Groups.Length; g++)
            {
                var group = new Group();
                group.Splitter = panel.AddSplitter();
                insertAt = Place(group.Splitter, insertAt);
                group.Splitter.SetActive(false);
                for (int i = 0; i < 4; i++)
                {
                    int gi = g, ii = i;
                    SliderComponent slider = panel.AddSliderRow("BlendShape " + (i + 1),
                        () => updater.GetWeight(gi, ii),
                        () => BlendShapes.DefaultWeight,
                        v => updater.SetWeight(gi, ii, v),
                        () => updater.SetWeight(gi, ii, BlendShapes.DefaultWeight),
                        0f, 100f,
                        () => BlendShapeTimeline.Select(kind, gi, ii));
                    if (BSAccess.DisplayTemplate != null) BSAccess.DisplayTemplate.SetValue(slider, "0");
                    insertAt = Place(slider.gameObject, insertAt);
                    group.Sliders[i] = slider;
                    group.Labels[i] = slider.GetComponentInChildren<Text>(true);
                    slider.gameObject.SetActive(false);
                }
                updater.Groups[g] = group;
            }
        }

        private static int Place(GameObject go, int insertAt)
        {
            if (insertAt < 0) return insertAt;
            go.transform.SetSiblingIndex(insertAt);
            return insertAt + 1;
        }
    }

    /// <summary>Keeps the rows in sync with whatever clothing the selected character is wearing.</summary>
    internal class BlendShapeRowsUpdater : MonoBehaviour
    {
        internal int Kind;
        internal Group[] Groups;

        private ChaControl _lastCha;
        private readonly UnityEngine.Object[] _lastComps = new UnityEngine.Object[2];
        private readonly bool[] _lastAlive = new bool[2];
        private int _lastCoordinate = -1;
        private bool _dirty = true;

        private void OnEnable()
        {
            _dirty = true;
        }

        // Pseudo Maker drops its clothing Timeline selection when the panel closes; do the same.
        private void OnDisable()
        {
            BlendShapeTimeline.Deselect(Kind);
        }

        // Keep Update free of Clothing BlendShape types: profilers such as FPS Counter patch the Update methods of every
        // plugin, and reading a method body whose locals use a type from a missing assembly crashes KK's Mono.
        private void Update()
        {
            Poll();
        }

        private void Poll()
        {
            ChaControl cha = SelectedCharacter();
            int coordinate = cha != null ? cha.fileStatus.coordinateType : -1;
            bool changed = _dirty || !ReferenceEquals(cha, _lastCha) || coordinate != _lastCoordinate;
            for (int g = 0; g < Groups.Length; g++)
            {
                ChaClothesBlendShape comp = BlendShapes.Component(cha, Kind, g);
                bool alive = comp != null;
                if (!ReferenceEquals(comp, _lastComps[g]) || alive != _lastAlive[g]) changed = true;
                _lastComps[g] = comp;
                _lastAlive[g] = alive;
            }
            _lastCha = cha;
            _lastCoordinate = coordinate;
            if (!changed) return;
            _dirty = false;
            Refresh(cha);
        }

        private void Refresh(ChaControl cha)
        {
            for (int g = 0; g < Groups.Length; g++)
            {
                Group group = Groups[g];
                string[] names = BlendShapes.Names(BlendShapes.Component(cha, Kind, g));
                bool any = false;
                for (int i = 0; i < 4; i++)
                {
                    bool visible = names != null && !string.IsNullOrEmpty(names[i]);
                    any |= visible;
                    SliderComponent slider = group.Sliders[i];
                    if (visible)
                    {
                        slider.Name = names[i];
                        if (group.Labels[i] != null) group.Labels[i].text = names[i];
                    }
                    if (slider.gameObject.activeSelf != visible) slider.gameObject.SetActive(visible);
                    if (visible) slider.UpdateValue(GetWeight(g, i));
                }
                if (group.Splitter.activeSelf != any) group.Splitter.SetActive(any);
            }
        }

        private static ChaControl SelectedCharacter()
        {
            PseudoMakerSceneController scene = PseudoMakerSceneController.Instance;
            return scene != null ? scene.SelectedCharacter : null;
        }

        internal float GetWeight(int group, int index)
        {
            return BlendShapes.GetWeight(SelectedCharacter(), Kind, group, index);
        }

        internal void SetWeight(int group, int index, float value)
        {
            BlendShapes.SetWeight(SelectedCharacter(), Kind, group, index, value);
        }
    }
}
