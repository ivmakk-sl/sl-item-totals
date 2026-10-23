# Changelog

All notable changes to this project are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/), and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [1.1.0] - 2026-10-23

### Changed

- The line reads `Usable at home: N` (Chinese `家里可用：N`): the game's own text of its count at home from game 1.1.19611, so it follows the game's language and each community language pack. The mod has no text of its own any more.
- The line has the look of the game's own line: green, 12px, with a thin rule above it.
- The item total counts the places that the game counts: a Brewing Barrel, a locked bag, a shredder hopper, a compressor bin, and a dye vat are left out, as the game's count leaves them out.
- No line in a tooltip where the game shows its own count (the storage, shop, and trade windows of game 1.1.19611), so a tooltip never shows two numbers.

### Removed

- Support for game versions before 1.1.19611: the line needs the game's text of its count at home, so on an older game it does not show, and the log has one warning.

## [1.0.2] - 2026-10-10

### Fixed

- The `Total:` line after a swap or a push of items, the drop onto an occupied cell of game 1.1.18385: it showed the count from before the move until the next item change.

## [1.0.1] - 2026-10-08

### Fixed

- Safer startup check at the title screen.

## [1.0.0] - 2026-10-05

### Added

- A `Total: N` line in the tooltip of an item: how many of that item the backpack and all home storage hold together. The line sits in its own tooltip block below the lines of the game.
- The line in each window that has an item tooltip: the storage window, the trade window, the shop window, the cooking window, the workbench window, the material rack, the aid depot, the brew panel, the drone hub, the dye panel, the Rat Cage window, the fertilizer window, the compressor, the shredder, and the greenhouse build window.
- The line in the head line of the item detail popup, after the weight and the size of the item.
- English and Chinese texts, which follow the display language of the game.
