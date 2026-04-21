# Changelog

## [1.0.1] - 2026-04-20
- Tool now creates a settings file if one did not exist before.

## [1.0.0] - 2026-04-13
- Asset processing is now on an opt-in basis to remove interference with regular import operations.
- Asset files can now be moved into their respective folder structures on import automatically.
- Materials and prefabs can be generated automatically from textures, meshes, and other asset types.
- Tool settings are now stored as JSON rather than using EditorPrefs.

## [0.1.0] - 2025-10-07
- Alpha release. Contains bugs.
- Existing bugs  
    - Asset variants are incorrectly organized. When imported, an empty variant folder is created.
    - Asset naming edge cases need to be resolved. For example, assets with names such as: SM_handgun, SM_handgun_mag_full, handgun_mag_full, etc.