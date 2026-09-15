#!/usr/bin/env bash
#
# Packages the published output as a macOS application bundle.
#
# A bare executable shows up in the Dock as a black square labelled with the
# file name; macOS needs a bundle with an Info.plist and an .icns before it
# treats something as an application.
#
# Apple Silicon refuses to run a binary carrying no signature at all, so the
# bundle is ad-hoc signed. That is not the same as being signed for
# distribution: without a paid developer identity the first launch is still
# refused, and the user has to allow it explicitly.
#
set -euo pipefail

CONFIGURATION="${1:-Release}"
RUNTIME="${2:-osx-arm64}"

ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/../.." && pwd)"
PROJECT="$ROOT/WahooFitToGarmin.UI/WahooFitToGarmin.UI.csproj"
STAGING="$ROOT/artifacts/macos/$RUNTIME"
APP="$STAGING/Wahoo Fit To Garmin.app"

say() { printf '\033[1m==>\033[0m %s\n' "$1"; }

say "Publishing for $RUNTIME ($CONFIGURATION)"
rm -rf "$STAGING"
dotnet publish "$PROJECT" \
    --configuration "$CONFIGURATION" \
    --runtime "$RUNTIME" \
    --self-contained true \
    -p:PublishSingleFile=false \
    --output "$STAGING/publish" \
    --nologo \
    --verbosity quiet

say "Assembling the bundle"
mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"

# The version lives in the csproj and nowhere else. Substituting it here means
# the bundle cannot quietly disagree with the binary it wraps.
VERSION=$(sed -n 's|.*<Version>\(.*\)</Version>.*|\1|p' "$ROOT/WahooFitToGarmin.UI/WahooFitToGarmin.UI.csproj")
if [ -z "$VERSION" ]; then
    echo "  could not read <Version> from the UI csproj" >&2
    exit 1
fi
sed "s/__VERSION__/$VERSION/g" "$ROOT/build/macos/Info.plist" > "$APP/Contents/Info.plist"
cp "$ROOT/WahooFitToGarmin.UI/Assets/AppIcon.icns" "$APP/Contents/Resources/AppIcon.icns"
cp -R "$STAGING/publish/." "$APP/Contents/MacOS/"

chmod +x "$APP/Contents/MacOS/WahooFitToGarmin"

say "Signing ad-hoc"
# Required for the binary to execute at all on Apple Silicon. Deep, because the
# bundle carries the runtime's own native libraries.
codesign --force --deep --sign - "$APP"
codesign --verify --verbose=1 "$APP" 2>&1 | sed 's/^/    /'

say "Done"
echo "    $APP"
echo
echo "First launch: macOS will refuse an application without a developer"
echo "identity. Either remove the quarantine attribute:"
echo "    xattr -dr com.apple.quarantine \"$APP\""
echo "or open System Settings, Privacy and Security, and allow it there."
