# Item Totals

Item Totals adds `Usable at home: N` to item tooltips in *Survival Log*: how many you have in your backpack and all home storage combined.

Since game 1.1.19611 the game shows this line itself in the storage, shop, and trade windows and in the recipe tooltips of the workbench. Item Totals adds the same line everywhere else: the cooking window, the workbench item tooltips, the material rack, the brewing, dye, and Rat Cage windows, the other station windows, and the item detail popup. Where the game shows its own line, the mod adds nothing, so you never see two numbers. The line uses the game's own text and green color, so it follows the game's language, also a community language pack, and updates as items change.

Nexus page: https://www.nexusmods.com/survivallog/mods/24

## What counts

The same places as the game's own "Usable at home" count:

- Your backpack and the home storage you can access.
- Not counted, as in the game: a Brewing Barrel, a locked bag, a shredder hopper, a compressor bin, a dye vat, dropped items, and unpacked pockets from exploration.

The mod only displays totals and adds nothing to your save. You can remove it at any time.

## Requirements

- Survival Log 1.1.19611 or later. On an older game version the line does not show.
- The [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12), the BepInEx 6 (IL2CPP) build for the game.

## Install

1. Install the [BepInEx Pack for Survival Log](https://www.nexusmods.com/survivallog/mods/12) (if no other mods were installed before, start the game once so BepInEx finishes setup, then quit).
2. Extract this mod's zip into the game folder (the folder with the game .exe). The DLL lands in `BepInEx\plugins`. Full path example:
   - Steam: `C:\Program Files (x86)\Steam\steamapps\common\Survival Log\BepInEx\plugins\ItemTotals.dll`

## Uninstall

Delete `ItemTotals.dll` from the `BepInEx\plugins` folder.

## Troubleshooting

Tested on Survival Log 1.1.19611 (Steam build `26157732`) with BepInEx `6.0.0-be.788`.

If a total is missing, check `BepInEx\LogOutput.log` for `Item Totals loaded.` and any warnings or errors. There are no player settings; `Verbose` in `BepInEx\config\com.ivmakk.survivallog.itemtotals.cfg` is for troubleshooting only.

## Build

This is a BepInEx 6 IL2CPP plugin. It compiles against the game's IL2CPP interop assemblies, so a game install with BepInEx set up and started once is required. Those assemblies are game-derived and are not part of this repo. The .NET 8 SDK is required.

The build also builds the page script (TypeScript and CSS in `src/Web/`) with Vite, so Node is required too. The mod root has a `mise.toml` for Node, and the npm packages install once after a clone:

```
mise trust
npm ci
dotnet build src/ItemTotals.csproj -c Release
```

`Directory.Build.props` sets `GameDir` to the default Steam install path. If the game is in another place, override it without an edit of the file: set a `GameDir` environment variable, or pass `-p:GameDir=...` on the build. The output DLL is at `src\bin\Release\ItemTotals.dll`.

The C# unit tests do not need the game:

```
dotnet test tests/ItemTotals.Tests
```

Page tests use the game's web UI files (Vitest with jsdom). Set `SL_WEBUI_DIR` to a copy of those files, or `SL_GAME_DIR` to a non-default game install. Run these checks from the mod root:

```
npm test            # builds the page script, then runs the page tests
npm run lint        # stylelint on the CSS files
npm run typecheck   # TypeScript check of the page script and its tests
```

## Package

Add `-p:Package=true` to a Release build to also write the ready-to-install zip at `dist\ItemTotals-<version>.zip`, laid out as `BepInEx\plugins\ItemTotals.dll` so a user extracts it at the game root. A plain build skips this step.

```
dotnet build src/ItemTotals.csproj -c Release -p:Package=true
```

## License

Licensed under the GNU General Public License v3.0. Copyright (C) 2026 ivmakk. See [LICENSE](LICENSE).

You may reuse and modify this mod, but you must keep it open under the same license and give credit. Do not reupload it without credit.
