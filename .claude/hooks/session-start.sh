#!/bin/bash
# Prepares cloud sessions to run the repo's Python tooling tests.
# Unity Editor tests (EditMode, Tools/check_unity_sources.py) need a licensed,
# matching Editor (6000.3.24f1) and are not installed here.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
  exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

# Third-party modules imported by Tools/ and Tools/tests (bpy/mathutils excluded:
# those run only inside Blender).
python3 -m pip install --quiet pillow cryptography numpy

python3 -c "import PIL, cryptography, numpy" 
echo "Python tooling deps ready. Run: python3 -m unittest discover -s Tools/tests"
