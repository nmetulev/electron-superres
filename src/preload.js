// See the Electron documentation for details on how to use preload scripts:
// https://www.electronjs.org/docs/latest/tutorial/process-model#preload-scripts

const { contextBridge, ipcRenderer } = require('electron');

// Expose protected methods to the renderer process via context bridge
contextBridge.exposeInMainWorld('superResolution', {
    // Check if Image Super Resolution is available on this device
    isAvailable: () => ipcRenderer.invoke('superres:isAvailable'),
    
    // Get the ready state of the model
    getReadyState: () => ipcRenderer.invoke('superres:getReadyState'),
    
    // Ensure the model is ready (downloads if necessary)
    ensureModelReady: () => ipcRenderer.invoke('superres:ensureModelReady'),
    
    // Scale an image with a given scale factor (1-8)
    scaleImage: (inputPath, outputPath, scaleFactor) => 
        ipcRenderer.invoke('superres:scaleImage', inputPath, outputPath, scaleFactor),
    
    // Sharpen an image without scaling
    sharpenImage: (inputPath, outputPath) => 
        ipcRenderer.invoke('superres:sharpenImage', inputPath, outputPath),
    
    // Open file dialog to select an image
    selectImage: () => ipcRenderer.invoke('dialog:selectImage'),
    
    // Get the app's temp directory path
    getTempPath: () => ipcRenderer.invoke('app:getTempPath')
});
