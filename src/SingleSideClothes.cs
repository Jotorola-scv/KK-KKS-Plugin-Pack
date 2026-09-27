// Single Side Clothes - show only the left or the right piece of gloves, pantyhose, legwear, shoes and paired
// accessories, per outfit, in the character maker, CharaStudio and the main game (Koikatsu / Koikatsu Sunshine).
//
// Each connected piece (island) of a clothing mesh is assigned to the character's left or right side from the skin
// weights it puts on *_L / *_R limb bones; rigid paired accessories use the island position relative to the body.
// Pieces that cannot be assigned (e.g. pantyhose joined at the waist) stay visible. The mesh asset is never
// modified: the renderer gets a runtime copy whose triangle lists are filtered.
//
// The idea of hiding one side of a clothing item comes from Nil's KK_SingleShoe (a Studio-only shoe plugin);
// this is an independent implementation.
// Code: Claude (Anthropic). Direction and in-game testing: Jotorola-scv. License: MIT.
using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using ExtensibleSaveFormat;
using KKAPI;
using KKAPI.Chara;
using KKAPI.Maker;
using KKAPI.Maker.UI;
using KKAPI.Studio;
using KKAPI.Studio.UI;
using Studio;
using UniRx;
using UnityEngine;

namespace SingleSideClothes
{
    public enum SideMode { Both = 0, LeftOnly = 1, RightOnly = 2 }

    public enum MakerPosition { AboveMaterialEditor, BelowMaterialEditor, Bottom }

    internal sealed class PartDef
    {
        public readonly string Title;
        public readonly int[] Kinds;            // ChaFileDefine.ClothesKind indices
        public readonly string DataKey;
        public readonly string[] BoneWords;
        public readonly string[] Labels;
        public PartDef(string title, int[] kinds, string key, string[] words, string[] labels)
        { Title = title; Kinds = kinds; DataKey = key; BoneWords = words; Labels = labels; }
    }

    [BepInPlugin(GUID, "Single Side Clothes / 單邊服裝", Version)]
    [BepInDependency(KoikatuAPI.GUID, "1.47")]
    [BepInDependency(ExtendedSave.GUID)]
    public class SingleSidePlugin : BaseUnityPlugin
    {
        public const string GUID = "jotorola.singlesideclothes";
        internal const string LegacyDataId = "local.koikatsu.singleglove";   // pre-release builds saved card data under this id
        public const string Version = "1.4.0";
        internal static ManualLogSource Log;
        internal static ConfigEntry<MakerPosition> Position;

        internal static readonly string[] ArmWords = { "hand", "arm", "elbo", "wrist", "shoulder", "thumb", "index", "middle", "ring", "little", "finger" };
        internal static readonly string[] LegWords = { "foot", "toe", "leg", "knee", "thigh", "ankle", "calf", "siri" };
        private static readonly string[] SideLabels = { "雙邊 Both", "僅左 Left only", "僅右 Right only" };

        internal static readonly PartDef[] Parts =
        {
            new PartDef("單邊手套 Single glove", new[] { 4 }, "modes", ArmWords, new[] { "雙手 Both", "僅左手 Left only", "僅右手 Right only" }),
            new PartDef("單邊褲襪 Single pantyhose", new[] { 5 }, "modes_panst", LegWords, SideLabels),
            new PartDef("單邊襪子 Single legwear", new[] { 6 }, "modes_socks", LegWords, SideLabels),
            new PartDef("單邊鞋子 Single shoe", new[] { 7, 8 }, "modes_shoes", LegWords, new[] { "雙腳 Both", "僅左腳 Left only", "僅右腳 Right only" }),
        };

        internal sealed class RadioEntry
        {
            public MakerRadioButtons Radio;
            public int Part;
        }
        internal static readonly List<RadioEntry> Radios = new List<RadioEntry>();
        internal static MakerRadioButtons AccRadio;   // accessory window, shared by all slots

        // Supported accessory types (ChaListDefine.CategoryNo):
        //   109-112 = ClothesToAccessories gloves / pantyhose / socks / shoes (skinned to the body -> bone weights)
        //   127 ao_leg, 128 ao_arm = regular paired accessories (usually rigid -> geometric side test)
        internal const int AoLeg = 127, AoArm = 128;
        internal static bool IsSideClothesType(int type) { return (type >= 109 && type <= 112) || type == AoLeg || type == AoArm; }
        internal static bool IsGeometricType(int type) { return type == AoLeg || type == AoArm; }
        internal static bool IsArmType(int type) { return type == 109 || type == AoArm; }
        internal static string[] WordsForType(int type) { return IsArmType(type) ? ArmWords : LegWords; }

        private static SingleSideController MakerController()
        {
            var cha = MakerAPI.GetCharacterControl();
            return cha == null ? null : cha.GetComponent<SingleSideController>();
        }

        private static int AccType(ChaControl cha, int slot)
        {
            var parts = cha == null || cha.nowCoordinate == null ? null : cha.nowCoordinate.accessory.parts;
            return parts != null && slot >= 0 && slot < parts.Length ? parts[slot].type : 120;
        }

        internal static void RefreshAccRadio()
        {
            var c = MakerController();
            int slot = AccessoriesApi.SelectedMakerAccSlot;
            if (AccRadio == null || c == null || slot < 0) return;
            AccRadio.SetValue((int)c.GetAccMode(slot), false);
        }

        private void Start()
        {
            Log = Logger;
            Position = Config.Bind("Maker", "Control position", MakerPosition.AboveMaterialEditor,
                "Where the single-side buttons appear in each clothing tab / 單邊按鈕在服裝分頁中的位置");
            CharacterApi.RegisterExtraBehaviour<SingleSideController>(GUID);

            MakerAPI.RegisterCustomSubCategories += (s, e) =>
            {
                Radios.Clear();
                AddRadio(e, MakerConstants.Clothes.Gloves, 0);
                AddRadio(e, MakerConstants.Clothes.Panst, 1);
                AddRadio(e, MakerConstants.Clothes.Socks, 2);
#if KKS
                AddRadio(e, MakerConstants.Clothes.OuterShoes, 3);
#else
                AddRadio(e, MakerConstants.Clothes.InnerShoes, 3);
                AddRadio(e, MakerConstants.Clothes.OuterShoes, 3);
#endif
                // Accessory window: only shown for ClothesToAccessories gloves / pantyhose / socks / shoes slots
                // KKAPI orders accessory-window controls by GroupingID; Material Editor's button is in group "Buttons".
                // "Button" sorts right before it, "Buttons+" right after, so the radio is created next to it.
                AccRadio = new MakerRadioButtons(null, this, "單邊（配件） Single side", SideLabels);
                AccRadio.GroupingID = Position.Value == MakerPosition.AboveMaterialEditor ? "Button"
                                    : Position.Value == MakerPosition.BelowMaterialEditor ? "Buttons+" : GUID;
                MakerAPI.AddAccessoryWindowControl(AccRadio);
                AccRadio.ValueChanged.Subscribe(v =>
                {
                    var c = MakerController();
                    int slot = AccessoriesApi.SelectedMakerAccSlot;
                    if (c != null && slot >= 0) c.SetAccMode(slot, (SideMode)v);
                });
                Radios.Add(new RadioEntry { Radio = AccRadio, Part = -1 });
            };
            MakerAPI.MakerExiting += (s, e) => { Radios.Clear(); AccRadio = null; MeButtons.Clear(); LaidOut.Clear(); };
            AccessoriesApi.SelectedMakerAccSlotChanged += (s, e) => RefreshAccRadio();
            AccessoriesApi.AccessoryTransferred += (s, e) =>
            {
                var c = MakerController();
                if (c != null) c.CopyAccMode(c.OutfitIndex, e.SourceSlotIndex, c.OutfitIndex, e.DestinationSlotIndex);
                RefreshAccRadio();
            };
            AccessoriesApi.AccessoriesCopied += (s, e) =>
            {
                var c = MakerController();
                if (c == null) return;
                foreach (int slot in e.CopiedSlotIndexes) c.CopyAccMode((int)e.CopySource, slot, (int)e.CopyDestination, slot);
                RefreshAccRadio();
            };

            if (StudioAPI.InsideStudio)
            {
                var cat = StudioAPI.GetOrCreateCurrentStateCategory("單邊服裝 Single side");
                for (int i = 0; i < Parts.Length; i++)
                {
                    int part = i;
                    var dd = new CurrentStateCategoryDropdown(Parts[part].Title, Parts[part].Labels, oci =>
                    {
                        var c = GetController(oci);
                        return c == null ? 0 : (int)c.GetCurrentMode(part);
                    });
                    dd.Value.Subscribe(v =>
                    {
                        foreach (var c in StudioAPI.GetSelectedControllers<SingleSideController>()) c.SetCurrentMode(part, (SideMode)v);
                    });
                    cat.AddControl(dd);
                }
            }
        }

        private void AddRadio(RegisterSubCategoriesEvent e, MakerCategory category, int part)
        {
            var radio = new MakerRadioButtons(category, this, Parts[part].Title, Parts[part].Labels);
            radio.BindToFunctionController<SingleSideController, int>(c => (int)c.GetCurrentMode(part), (c, v) => c.SetCurrentMode(part, (SideMode)v));
            e.AddControl(radio);
            Radios.Add(new RadioEntry { Radio = radio, Part = part });
        }

        internal static void RefreshMakerRadios(SingleSideController c)
        {
            foreach (var r in Radios) if (r.Part >= 0) r.Radio.SetValue((int)c.GetCurrentMode(r.Part), false);
            RefreshAccRadio();
        }

        private void Update()
        {
            UpdateAccRadioVisibility();
            SyncAccToggles();
            PlaceRadios();
        }

        private static readonly HashSet<int> LaidOut = new HashSet<int>();

        // The accessory window is narrower than the clothing tabs: KKAPI puts the options in the rightmost 280 px on the
        // title's row, so a long (auto-translated) title runs under them. Put the title on its own row, options below.
        private static void TwoRowLayout(Transform tr)
        {
            var le = tr.GetComponent<UnityEngine.UI.LayoutElement>();
            if (le != null) { le.minHeight = 76; le.preferredHeight = 76; }
            float left = 10f;
            var title = tr.Find("textTglTitle") as RectTransform;
            if (title != null)
            {
                if (title.anchorMin.x == 0f) left = Mathf.Max(title.offsetMin.x, 4f);
                title.anchorMin = new Vector2(0f, 0.5f);
                title.anchorMax = new Vector2(1f, 1f);
                title.offsetMin = new Vector2(left, 0f);
                title.offsetMax = new Vector2(-4f, 0f);
            }
            var toggles = new List<RectTransform>();
            foreach (var t in tr.GetComponentsInChildren<UnityEngine.UI.Toggle>(true)) toggles.Add(t.GetComponent<RectTransform>());
            toggles.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
            for (int i = 0; i < toggles.Count; i++)
            {
                var rt = toggles[i];
                rt.anchorMin = new Vector2((float)i / toggles.Count, 0f);
                rt.anchorMax = new Vector2((float)(i + 1) / toggles.Count, 0.5f);
                rt.offsetMin = new Vector2(i == 0 ? left : 0f, 2f);
                rt.offsetMax = new Vector2(0f, -2f);
            }
        }

        // KKAPI bug workaround: MakerRadioButtons keeps one Buttons list per control, overwritten by each per-slot copy,
        // so only the last accessory slot's copy gets its toggles cleared -> several options look selected at once.
        // Keep every copy's toggles in line with the shared value ourselves (visible copy every frame, others every 30).
        private static void SyncAccToggles()
        {
            if (AccRadio == null || !MakerAPI.InsideAndLoaded) return;
            bool all = Time.frameCount % 30 == 0;
            int value = AccRadio.Value;
            foreach (GameObject go in AccRadio.ControlObjects)
            {
                if (!all && !go.activeInHierarchy) continue;
                if (LaidOut.Add(go.GetInstanceID())) TwoRowLayout(go.transform);
                foreach (var t in go.GetComponentsInChildren<UnityEngine.UI.Toggle>(true))
                {
                    int idx;
                    string n = t.gameObject.name;           // rb00, rb01, rb02
                    if (n.Length < 4 || !n.StartsWith("rb") || !int.TryParse(n.Substring(2), out idx)) continue;
                    bool want = idx == value;
                    if (t.isOn != want) t.isOn = want;       // KKAPI listener ignores these (same value / not selected)
                }
            }
        }

        // The accessory radio only makes sense for clothing-type accessory slots.
        private static void UpdateAccRadioVisibility()
        {
            if (AccRadio == null || !MakerAPI.InsideAndLoaded) return;
            bool show = IsSideClothesType(AccType(MakerAPI.GetCharacterControl(), AccessoriesApi.SelectedMakerAccSlot));
            if (AccRadio.Visible.Value != show) AccRadio.Visible.OnNext(show);
        }

        // Move each radio next to the "Material Editor" button of its tab / accessory slot window (same KKAPI container).
        // Re-checked twice a second: KKAPI makes one copy of an accessory-window control per slot, and MoreAccessories adds slots later.
        private static readonly Dictionary<Transform, Transform> MeButtons = new Dictionary<Transform, Transform>();

        private void PlaceRadios()
        {
            if (Radios.Count == 0 || Position.Value == MakerPosition.Bottom || !MakerAPI.InsideAndLoaded) return;
            if (Time.frameCount % 30 != 0) return;
            foreach (var r in Radios)
            {
                if (r.Part < 0) continue;     // accessory radio is positioned by its GroupingID instead
                foreach (GameObject go in r.Radio.ControlObjects) PlaceNextToMaterialEditor(go.transform);
            }
        }

        private static void PlaceNextToMaterialEditor(Transform ours)
        {
            Transform parent = ours.parent;
            if (parent == null) return;
            Transform me;
            if (!MeButtons.TryGetValue(parent, out me) || me == null || me.parent != parent)
            {
                me = null;
                for (int i = 0; i < parent.childCount && me == null; i++)
                {
                    Transform c = parent.GetChild(i);
                    if (c != ours && HasLabel(c, "Material Editor")) me = c;
                }
                if (me == null) return;          // no Material Editor here (yet); stays where KKAPI put it
                MeButtons[parent] = me;
            }
            int mi = me.GetSiblingIndex();
            int cur = ours.GetSiblingIndex();
            bool above = Position.Value == MakerPosition.AboveMaterialEditor;
            if (cur == (above ? mi - 1 : mi + 1)) return;
            if (above) ours.SetSiblingIndex(cur < mi ? mi - 1 : mi);
            else ours.SetSiblingIndex(cur < mi ? mi : mi + 1);
        }

        // Label components differ between games (uGUI Text / TextMeshPro); read "text" by reflection.
        private static bool HasLabel(Transform t, string label)
        {
            foreach (Component comp in t.GetComponentsInChildren<Component>(true))
            {
                if (comp == null) continue;
                Type type = comp.GetType();
                if (type.Name != "Text" && !type.Name.StartsWith("TextMeshPro")) continue;
                var prop = type.GetProperty("text");
                var s = prop == null ? null : prop.GetValue(comp, null) as string;
                if (s != null && s.Trim() == label) return true;
            }
            return false;
        }

        private static SingleSideController GetController(OCIChar oci)
        {
            if (oci == null || oci.charInfo == null) return null;
            return oci.charInfo.GetComponent<SingleSideController>();
        }
    }

    public class SingleSideController : CharaCustomFunctionController
    {
        private sealed class PartState
        {
            public readonly PartDef Def;
            public readonly List<int> Modes = new List<int>();
            public readonly List<SideMeshBinding> Bindings = new List<SideMeshBinding>();
            public readonly HashSet<int> Failed = new HashSet<int>();
            public GameObject[] LastObjs;
            public SideMode Applied = SideMode.Both;
            public bool Dirty = true;
            public PartState(PartDef def) { Def = def; LastObjs = new GameObject[def.Kinds.Length]; }
        }

        private sealed class AccState
        {
            public GameObject LastObj;
            public int Type = -1;
            public readonly List<SideMeshBinding> Bindings = new List<SideMeshBinding>();
            public readonly HashSet<int> Failed = new HashSet<int>();
            public SideMode Applied = (SideMode)(-1);
        }

        private const string AccKey = "acc";          // "coord:slot:mode;..." (non-Both entries only)
        private const long AccStride = 100000;
        private readonly Dictionary<long, int> _accModes = new Dictionary<long, int>();
        private readonly Dictionary<int, AccState> _accStates = new Dictionary<int, AccState>();
        private readonly List<int> _accWanted = new List<int>();
        private readonly List<int> _accDrop = new List<int>();

        private PartState[] _parts;
        private int _lastCoordinate = -1;
        private bool _broken;

        public int OutfitIndex { get { return CoordinateIndex; } }

        public SideMode GetAccMode(int slot) { return GetAccMode(CoordinateIndex, slot); }

        public SideMode GetAccMode(int coord, int slot)
        {
            int m;
            return slot >= 0 && _accModes.TryGetValue(coord * AccStride + slot, out m) && m >= 0 && m <= 2 ? (SideMode)m : SideMode.Both;
        }

        public void SetAccMode(int slot, SideMode mode) { SetAccMode(CoordinateIndex, slot, mode); }

        public void SetAccMode(int coord, int slot, SideMode mode)
        {
            if (coord < 0 || slot < 0) return;
            long key = coord * AccStride + slot;
            if (mode == SideMode.Both) _accModes.Remove(key);
            else _accModes[key] = (int)mode;
        }

        public void CopyAccMode(int fromCoord, int fromSlot, int toCoord, int toSlot)
        {
            SetAccMode(toCoord, toSlot, GetAccMode(fromCoord, fromSlot));
        }

        private PartState[] Parts
        {
            get
            {
                if (_parts == null)
                {
                    _parts = new PartState[SingleSidePlugin.Parts.Length];
                    for (int i = 0; i < _parts.Length; i++) _parts[i] = new PartState(SingleSidePlugin.Parts[i]);
                }
                return _parts;
            }
        }

        private int CoordinateIndex { get { return ChaControl.fileStatus.coordinateType; } }

        public SideMode GetCurrentMode(int part)
        {
            var modes = Parts[part].Modes;
            int i = CoordinateIndex;
            return i >= 0 && i < modes.Count && modes[i] >= 0 && modes[i] <= 2 ? (SideMode)modes[i] : SideMode.Both;
        }

        public void SetCurrentMode(int part, SideMode value)
        {
            var p = Parts[part];
            int i = CoordinateIndex;
            if (i < 0) return;
            while (p.Modes.Count <= i) p.Modes.Add(0);
            if (p.Modes[i] == (int)value) return;
            p.Modes[i] = (int)value;
            p.Dirty = true;
        }

        protected override void OnCardBeingSaved(GameMode currentGameMode)
        {
            PluginData data = null;
            foreach (var p in Parts)
            {
                bool any = false;
                foreach (int m in p.Modes) if (m != 0) any = true;
                if (!any) continue;
                var parts = new string[p.Modes.Count];
                for (int i = 0; i < parts.Length; i++) parts[i] = p.Modes[i].ToString();
                if (data == null) data = new PluginData { version = 1 };
                data.data[p.Def.DataKey] = string.Join(",", parts);
            }
            if (_accModes.Count > 0)
            {
                var entries = new List<string>();
                foreach (var kv in _accModes)
                    if (kv.Value != 0) entries.Add((kv.Key / AccStride) + ":" + (kv.Key % AccStride) + ":" + kv.Value);
                if (entries.Count > 0)
                {
                    if (data == null) data = new PluginData { version = 1 };
                    data.data[AccKey] = string.Join(";", entries.ToArray());
                }
            }
            SetExtendedData(data);
            ExtendedSave.SetExtendedDataById(ChaFileControl, SingleSidePlugin.LegacyDataId, null);   // migrated; don't let it come back
        }

        protected override void OnReload(GameMode currentGameMode, bool maintainState)
        {
            if (maintainState) return;
            var data = GetExtendedData() ?? ExtendedSave.GetExtendedDataById(ChaFileControl, SingleSidePlugin.LegacyDataId);
            foreach (var p in Parts)
            {
                p.Modes.Clear();
                p.Failed.Clear();
                p.Dirty = true;
                object raw;
                if (data == null || !data.data.TryGetValue(p.Def.DataKey, out raw) || !(raw is string)) continue;
                foreach (var s in ((string)raw).Split(','))
                {
                    int m;
                    p.Modes.Add(int.TryParse(s, out m) ? m : 0);
                }
            }
            _accModes.Clear();
            ReleaseAccessories();
            object accRaw;
            if (data != null && data.data.TryGetValue(AccKey, out accRaw) && accRaw is string)
            {
                foreach (var entry in ((string)accRaw).Split(';'))
                {
                    var f = entry.Split(':');
                    int c, sl, m;
                    if (f.Length == 3 && int.TryParse(f[0], out c) && int.TryParse(f[1], out sl) && int.TryParse(f[2], out m))
                        SetAccMode(c, sl, (SideMode)m);
                }
            }
            _broken = false;
            if (MakerAPI.InsideAndLoaded && MakerAPI.GetCharacterControl() == ChaControl) SingleSidePlugin.RefreshMakerRadios(this);
        }

        protected override void Update()
        {
            base.Update();
            int coord = CoordinateIndex;
            if (coord == _lastCoordinate) return;
            _lastCoordinate = coord;
            foreach (var p in Parts) p.Dirty = true;
            if (MakerAPI.InsideAndLoaded && MakerAPI.GetCharacterControl() == ChaControl) SingleSidePlugin.RefreshMakerRadios(this);
        }

        private void LateUpdate()
        {
            if (_broken) return;
            try
            {
                for (int i = 0; i < Parts.Length; i++) Process(Parts[i], GetCurrentMode(i));
                ProcessAccessories();
            }
            catch (Exception ex)
            {
                SingleSidePlugin.Log.LogError("Single Side Clothes: " + ex);
                foreach (var p in Parts) Release(p);
                ReleaseAccessories();
                _broken = true;   // cleared on next card reload
            }
        }

        // ClothesToAccessories slots (gloves / pantyhose / socks / shoes types) with a non-Both mode in the current outfit.
        private void ProcessAccessories()
        {
            int coord = CoordinateIndex;
            _accWanted.Clear();
            foreach (var kv in _accModes)
                if (kv.Value != 0 && kv.Key / AccStride == coord) _accWanted.Add((int)(kv.Key % AccStride));

            _accDrop.Clear();
            foreach (var slot in _accStates.Keys) if (!_accWanted.Contains(slot)) _accDrop.Add(slot);
            foreach (var slot in _accDrop) { ReleaseAcc(_accStates[slot]); _accStates.Remove(slot); }
            if (_accWanted.Count == 0) return;

            GameObject[] objs = ChaControl.objAccessory;
            var parts = ChaControl.nowCoordinate == null ? null : ChaControl.nowCoordinate.accessory.parts;
            foreach (int slot in _accWanted)
            {
                AccState st;
                if (!_accStates.TryGetValue(slot, out st)) { st = new AccState(); _accStates[slot] = st; }
                int type = parts != null && slot < parts.Length ? parts[slot].type : 120;
                GameObject obj = objs != null && slot < objs.Length && SingleSidePlugin.IsSideClothesType(type) ? objs[slot] : null;

                bool rescan = obj != st.LastObj || type != st.Type;
                if (!rescan)
                    foreach (var b in st.Bindings)
                        if (b.Renderer == null || SideMeshBinding.GetMesh(b.Renderer) != b.Working) { rescan = true; break; }
                if (rescan)
                {
                    ReleaseAcc(st);
                    st.LastObj = obj;
                    st.Type = type;
                    st.Applied = (SideMode)(-1);
                    if (obj != null)
                    {
                        SideReference geo = BodyReference(SingleSidePlugin.IsArmType(type));
                        foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
                        {
                            Mesh mesh = SideMeshBinding.GetMesh(r);
                            if (mesh == null || st.Failed.Contains(mesh.GetInstanceID())) continue;
                            try { st.Bindings.Add(new SideMeshBinding(r, mesh, SingleSidePlugin.WordsForType(type), geo)); }
                            catch (Exception ex)
                            {
                                st.Failed.Add(mesh.GetInstanceID());
                                SingleSidePlugin.Log.LogWarning("Single Side Clothes: unsupported accessory mesh '" + r.name + "' (slot " + (slot + 1) + "): " + ex.Message);
                            }
                        }
                    }
                }
                SideMode mode = GetAccMode(coord, slot);
                if (st.Applied != mode)
                {
                    foreach (var b in st.Bindings) b.Apply(mode);
                    st.Applied = mode;
                }
            }
        }

        private readonly Dictionary<string, Transform> _boneCache = new Dictionary<string, Transform>();

        private Transform BodyBone(string name)
        {
            Transform t;
            if (_boneCache.TryGetValue(name, out t) && t != null) return t;
            t = ChaControl.objBodyBone == null ? null : FindDeep(ChaControl.objBodyBone.transform, name);
            _boneCache[name] = t;
            return t;
        }

        private static Transform FindDeep(Transform root, string name)
        {
            if (root.name == name) return root;
            for (int i = 0; i < root.childCount; i++)
            {
                var f = FindDeep(root.GetChild(i), name);
                if (f != null) return f;
            }
            return null;
        }

        // Upper arms for arm-type accessories, thighs for the rest.
        private SideReference BodyReference(bool arm)
        {
            Transform l = BodyBone(arm ? "cf_j_arm00_L" : "cf_j_thigh00_L");
            Transform r = BodyBone(arm ? "cf_j_arm00_R" : "cf_j_thigh00_R");
            return l == null || r == null ? null : new SideReference(l.position, r.position);
        }

        private static void ReleaseAcc(AccState st)
        {
            foreach (var b in st.Bindings) b.Dispose();
            st.Bindings.Clear();
        }

        private void ReleaseAccessories()
        {
            foreach (var st in _accStates.Values) ReleaseAcc(st);
            _accStates.Clear();
        }

        private void Process(PartState p, SideMode mode)
        {
            if (mode == SideMode.Both)
            {
                if (p.Bindings.Count > 0) Release(p);
                for (int k = 0; k < p.LastObjs.Length; k++) p.LastObjs[k] = null;
                p.Applied = SideMode.Both;
                p.Dirty = false;
                return;
            }
            if (NeedsRescan(p)) { Scan(p); p.Dirty = true; }
            if (p.Dirty || p.Applied != mode)
            {
                foreach (var b in p.Bindings) b.Apply(mode);
                p.Applied = mode;
                p.Dirty = false;
            }
        }

        private GameObject ClothesObject(int kind)
        {
            var objs = ChaControl.objClothes;
            return objs != null && objs.Length > kind ? objs[kind] : null;
        }

        // Cheap per-frame check: clothes object replaced, or a renderer lost our working mesh.
        private bool NeedsRescan(PartState p)
        {
            for (int k = 0; k < p.Def.Kinds.Length; k++)
                if (ClothesObject(p.Def.Kinds[k]) != p.LastObjs[k]) return true;
            foreach (var b in p.Bindings)
                if (b.Renderer == null || SideMeshBinding.GetMesh(b.Renderer) != b.Working) return true;
            return false;
        }

        private void Scan(PartState p)
        {
            Release(p);
            for (int k = 0; k < p.Def.Kinds.Length; k++)
            {
                GameObject obj = ClothesObject(p.Def.Kinds[k]);
                p.LastObjs[k] = obj;
                if (obj == null) continue;
                foreach (var r in obj.GetComponentsInChildren<Renderer>(true))
                {
                    Mesh mesh = SideMeshBinding.GetMesh(r);
                    if (mesh == null || p.Failed.Contains(mesh.GetInstanceID())) continue;
                    try { p.Bindings.Add(new SideMeshBinding(r, mesh, p.Def.BoneWords)); }
                    catch (Exception ex)
                    {
                        p.Failed.Add(mesh.GetInstanceID());
                        SingleSidePlugin.Log.LogWarning("Single Side Clothes: unsupported mesh '" + r.name + "': " + ex.Message);
                    }
                }
            }
        }

        private static void Release(PartState p)
        {
            foreach (var b in p.Bindings) b.Dispose();
            p.Bindings.Clear();
        }

        protected override void OnDestroy()
        {
            if (_parts != null) foreach (var p in _parts) Release(p);
            ReleaseAccessories();
            base.OnDestroy();
        }
    }

    // Character's own right->left axis taken from a pair of body bones (upper arms or thighs).
    internal sealed class SideReference
    {
        private readonly Vector3 _mid, _axis;
        private readonly float _half;
        public SideReference(Vector3 leftPos, Vector3 rightPos)
        {
            _mid = (leftPos + rightPos) * 0.5f;
            Vector3 d = leftPos - rightPos;
            _half = Mathf.Max(d.magnitude * 0.5f, 0.0001f);
            _axis = d.normalized;
        }
        public int Side(Vector3 p)
        {
            float t = Vector3.Dot(p - _mid, _axis) / _half;
            return t >= 0.25f ? 1 : t <= -0.25f ? -1 : 0;
        }
    }

    // Works out which side each vertex of a mesh belongs to: +1 = the character's own left, -1 = right, 0 = unknown.
    internal static class SideClassifier
    {
        private static readonly char[] Separators = { '_', '.', ' ', '-' };

        // A bone names a side when it contains one of the limb words and a stand-alone "l"/"r" (or left/right) token.
        public static int SideOfName(string name, string[] limbWords)
        {
            if (string.IsNullOrEmpty(name)) return 0;
            string lower = name.ToLowerInvariant();
            bool isLimb = false;
            for (int i = 0; i < limbWords.Length && !isLimb; i++) isLimb = lower.IndexOf(limbWords[i], StringComparison.Ordinal) >= 0;
            if (!isLimb) return 0;
            foreach (string token in lower.Split(Separators))
            {
                if (token == "l" || token == "left") return 1;
                if (token == "r" || token == "right") return -1;
            }
            if (lower.IndexOf("left", StringComparison.Ordinal) >= 0) return 1;
            if (lower.IndexOf("right", StringComparison.Ordinal) >= 0) return -1;
            return 0;
        }

        // First side found walking from the transform towards the scene root.
        public static int SideOfTransform(Transform t, string[] limbWords)
        {
            for (int depth = 0; t != null && depth < 32; depth++, t = t.parent)
            {
                int side = SideOfName(t.name, limbWords);
                if (side != 0) return side;
            }
            return 0;
        }

        // Skin weight that each vertex puts on left-side and on right-side bones.
        public static void SkinSideWeights(SkinnedMeshRenderer smr, Mesh mesh, string[] limbWords, float[] leftW, float[] rightW)
        {
            Transform[] bones = smr.bones;
            var boneSide = new int[bones.Length];
            for (int b = 0; b < bones.Length; b++) boneSide[b] = SideOfTransform(bones[b], limbWords);
            BoneWeight[] weights = mesh.boneWeights;
            int count = Math.Min(weights.Length, leftW.Length);
            for (int v = 0; v < count; v++)
            {
                BoneWeight bw = weights[v];
                Accumulate(boneSide, bw.boneIndex0, bw.weight0, ref leftW[v], ref rightW[v]);
                Accumulate(boneSide, bw.boneIndex1, bw.weight1, ref leftW[v], ref rightW[v]);
                Accumulate(boneSide, bw.boneIndex2, bw.weight2, ref leftW[v], ref rightW[v]);
                Accumulate(boneSide, bw.boneIndex3, bw.weight3, ref leftW[v], ref rightW[v]);
            }
        }

        private static void Accumulate(int[] boneSide, int bone, float weight, ref float leftW, ref float rightW)
        {
            if (weight <= 0f || bone < 0 || bone >= boneSide.Length) return;
            switch (boneSide[bone])
            {
                case 1: leftW += weight; break;
                case -1: rightW += weight; break;
            }
        }

        // Connected pieces of the mesh: returns an island id (0..islandCount-1) per vertex.
        public static int[] Islands(int vertexCount, int[][] submeshes, out int islandCount)
        {
            var root = new int[vertexCount];
            var size = new int[vertexCount];
            for (int v = 0; v < vertexCount; v++) { root[v] = v; size[v] = 1; }
            foreach (int[] tris in submeshes)
            {
                if (tris.Length % 3 != 0) throw new ArgumentException("Index buffer is not a triangle list");
                foreach (int index in tris)
                    if (index < 0 || index >= vertexCount) throw new ArgumentException("Triangle index out of range");
                for (int t = 0; t < tris.Length; t += 3)
                {
                    Join(root, size, tris[t], tris[t + 1]);
                    Join(root, size, tris[t], tris[t + 2]);
                }
            }
            var island = new int[vertexCount];
            var idOfRoot = new Dictionary<int, int>();
            for (int v = 0; v < vertexCount; v++)
            {
                int r = Root(root, v);
                int id;
                if (!idOfRoot.TryGetValue(r, out id)) { id = idOfRoot.Count; idOfRoot.Add(r, id); }
                island[v] = id;
            }
            islandCount = idOfRoot.Count;
            return island;
        }

        private static int Root(int[] root, int v)
        {
            int r = v;
            while (root[r] != r) r = root[r];
            while (root[v] != r) { int next = root[v]; root[v] = r; v = next; }   // flatten the path we walked
            return r;
        }

        private static void Join(int[] root, int[] size, int a, int b)
        {
            a = Root(root, a);
            b = Root(root, b);
            if (a == b) return;
            if (size[a] < size[b]) { int tmp = a; a = b; b = tmp; }
            root[b] = a;
            size[a] += size[b];
        }

        // Side per vertex. An island takes a side when at least 90% of its side-carrying skin weight is on that side.
        // Islands without any side weight use the geometric reference (island centre vs. the body's mid-plane) if given.
        public static int[] Decide(int[] island, int islandCount, float[] leftW, float[] rightW, Vector3[] world, SideReference geo)
        {
            var totalL = new double[islandCount];
            var totalR = new double[islandCount];
            var centre = new Vector3[islandCount];
            var members = new int[islandCount];
            bool useGeo = geo != null && world != null && world.Length == island.Length;
            for (int v = 0; v < island.Length; v++)
            {
                int k = island[v];
                totalL[k] += leftW[v];
                totalR[k] += rightW[v];
                if (useGeo) { centre[k] += world[v]; members[k]++; }
            }
            var islandSide = new int[islandCount];
            for (int k = 0; k < islandCount; k++)
            {
                double total = totalL[k] + totalR[k];
                if (total > 1e-3)
                {
                    if (totalL[k] >= 0.9 * total) islandSide[k] = 1;
                    else if (totalR[k] >= 0.9 * total) islandSide[k] = -1;
                }
                else if (useGeo && members[k] > 0)
                {
                    islandSide[k] = geo.Side(centre[k] / members[k]);
                }
            }
            var side = new int[island.Length];
            for (int v = 0; v < island.Length; v++) side[v] = islandSide[island[v]];
            return side;
        }
    }

    // Swaps a renderer's mesh for a runtime copy whose triangle lists can be filtered by side.
    // The shared mesh asset itself is never modified; Dispose puts the original back.
    internal sealed class SideMeshBinding : IDisposable
    {
        public readonly Renderer Renderer;
        public Mesh Working { get; private set; }
        private readonly Mesh _original;
        private readonly int[][] _triangles;
        private readonly int[] _vertexSide;
        private SideMode _shown = (SideMode)(-1);

        public SideMeshBinding(Renderer renderer, Mesh source, string[] limbWords, SideReference geo = null)
        {
            Renderer = renderer;
            _original = source;
            if (!source.isReadable) throw new InvalidOperationException("Mesh is not CPU-readable");
            _triangles = new int[source.subMeshCount][];
            for (int s = 0; s < _triangles.Length; s++)
            {
                if (source.GetTopology(s) != MeshTopology.Triangles) throw new InvalidOperationException("Submesh " + s + " is not triangles");
                _triangles[s] = source.GetTriangles(s);
            }

            int n = source.vertexCount;
            var leftW = new float[n];
            var rightW = new float[n];
            var smr = renderer as SkinnedMeshRenderer;
            if (smr != null)
            {
                SideClassifier.SkinSideWeights(smr, source, limbWords, leftW, rightW);
            }
            else if (geo == null)
            {
                // Rigid mesh without a reference: the whole mesh follows the side of its transform chain.
                int side = SideClassifier.SideOfTransform(renderer.transform, limbWords);
                if (side == 1) for (int v = 0; v < n; v++) leftW[v] = 1f;
                else if (side == -1) for (int v = 0; v < n; v++) rightW[v] = 1f;
            }
            // Rigid mesh with a reference (paired accessory): its attach point (e.g. a_n_arm_L) says nothing about
            // which piece is which, so no weights are set and every island uses the position test.

            int islandCount;
            int[] island = SideClassifier.Islands(n, _triangles, out islandCount);
            _vertexSide = SideClassifier.Decide(island, islandCount, leftW, rightW, geo == null ? null : WorldPositions(renderer, source), geo);

            Working = UnityEngine.Object.Instantiate(source);
            Working.name = source.name + " [SingleSide]";
            Working.hideFlags = HideFlags.DontSave;
            SetMesh(renderer, Working);
        }

        // Current-pose vertex positions in world space.
        private static Vector3[] WorldPositions(Renderer renderer, Mesh source)
        {
            Vector3[] pos;
            Matrix4x4 toWorld;
            var smr = renderer as SkinnedMeshRenderer;
            if (smr != null)
            {
                var baked = new Mesh();
                smr.BakeMesh(baked);
                pos = baked.vertices;
                UnityEngine.Object.Destroy(baked);
                toWorld = Matrix4x4.TRS(renderer.transform.position, renderer.transform.rotation, Vector3.one);   // baked vertices are already scaled
            }
            else
            {
                pos = source.vertices;
                toWorld = renderer.transform.localToWorldMatrix;
            }
            for (int v = 0; v < pos.Length; v++) pos[v] = toWorld.MultiplyPoint3x4(pos[v]);
            return pos;
        }

        // A triangle belongs to a side only when all three corners agree; anything else counts as unknown (kept visible).
        private int TriangleSide(int a, int b, int c)
        {
            int s = _vertexSide[a];
            return s == _vertexSide[b] && s == _vertexSide[c] ? s : 0;
        }

        public void Apply(SideMode mode)
        {
            if (_shown == mode || Working == null) return;
            int hidden = mode == SideMode.LeftOnly ? -1 : mode == SideMode.RightOnly ? 1 : 0;
            for (int s = 0; s < _triangles.Length; s++)
            {
                int[] all = _triangles[s];
                if (hidden == 0) { Working.SetTriangles(all, s, false); continue; }
                var kept = new List<int>(all.Length);
                for (int t = 0; t < all.Length; t += 3)
                {
                    if (TriangleSide(all[t], all[t + 1], all[t + 2]) == hidden) continue;
                    kept.Add(all[t]); kept.Add(all[t + 1]); kept.Add(all[t + 2]);
                }
                Working.SetTriangles(kept.ToArray(), s, false);
            }
            _shown = mode;
        }

        public static Mesh GetMesh(Renderer r)
        {
            if (r == null) return null;
            var smr = r as SkinnedMeshRenderer;
            if (smr != null) return smr.sharedMesh;
            var mf = r.GetComponent<MeshFilter>();
            return mf != null ? mf.sharedMesh : null;
        }

        private static void SetMesh(Renderer r, Mesh mesh)
        {
            var smr = r as SkinnedMeshRenderer;
            if (smr != null) { smr.sharedMesh = mesh; return; }
            var mf = r.GetComponent<MeshFilter>();
            if (mf != null) mf.sharedMesh = mesh;
        }

        public void Dispose()
        {
            if (Working == null) return;
            if (Renderer != null && GetMesh(Renderer) == Working) SetMesh(Renderer, _original);
            UnityEngine.Object.Destroy(Working);
            Working = null;
        }
    }
}
