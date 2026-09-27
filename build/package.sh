#!/bin/sh
# Builds every plugin for both games and copies the release assets to dist/:
#
#   sh build/package.sh <Koikatsu folder> <Koikatsu Sunshine folder> [Koikatsu folder with CharaStudio plugins]
#
# The third folder is only needed when KK's CharaStudio plugins (Pseudo Maker etc.) live in a separate install.
#
#   dist/KK_SingleSideClothes.dll      dist/KKS_SingleSideClothes.dll
#   dist/KK_PseudoMakerExtras.dll      dist/KKS_PseudoMakerExtras.dll
#   dist/KK_ShaderSwapperNullFix.dll   dist/KKS_ShaderSwapperNullFix.dll
#   dist/KK_KKUTSClothesCompat.dll     dist/KKS_KKUTSClothesRedirect.dll
#   dist/KK_StripTease2Timeline.dll    dist/KKS_StripTease2Timeline.dll
#
# READMEs and LICENSE live on the repository page; source code archives are attached to GitHub releases
# automatically from the tag.
set -e
KK_DIR="$1"
KKS_DIR="$2"
KK_STUDIO_DIR="${3:-$1}"
if [ -z "$KK_DIR" ] || [ -z "$KKS_DIR" ]; then
    echo "usage: sh build/package.sh <Koikatsu folder> <Koikatsu Sunshine folder> [KK CharaStudio folder]"; exit 1
fi
ROOT="$(cd "$(dirname "$0")/.." && pwd)"
B="$ROOT/build/build.sh"

# Single Side Clothes first: Pseudo Maker Extras references the freshly built dll.
for P in SingleSideClothes ShaderSwapperNullFix KKUTSClothesRedirect StripTease2Timeline; do
    sh "$B" "$P" KK "$KK_DIR"
    sh "$B" "$P" KKS "$KKS_DIR"
done
sh "$B" PseudoMakerExtras KK "$KK_STUDIO_DIR"
sh "$B" PseudoMakerExtras KKS "$KKS_DIR"

rm -rf "$ROOT/dist"
mkdir -p "$ROOT/dist"
cp "$ROOT/bin/"*.dll "$ROOT/dist/"
for f in "$ROOT"/*/*.cs; do
    printf '%-24s %s\n' "$(basename "$f" .cs)" "$(sed -n 's/.*public const string Version = "\([0-9.]*\)".*/\1/p' "$f")"
done
ls -l "$ROOT/dist"
