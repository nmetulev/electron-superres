using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.JavaScript.NodeApi;
using Microsoft.Windows.AI;
using Microsoft.Windows.AI.Imaging;
using Windows.Graphics.Imaging;
using Windows.Storage;
using Windows.Storage.Streams;

namespace csAddon
{
    /// <summary>
    /// C# addon for Image Super Resolution using Windows AI APIs.
    /// This class provides image upscaling and sharpening capabilities using the NPU.
    /// </summary>
    [JSExport]
    public class Addon
    {
        /// <summary>
        /// Checks if the Image Super Resolution model is available on this device.
        /// </summary>
        /// <returns>True if the model is available, false otherwise</returns>
        [JSExport]
        public static async Task<bool> IsImageScalerAvailable()
        {
            try
            {
                var readyState = ImageScaler.GetReadyState();
                return readyState == AIFeatureReadyState.Ready || 
                       readyState == AIFeatureReadyState.NotReady;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the ready state of the Image Super Resolution model.
        /// </summary>
        /// <returns>A string describing the ready state</returns>
        [JSExport]
        public static string GetReadyState()
        {
            try
            {
                var readyState = ImageScaler.GetReadyState();
                return readyState switch
                {
                    AIFeatureReadyState.Ready => "Ready",
                    AIFeatureReadyState.NotReady => "NotReady",
                    AIFeatureReadyState.DisabledByUser => "DisabledByUser",
                    AIFeatureReadyState.NotSupportedOnCurrentSystem => "NotSupportedOnCurrentSystem",
                    _ => readyState.ToString()
                };
            }
            catch (Exception ex)
            {
                // Check for specific error patterns that indicate NPU/hardware issues
                if (ex.Message.Contains("0x80070032") || ex.Message.Contains("not supported"))
                {
                    return "NotSupportedOnCurrentSystem";
                }
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Ensures the Image Super Resolution model is ready for use.
        /// Downloads and initializes the model if necessary.
        /// </summary>
        /// <returns>A result indicating success or failure</returns>
        [JSExport]
        public static async Task<string> EnsureModelReady()
        {
            try
            {
                var readyState = ImageScaler.GetReadyState();
                
                if (readyState == AIFeatureReadyState.DisabledByUser)
                {
                    return "Error: AI features are disabled by user in Windows settings.";
                }
                
                if (readyState == AIFeatureReadyState.NotSupportedOnCurrentSystem)
                {
                    return "Error: Image Super Resolution is not supported on this system. A Copilot+ PC with NPU is required.";
                }

                if (readyState == AIFeatureReadyState.NotReady)
                {
                    var result = await ImageScaler.EnsureReadyAsync();
                    if (result.Status != AIFeatureReadyResultState.Success)
                    {
                        // AIFeatureReadyResultState has: Success, Failure
                        // When Failure, we need to provide helpful context about NPU requirements
                        var errorMsg = "The AI model failed to initialize. This feature requires a Copilot+ PC with a Neural Processing Unit (NPU).";
                        return $"Error: {errorMsg}";
                    }
                }

                return "Ready";
            }
            catch (Exception ex)
            {
                // Provide more helpful error messages for common issues
                if (ex.Message.Contains("0x80070032") || ex.Message.Contains("not supported"))
                {
                    return "Error: This device does not support Image Super Resolution. A Copilot+ PC with a Neural Processing Unit (NPU) is required.";
                }
                if (ex.Message.Contains("capability"))
                {
                    return "Error: Missing required capability. Ensure the app has 'systemAIModels' capability in the manifest.";
                }
                return $"Error: {ex.Message}";
            }
        }

        /// <summary>
        /// Scales and sharpens an image using AI-powered Super Resolution.
        /// </summary>
        /// <param name="inputImagePath">Path to the input image file</param>
        /// <param name="outputImagePath">Path where the scaled image will be saved</param>
        /// <param name="scaleFactor">Scale factor (1-8). Use 1 for sharpening only.</param>
        /// <returns>A result object with success status and message</returns>
        [JSExport]
        public static async Task<ScaleResult> ScaleImage(string inputImagePath, string outputImagePath, int scaleFactor)
        {
            try
            {
                // Validate scale factor
                if (scaleFactor < 1 || scaleFactor > 8)
                {
                    return new ScaleResult 
                    { 
                        Success = false, 
                        Message = "Scale factor must be between 1 and 8",
                        OutputPath = ""
                    };
                }

                // Check if model is ready
                var readyState = ImageScaler.GetReadyState();
                if (readyState == AIFeatureReadyState.NotReady)
                {
                    var ensureResult = await ImageScaler.EnsureReadyAsync();
                    if (ensureResult.Status != AIFeatureReadyResultState.Success)
                    {
                        return new ScaleResult 
                        { 
                            Success = false, 
                            Message = $"Failed to initialize model: {ensureResult.Status}",
                            OutputPath = ""
                        };
                    }
                }
                else if (readyState != AIFeatureReadyState.Ready)
                {
                    return new ScaleResult 
                    { 
                        Success = false, 
                        Message = $"Image Super Resolution not available: {readyState}",
                        OutputPath = ""
                    };
                }

                // Load the input image
                var inputFile = await StorageFile.GetFileFromPathAsync(inputImagePath);
                using var inputStream = await inputFile.OpenAsync(FileAccessMode.Read);
                var decoder = await BitmapDecoder.CreateAsync(inputStream);
                var softwareBitmap = await decoder.GetSoftwareBitmapAsync(
                    BitmapPixelFormat.Bgra8, 
                    BitmapAlphaMode.Premultiplied);

                // Calculate target dimensions
                int targetWidth = (int)(softwareBitmap.PixelWidth * scaleFactor);
                int targetHeight = (int)(softwareBitmap.PixelHeight * scaleFactor);

                // Create the ImageScaler and scale the image
                var imageScaler = await ImageScaler.CreateAsync();
                var scaledBitmap = imageScaler.ScaleSoftwareBitmap(softwareBitmap, targetWidth, targetHeight);

                // Save the scaled image
                var outputFolder = await StorageFolder.GetFolderFromPathAsync(
                    Path.GetDirectoryName(outputImagePath)!);
                var outputFile = await outputFolder.CreateFileAsync(
                    Path.GetFileName(outputImagePath), 
                    CreationCollisionOption.ReplaceExisting);

                using var outputStream = await outputFile.OpenAsync(FileAccessMode.ReadWrite);
                var encoder = await BitmapEncoder.CreateAsync(BitmapEncoder.PngEncoderId, outputStream);
                encoder.SetSoftwareBitmap(scaledBitmap);
                await encoder.FlushAsync();

                return new ScaleResult 
                { 
                    Success = true, 
                    Message = $"Image scaled successfully from {softwareBitmap.PixelWidth}x{softwareBitmap.PixelHeight} to {targetWidth}x{targetHeight}",
                    OutputPath = outputImagePath,
                    OriginalWidth = softwareBitmap.PixelWidth,
                    OriginalHeight = softwareBitmap.PixelHeight,
                    ScaledWidth = targetWidth,
                    ScaledHeight = targetHeight
                };
            }
            catch (Exception ex)
            {
                return new ScaleResult 
                { 
                    Success = false, 
                    Message = $"Error scaling image: {ex.Message}",
                    OutputPath = ""
                };
            }
        }

        /// <summary>
        /// Sharpens an image without changing its dimensions using AI-powered Super Resolution.
        /// </summary>
        /// <param name="inputImagePath">Path to the input image file</param>
        /// <param name="outputImagePath">Path where the sharpened image will be saved</param>
        /// <returns>A result object with success status and message</returns>
        [JSExport]
        public static async Task<ScaleResult> SharpenImage(string inputImagePath, string outputImagePath)
        {
            return await ScaleImage(inputImagePath, outputImagePath, 1);
        }
    }

    /// <summary>
    /// Result object for image scaling operations.
    /// </summary>
    [JSExport]
    public class ScaleResult
    {
        [JSExport]
        public bool Success { get; set; }
        
        [JSExport]
        public string Message { get; set; } = "";
        
        [JSExport]
        public string OutputPath { get; set; } = "";
        
        [JSExport]
        public int OriginalWidth { get; set; }
        
        [JSExport]
        public int OriginalHeight { get; set; }
        
        [JSExport]
        public int ScaledWidth { get; set; }
        
        [JSExport]
        public int ScaledHeight { get; set; }
    }
}
