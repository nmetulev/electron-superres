const { app, BrowserWindow, ipcMain, dialog } = require('electron');
const path = require('node:path');
const os = require('node:os');
const fs = require('node:fs');

// Load the C# addon for Image Super Resolution
let csAddon;
try {
  csAddon = require('../csAddon/dist/csAddon.node');
  console.log('✅ C# addon loaded successfully');
} catch (error) {
  console.error('❌ Failed to load C# addon:', error.message);
  console.log('Make sure to run "npm run build-csAddon" first');
}

// Handle creating/removing shortcuts on Windows when installing/uninstalling.
if (require('electron-squirrel-startup')) {
  app.quit();
}

const createWindow = () => {
  // Create the browser window.
  const mainWindow = new BrowserWindow({
    width: 1200,
    height: 800,
    webPreferences: {
      preload: path.join(__dirname, 'preload.js'),
    },
  });

  // and load the index.html of the app.
  mainWindow.loadFile(path.join(__dirname, 'index.html'));

  // Open the DevTools.
  mainWindow.webContents.openDevTools();
};

// IPC Handlers for Super Resolution APIs
ipcMain.handle('superres:isAvailable', async () => {
  if (!csAddon) return false;
  try {
    return await csAddon.Addon.isImageScalerAvailable();
  } catch (error) {
    console.error('Error checking availability:', error);
    return false;
  }
});

ipcMain.handle('superres:getReadyState', async () => {
  if (!csAddon) return 'Addon not loaded';
  try {
    return csAddon.Addon.getReadyState();
  } catch (error) {
    return `Error: ${error.message}`;
  }
});

ipcMain.handle('superres:ensureModelReady', async () => {
  if (!csAddon) return 'Addon not loaded';
  try {
    return await csAddon.Addon.ensureModelReady();
  } catch (error) {
    return `Error: ${error.message}`;
  }
});

ipcMain.handle('superres:scaleImage', async (event, inputPath, outputPath, scaleFactor) => {
  if (!csAddon) {
    return { success: false, message: 'Addon not loaded' };
  }
  try {
    return await csAddon.Addon.scaleImage(inputPath, outputPath, scaleFactor);
  } catch (error) {
    return { success: false, message: error.message };
  }
});

ipcMain.handle('superres:sharpenImage', async (event, inputPath, outputPath) => {
  if (!csAddon) {
    return { success: false, message: 'Addon not loaded' };
  }
  try {
    return await csAddon.Addon.sharpenImage(inputPath, outputPath);
  } catch (error) {
    return { success: false, message: error.message };
  }
});

ipcMain.handle('dialog:selectImage', async () => {
  const result = await dialog.showOpenDialog({
    properties: ['openFile'],
    filters: [
      { name: 'Images', extensions: ['jpg', 'jpeg', 'png', 'bmp', 'gif', 'webp'] }
    ]
  });
  
  if (result.canceled || result.filePaths.length === 0) {
    return null;
  }
  
  return result.filePaths[0];
});

ipcMain.handle('app:getTempPath', () => {
  const tempDir = path.join(os.tmpdir(), 'electron-superres');
  if (!fs.existsSync(tempDir)) {
    fs.mkdirSync(tempDir, { recursive: true });
  }
  return tempDir;
});

// This method will be called when Electron has finished
// initialization and is ready to create browser windows.
// Some APIs can only be used after this event occurs.
app.whenReady().then(() => {
  createWindow();

  // On OS X it's common to re-create a window in the app when the
  // dock icon is clicked and there are no other windows open.
  app.on('activate', () => {
    if (BrowserWindow.getAllWindows().length === 0) {
      createWindow();
    }
  });
});

// Quit when all windows are closed, except on macOS. There, it's common
// for applications and their menu bar to stay active until the user quits
// explicitly with Cmd + Q.
app.on('window-all-closed', () => {
  if (process.platform !== 'darwin') {
    app.quit();
  }
});

// In this file you can include the rest of your app's specific main process
// code. You can also put them in separate files and import them here.
