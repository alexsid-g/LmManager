#!/usr/bin/env bash
set -euo pipefail

ROOT_DIR="$(cd "$(dirname "$0")/.." && pwd)"
APP_DIR="$ROOT_DIR/artifacts/macos/LMManager.app"

if [[ "$(uname -s)" != "Darwin" ]]; then
  echo "macOS code signing must run on macOS." >&2
  exit 1
fi

if [[ -z "${SIGNING_IDENTITY:-}" ]]; then
  echo "Set SIGNING_IDENTITY to your installed Developer ID Application identity." >&2
  exit 1
fi

if [[ ! -d "$APP_DIR" ]]; then
  echo "App bundle not found. Run the Publish macOS app task first." >&2
  exit 1
fi

codesign \
  --force \
  --deep \
  --options runtime \
  --timestamp \
  --sign "$SIGNING_IDENTITY" \
  "$APP_DIR"

codesign --verify --deep --strict --verbose=2 "$APP_DIR"
printf 'Signed app bundle: %s\n' "$APP_DIR"
