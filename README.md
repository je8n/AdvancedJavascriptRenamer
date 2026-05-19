# Advanced Javascript Renamer

Lightweight WinForms file renaming tool for Windows 10+.

## Target

- .NET Framework 4.6.2
- Windows Forms
- No Node.js or external runtime required

## NuGet Packages

The project already uses `PackageReference`. Visual Studio or `dotnet restore` will install:

```powershell
Install-Package Jint -Version 4.8.0
Install-Package MetadataExtractor -Version 2.9.3
Install-Package TagLibSharp -Version 2.3.0
```

## Build

```powershell
dotnet restore .\advancedRenamer.csproj
dotnet build .\advancedRenamer.csproj -c Release
```

If you use Visual Studio, open `advancedRenamer.csproj`, restore NuGet packages, then build.

## JavaScript Example

```javascript
return index.toString().padStart(3, "0") + "_" + name.replace(/ /g, "_") + ext;
```

Available variables:

- `name`
- `ext`
- `path`
- `index`
- `isImage`
- `isMusic`
- `isVideo`
- `isApp`
- `size`
- `fullName`
- `created`
- `modified`
- `accessed`
- `attributes`
- `meta.name`
- `meta.extension`
- `meta.fullName`
- `meta.path`
- `meta.sizeBytes`
- `meta.sizeText`
- `meta.creationDate`
- `meta.modifiedDate`
- `meta.accessedDate`
- `meta.attributes`
- `meta.isReadOnly`
- `meta.isHidden`
- `meta.isSystem`
- `meta.isArchive`
- `meta.width`
- `meta.height`
- `meta.dpiX`
- `meta.dpiY`
- `meta.cameraMake`
- `meta.cameraModel`
- `meta.fStop`
- `meta.exposureTime`
- `meta.iso`
- `meta.focalLength`
- `meta.dateTaken`
- `meta.digitizedDate`
- `meta.gpsLatitude`
- `meta.gpsLongitude`
- `meta.orientation`
- `meta.artist`
- `meta.artists`
- `meta.album`
- `meta.title`
- `meta.duration`
- `meta.durationText`
- `meta.year`
- `meta.genre`
- `meta.trackNumber`
- `meta.bpm`
- `meta.videoWidth`
- `meta.videoHeight`
- `meta.bitrateKbps`
- `meta.frameRate`
- `meta.audioChannels`
- `meta.audioSampleRate`
- `meta.audioBitrateKbps`
- `meta.videoCodec`
- `meta.audioCodec`
- `meta.productName`
- `meta.fileVersion`
- `meta.copyright`
- `meta.description`
- `meta.isSigned`
- `meta.signatureValid`
- `meta.publisher`

The context menu checkbox writes per-user registry keys under `HKCU\Software\Classes`, so admin rights are not required.

## Script Templates

Static and Dynamic scripts can be saved as named templates from the toolbar. Templates are stored beside the executable in:

```text
script-templates.json
```
