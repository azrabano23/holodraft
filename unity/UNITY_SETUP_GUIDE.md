# Unity Setup Guide - HoloDraft CAD Editor

🎯 **Complete guide for teammates to set up and run the Unity AR CAD Editor project**

## 📋 Prerequisites

### Required Software
- **Unity Hub** (latest version)
- **Unity Editor 2022.3.x LTS** or newer
- **Git** (for cloning the repository)
- **Visual Studio Code** or **Visual Studio** (for C# development)

### Hardware Requirements
- **Development**: Any modern computer with at least 8GB RAM
- **AR Testing**: Meta Quest 2/3, HoloLens 2, or AR-compatible device
- **Graphics**: DirectX 11+ compatible graphics card

## 🚀 Quick Start

### 1. Clone the Repository
```bash
git clone https://github.com/azrabano23/cad-editor-react-website.git
cd cad-editor-react-website
```

### 2. Install Unity Hub
1. Download Unity Hub from [unity.com](https://unity.com/download)
2. Install Unity Editor 2022.3.x LTS through Unity Hub
3. Make sure to include the following modules:
   - **WebGL Build Support**
   - **Windows Build Support** (if on Windows)
   - **Android Build Support** (for Quest devices)
   - **Visual Studio Code Editor** (or Visual Studio)

### 3. Open the Project
1. Open Unity Hub
2. Click "Add" → "Add project from disk"
3. Navigate to the cloned repository folder
4. Select the root directory containing `Assets/`, `ProjectSettings/`, etc.
5. Click "Open"

## 📁 Project Structure

```
cad-editor-react-website/
├── Assets/
│   ├── Scenes/
│   │   └── SampleScene.unity          # Main CAD editor scene
│   ├── Scripts/
│   │   ├── CADManager.cs              # Core CAD management
│   │   ├── CADModel.cs                # CAD model data structure
│   │   ├── CADModelManager.cs         # Advanced CAD features
│   │   ├── CADAnnotation.cs           # 3D annotations
│   │   ├── CADDimension.cs            # Measurement tools
│   │   ├── CADModelInteraction.cs     # User interactions
│   │   ├── CADMaterialManager.cs      # Material system
│   │   ├── CADMeasurementTools.cs     # Measurement utilities
│   │   ├── CADToolbar.cs              # UI toolbar
│   │   └── CADAnalytics.cs            # Usage analytics
│   ├── MRTK/                          # Mixed Reality Toolkit
│   └── DefaultVolumeProfile.asset     # Rendering settings
├── ProjectSettings/                   # Unity project configuration
├── Packages/                          # Package dependencies
└── unity-integration/                 # Integration scripts
    └── Scripts/
        ├── WebGL/
        │   ├── WebGLBridge.cs         # React-Unity bridge
        │   └── CADModelLoader.cs      # Model loading
        └── AR/
            └── ARCADEditor.cs         # AR functionality
```

## 🛠️ Unity Setup Steps

### 1. Configure Build Settings
1. Go to **File** → **Build Settings**
2. Select your target platform:
   - **WebGL** (for web integration)
   - **Android** (for Quest devices)
   - **Universal Windows Platform** (for HoloLens)
3. Click "Switch Platform" if needed

### 2. Import MRTK (Mixed Reality Toolkit)
1. The MRTK packages are already included in the project
2. If you see import errors, go to **Window** → **Package Manager**
3. Check that these packages are installed:
   - **Mixed Reality Toolkit Foundation**
   - **Mixed Reality Toolkit Examples**
   - **Mixed Reality Toolkit Extensions**

### 3. Open the Main Scene
1. In the Project window, navigate to `Assets/Scenes/`
2. Double-click `SampleScene.unity` to open it
3. This is the main CAD editor scene with all components

### 4. Configure Scene Setup
The scene should contain these GameObjects:
- **CADManager** - Main management system
- **CADToolbar** - UI controls
- **Test Model** - Sample CAD model
- **Mixed Reality Toolkit** - AR/VR support
- **Main Camera** - Scene camera

If any are missing, you can recreate them:

#### Create CADManager GameObject:
1. Right-click in Hierarchy → **Create Empty**
2. Name it "CADManager"
3. Add Component → **CADManager** script
4. Add Component → **CADModelManager** script
5. Add Component → **CADMaterialManager** script

#### Create CADToolbar:
1. Right-click in Hierarchy → **UI** → **Canvas**
2. Name it "CADToolbar"
3. Add Component → **CADToolbar** script
4. Create child buttons for:
   - Wireframe Toggle
   - Exploded View
   - Measurement Tools
   - Reset View

## 🎮 Testing the CAD Editor

### 1. Play Mode Testing
1. Click the **Play** button in Unity
2. Use these controls:
   - **Mouse**: Rotate camera
   - **WASD**: Move camera
   - **Scroll**: Zoom in/out
   - **UI Buttons**: Test CAD features

### 2. Available Features
- **Wireframe Mode**: Toggle between solid and wireframe rendering
- **Exploded View**: Separate model components
- **Cross-Section**: Cut through models
- **Measurements**: Distance, angle, and volume calculations
- **Annotations**: Add 3D text labels
- **Material Editor**: Change model materials
- **Layer Management**: Show/hide model layers

### 3. WebGL Build Testing
1. Go to **File** → **Build Settings**
2. Select **WebGL** platform
3. Click "Build and Run"
4. Choose a build folder (e.g., `Build/`)
5. Unity will build and open in browser

## 🔧 Development Workflow

### 1. Core Scripts Overview

#### CADManager.cs
- Central controller for all CAD operations
- Handles model loading and scene management
- Integrates with React frontend via WebGL bridge

#### CADModelManager.cs
- Advanced CAD editing features
- Wireframe, exploded view, cross-sections
- Material and layer management

#### CADMeasurementTools.cs
- Distance, angle, and volume calculations
- 3D measurement visualization
- Precision measurement tools

#### CADModelInteraction.cs
- Mouse, touch, and gesture handling
- Model manipulation (rotate, scale, translate)
- Selection and highlighting

### 2. Adding New Features
1. Create new C# script in `Assets/Scripts/`
2. Inherit from `MonoBehaviour` if needed
3. Reference `CADManager` for core functionality
4. Add UI elements to `CADToolbar`
5. Test in Play Mode

### 3. AR Development
1. Switch to **Android** platform for Quest
2. Enable **XR Plug-in Management** in Project Settings
3. Configure **Oculus XR Plugin**
4. Test with **Quest Link** or build to device

## 📱 Building for Different Platforms

### WebGL (Web Integration)
```bash
# Build Settings
Platform: WebGL
Compression Format: Gzip
Development Build: ✓ (for debugging)
```

### Android (Quest Devices)
```bash
# Build Settings
Platform: Android
Texture Compression: ASTC
API Level: 29+
XR Settings: Oculus
```

### Universal Windows Platform (HoloLens)
```bash
# Build Settings
Platform: Universal Windows Platform
Target Family: Universal
SDK: Latest
Scripting Backend: IL2CPP
```

## 🐛 Common Issues & Solutions

### 1. MRTK Import Errors
```
Error: Mixed Reality Toolkit not found
Solution: Window → Package Manager → Install MRTK packages
```

### 2. Script Compilation Errors
```
Error: CS0246: The type or namespace name 'CADManager' could not be found
Solution: Check that all scripts are in Assets/Scripts/ folder
```

### 3. WebGL Build Errors
```
Error: WebGL build failed
Solution: 
1. Check Build Settings → Player Settings
2. Ensure WebGL template is selected
3. Disable Heavy scripts for WebGL
```

### 4. AR Camera Issues
```
Error: AR camera not tracking
Solution:
1. Check XR Plug-in settings
2. Ensure device permissions
3. Test with MRTK examples first
```

## 🔗 Integration with React Frontend

### WebGL Bridge
The Unity project communicates with the React frontend through:
- `WebGLBridge.cs` - Handles messages from React
- `CADModelLoader.cs` - Loads models from backend
- JavaScript interop for seamless integration

### API Commands
React can send these commands to Unity:
```javascript
// Load a new CAD model
unityInstance.SendMessage("CADManager", "LoadModel", modelData);

// Change view mode
unityInstance.SendMessage("CADManager", "SetWireframeMode", true);

// Get measurement data
unityInstance.SendMessage("CADManager", "GetMeasurements", "");
```

## 📊 Performance Optimization

### 1. Model Optimization
- Use LOD (Level of Detail) for complex models
- Optimize mesh topology
- Compress textures appropriately

### 2. Rendering Settings
- Use occlusion culling
- Configure shadow settings
- Optimize lighting

### 3. Memory Management
- Unload unused assets
- Use object pooling for UI elements
- Monitor memory usage in Profiler

## 🚀 Deployment

### 1. Development Environment
1. Test in Unity Editor Play Mode
2. Build WebGL for local testing
3. Deploy to React development server

### 2. Production Environment
1. Build optimized WebGL build
2. Configure for production server
3. Test with real CAD models

## 📚 Additional Resources

### Unity Documentation
- [Unity Manual](https://docs.unity3d.com/Manual/index.html)
- [Unity Scripting API](https://docs.unity3d.com/ScriptReference/index.html)
- [WebGL Development](https://docs.unity3d.com/Manual/webgl.html)

### MRTK Documentation
- [MRTK Getting Started](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/)
- [Hand Tracking](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/features/input/hand-tracking)
- [Spatial Mapping](https://docs.microsoft.com/en-us/windows/mixed-reality/mrtk-unity/features/spatial-awareness/spatial-awareness-getting-started)

### CAD Integration
- [FBX Import](https://docs.unity3d.com/Manual/FBXImporter.html)
- [Mesh Processing](https://docs.unity3d.com/Manual/MeshRenderer.html)
- [Animation System](https://docs.unity3d.com/Manual/AnimationOverview.html)

## 🤝 Team Development

### 1. Version Control
- Use Git LFS for large assets
- Ignore `Library/` and `Temp/` folders
- Commit `.meta` files with assets

### 2. Code Standards
- Follow Unity C# coding standards
- Use clear variable names
- Add XML documentation comments
- Test all public methods

### 3. Scene Management
- Save scenes before major changes
- Use prefabs for reusable components
- Keep scenes organized and clean

## 🔄 Updates and Maintenance

### 1. Unity Updates
- Keep Unity LTS version updated
- Test after each Unity update
- Update MRTK packages regularly

### 2. Package Management
- Monitor package dependencies
- Update packages cautiously
- Test thoroughly after updates

### 3. Performance Monitoring
- Use Unity Profiler regularly
- Monitor frame rates
- Optimize based on target platform

---

## 💡 Tips for Success

1. **Start Simple**: Begin with basic CAD viewing before adding complex features
2. **Test Early**: Build and test on target devices frequently
3. **Use Prefabs**: Create reusable components for common CAD elements
4. **Document Code**: Add comments and documentation for complex algorithms
5. **Performance First**: Always consider performance impact of new features

## 🆘 Getting Help

If you encounter issues:
1. Check Unity Console for error messages
2. Review this guide for common solutions
3. Check Unity Documentation
4. Ask the team in our development chat
5. Create GitHub issues for project-specific problems

---

**Happy Development! 🎉**

The HoloDraft CAD Editor project brings the future of CAD design to AR/VR. Follow this guide to get started, and don't hesitate to contribute new features and improvements!
