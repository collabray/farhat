#!/usr/bin/env bash
# Type-checks the Unity C# templates against minimal API stubs using Mono's mcs compiler.
# This catches typos, wrong signatures and missing members in OUR code. It cannot catch differences from the real Unity API,
# so the real compile in Unity (scripts/unity_cli.py compile) remains the final authority.
set -euo pipefail
HERE="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "$HERE/../.." && pwd)"
if ! command -v mcs >/dev/null 2>&1; then
  echo "mcs (Mono C# compiler) not found. Install mono-mcs (Debian/Ubuntu: apt-get install mono-mcs) to run this check." >&2
  exit 2
fi
WORK="$(mktemp -d)"
OUT="$WORK/farhat-check.dll"
# Mono's mcs supports C# 7 only. The templates use `readonly struct` (valid in Unity 6, C# 9), so strip that
# modifier in a scratch copy purely for this check. The shipped sources are not modified.
mkdir -p "$WORK/src"
(cd "$ROOT/templates/unity/Assets/Game" && find . -name '*.cs' | sort | while read -r f; do
  mkdir -p "$WORK/src/$(dirname "$f")"
  sed -E 's/\breadonly struct\b/struct/g' "$f" > "$WORK/src/$f"
done)
mcs -target:library -langversion:7 -nowarn:0169,0649,0414,0219,0067 -out:"$OUT" \
  "$HERE/UnityStubs.cs" \
  $(find "$WORK/src" -name '*.cs' | sort)
echo "C# type-check passed ($(find "$ROOT/templates/unity/Assets/Game" -name '*.cs' | wc -l) files)."
