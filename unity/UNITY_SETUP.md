# Unity AR Integration Setup Guide

This guide will help you set up the complete Unity integration for HoloDraft, enabling AR CAD viewing and editing on MetaQuest devices.

## Overview

The integration consists of three main components:
1. **Unity WebGL Build** - Embedded 3D viewer in your web app
2. **Unity MetaQuest AR App** - Standalone AR application 
3. **Real-time Synchronization** - Supabase-powered live collaboration

## Prerequisites

### Software Requirements
- Unity 2022.3 LTS or later
- Visual Studio or VSCode with C# support
- Node.js 16+ 
- MetaQuest Developer Hub
- Blender 4.0+ (for advanced model processing)

### Hardware Requirements
- MetaQuest 2 or MetaQuest 3 device
- Development PC with adequate GPU
- Stable internet connection for real-time sync

### Accounts/Services
- Meta Developer Account (for Quest deployment)
- Supabase Project (already configured)
- Unity Account with valid license

## Step 1: Unity Project Setup

### 1.1 Create New Unity Project
```bash
# Open Unity Hub and create new 3D project
Project Name: HoloDraft-Unity
Template: 3D (Built-in Render Pipeline)
Unity Version: 2022.3 LTS
```

### 1.2 Install Required Packages
In Unity Package Manager, install:
- **Oculus Integration** (from Asset Store)
- **XR Interaction Toolkit**
- **XR Plugin Management**
- **Newtonsoft Json**
- **Unity WebGL Build Support**

### 1.3 Project Structure
Create the following folder structure in `Assets/`:
```
Assets/
├── Scripts/
│   ├── WebGL/
│   ├── AR/
│   └── Shared/
├── Prefabs/
├── Materials/
├── Scenes/
│   ├── WebGLViewer.unity
│   └── ARViewer.unity
└── StreamingAssets/
```

### 1.4 Copy Unity Scripts
Copy all C# scripts from `unity-integration/Scripts/` to your Unity project's `Assets/Scripts/` folder.

## Step 2: WebGL Configuration

### 2.1 Build Settings for WebGL
1. Go to **File > Build Settings**
2. Select **WebGL** platform
3. Click **Switch Platform**
4. Add scenes:
   - `Scenes/WebGLViewer.unity`

### 2.2 WebGL Player Settings
1. Go to **Edit > Project Settings > Player > WebGL**
2. Configure:
   - **Company Name**: HoloDraft
   - **Product Name**: CAD Viewer
   - **Resolution and Presentation**:
     - Default Canvas Width: 1920
     - Default Canvas Height: 1080
     - Run In Background: ✓
   - **Publishing Settings**:
     - Compression Format: Gzip
     - Name Files As Hashes: ✓
     - Data caching: ✓

### 2.3 WebGL Template
1. Create custom WebGL template in `Assets/WebGLTemplates/HoloDraft/`
2. Copy the Unity bridge JavaScript to template
3. Set template in Player Settings

### 2.4 Build WebGL
```bash
# Build for WebGL
Build Location: /path/to/your/react-app/public/unity-builds/webgl/
```

## Step 3: MetaQuest AR Configuration

### 3.1 XR Settings
1. Go to **Edit > Project Settings > XR Plug-in Management**
2. Enable **Oculus** provider
3. In **Oculus** settings:
   - Target Device: Quest 2 & 3
   - Stereo Rendering Mode: Multi Pass
   - V2 Signing: ✓

### 3.2 Android Build Settings
1. Go to **File > Build Settings**
2. Select **Android** platform
3. Click **Switch Platform**
4. Add scenes:
   - `Scenes/ARViewer.unity`

### 3.3 Android Player Settings
1. Go to **Edit > Project Settings > Player > Android**
2. Configure:
   - **Company Name**: HoloDraft
   - **Product Name**: HoloDraft AR
   - **Package Name**: com.holodraft.ar
   - **Minimum API Level**: Android 10.0 (API 29)
   - **Target API Level**: Android 13.0 (API 33)
   - **Scripting Backend**: IL2CPP
   - **Target Architectures**: ARM64 ✓

### 3.4 Quest-Specific Settings
1. In **Publishing Settings**:
   - Custom Main Manifest: Enable
   - Custom Main Gradle Template: Enable
2. Add Quest permissions in AndroidManifest.xml

## Step 4: Scene Setup

### 4.1 WebGL Scene (WebGLViewer.unity)
1. Create new scene
2. Add GameObject with `WebGLBridge` script
3. Add GameObject with `CADModelLoader` script
4. Set up default camera and lighting
5. Configure UI Canvas for controls

### 4.2 AR Scene (ARViewer.unity)
1. Create new scene
2. Add **OVRCameraRig** prefab from Oculus Integration
3. Add GameObject with `ARCADEditor` script
4. Add GameObject with `HandTrackingController` scripts
5. Set up AR-specific lighting and post-processing

## Step 5: Backend API Extensions

### 5.1 Add Unity Endpoints
Add these endpoints to your Express server (`backend/server.js`):

```javascript
// Unity-specific endpoints
app.get('/api/unity/model/:fileId', async (req, res) => {
  // Serve model data optimized for Unity
});

app.post('/api/unity/sync', async (req, res) => {
  // Handle real-time model synchronization
});

app.ws('/ws/collaborative/:sessionId', (ws, req) => {
  // WebSocket for real-time collaboration
});
```

### 5.2 Supabase Real-time Setup
Add real-time subscriptions for collaborative editing:

```sql
-- Enable real-time for Unity integration
ALTER PUBLICATION supabase_realtime ADD TABLE holodraft_files;
ALTER PUBLICATION supabase_realtime ADD TABLE holodraft_analytics;
```

## Step 6: React Integration

### 6.1 Install Unity Bridge
Add the Unity bridge script to your React app:

```html
<!-- In public/index.html -->
<script src="/unity-bridge.js"></script>
<script src="/unity-builds/webgl/Build/UnityLoader.js"></script>
```

### 6.2 Use Unity Components
Update your main App component:

```jsx
import UnityCADViewer from './components/UnityCADViewer';

// In your file list, add button to open Unity viewer
<button onClick={() => setSelectedFile(file)}>
  🥽 View in Unity
</button>

// Render Unity viewer when file is selected
{selectedFile && (
  <UnityCADViewer 
    file={selectedFile}
    onClose={() => setSelectedFile(null)}
  />
)}
```

## Step 7: Development Workflow

### 7.1 Local Development
1. Start React dev server: `npm start`
2. Start backend server: `cd backend && npm run dev`
3. Serve Unity WebGL build from React public folder
4. Connect MetaQuest via USB for AR testing

### 7.2 Testing WebGL Integration
1. Upload CAD file through React app
2. Convert to FBX format
3. Open Unity viewer
4. Test model loading and manipulation
5. Verify real-time sync

### 7.3 Testing AR on MetaQuest
1. Build and deploy AR app to Quest
2. Enable Developer Mode on Quest device
3. Install via Quest Developer Hub
4. Test AR session connectivity
5. Verify hand tracking and model manipulation

## Step 8: Production Deployment

### 8.1 WebGL Deployment
1. Build optimized Unity WebGL build
2. Deploy to your web server or CDN
3. Update React app to reference production Unity build
4. Configure HTTPS for WebGL loading

### 8.2 MetaQuest App Distribution
Choose one of:
- **App Lab**: Public beta distribution
- **Private Channel**: Internal team distribution  
- **Sideloading**: Manual installation per device

### 8.3 Performance Optimization
- Enable **Managed Stripping Level**: High
- Use **IL2CPP Code Generation**: Faster (smaller) builds
- Optimize textures and models for mobile
- Implement LOD (Level of Detail) for complex models

## Step 9: Advanced Features

### 9.1 Hand Tracking Gestures
Implement custom gestures in `HandTrackingController.cs`:
- Pinch to select
- Grab to move
- Scale with two hands
- Voice commands

### 9.2 Collaborative Editing
Set up real-time collaboration:
- Multiple users in same AR session
- Live cursor positions
- Synchronized model changes
- Voice chat integration

### 9.3 Cloud Rendering
For complex models, implement cloud-based rendering:
- Stream high-quality visuals to Quest
- Reduce local processing requirements
- Enable more complex CAD file support

## Troubleshooting

### Common WebGL Issues
- **Unity not loading**: Check Unity loader script path
- **Models not appearing**: Verify file format and conversion
- **Performance issues**: Reduce model complexity or texture resolution

### Common AR Issues
- **Hand tracking not working**: Check Oculus Integration setup
- **Models too large/small**: Adjust `modelScaleFactor` in AR settings
- **Tracking lost**: Ensure adequate lighting and tracking space

### Supabase Connection Issues
- Check API keys and database connection
- Verify RLS policies for Unity endpoints
- Test real-time subscriptions

## Support Resources

- Unity Documentation: https://docs.unity3d.com/
- Oculus Developer Center: https://developer.oculus.com/
- Supabase Docs: https://supabase.com/docs
- Meta Quest Developer Hub: https://developer.oculus.com/downloads/

## Next Steps

1. Set up Unity project with provided scripts
2. Build and test WebGL integration
3. Configure MetaQuest development environment
4. Deploy and test AR functionality
5. Implement advanced features as needed

Your HoloDraft Unity integration should now be ready for AR CAD viewing and editing on MetaQuest devices!
