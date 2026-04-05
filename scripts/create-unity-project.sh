#!/bin/bash
# Create Unity URP project for Afterhumans after Editor install completes.
# Usage: bash scripts/create-unity-project.sh
#
# Prerequisites:
#   - Unity Editor 6000.0.72f1 installed at $HOME/Applications/Unity/Hub/Editor/
#   - Unity Hub CLI available at /Applications/Unity Hub.app/Contents/MacOS/Unity Hub
#   - User logged in to Unity Hub (Personal license)
#   - Current directory = ~/afterhumans

set -e

PROJECT_PATH="$HOME/afterhumans"
UNITY_VERSION="6000.0.72f1"
UNITY_EDITOR="$HOME/Applications/Unity/Hub/Editor/$UNITY_VERSION/Unity.app/Contents/MacOS/Unity"
HUB_CLI="/Applications/Unity Hub.app/Contents/MacOS/Unity Hub"

if [ ! -x "$UNITY_EDITOR" ]; then
  echo "ERROR: Unity Editor not found at $UNITY_EDITOR"
  echo "Make sure install completed first."
  exit 1
fi

echo "=== Unity Editor found: $UNITY_EDITOR ==="
"$UNITY_EDITOR" -version 2>&1 | head -3 || true

echo ""
echo "=== Creating Unity project at $PROJECT_PATH ==="

# Unity will use existing Assets/ folder — we already have:
#   Assets/Dialogues/dataland.ink
#   Assets/_Project/Scripts/*.cs
#   Assets/_Project/Audio/ (empty folders)
#   Assets/_Project/Settings/*.md
#   Assets/_Project/Editor/BuildScript.cs
# And existing Packages/manifest.json with URP + Ink + Input System

# Run Unity once in batchmode to trigger project initialization
# -createProject won't overwrite existing files, just initializes the project structure
# -quit makes it exit after initialization

"$UNITY_EDITOR" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -logFile "$PROJECT_PATH/unity-first-open.log" \
  -createProject "$PROJECT_PATH" 2>&1 || {
    echo "Warning: first open had warnings. Check unity-first-open.log"
  }

echo ""
echo "=== Check project structure ==="
ls -la "$PROJECT_PATH/" | grep -E "Library|ProjectSettings|Packages|Assets"
echo ""
echo "=== Assets/ contents ==="
ls -la "$PROJECT_PATH/Assets/" | head

echo ""
echo "=== Install Ink Unity integration via git URL ==="
# Ink integration comes from inkle's GitHub:
#   https://github.com/inkle/ink-unity-integration
# We need to add it to Packages/manifest.json as a git URL dep.
# Already done at write-time if manifest.json includes the git URL.
# Otherwise: manual step later.

echo ""
echo "=== Next steps ==="
echo "1. Open project in Unity Hub GUI to verify it builds"
echo "2. Install Ink Unity integration: Window > Package Manager > + > Add from git URL"
echo "   URL: https://github.com/inkle/ink-unity-integration.git#upm"
echo "3. Compile dataland.ink → dataland.json (Inky editor or Ink integration auto-compile)"
echo "4. Test smoke build: Build Settings > Switch Platform > macOS Apple Silicon > Build"
echo ""
echo "=== DONE ==="
