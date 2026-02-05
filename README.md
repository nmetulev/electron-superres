# Image Super Resolution - Electron Demo

> 🤖 **Built with AI assistance** - This sample was created using GitHub Copilot to demonstrate Windows AI APIs in an Electron app.

A demo Electron application showcasing the **Image Super Resolution** AI APIs from the Windows App SDK. This app uses NPU-accelerated machine learning to upscale and sharpen images up to 8x their original resolution.

![Demo Screenshot](docs/screenshot.png)

## What This Demo Shows

- ✅ Calling Windows AI APIs (`ImageScaler`) from Electron via a native C# addon
- ✅ Using [winapp CLI](https://github.com/microsoft/winappCli) to set up Windows SDK integration
- ✅ Running with app identity for testing protected Windows APIs
- ✅ Building a native addon with [node-api-dotnet](https://github.com/microsoft/node-api-dotnet)

## Requirements

| Requirement | Details |
|-------------|---------|
| **OS** | Windows 11 |
| **Hardware** | Copilot+ PC with NPU (for AI acceleration) |
| **Node.js** | v18 or later |
| **.NET SDK** | v10 or later |
| **Visual Studio** | With Native Desktop workload |

> ⚠️ **Note**: Image Super Resolution requires a Copilot+ PC with a Neural Processing Unit (NPU). On devices without an NPU, the app will display a helpful message explaining this requirement.

## Quick Start

```bash
# Install dependencies (automatically sets up Windows SDKs and debug identity)
npm install

# Build the C# native addon
npm run build-csAddon

# Run the app
npm start
```

## How It Was Built

This project was created following the [winapp CLI Electron guide](https://github.com/microsoft/winappCli/blob/main/docs/electron-get-started.md):

### 1. Create Electron App
```bash
npm create electron-app@latest electron-superres
cd electron-superres
```

### 2. Install winapp CLI
```bash
npm install --save-dev @microsoft/winappcli
```

### 3. Initialize Windows Development Environment
```bash
npx winapp init --use-defaults
```

This command:
- Downloads Windows SDK and Windows App SDK packages to `.winapp/`
- Creates `appxmanifest.xml` with app identity
- Generates `Assets/` folder with app icons
- Creates `devcert.pfx` for code signing
- Creates `winapp.yaml` for SDK version management

### 4. Create C# Native Addon
```bash
npx winapp node create-addon --template cs
```

This creates a `csAddon/` folder with a C# project that bridges JavaScript and Windows APIs using [node-api-dotnet](https://github.com/microsoft/node-api-dotnet).

### 5. Add Required Capability

Edit `appxmanifest.xml` to add the `systemAIModels` capability required for Windows AI APIs:

```xml
<Capabilities>
  <rescap:Capability Name="runFullTrust" />
  <rescap:Capability Name="systemAIModels" />
</Capabilities>
```

Also update `MaxVersionTested` in Dependencies:
```xml
<TargetDeviceFamily Name="Windows.Desktop" MinVersion="10.0.18362.0" MaxVersionTested="10.0.26226.0" />
```

### 6. Implement the ImageScaler API

The C# addon (`csAddon/addon.cs`) uses the `Microsoft.Windows.AI.Imaging.ImageScaler` API:

```csharp
using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Imaging;

// Check if the model is available
var readyState = ImageScaler.GetReadyState();

// Ensure the model is ready (downloads if needed)
await ImageScaler.EnsureReadyAsync();

// Create scaler and process image
var imageScaler = await ImageScaler.CreateAsync();
var scaledBitmap = imageScaler.ScaleSoftwareBitmap(inputBitmap, targetWidth, targetHeight);
```

### 7. Set Up Debug Identity

Add the postinstall script to `package.json`:

```json
{
  "scripts": {
    "postinstall": "winapp restore && winapp cert generate --if-exists skip && winapp node add-electron-debug-identity"
  }
}
```

This enables package identity for testing Windows APIs during development.

## Project Structure

```
electron-superres/
├── src/
│   ├── index.js          # Main process - IPC handlers for addon calls
│   ├── index.html        # Renderer - UI for image selection/preview
│   ├── index.css         # Styling
│   └── preload.js        # Context bridge exposing APIs to renderer
├── csAddon/
│   ├── addon.cs          # C# code calling Windows AI APIs
│   ├── csAddon.csproj    # Project file with SDK references
│   └── dist/             # Compiled native addon (.node file)
├── appxmanifest.xml      # Windows app manifest with capabilities
├── Assets/               # App icons for Windows
├── .winapp/              # Windows SDK packages (gitignored)
├── devcert.pfx           # Dev certificate (gitignored)
├── winapp.yaml           # SDK version configuration
└── package.json
```

## Available Scripts

| Script | Description |
|--------|-------------|
| `npm start` | Run the Electron app |
| `npm run build-csAddon` | Build the C# native addon |
| `npm run build` | Build everything |
| `npm run package` | Package the app with Electron Forge |

## How It Works

1. **User selects an image** via the file picker
2. **Main process receives the request** through IPC
3. **C# addon calls ImageScaler API** which runs on the NPU
4. **Scaled image is saved** to a temp file
5. **Renderer displays the result** side-by-side with original

The `ImageScaler` API:
- Uses on-device AI models running on the NPU
- Can scale images up to 8x their original resolution
- Improves sharpness and clarity during scaling
- Requires no cloud connectivity

## Packaging for Distribution

To create an MSIX installer:

```bash
# Build production files
npm run package

# Create signed MSIX
npx winapp pack ./out/electron-superres-win32-x64 --cert ./devcert.pfx
```

## Resources

- [winapp CLI Documentation](https://github.com/microsoft/winappCli)
- [Image Super Resolution API Docs](https://learn.microsoft.com/windows/ai/apis/image-super-resolution)
- [Windows AI APIs Overview](https://learn.microsoft.com/windows/ai/apis/)
- [AI Dev Gallery](https://aka.ms/aidevgallery) - Sample gallery of all AI APIs
- [node-api-dotnet](https://github.com/microsoft/node-api-dotnet) - C# ↔ JavaScript interop

## License

MIT

---

*This sample was generated with AI assistance using GitHub Copilot to demonstrate integrating Windows AI capabilities into Electron applications.*
