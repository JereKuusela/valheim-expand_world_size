# Expand World Size

Allows configuring the world size and altitude settings.

Always back up your world before making any changes!

Install on all clients and on the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

# Configuration

Settings are automatically reloaded (consider using [Configuration manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/)). This can lead to weird behavior so it's recommended to make a fresh world after you are done configuring.

Note: Pay extra attention when loading old worlds. Certain configurations can modify the terrain significantly and destroy your buildings.

Note: Minimap is generated as a background task. This is indicated by a small `Loading` text on the upper right corner.

Note: Old configuration from Expand World is automatically migrated to this mod.

# World size

The size can be increased by changing the `World radius` and `World edge size` settings. The total size is sum of these (default is 10000 + 500 = 10500 meters). Usually there is no need to change the edge size.

The world can be stretched with `Stretch world` setting. This can be used to keep the same world but islands and oceans are just bigger. This will also make biomes bigger which can be further tweaked with `Stretch biomes` setting (for example using 0.5 biome stretch with 2 world stretch).

The amount of locations (like boss altars) can be changed with `Locations` setting. This can significantly increase the initial world generation time (especially when the game fails to place most locations). If changing this on existing worlds, use `genloc` command to distribute unplaced locations.

Note: 2x world radius means 4x world area. So for 20000 radius you would need 4x locations and for 40000 radius you would need 16x locations.

Note: If the game fails to place the spawn altar (for example if no Meadows), then it is forcefully placed at the middle of the map. With bad luck, this can be underwater.

## Minimap

The minimap size must be manually changed because there are two different settings. Both of the settings increase the minimap size but have a different drawback. `Minimap size` significantly increases the minimap generation time while `Minimap pixel size` makes the minimap less detailed.

Recommended settings:
- 20000 radius: 2 size, 1 pixel.
- 40000 radius: 2 size, 2 pixel.
- 80000 radius: 4 size, 2 pixel.
- 160000 radius: 4 size, 4 pixel.

Example generation times:
- Default: 15 seconds.
- 2 size: 1 minute.
- 4 size: 4 minutes.
- 8 size: 16 minutes.
- 16 size: 1 hour.

Note: Changing `Minimap size` resets explored areas.

# Altitude

For the altitude, there are two settings: `Altitude delta` and `Altitude multiplier`. The multiplier multiplies the distance to the water level (by default at 30 meters). So increasing the multiplier will make water more deeper and other terrain higher. The delta directly affects the altitude. For example positive values will make the underwater terrain more shallow.

The formula is: `water level + (altitude - water level) * multiplier + delta`.

Note: Altitude affects biome distribution. For example increasing the altitude will cause more mountains.

Note: Altitude based snow is hard coded and can't be changed.

The final water depth can be multiplied with `Water depth multiplier`.

Amount of forest can be changed with `Forest multiplier`. 

# Seeds

The layout of the world is [pre-determined](https://www.reddit.com/r/valheim/comments/qere7a/the_world_map/), and each world is just a snapshot of it.

The world can be manually moved in this layout with `Offset X` (to west) and `Offset Y` (to south) settings.

For example x = 0 and y = 0 would move the world center to the center of the big map. Similarly x = -20000 and y = 0 would move it to the edge of the big map.

Command `ew_seeds` prints the default offset and other seeds of the world.

Each biome adds some height variation on top of the base altitude. This can be controlled with `Height variation seed` setting.

The whole seed can be replaced with `Seed` setting. This gets permanently saved to the save file.
