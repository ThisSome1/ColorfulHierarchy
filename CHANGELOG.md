# Changelog

## [1.3.4] - 2025-07-29

### Removed
- Removed redundant meta file generating warning.

## [1.3.3] - 2025-07-17

### Fixed
- Fixed Undo/Redo bug (needed two undo/redos to undo/redo change).

### Changed
- Improved class and folder naming.

## [1.3.2] - 2025-07-10

### Added
- Now saves structures in EditorPrefs.

### Changed
- Improve FolderStructureWindow.

### Removed
- Data folder.
- ScriptExtension.cs script.

## [1.3.1] - 2025-07-04

### Fixed
- Fixed CHANGELOG formatting.

## [1.3.0] - 2025-07-04

### Added
- Added functionality to apply transform changes to hierarchy folders. Changes now propagate to all children while the folder transform itself remain in reset state.

## [1.2.0] - 2025-07-03

### Added
- Added functionality to save folder structures created in the hierarchy by right-clicking and choosing `ThisSome1 > Colorful Hierarchy > Save Folder Structure` option.

### Fixed
- Fixed custom folder structure couldn't be added in the inspector.
- Fixed the serialization depth limit warning.

### Changed
- Improved the Folder Structure window.

## [1.1.0] - 2025-06-11

### Added
- Added custom folder structure creation.
- Added window to show your saved folder structures at `Window > ThisSome1 > Colorful Hierarchy > Folder Structures`.
- Added functionality to add folder structure. right-click in the hierarchy and choose `ThisSome1 > Colorful Hierarchy > Folder Structure` or same path in the `GameObject` menu.
