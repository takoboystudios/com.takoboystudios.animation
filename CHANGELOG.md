# Changelog

All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.1.0] - 2025-10-23

### Added
- GIF to sprite sheet converter tool (GifToSpriteSheet.cs)
- Automatically extracts frames from animated GIFs
- Preserves frame timing data in JSON format
- Creates optimized sprite sheets compatible with animation workflow
- Menu command: Assets → TakoBoy Studios → Animation → Convert GIF to Sprite Sheet
- Supports naming convention: AssetName@AnimationName.gif

## [1.0.0] - 2025-10-23

### Added
- Initial release of TakoBoy Studios Animation package
- Frame-by-frame sprite animation for SpriteRenderer and UI Image
- Editor preview player with play/pause/stop controls and frame scrubbing
- Multiple loop modes: No loop, loop to start, loop to specific frame
- Object pooling support with automatic pool detection
- Smart naming convention for automatic animation creation
- JSON-based frame timing support
- Sprite processing and slicing editor tools
- SpriteCopier component for real-time sprite copying
- SpriteMaskAnimationCopier for animated masking
- DestroyAfterAnimation component with pool integration
- Odin Inspector integration support
- Comprehensive editor workflow tools
