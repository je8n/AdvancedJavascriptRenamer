# Project Generation Prompt

Use this prompt to recreate the current project from an empty folder.

```text
Answer in English. Act as a Senior Software Engineer specializing in C# and Windows Desktop development.

Create a lightweight and completely free WinForms file renaming tool for Windows 10 and later from an empty folder. Keep the project folder and project name as `advancedRenamer`, but set the user-facing application name to `Advanced Javascript Renamer`.

Technical targets:

- Create an SDK-style `advancedRenamer.csproj` targeting .NET Framework 4.6.2.
- Use `WinExe` as the output type.
- Do not require Node.js or any external runtime.
- NuGet packages:
  - Jint 4.8.0
  - MetadataExtractor 2.9.3
  - TagLibSharp 2.3.0
- Use Windows Forms.
- Main files:
  - `Program.cs`
  - `Form1.cs`
  - `Localization.cs`
  - `RegistryHelper.cs`
  - `App.config`
  - `README.md`
  - `.gitignore`

UI requirements:

- The main form title must be `Advanced Javascript Renamer`.
- The top toolbar must include:
  - `Add Files/Folders`
  - `Simulate (Preview)`
  - `Apply Changes`
  - `Undo Last`
  - a TextBox for the template name
  - a ComboBox for selecting templates
  - `Load Template`
  - `Save Template`
  - `Add to Context Menu` CheckBox
  - a file count Label
- The main grid must be a `ListView` in `Details` view with these columns:
  - Current Name
  - New Name
  - Path
  - Size
  - Type
  - Status
- Add a script editor section below the grid.
- The script editor must be a `TabControl` with two tabs:
  - `Static`
  - `Dynamic`
- Both script editors must be multiline TextBoxes using a monospace font, accepting tabs, and showing scrollbars.
- Add a variables guide panel on the right; it must be a read-only TextBox with a monospace font and a vertical scrollbar.

Language support:

- On first launch, ask the user to select the UI language.
- Save the selected language in a JSON file beside the executable:
  - `language-settings.json`
- On later launches, load that language automatically.
- Supported languages:
  - English
  - Turkish
  - Kazakhstan Turkish
  - Azerbaijani Turkish
  - Russian
- Do not add extra NuGet packages for localization or JSON; use local dictionaries and `DataContractJsonSerializer`.
- Ignore `language-settings.json` in `.gitignore`.
- Add a toolbar language selector that changes UI text immediately without restarting the application or losing the current file/folder list.

File adding:

- Allow selecting multiple files with `OpenFileDialog`.
- Allow selecting a folder with `FolderBrowserDialog`.
- When a folder is added, recursively add files under it.
- Support drag and drop on the form and ListView.
- Do not add the same file twice.

JavaScript execution model:

- Execute JavaScript with Jint.
- `Static` script must run once at the start of each Simulate/Apply operation.
- `Dynamic` script must run once per file using the same Jint engine.
- Variables, counters, constants, and helper functions defined in Static must be visible to Dynamic.
- Dynamic must return the new filename as a string.
- Default Static script must include commented examples:
  - `let counter = 0;`
  - `const prefix = "file_";`
  - `function nextName(ext) { return prefix + counter++ + ext; }`
- Default Dynamic script must include this commented example:
  - `return index.toString().padStart(3, "0") + "_" + name + ext;`
- Dynamic must default to `return name + ext;`.
- Set timeout and recursion limits for Jint.

Dynamic script variables:

- `name`: filename without extension
- `ext`: extension, e.g. `.jpg`
- `path`: directory path
- `index`: zero-based list index
- `isImage`
- `isMusic`
- `isVideo`
- `isApp`
- `size`
- `fullName`
- `created`: JS Date
- `modified`: JS Date
- `accessed`: JS Date
- `attributes`
- `meta`: metadata object

Metadata requirements:

- Populate general file information from `System.IO.FileInfo`:
  - name, extension, fullName, path, sizeBytes, sizeText
  - creationDate, modifiedDate, accessedDate
  - attributes, isReadOnly, isHidden, isSystem, isArchive
- Use MetadataExtractor for image/EXIF:
  - width, height
  - dpiX, dpiY
  - cameraMake, cameraModel
  - fStop, exposureTime, iso, focalLength
  - dateTaken, digitizedDate
  - gpsLatitude, gpsLongitude
  - orientation
- Use TagLibSharp for audio/video:
  - duration, durationText
  - title, artist, artists, album, year, genre, trackNumber, bpm
  - audioChannels, audioSampleRate, audioBitrateKbps
  - videoWidth, videoHeight, bitrateKbps
  - videoCodec, audioCodec
  - include a frameRate field; it may remain 0 if unsupported
- For `.exe` and `.dll`:
  - use FileVersionInfo for productName, fileVersion, copyright, description
  - use X509Certificate for isSigned, signatureValid, publisher

Rename safety:

- Sanitize invalid Windows filename characters from JS output:
  - `\ / : * ? " < > |`
- Do not apply empty results.
- Mark duplicate target names as invalid.
- Mark existing target files as invalid/skipped.
- Simulate must not change the file system.
- Ask for confirmation before Apply.
- Mark successfully applied rows as `Renamed`.

Undo:

- Add an `Undo Last` button.
- It must only undo the last successful Apply operation.
- Store rename operations as originalPath/newPath.
- Undo must run in reverse order.
- If the original target already exists or the renamed file is missing, mark the row as skipped/error.
- Refresh ListView and FileEntry data after undo.

Template system:

- Allow saving named Static/Dynamic script pairs.
- The toolbar must have a template name TextBox, template ComboBox, `Load Template`, and `Save Template` buttons.
- Selecting a template must load its contents into the editors.
- Ask for overwrite confirmation when saving with an existing name.
- Store templates as one JSON file beside the executable:
  - `script-templates.json`
- Do not add extra NuGet packages for JSON; use .NET Framework `DataContractJsonSerializer`.
- Ignore `script-templates.json` in `.gitignore`.

Build robustness:

- Add a small MSBuild target to `advancedRenamer.csproj` that copies `@(ReferenceCopyLocalPaths)` to `$(IntermediateOutputPath)` after build.
- Name the target `CopyRuntimeDependenciesToIntermediateOutput`.
- This target only reduces dependency errors if the intermediate exe under `obj` is accidentally launched; README must still emphasize that the correct runtime folder is `bin\Release\net462`.

Explorer context menu:

- Create `RegistryHelper.cs`.
- Manage these keys:
  - `HKCU\Software\Classes\Directory\shell\advancedRenamer`
  - `HKCU\Software\Classes\Directory\Background\shell\advancedRenamer`
- Menu text must be `Open with Advanced Javascript Renamer`.
- If localization is active, the menu text may use the currently selected UI language.
- The command must point to the full path of the currently running `advancedRenamer.exe`.
- Do not require admin rights.
- Add when the checkbox is checked, remove when unchecked.

Error handling:

- Catch startup exceptions in `Program.cs`.
- Write `advancedRenamer-error.log` beside the executable.
- Delete stale logs at startup.
- Show errors with MessageBox.

Documentation:

- Write `README.md` in English.
- Clearly document build and run steps.
- State that the correct runtime folder is `bin\Release\net462`.
- Explain that `obj` is an intermediate build folder and the app should not normally be launched from there.
- List all JS variables and metadata fields.
- Include context menu, template, undo, and troubleshooting sections.
- Include the first-run language selection and `language-settings.json` behavior.

Git:

- Add `.gitignore`.
- Ignore `bin/`, `obj/`, `.vs/`, `script-templates.json`, `language-settings.json`, `advancedRenamer-error.log`, NuGet/cache/log files.
```
