# HoloDraft

**Upload a CAD/3D file in the browser, edit and annotate it in AR, collaborate in a shared session, and send it to a 3D printer — a full pipeline from file to spatial model to physical print.**

HoloDraft is an end-to-end AR CAD platform: a React web app and Node backend handle upload, conversion, accounts, and printing; a Unity (C#/XR) application provides the in-headset CAD editor, compiled to WebGL and embedded back into the web app. This repository contains the **complete stack** — web frontend, backend, the Unity source, and the compiled WebGL build — in one place.

![Stack](https://img.shields.io/badge/stack-React%20%C2%B7%20Node%20%C2%B7%20Unity%20(C%23)%20%C2%B7%20Supabase-22c55e)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](LICENSE)

---

## Architecture

```
 Browser (React + TS)
   ├─ upload .stl / .obj  ──►  Node/Express backend  ──►  Python (trimesh) STL→mesh conversion
   ├─ Supabase            ◄──►  auth · projects · printers · print jobs (Postgres)
   └─ embeds Unity WebGL  ◄──►  Unity bridge (unity-bridge.js)
                                   │
 Unity (C#, XR)  ──build──►  WebGL  ─┘
   └─ CAD editor: load model, manipulate, measure, dimension, annotate; AR session (XR/Quest)
```

Five layers, all in this repo:

| Layer | Where | What it does |
|---|---|---|
| **Web frontend** | `src/` | React + TypeScript app: upload zone, dashboard, project workspace, AR/WebGL viewers, collaboration, print dialog, auth |
| **Backend** | `backend/` | Express server; `/convert` accepts an upload and runs `scripts/convert_stl_to_fbx.py` to produce a web-ready mesh |
| **Unity source** | `unity/` | C# CAD editor — `Assets/Scripts/CAD/*` (model, interaction, measurement, dimensioning, annotation, materials), `Editor/*` build tooling, and `unity-integration/Scripts/AR/ARCADEditor.cs` (XR session, Quest config) |
| **Compiled AR build** | `public/unity-builds/` | WebGL loader + template for the embedded build. The heavy build artifacts (`Build/*.wasm/.data`) are gitignored (GitHub size limits) — regenerate from `unity/` (see `unity/UNITY_SETUP.md`) |
| **Data** | `supabase/` + `src/lib/supabaseClient.ts` | Supabase (Postgres) auth and schema for users, projects, printers, and print jobs |

## Key components (real, in this repo)

- **File → mesh conversion** — `backend/server.js` + `backend/scripts/convert_stl_to_fbx.py`: upload an STL/OBJ, get back a WebGL-loadable model.
- **Unity CAD editor** — `unity/Assets/Scripts/CAD/`: `CADModel`, `CADModelInteraction`, `CADMeasurementTools`, `CADDimension`, `CADAnnotation`, `CADMaterialManager`, plus `CADToolbar` UI.
- **AR session** — `unity/unity-integration/Scripts/AR/ARCADEditor.cs`: configures Unity XR (targets Meta Quest) and starts an AR session for a given model.
- **Web ↔ Unity bridge** — `public/unity-bridge.js` + `unity-integration/Scripts/WebGL/WebGLBridge.cs` / `CADModelLoader.cs`: passes model URLs and events between the React app and the embedded WebGL build.
- **Auth & data** — Supabase email auth (`AuthPage.tsx` → `supabaseClient.ts`); schema in `supabase/schema.sql`.
- **3D printing** — `src/lib/printService.ts`: Supabase-backed printer and print-job model (USB/network/Bluetooth printer records, job status tracking).
- **Collaboration** — `MultiUserARSession.tsx`, `CollaborationModal.tsx`: scaffolding for shared AR sessions.

## Status — what's solid vs. prototype

This started as a hackathon build and is an **active prototype**, not a finished product. Being precise about that:

| Solid | Prototype / scaffolded |
|---|---|
| STL/OBJ upload + server-side conversion | Multi-user AR sync (UI + session scaffolding; no realtime backend wired) |
| React app, routing, dashboard, project workspace | 3D printing (data model + UI present; no live printer driver/discovery) |
| Supabase email auth + schema | Headset coverage: Unity XR is configured for **Quest**; other headsets are not implemented |
| Unity CAD editor C# source + compiled WebGL build | `src/lib/mockAuth.ts` exists as a local fallback path |

## Run it

```bash
# 1. Web frontend
npm install
npm start                      # http://localhost:3000

# 2. Backend (separate terminal) — needs Python with trimesh for conversion
cd backend && npm install && node server.js   # http://localhost:3001
pip install trimesh

# 3. Supabase — create a project, run supabase/schema.sql, set keys in .env
#    REACT_APP_SUPABASE_URL=...  REACT_APP_SUPABASE_ANON_KEY=...

# 4. Unity (to build the AR editor) — open unity/ in Unity, build WebGL
#    into public/unity-builds/. The build artifacts are gitignored, so this
#    step is required to run the embedded AR viewer locally.
```

See `DEPLOYMENT_INSTRUCTIONS.md` for the Vercel deployment path and `unity/UNITY_SETUP.md` for the Unity build steps.

## Repository layout

```
src/                 React + TypeScript web app (main CRA application)
website/             HoloDraft landing/auth site (Vite + React + Tailwind, Supabase auth)
backend/             Express server + STL→mesh conversion (Python)
public/              static assets + prebuilt Unity WebGL (unity-builds/)
unity/               Unity CAD editor source
  Assets/Scripts/CAD/    model, interaction, measurement, dimensioning, annotation
  Assets/Editor/         STL→FBX import + WebGL build tooling
  unity-integration/     AR session, WebGL bridge, React wrapper
supabase/            Postgres schema + SQL integration
```

## License

MIT — see [LICENSE](LICENSE).

