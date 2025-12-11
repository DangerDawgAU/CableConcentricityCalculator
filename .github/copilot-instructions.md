Purpose
-------

This short guide orients assistant agents to the CableConcentricityCalculator codebase so they can be productive immediately. It focuses on the project's architecture, developer workflows, important patterns, integration points, and quick commands to build/run the app locally.

**Big Picture**
- **Two front-ends:** a console app (`CableConcentricityCalculator`) and an Avalonia GUI (`CableConcentricityCalculator.Gui`). Both share the same core model and service code.
- **Core layers:** `Models/` (data classes), `Services/` (calculation logic, configuration, and libraries), `Visualization/` (image/interactive generation), `Reports/` (PDF generation).
- **Why this layout:** calculation & domain models are shared between console and GUI; visualization and report generation are separated to keep numeric/geometry logic testable and UI-agnostic.

**Key files and directories**
- `CableConcentricityCalculator/Models/` : canonical data model (e.g. `Cable.cs`, `CableCore.cs`, `CableAssembly.cs`, `HeatShrink.cs`, `OverBraid.cs`, `Annotation.cs`).
- `CableConcentricityCalculator/Services/` : algorithm and libraries (e.g. `ConcentricityCalculator.cs`, `CableLibrary.cs`, `LibraryLoader.cs`, `HeatShrinkService.cs`, `OverBraidService.cs`, `ConfigurationService.cs`).
- `CableConcentricityCalculator/Libraries/` : JSON-based component libraries (`CableLibrary.json`, `HeatShrinkLibrary.json`, `OverBraidLibrary.json`) with README documentation.
- `CableConcentricityCalculator/Visualization/` : rendering helpers (`CableVisualizer.cs`, `Cable3DVisualizer.cs`, `Cable3DSTLVisualizer.cs`, `InteractiveVisualizer.cs`, `LayLengthVisualizer.cs`).
- `CableConcentricityCalculator/Reports/` : `PdfReportGenerator.cs` — report creation uses models and images from visualizers; includes cross-sections, lay length diagrams, and BOM.
- `CableConcentricityCalculator.Gui/` : Avalonia UI, `ViewModels/` drives UI state (CommunityToolkit.Mvvm). `MainWindowViewModel.cs` is the main view model.
- `CableConcentricityCalculator.Gui/Views/` : XAML views including `MainWindow.axaml`, `CableBrowserDialog.axaml`, and `CustomCableDialog.axaml`.
- `Samples/` : sample JSON assemblies for manual testing (e.g. `sample-19-conductor.json`).

**Build & Run (Windows PowerShell)**
- Build entire solution: `dotnet build CableConcentricityCalculator.sln`
- Run GUI (dev): `dotnet run --project CableConcentricityCalculator.Gui`
- Run console demo: `dotnet run --project CableConcentricityCalculator -- --demo`
- Restore or clean: `dotnet restore` / `dotnet clean`
- Publish (release) targets are defined in VS Code tasks (see workspace tasks): use those for platform-specific self-contained builds.

**Project-specific patterns & conventions**
- **JSON-based libraries**: Component libraries (cables, heat shrinks, over-braids) are stored as JSON files in `Libraries/` folder. Use `LibraryLoader` to load/save these. Prefer editing JSON directly for adding components.
- `ConfigurationService` provides JSON load/save helpers for cable assemblies. Use it to create or persist `CableAssembly` instances.
- Visualizers return raw image `byte[]` or an `InteractiveImageResult`; the GUI writes bytes directly to files or binds images to UI controls.
- Geometry & packing logic is in `ConcentricityCalculator` (pure algorithms). Prefer modifying/adding math here rather than in UI code.
- Models are mutable and used directly by ViewModels; `MainWindowViewModel` uses `ObservableCollection` and CommunityToolkit attributes (see `MainWindowViewModel.cs`). Keep changes to models compatible with the UI binding (no large breaking API changes without updating the ViewModel).
- **Adding components**:
  - For cables, heat shrinks, or over-braids: Edit the appropriate JSON file in `Libraries/` (recommended)
  - Or add programmatic generation in `CableLibrary.cs`, `HeatShrinkService.cs`, or `OverBraidService.cs` and export to JSON
  - See `Libraries/README.md` for JSON schema documentation
- **UI patterns**:
  - Heat shrink and over-braid sections follow consistent UI pattern (selector dropdown + "Add Selected" button + applied items list)
  - Cable browser uses dialog pattern with filtering capabilities (`CableBrowserDialog.axaml`)
  - Interactive visualization supports click-to-select with hit-testing (`InteractiveVisualizer.cs`)

**Integration points & external deps**
- UI: Avalonia + ReactiveUI + CommunityToolkit.Mvvm (`CableConcentricityCalculator.Gui`).
- Console: Spectre.Console for text UI & prompts (`Program.cs`).
- PDF: `PdfReportGenerator` (internal) — inspect that file for report templates and fonts used.
- No external web services; main external surface is filesystem (load/save JSON, write image/pdf outputs).

**Quick examples for code changes**
- **To change cross-section rendering**: Edit `CableConcentricityCalculator/Visualization/CableVisualizer.cs` and run `dotnet run --project CableConcentricityCalculator.Gui` to see the GUI update.
- **To add a new cable to the library**: Edit `Libraries/CableLibrary.json` directly (fastest) or add helper logic in `Services/CableLibrary.cs`, uncomment save call, run once, then re-comment.
- **To add heat shrinks or over-braids**: Edit `Libraries/HeatShrinkLibrary.json` or `Libraries/OverBraidLibrary.json` following the schema in `Libraries/README.md`.
- **To change UI layout**: Edit `CableConcentricityCalculator.Gui/Views/MainWindow.axaml` for main window or create new dialogs in `Views/` folder.
- **To validate geometry changes**: Use `ConcentricityCalculator.ValidateAssembly` or `OptimizeFillers` from `Services/`.
- **To modify PDF reports**: Edit `Reports/PdfReportGenerator.cs` using QuestPDF fluent API.

**Testing / verification (manual)**
- Load samples from `Samples/` via GUI `Load` or console `--load` to verify output (PDF + cross-section image written to `output/`).
- Use `dotnet run --project CableConcentricityCalculator -- --load Samples/sample-19-conductor.json` to generate a PDF and image quickly.

**PR guidance for assistants**
- Keep changes focused: update models/services/visualization in small commits. Update `MainWindowViewModel` only when necessary.
- Run `dotnet build` and smoke-run the GUI or console demo for verification. Include sample outputs (PDF/png) in the PR description when visual changes are involved.

If anything in this summary is unclear or you want more examples (e.g., walkthrough of a specific module or a suggested small starter issue to work on), tell me which area and I'll expand the instructions.
