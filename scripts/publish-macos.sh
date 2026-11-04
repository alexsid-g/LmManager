#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
PROJECT="$(find "$ROOT_DIR" -maxdepth 2 -name 'LmManager.csproj' -print -quit)"
if [[ -z "$PROJECT" ]]; then
  echo "Could not find LmManager.csproj under $ROOT_DIR." >&2
  exit 1
fi

PROJECT_DIR="$(dirname "$PROJECT")"
PUBLISH_DIR="$PROJECT_DIR/bin/Release/net10.0/osx-arm64/publish"
APP_DIR="$ROOT_DIR/artifacts/macos/LMManager.app"

dotnet publish "$PROJECT" \
  --configuration Release \
  --runtime osx-arm64 \
  --self-contained true

rm -rf "$APP_DIR"
mkdir -p "$APP_DIR/Contents/MacOS" "$APP_DIR/Contents/Resources"
ditto "$PUBLISH_DIR" "$APP_DIR/Contents/MacOS"
cp "$PROJECT_DIR/Info.plist" "$APP_DIR/Contents/Info.plist"
cp "$PROJECT_DIR/Assets/lm-manager-logo.icns" \
  "$APP_DIR/Contents/Resources/lm-manager-logo.icns"
printf 'APPL????' > "$APP_DIR/Contents/PkgInfo"
chmod +x "$APP_DIR/Contents/MacOS/LmManager"

plutil -lint "$APP_DIR/Contents/Info.plist"
printf 'Published app bundle: %s\n' "$APP_DIR"
