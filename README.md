# Advanced Javascript Renamer

Advanced Javascript Renamer is a lightweight WinForms file renaming tool for Windows 10 and later. New file names are generated with JavaScript executed by Jint, and metadata can be read from image, audio, video, and application files.

The project and output names are intentionally preserved:

- Project folder: `advancedRenamer`
- Project file: `advancedRenamer.csproj`
- Output executable: `advancedRenamer.exe`
- Display name: `Advanced Javascript Renamer`

## Features

- JavaScript-based renaming with support for `substr`, `replace`, `indexOf`, regex, and modern JS string methods.
- Static/Dynamic script model:
  - `Static` runs once at the start of an operation.
  - `Dynamic` runs once for each file in the list.
- Simulation/preview before touching the file system.
- Apply valid preview results to the file system.
- Undo the last successful Apply operation in reverse order.
- Drag and drop files or folders into the list.
- Metadata support:
  - General file information from `System.IO`
  - Image/EXIF data from `MetadataExtractor`
  - Audio/video data from `TagLibSharp`
  - `.exe`/`.dll` version and signature data from Windows APIs
- Windows Explorer context menu integration under per-user `HKCU\Software\Classes`, with no admin rights required.
- Script templates: save named Static/Dynamic script pairs to a JSON file.
- First-run language selection with saved UI language. Supported languages: English, Turkish, Kazakhstan Turkish, Azerbaijani Turkish, and Russian.
- Safety: invalid Windows filename characters returned by JS are sanitized automatically.

## Requirements

- Windows 10 or later
- .NET Framework 4.6.2 runtime
- Visual Studio 2022 or .NET SDK/MSBuild for building
- Node.js is not required

## NuGet Packages

The project uses `PackageReference`. Packages are restored automatically:

```powershell
dotnet restore .\advancedRenamer.csproj
```

Packages:

- `Jint` 4.8.0
- `MetadataExtractor` 2.9.3
- `TagLibSharp` 2.3.0

## Build

Debug build:

```powershell
dotnet build .\advancedRenamer.csproj
```

Release build:

```powershell
dotnet build .\advancedRenamer.csproj -c Release
```

With Visual Studio:

1. Open `advancedRenamer.sln` or `advancedRenamer.csproj`.
2. Wait for NuGet restore to finish.
3. Select the `Release` configuration.
4. Build the project.

## Run

Correct runtime folder:

```text
bin\Release\net462\
```

Executable:

```text
bin\Release\net462\advancedRenamer.exe
```

Important: `obj` is an intermediate build folder. The app should normally not be launched from `obj\Release\net462`. If the executable is copied elsewhere, copy `Jint.dll`, `MetadataExtractor.dll`, `TagLibSharp.dll`, and the other dependency DLLs with it.

Technical note: the project includes a small MSBuild target that also copies runtime dependency DLLs into the `obj` intermediate output folder to reduce dependency errors when the intermediate executable is launched by mistake. This is not a deployment method; the real runtime and distribution folder is still `bin\Release\net462`.

## Project Structure

```text
advancedRenamer.csproj  Project and NuGet references
advancedRenamer.sln     Visual Studio solution
App.config              .NET Framework runtime config
Form1.cs                Main UI, script engine, metadata, rename/undo logic
Localization.cs         First-run language selection and localized UI strings
Program.cs              App startup and startup error logging
RegistryHelper.cs       Explorer context menu install/remove logic
.gitignore              Excludes build/cache/runtime files from Git
README.md               Turkish usage and development documentation
README.en.md            English usage and development documentation
prompt.md               Turkish project generation prompt
prompt.en.md            English project generation prompt
```

Folders that should not be committed:

```text
bin/
obj/
.vs/
```

These folders are generated automatically during build.

## Usage Flow

1. Add files or folders with `Add Files/Folders`.
2. Optionally drag and drop files into the list.
3. Edit the Static/Dynamic scripts or select a template.
4. Use `Simulate (Preview)` to inspect new names.
5. Use `Apply Changes` if the preview is correct.
6. Use `Undo Last` if you need to revert the last successful Apply operation.

Main grid columns:

- `Current Name`
- `New Name`
- `Path`
- `Size`
- `Type`
- `Status`

## Static and Dynamic Scripts

`Static` runs once before the operation starts. Use it for constants, counters, and helper functions:

```javascript
let counter = 0;
const prefix = "file_";

function nextName(ext) {
    return prefix + counter++.toString().padStart(3, "0") + ext;
}
```

`Dynamic` runs once for each file and must return the new filename as a string:

```javascript
return nextName(ext);
```

Simple index example:

```javascript
return index.toString().padStart(3, "0") + "_" + name + ext;
```

Empty or invalid results are not applied. Duplicate targets and existing target files are marked as `Invalid` or `Skipped`.

## Available JS Variables

Available inside the per-file Dynamic script:

```text
name        Filename without extension
ext         Extension, e.g. .jpg
path        Directory path
index       Zero-based index in the list
isImage     Whether the file is an image
isMusic     Whether the file is audio
isVideo     Whether the file is video
isApp       Whether the file is .exe or .dll
size        File size in bytes
fullName    Full file path
created     JS Date
modified    JS Date
accessed    JS Date
attributes  FileAttributes text
meta        Metadata object
```

## Metadata Fields

General file fields:

```text
meta.name
meta.extension
meta.fullName
meta.path
meta.sizeBytes
meta.sizeText
meta.creationDate
meta.modifiedDate
meta.accessedDate
meta.attributes
meta.isReadOnly
meta.isHidden
meta.isSystem
meta.isArchive
```

Image/EXIF fields:

```text
meta.width
meta.height
meta.dpiX
meta.dpiY
meta.cameraMake
meta.cameraModel
meta.fStop
meta.exposureTime
meta.iso
meta.focalLength
meta.dateTaken
meta.digitizedDate
meta.gpsLatitude
meta.gpsLongitude
meta.orientation
```

Audio/music fields:

```text
meta.title
meta.artist
meta.artists
meta.album
meta.year
meta.genre
meta.trackNumber
meta.bpm
meta.duration
meta.durationText
meta.audioChannels
meta.audioSampleRate
meta.audioBitrateKbps
meta.audioCodec
```

Video fields:

```text
meta.duration
meta.durationText
meta.videoWidth
meta.videoHeight
meta.bitrateKbps
meta.frameRate
meta.audioChannels
meta.audioSampleRate
meta.audioBitrateKbps
meta.videoCodec
meta.audioCodec
```

Note: `frameRate` may remain `0` because TagLibSharp does not provide that field for every video format.

Application file fields:

```text
meta.productName
meta.fileVersion
meta.copyright
meta.description
meta.isSigned
meta.signatureValid
meta.publisher
```

## Script Templates

Named Static/Dynamic script pairs can be saved from the toolbar.

The template file is stored beside the executable:

```text
script-templates.json
```

This is runtime user data, so it is ignored by Git.

## Language Settings

On first launch the app asks for the UI language, then stores the selection beside the executable:

```text
language-settings.json
```

Supported languages are English, Turkish, Kazakhstan Turkish, Azerbaijani Turkish, and Russian. The toolbar language selector can change the UI immediately without restarting or losing the current file/folder list. Delete `language-settings.json` to make the app ask again on the next launch.

## Explorer Context Menu

The `Add to Context Menu` checkbox manages these per-user registry paths:

```text
HKCU\Software\Classes\Directory\shell\advancedRenamer
HKCU\Software\Classes\Directory\Background\shell\advancedRenamer
```

The command stores the full path of the executable that is running when the checkbox is enabled. To make the context menu point at the correct Release build, launch this file and toggle the checkbox off/on:

```text
bin\Release\net462\advancedRenamer.exe
```

## Error Log

Unhandled startup errors are written beside the executable:

```text
advancedRenamer-error.log
```

This file is ignored by Git.

## Troubleshooting

### MetadataExtractor could not be loaded

The app was probably launched from `obj` or copied as a standalone exe without its dependency DLLs. Run it from:

```text
bin\Release\net462\advancedRenamer.exe
```

If the exe is moved elsewhere, move all dependency DLLs with it.

### Release build cannot write the exe

`advancedRenamer.exe` may still be running. Close the app and build again:

```powershell
dotnet build .\advancedRenamer.csproj -c Release
```

### Context menu opens the wrong exe

Run the app from the correct Release folder, then toggle the checkbox off and on. The registry command will be updated to the new executable path.

## Git Notes

Commit source files only. Do not commit:

```text
bin/
obj/
.vs/
script-templates.json
language-settings.json
advancedRenamer-error.log
```
