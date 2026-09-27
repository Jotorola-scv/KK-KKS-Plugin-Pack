#!/bin/sh
# Builds one plugin of the pack with the .NET SDK's Roslyn compiler against a game install's own assemblies (no NuGet).
#
#   sh build/build.sh <plugin> KK|KKS <game folder>
#
#   sh build/build.sh SingleSideClothes    KK  "D:/Games/Koikatsu"           -> bin/KK_SingleSideClothes.dll
#   sh build/build.sh PseudoMakerExtras    KKS "D:/Games/Koikatsu Sunshine"  -> bin/KKS_PseudoMakerExtras.dll
#   sh build/build.sh ShaderSwapperNullFix KK  "D:/Games/Koikatsu"           -> bin/KK_ShaderSwapperNullFix.dll
#   sh build/build.sh KKUTSClothesRedirect KKS "D:/Games/Koikatsu Sunshine"  -> bin/KKS_KKUTSClothesRedirect.dll
#   sh build/build.sh KKUTSClothesRedirect KK  "D:/Games/Koikatsu"           -> bin/KK_KKUTSClothesCompat.dll
#   sh build/build.sh StripTease2Timeline  KKS "D:/Games/Koikatsu Sunshine"  -> bin/KKS_StripTease2Timeline.dll
#
# The plugins that a project talks to must be installed in that game folder (see each plugin's README).
# PseudoMakerExtras also references Single Side Clothes: bin/<KK|KKS>_SingleSideClothes.dll is used when it has been
# built, otherwise the installed copy.
# CSC can be overridden, e.g. CSC="C:/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll".
set -e
PLUGIN="$1"
GAMEKIND="$2"
G="$3"
if [ -z "$PLUGIN" ] || [ -z "$GAMEKIND" ] || [ -z "$G" ]; then
    echo "usage: sh build/build.sh <plugin> KK|KKS <game folder>"; exit 1
fi
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
if [ -z "$CSC" ]; then
    CSC="$(ls -d "C:/Program Files/dotnet/sdk/"*/Roslyn/bincore/csc.dll 2>/dev/null | tail -1)"
fi
[ -f "$CSC" ] || { echo "csc.dll not found - install the .NET SDK or set CSC"; exit 1; }
P="$G/BepInEx/plugins"
need() { # find a dll in BepInEx/plugins by exact file name, or fail
    f="$(find "$P" -iname "$1" | head -1)"
    [ -n "$f" ] || { echo "$1 not found in $P" >&2; exit 1; }
    echo "$f"
}

# Game assemblies. KK: Koikatu_Data's Assembly-CSharp also contains the Studio types; a Studio-only install has
# CharaStudio_Data instead. UniRx lives in Assembly-CSharp-firstpass in KK.
if [ "$GAMEKIND" = "KK" ]; then
    for d in Koikatu_Data CharaStudio_Data; do [ -d "$G/$d/Managed" ] && M="$G/$d/Managed" && break; done
    [ -n "$M" ] || { echo "Managed folder not found under $G"; exit 1; }
    set -- "-r:$M/mscorlib.dll" "-r:$M/System.dll" "-r:$M/System.Core.dll" "-r:$M/System.Xml.dll" \
           "-r:$M/UnityEngine.dll" "-r:$M/UnityEngine.UI.dll" "-r:$M/Assembly-CSharp.dll" "-r:$M/Assembly-CSharp-firstpass.dll"
    API="$(need KKAPI.dll)"
elif [ "$GAMEKIND" = "KKS" ]; then
    for d in KoikatsuSunshine_Data CharaStudio_Data; do [ -d "$G/$d/Managed" ] && M="$G/$d/Managed" && break; done
    [ -n "$M" ] || { echo "Managed folder not found under $G"; exit 1; }
    set -- "-define:KKS" "-r:$M/mscorlib.dll" "-r:$M/System.dll" "-r:$M/System.Core.dll" "-r:$M/System.Xml.dll" \
           "-r:$M/netstandard.dll" "-r:$M/UnityEngine.dll" "-r:$M/UnityEngine.CoreModule.dll" "-r:$M/UnityEngine.UI.dll" \
           "-r:$M/UnityEngine.UIModule.dll" "-r:$M/Assembly-CSharp.dll" "-r:$M/Assembly-CSharp-firstpass.dll" "-r:$M/UniRx.dll"
    API="$(need KKSAPI.dll)"
else
    echo "second argument must be KK or KKS"; exit 1
fi
set -- "$@" "-r:$G/BepInEx/core/BepInEx.dll" "-r:$API"

OUTNAME="$PLUGIN"
case "$PLUGIN" in
SingleSideClothes)
    if [ "$GAMEKIND" = "KK" ]; then ESF="$(need ExtensibleSaveFormat.dll)"; else ESF="$(need KKS_ExtensibleSaveFormat.dll)"; fi
    set -- "$@" "-r:$ESF"
    SRC="$ROOT/SingleSideClothes/SingleSideClothes.cs" ;;
PseudoMakerExtras)
    # CharaStudio only. Single Side Clothes, ClothingBlendShape and EditFangs are optional at run time, but the
    # compiler needs them; ABMX is required by Pseudo Maker itself.
    SSC="$ROOT/bin/${GAMEKIND}_SingleSideClothes.dll"
    [ -f "$SSC" ] || SSC="$(need "${GAMEKIND}_SingleSideClothes.dll")"
    if [ "$GAMEKIND" = "KK" ]; then ABMX="$(need KKABMX.dll)"; else ABMX="$(need KKSABMX.dll)"; fi
    PM="$(need "${GAMEKIND}_StudioPseudoMaker.dll")"
    CBS="$(need "${GAMEKIND}_ClothingBlendShape.dll")"
    FANGS="$(need "${GAMEKIND}_EditFangs.dll")"
    set -- "$@" "-r:$G/BepInEx/core/0Harmony.dll" "-r:$PM" "-r:$SSC" "-r:$CBS" "-r:$FANGS" "-r:$ABMX"
    SRC="$ROOT/PseudoMakerExtras/PseudoMakerExtras.cs" ;;
ShaderSwapperNullFix)
    set -- "$@" "-r:$G/BepInEx/core/0Harmony.dll"
    SRC="$ROOT/ShaderSwapperNullFix/ShaderSwapperNullFix.cs" ;;
KKUTSClothesRedirect)
    # KKS: KKUTS -> KKUTSclothes on clothes. KK: the reverse mapping for cards saved in KKS ("KKUTSclothes Compat").
    ME="$(need "${GAMEKIND}_MaterialEditor.dll")"
    set -- "$@" "-r:$G/BepInEx/core/0Harmony.dll" "-r:$ME"
    if [ "$GAMEKIND" = "KKS" ]; then
        RR="$(need XUnity.ResourceRedirector.dll)"
        set -- "$@" "-r:$G/BepInEx/core/XUnity.Common.dll" "-r:$RR"
        SRC="$ROOT/KKUTSClothesRedirect/KKUTSClothesRedirect.cs"
    else
        OUTNAME="KKUTSClothesCompat"
        SRC="$ROOT/KKUTSClothesRedirect/KKUTSClothesCompat.cs"
    fi ;;
StripTease2Timeline)
    # CharaStudio only. StripTease2 itself is reached through reflection, so it is not needed to compile.
    set -- "$@" "-r:$G/BepInEx/core/0Harmony.dll"
    [ "$GAMEKIND" = "KKS" ] && set -- "$@" "-r:$M/UnityEngine.IMGUIModule.dll"
    SRC="$ROOT/StripTease2Timeline/StripTease2Timeline.cs" ;;
*)
    echo "unknown plugin: $PLUGIN (SingleSideClothes, PseudoMakerExtras, ShaderSwapperNullFix, KKUTSClothesRedirect, StripTease2Timeline)"; exit 1 ;;
esac

mkdir -p "$ROOT/bin"
OUT="$ROOT/bin/${GAMEKIND}_${OUTNAME}.dll"
dotnet "$CSC" -nologo -noconfig -nostdlib+ -target:library -langversion:7.3 -optimize+ -deterministic -utf8output \
    -out:"$OUT" "$@" "$SRC"
echo "built $OUT"
