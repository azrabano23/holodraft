import React, { useEffect, useRef, useState, useCallback } from 'react';
import { HoloDraftFile } from '../../src/lib/supabaseClient';

interface UnityInstance {
  SendMessage: (objectName: string, methodName: string, value?: any) => void;
  Quit: () => Promise<void>;
}

interface UnityWebGLWrapperProps {
  file: HoloDraftFile;
  onUnityReady?: () => void;
  onModelLoaded?: (fileId: string) => void;
  onModelTransformed?: (fileId: string, transform: any) => void;
  onARSessionStatus?: (isActive: boolean, sessionId?: string) => void;
  onError?: (error: string) => void;
  width?: string;
  height?: string;
  className?: string;
}

interface UnityEvent {
  type: string;
  data: any;
}

interface UnityWebGLWrapperRef {
  transformModel: (position: any, rotation: any, scale: any) => void;
  updateMaterial: (materialName: string, color: any, metallic: number, roughness: number) => void;
  startARSession: (config?: any) => void;
  stopARSession: () => void;
  exportModel: (format?: string) => void;
  loadModel: () => void;
}

export const UnityWebGLWrapper = React.forwardRef<UnityWebGLWrapperRef, UnityWebGLWrapperProps>(
  (
    {
      file,
      onUnityReady,
      onModelLoaded,
      onModelTransformed,
      onARSessionStatus,
      onError,
      width = '100%',
      height = '600px',
      className = ''
    },
    ref
  ) => {
  const canvasRef = useRef<HTMLCanvasElement>(null);
  const [unityInstance, setUnityInstance] = useState<UnityInstance | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [loadingProgress, setLoadingProgress] = useState(0);
  const [unityStatus, setUnityStatus] = useState<'loading' | 'ready' | 'error'>('loading');

  // Unity event handlers
  const handleUnityMessage = useCallback((eventType: string, data: string) => {
    try {
      const parsedData = JSON.parse(data);
      
      switch (eventType) {
        case 'onUnityReady':
          setUnityStatus('ready');
          setIsLoading(false);
          onUnityReady?.();
          // Automatically load the model when Unity is ready
          loadModelInUnity();
          break;
          
        case 'onModelLoadProgress':
          setLoadingProgress(parsedData.progress * 100);
          if (parsedData.status === 'complete') {
            onModelLoaded?.(parsedData.fileId);
          } else if (parsedData.status === 'error') {
            onError?.(`Model loading failed: ${parsedData.fileId}`);
          }
          break;
          
        case 'onModelTransformed':
          onModelTransformed?.(parsedData.fileId, parsedData.transform);
          break;
          
        case 'onARSessionStatus':
          onARSessionStatus?.(parsedData.isActive, parsedData.sessionId);
          break;
          
        case 'onModelExported':
          console.log('Model exported:', parsedData);
          break;
          
        default:
          console.log('Unknown Unity event:', eventType, parsedData);
      }
    } catch (error) {
      console.error('Error parsing Unity message:', error);
      onError?.('Error communicating with Unity');
    }
  }, [onUnityReady, onModelLoaded, onModelTransformed, onARSessionStatus, onError]);

  // Load Unity WebGL build
  useEffect(() => {
    if (!canvasRef.current) return;

    const canvas = canvasRef.current;
    canvas.id = 'unity-canvas-' + Date.now();
    
    // Initialize Unity using the bridge
    if (window.unityBridge) {
      try {
        const instance = window.unityBridge.initialize(canvas.id);
        setUnityInstance(instance);
        
        // Register message handlers
        window.unityMessageHandler = handleUnityMessage;
        
        // Listen for Unity messages
        const handleUnityMessage = (event: CustomEvent) => {
          const { type, data } = event.detail;
          handleUnityMessage(type, JSON.stringify(data));
        };
        
        window.addEventListener('unityMessage', handleUnityMessage);
        
        // Cleanup
        return () => {
          window.removeEventListener('unityMessage', handleUnityMessage);
          if (instance) {
            instance.Quit();
          }
        };
      } catch (error) {
        console.error('Unity loading error:', error);
        setUnityStatus('error');
        onError?.('Failed to load Unity viewer');
      }
    } else {
      console.error('Unity bridge not available');
      setUnityStatus('error');
      onError?.('Unity bridge not available');
    }
  }, [handleUnityMessage]);

  // Load model in Unity
  const loadModelInUnity = useCallback(() => {
    if (!unityInstance || !file.converted_url) return;

    const fileLoadData = {
      fileId: file.id,
      fileName: file.original_name,
      downloadUrl: file.converted_url,
      format: 'fbx', // Converted format
      fileSize: file.file_size
    };

    unityInstance.SendMessage('WebGLBridge', 'LoadCADModel', JSON.stringify(fileLoadData));
  }, [unityInstance, file]);

  // Public methods for parent components
  const transformModel = useCallback((position: any, rotation: any, scale: any) => {
    if (!unityInstance) return;

    const transformData = {
      fileId: file.id,
      position: position,
      rotation: rotation,
      scale: scale
    };

    unityInstance.SendMessage('WebGLBridge', 'TransformModel', JSON.stringify(transformData));
  }, [unityInstance, file.id]);

  const updateMaterial = useCallback((materialName: string, color: any, metallic: number, roughness: number) => {
    if (!unityInstance) return;

    const materialData = {
      fileId: file.id,
      materialName: materialName,
      color: color,
      metallic: metallic,
      roughness: roughness
    };

    unityInstance.SendMessage('WebGLBridge', 'UpdateModelMaterial', JSON.stringify(materialData));
  }, [unityInstance, file.id]);

  const startARSession = useCallback((config: any = {}) => {
    if (!unityInstance) return;

    const arConfig = {
      fileId: file.id,
      enableHandTracking: true,
      enableCollaboration: false,
      collaborationRoomId: null,
      ...config
    };

    unityInstance.SendMessage('WebGLBridge', 'StartARSession', JSON.stringify(arConfig));
  }, [unityInstance, file.id]);

  const stopARSession = useCallback(() => {
    if (!unityInstance) return;
    unityInstance.SendMessage('WebGLBridge', 'StopARSession');
  }, [unityInstance]);

  const exportModel = useCallback((format: string = 'fbx') => {
    if (!unityInstance) return;

    const exportConfig = {
      fileId: file.id,
      format: format,
      includeAnimations: false,
      includeMaterials: true
    };

    unityInstance.SendMessage('WebGLBridge', 'ExportModel', JSON.stringify(exportConfig));
  }, [unityInstance, file.id]);

  // Expose methods to parent component
  React.useImperativeHandle(ref, () => ({
    transformModel,
    updateMaterial,
    startARSession,
    stopARSession,
    exportModel,
    loadModel: loadModelInUnity
  }));

  return (
    <div className={`unity-webgl-wrapper ${className}`} style={{ width, height }}>
      {isLoading && (
        <div className="unity-loading-overlay">
          <div className="loading-content">
            <div className="loading-spinner"></div>
            <div className="loading-text">
              {unityStatus === 'loading' ? 'Loading Unity CAD Viewer...' : 'Loading Model...'}
            </div>
            <div className="loading-progress">
              <div 
                className="progress-bar" 
                style={{ width: `${loadingProgress}%` }}
              ></div>
              <span className="progress-text">{Math.round(loadingProgress)}%</span>
            </div>
          </div>
        </div>
      )}
      
      {unityStatus === 'error' && (
        <div className="unity-error-overlay">
          <div className="error-content">
            <h3>Failed to Load CAD Viewer</h3>
            <p>Please ensure Unity WebGL is supported in your browser.</p>
            <button onClick={() => window.location.reload()}>
              Retry
            </button>
          </div>
        </div>
      )}
      
      <canvas
        ref={canvasRef}
        style={{
          width: '100%',
          height: '100%',
          display: unityStatus === 'ready' ? 'block' : 'none'
        }}
      />
      
      <style>{`
        .unity-webgl-wrapper {
          position: relative;
          background: #1a1a1a;
          border-radius: 8px;
          overflow: hidden;
        }
        
        .unity-loading-overlay,
        .unity-error-overlay {
          position: absolute;
          top: 0;
          left: 0;
          right: 0;
          bottom: 0;
          display: flex;
          align-items: center;
          justify-content: center;
          background: rgba(0, 0, 0, 0.8);
          color: white;
          z-index: 10;
        }
        
        .loading-content,
        .error-content {
          text-align: center;
          padding: 2rem;
        }
        
        .loading-spinner {
          width: 40px;
          height: 40px;
          border: 4px solid #333;
          border-top: 4px solid #00d4ff;
          border-radius: 50%;
          animation: spin 1s linear infinite;
          margin: 0 auto 1rem;
        }
        
        @keyframes spin {
          0% { transform: rotate(0deg); }
          100% { transform: rotate(360deg); }
        }
        
        .loading-text {
          font-size: 1.1rem;
          margin-bottom: 1rem;
        }
        
        .loading-progress {
          width: 200px;
          margin: 0 auto;
        }
        
        .progress-bar {
          height: 4px;
          background: #00d4ff;
          border-radius: 2px;
          transition: width 0.3s ease;
        }
        
        .progress-text {
          display: block;
          margin-top: 0.5rem;
          font-size: 0.9rem;
          color: #ccc;
        }
        
        .error-content h3 {
          color: #ff4444;
          margin-bottom: 1rem;
        }
        
        .error-content button {
          background: #00d4ff;
          color: white;
          border: none;
          padding: 0.5rem 1rem;
          border-radius: 4px;
          cursor: pointer;
          margin-top: 1rem;
        }
        
        .error-content button:hover {
          background: #0099cc;
        }
        
        canvas {
          background: #1a1a1a;
        }
      `}</style>
    </div>
  );
});

export default UnityWebGLWrapper;
