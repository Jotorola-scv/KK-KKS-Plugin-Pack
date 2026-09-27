#!/bin/sh
# Builds Single Side Clothes with the .NET SDK's Roslyn compiler against a game install's own assemblies (no NuGet).
#
#   sh build/build.sh KK  "D:/Games/Koikatsu"            -> bin/KK_SingleSideClothes.dll
#   sh build/build.sh KKS "D:/Games/Koikatsu Sunshine"   -> bin/KKS_SingleSideClothes.dll
#
# Needs the modding API (KKAPI / KKSAPI) and ExtensibleSaveFormat installed in that game folder.
# CSC can be overridden, e.g. CSC="C:/Program Files/dotnet/sdk/8.0.425/Roslyn/bincore/csc.dll".
set -e
GAMEKIND="$1"
G="$2"
if [ -z "$GAMEKIND" ] || [ -z "$G" ]; then
    echo "usage: sh build/build.sh KK|KKS <game folder>"; exit 1
fi
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
if [ -z "$CSC" ]; then
    CSC="$(ls -d "C:/Program Files/dotnet/sdk/"*/Roslyn/bincore/csc.dll 2>/dev/null | tail -1)"
fi
[ -f "$CSC" ] || { echo "csc.dll not found - install the .NET SDK or set CSC"; exit 1; }
find1() { find "$G/BepInEx/plugins" -iname "$1" | head -1; }

if [ "$GAMEKIND" = "KK" ]; then
    # Koikatu_Data's Assembly-CSharp also contains the Studio types; UniRx lives in Assembly-CSharp-firstpass.
    for d in Koikatu_Data CharaStudio_Data; do [ -d "$G/$d/Managed" ] && M="$G/$d/Managed" && break; done
    API="$(find1 KKAPI.dll)"; ESF="$(find1 ExtensibleSaveFormat.dll)"
    set -- "-r:$M/mscorlib.dll" "-r:$M/System.dll" "-r:$M/System.Core.dll" "-r:$M/UnityEngine.dll" "-r:$M/UnityEngine.UI.dll" \
           "-r:$M/Assembly-CSharp.dll" "-r:$M/Assembly-CSharp-firstpass.dll"
elif [ "$GAMEKIND" = "KKS" ]; then
    for d in KoikatsuSunshine_Data CharaStudio_Data; do [ -d "$G/$d/Managed" ] && M="$G/$d/Managed" && break; done
    API="$(find1 KKSAPI.dll)"; ESF="$(find1 KKS_ExtensibleSaveFormat.dll)"
    set -- "-define:KKS" "-r:$M/mscorlib.dll" "-r:$M/System.dll" "-r:$M/System.Core.dll" "-r:$M/netstandard.dll" \
           "-r:$M/UnityEngine.dll" "-r:$M/UnityEngine.CoreModule.dll" "-r:$M/UnityEngine.UI.dll" \
           "-r:$M/Assembly-CSharp.dll" "-r:$M/Assembly-CSharp-firstpass.dll" "-r:$M/UniRx.dll"
else
    echo "first argument must be KK or KKS"; exit 1
fi
[ -n "$M" ] || { echo "Managed folder not found under $G"; exit 1; }
[ -n "$API" ] || { echo "KKAPI/KKSAPI dll not found in $G/BepInEx/plugins"; exit 1; }
[ -n "$ESF" ] || { echo "ExtensibleSaveFormat dll not found in $G/BepInEx/plugins"; exit 1; }

mkdir -p "$ROOT/bin"
OUT="$ROOT/bin/${GAMEKIND}_SingleSideClothes.dll"
dotnet "$CSC" -nologo -noconfig -nostdlib+ -target:library -langversion:7.3 -optimize+ -deterministic -utf8output \
    -out:"$OUT" "$@" "-r:$G/BepInEx/core/BepInEx.dll" "-r:$API" "-r:$ESF" \
    "$ROOT/src/SingleSideClothes.cs"
echo "built $OUT"
