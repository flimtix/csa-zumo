#!/bin/bash
set -euo pipefail

TARGET_DIR="$1"
PROJECT_NAME="$2"

rsync -az --delete "$TARGET_DIR" "pi@hslu-rpi:netcore/$PROJECT_NAME"