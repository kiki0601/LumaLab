# LumaLab

A Windows desktop photo editor inspired by professional RAW/photo workflows.

## Current MVP

- WPF / .NET 8 Windows desktop shell
- Dark editing interface
- Image import for common raster formats
- Non-destructive slider state
- Exposure, contrast, temperature, tint and saturation processing
- Live pixel preview
- JPEG and PNG export
- Reset workflow

## Roadmap

1. RAW pipeline with LibRaw
2. Highlights / shadows / whites / blacks as true tonal controls
3. HSL and curves
4. Crop, rotate and straighten
5. Presets and sidecar metadata
6. Undo/redo edit history
7. Batch processing
8. GPU-accelerated rendering
9. Catalog and thumbnail browser
10. Windows installer and signed release builds

## Build

Requires the .NET 8 SDK and Windows. Open `LumaLab.csproj` in Visual Studio 2022+ or build with `dotnet build` on Windows.
