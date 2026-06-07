# Unity Integration for HoloDraft - MetaQuest AR CAD Editor

This directory contains Unity scripts and configurations to connect your React CAD editor website with Unity for MetaQuest AR experiences.

## Architecture Overview

```
React Web App (Supabase) ↔ Unity WebGL Build ↔ MetaQuest AR App
```

## Components

### 1. Unity WebGL Build
- Embedded in React app via iframe
- Handles 3D model rendering
- Manages real-time updates
- Communicates with React via JavaScript bridge

### 2. MetaQuest AR Application
- Standalone Unity app for AR viewing/editing
- Connects to Supabase for real-time sync
- Hand tracking and spatial interactions
- Real-time collaborative editing

### 3. Communication Bridge
- WebGL ↔ React communication
- Supabase real-time subscriptions
- File synchronization
- State management

## Setup Instructions

### Prerequisites
- Unity 2022.3 LTS or later
- MetaQuest SDK
- Oculus Integration package
- WebGL build support

### 1. Unity Project Setup
1. Create new Unity 3D project
2. Import Oculus Integration from Asset Store
3. Copy scripts from this directory to Assets/Scripts/
4. Configure build settings for WebGL and Android (Quest)

### 2. WebGL Integration
1. Build Unity project for WebGL
2. Host WebGL build on your web server
3. Embed in React component using provided wrapper

### 3. MetaQuest Deployment
1. Configure XR settings for Oculus Quest
2. Build and deploy to Quest device
3. Configure Supabase connection

## File Structure

```
unity-integration/
├── Scripts/
│   ├── WebGL/
│   │   ├── WebGLBridge.cs          # React ↔ Unity communication
│   │   ├── CADModelLoader.cs       # Load FBX models from web
│   │   └── RealtimeSync.cs         # Supabase real-time sync
│   ├── AR/
│   │   ├── ARCADEditor.cs          # Main AR editing controller
│   │   ├── HandTrackingController.cs # Hand gesture controls
│   │   ├── ModelManipulator.cs     # 3D model manipulation
│   │   └── CollaborativeEditor.cs  # Multi-user editing
│   └── Shared/
│       ├── SupabaseClient.cs       # Supabase Unity client
│       ├── CADModel.cs             # Model data structure
│       └── NetworkManager.cs       # Network communication
├── Prefabs/
│   ├── CADViewer.prefab           # WebGL viewer prefab
│   ├── ARController.prefab        # AR controller setup
│   └── HandUI.prefab              # AR hand-based UI
├── Materials/
│   ├── CADMaterial.mat            # Default CAD material
│   └── HighlightMaterial.mat      # Selection highlight
└── React/
    ├── UnityWebGLWrapper.tsx      # React Unity wrapper
    ├── ARSessionManager.tsx       # AR session management
    └── RealtimeEditor.tsx         # Real-time editing UI
```

## Features

### WebGL Features
- ✅ 3D model viewing
- ✅ Basic rotation/zoom controls
- ✅ Material editing
- ✅ Real-time updates from React
- ✅ Export modified models

### AR Features (MetaQuest)
- ✅ Spatial 3D model placement
- ✅ Hand tracking for manipulation
- ✅ Voice commands
- ✅ Real-time collaborative editing
- ✅ Measurement tools
- ✅ Cross-sectional views
- ✅ Annotation system

### Real-time Sync
- ✅ Model transformations
- ✅ Material changes
- ✅ Collaborative cursors
- ✅ Chat/annotations
- ✅ Version control

## API Endpoints

### New endpoints for Unity integration:
- `GET /api/unity/model/:fileId` - Get model for Unity
- `POST /api/unity/sync` - Sync model changes
- `WebSocket /ws/collaborative/:sessionId` - Real-time collaboration

## Development Workflow

1. **Web Development**: Develop and test in React app
2. **WebGL Testing**: Test Unity WebGL integration
3. **AR Development**: Build and test AR features on Quest
4. **Integration**: Ensure seamless sync between all platforms

## Deployment

### WebGL
- Build Unity project for WebGL
- Deploy to your web server
- Update React component paths

### MetaQuest
- Build Unity project for Android
- Deploy via Oculus Developer Hub
- Distribute via App Lab or sideloading
