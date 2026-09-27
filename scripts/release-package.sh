#!/usr/bin/env bash
set -euo pipefail

TAG="${1:?usage: release-package.sh <PackageId>-v<Version>}"

PACKAGES=(
  "Corely.DataAccess:Corely.DataAccess/Corely.DataAccess.csproj"
)

fail() {
  echo "$TAG: $1" >&2
  exit 1
}

csproj_of() {
  for entry in "${PACKAGES[@]}"; do
    if [ "${entry%%:*}" = "$1" ]; then
      echo "${entry#*:}"
      return
    fi
  done
}

version_of() {
  sed -n 's:.*<Version>\(.*\)</Version>.*:\1:p' "$1" | head -1
}

case "$TAG" in
  *-v*) ;;
  *) fail "not <PackageId>-v<Version>" ;;
esac

id="${TAG%-v*}"
version="${TAG##*-v}"

[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+(-[0-9A-Za-z.-]+)?$ ]] || fail "'$version' is not a version"

csproj="$(csproj_of "$id")"
[ -n "$csproj" ] || fail "'$id' is not a package this repository publishes"

csproj_version="$(version_of "$csproj")"
[ "$csproj_version" = "$version" ] || fail "$csproj is at $csproj_version, not $version"

pack_args="-p:IncludeSymbols=true -p:SymbolPackageFormat=snupkg"

echo "Releasing $id $version from $csproj"
outputs="csproj=$csproj
pack_args=$pack_args"
if [ -n "${GITHUB_OUTPUT:-}" ]; then
  echo "$outputs" >> "$GITHUB_OUTPUT"
else
  echo "$outputs"
fi
