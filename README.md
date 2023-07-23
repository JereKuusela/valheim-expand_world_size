# Expand world

This mod allows adding new biomes and changing most of the world generation.

Always back up your world before making any changes!

Install on all clients and on the server (modding [guide](https://youtu.be/L9ljm2eKLrk)).

# Features

- Make the world bigger or taller.
- The minimap is generated in the background, lowering loading times.
- Config sync to ensure all clients use the same settings.

# Configuration

The mod supports live reloading when changing the configuration (either with [Configuration manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/) or by saving the config file). This can lead to weird behavior so after playing with the settings it's recommended to make a fresh world.

Note: Pay extra attention when loading old worlds. Certain configurations can cause alter the terrain significantly and destroy your buildings.

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

Note: The minimap is generated on the background. This is indicated by a small `Loading` text on the upper right corner.

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

# Water

Water settings are in the main `expand_world.cfg` file.

Water level can be changed with `Water level` setting. This is currently experimental and probably causes some glitches.

Similarly wave size can be changed with `Wave multiplier` setting. With the `Wave only height` setting causing slightly different behavior. This is also experimental.

## Lakes

Lakes are needed to generate rivers. The code searches for points with enough water and then merges them to lake objects. Use command `ew_lakes` to show their positions on the map.

Note: Lake object is an abstract concept, not a real thing. So the settings only affect river generation.

Settings to find lakes:

- Lake search interval (default: `128` meters): How often a point is checked for lakes (meters). Increase to find more smaller lakes.
- Lake depth (default: `-20` meters): How deep the point must be to be considered a lake. Increase to find more shallow lakes.
- Lake merge radius (default: `800` meters): How big area is merged to a single lake. Decrease to get more lakes.

## Rivers

Rivers are generated between lakes. So generally increasing the amount of lakes also increases the amount of rivers.

However the lakes must have terrain higher than `Lake point depth` between them. So increase that value removes some of the rivers.

Settings to place rivers:

- River seed: Seed which determines the order of lakes (when selected by random). By default derived from the world seed.
- Lake max distance 1 (default: `2000` meters): Lakes within this distance get a river between them. Increase to place more and longer rivers.
- Lake max distance 2 (default: `5000` meters): Fallback. Lakes without a river do a longer search and place one river to a random lake. Increase to enable very long rivers without increasing the total amount that much. 
- River max altitude (default: `50` meters): The river is not valid if this terrain altitude is found between the lakes.
- River check interval (default: `128` meters): How often the river altitude is checked. Both `River max altitude` and `Lake point depth`.

Rivers have params:

- River seed: Seed which determines the random river widths. By default derived from the world seed.
- River maximum width (default: `100`): For each river, the maximum width is randomly selected between this and `River minimum width`.
- River minimum width (default: `60`): For each river, the minimum width is randomly selected between this and selected maximum width. So the average width is closer to the `River minimum width` than the `River maximum width`.
- River curve width (default: `15`): How wide the curves are.
- River curve wave length (default: `20`): How often the river changes direction.

## Streams

Streams are generated by trying to find random points within an altitude range. 

- Stream seed: Seed which determines the stream positions. By default derived from the world seed.
- Max streams (default: `3000`): How many times the code tries to place a stream. This is NOT scaled with the world radius.
- Stream search iterations (default: `100`): How many times the code tries to find a suitable start and end point.
- Stream start min altitude (default: `-4` meters): Minimum terrain height for stream starts.
- Stream start max altitude (default: `1` meter): Maximum terrain height for stream starts.
- Stream end min altitude (default: `6` meters): Minimum terrain height for stream ends.
- Stream end max altitude (default: `14` meters): Maximum terrain height for stream ends.

Streams have params:

- Stream seed: Seed which determines the random stream widths. By default derived from the world seed.
- Stream maximum width (default: `20`): For each stream, the maximum width is randomly selected between this and `Stream minimum width`.
- Stream minimum width (default: `20`): For each stream, the minimum width is randomly selected between this and selected maximum width. So the average width is closer to the `Stream minimum width` than the `Stream maximum width`.
- Stream min length (default: `80` meters): Minimum length for streams.
- Stream max length (default: `299` meters): Maximum length for streams.
- Stream curve width (default: `15`): How wide the curves are.
- Stream curve wave length (default: `20`): How often the stream changes direction.
