// Unity WebGL Bridge for React Integration - Updated with Mock Support
window.unityBridge = {
    unityInstance: null,
    
    initialize: function(canvasId) {
        console.log('🌉 Unity Bridge: Initializing...');
        
        // Load Unity instance
        if (window.UnityLoader) {
            this.unityInstance = window.UnityLoader.instantiate(canvasId, '/unity-builds/webgl/Build/Build.json');
            console.log('🌉 Unity Bridge: Instance created');
        } else {
            console.error('🌉 Unity Bridge: UnityLoader not found');
        }
        
        return this.unityInstance;
    },
    
    sendMessage: function(objectName, methodName, value) {
        if (this.unityInstance) {
            console.log('🌉 Unity Bridge: Sending message', objectName, methodName, value);
            this.unityInstance.SendMessage(objectName, methodName, value);
        } else {
            console.error('🌉 Unity Bridge: Unity instance not initialized');
        }
    },
    
    loadModel: function(fileData) {
        console.log('🌉 Unity Bridge: Loading model', fileData);
        this.sendMessage('WebGLBridge', 'LoadCADModel', JSON.stringify(fileData));
    },
    
    transformModel: function(transformData) {
        console.log('🌉 Unity Bridge: Transforming model', transformData);
        this.sendMessage('WebGLBridge', 'TransformModel', JSON.stringify(transformData));
    },
    
    updateMaterial: function(materialData) {
        console.log('🌉 Unity Bridge: Updating material', materialData);
        this.sendMessage('WebGLBridge', 'UpdateModelMaterial', JSON.stringify(materialData));
    },
    
    startARSession: function(config) {
        console.log('🌉 Unity Bridge: Starting AR session', config);
        this.sendMessage('WebGLBridge', 'StartARSession', JSON.stringify(config));
    },
    
    stopARSession: function() {
        console.log('🌉 Unity Bridge: Stopping AR session');
        this.sendMessage('WebGLBridge', 'StopARSession');
    },
    
    exportModel: function(config) {
        console.log('🌉 Unity Bridge: Exporting model', config);
        this.sendMessage('WebGLBridge', 'ExportModel', JSON.stringify(config));
    }
};

// Global message handler for Unity to call
window.unityMessageHandler = function(eventType, data) {
    console.log('🌉 Unity Message:', eventType, data);
    
    // Dispatch custom event for React to listen to
    const event = new CustomEvent('unityMessage', {
        detail: { type: eventType, data: JSON.parse(data) }
    });
    window.dispatchEvent(event);
};

console.log('🌉 Unity Bridge initialized');
