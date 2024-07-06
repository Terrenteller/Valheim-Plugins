# At a Glance

Each plugin in this repo is briefly described below. Please refer to settings as defined in source for up-to-date configuration information.

### Custom Slot Item Lib | [Settings](CustomSlotItemLib/CustomSlotItemLib.cs)

Allows items to be equipped in custom slots defined by other plugins or a configuration value.

This is an update and drop-in replacement of [CustomSlotItemLib](https://thunderstore.io/c/valheim/p/nearbear/CustomSlotItemLib/) originally written by [nearbear](https://github.com/nearbear/ValheimBepinexMods/tree/main/CustomSlotItemLib). It was broken by an unknown game update and appears to be abandoned. More information can be found in the original [README](https://github.com/nearbear/ValheimBepinexMods/blob/main/CustomSlotItemLib/Package/README.md).

WARNING: Addons made for the original plugin, like [WishboneSlot](https://thunderstore.io/c/valheim/p/nearbear/WishboneSlot/) by [nearbear](https://github.com/nearbear/ValheimBepinexMods/tree/main/WishboneSlot) and [WisplightSlot](https://thunderstore.io/c/valheim/p/ValheimAddicts/WisplightSlot/) by [ValheimAddicts](https://github.com/DerNap/ValheimAddicts/tree/main/WisplightSlot), are compatible with this one, BUT ARE NOT COMPATIBLE THROUGH MOD MANAGERS DUE TO THEIR THUNDERSTORE DEPENDENCY ON THE ORIGINAL PLUGIN. The functionality of these two is provided by default to account for this.

### Longer Pet Names | [Settings](LongerPetNames/LongerPetNames.cs)

Increases the tamed animal name input length limit from 10 to 100 characters.

### Magnetic Wishbone | [Settings](MagneticWishbone/MagneticWishbone.cs)

Think the Wishbone looks a bit like a horseshoe magnet? Think no-cost auto-pickup range-extending plugins are too OP? Want that feeling of pride and accomplishment hogging another precious inventory slot?

MagneticWishbone adds three highly-configurable levels of item magnet upgrades to the Wishbone. The first upgrade, meant to be a basic magnet, is made at a forge and increases the auto-pickup radius to that of the normal maximum interaction distance (five meters). The second upgrade, meant to be an electromagnet, is made at an artisan table and increases the radius to ten meters. The third upgrade, meant to be a magical magnet, is a placeholder and disabled by default. The Wishbone must be equipped, of course.

Because this plugin does not add new assets it is perfectly safe to use in a multiplayer environment. Clients without this plugin receive no benefits from equipping an upgraded Wishbone. Servers with this plugin will enforce recipes and radii.

### Mastercraft Hammer | [Settings](MastercraftHammer/MastercraftHammer.cs)

Greatly enhances the control over object damage states for placement and repair. Building condemned structures has never been easier!

Known to be compatible with AzuAreaRepair. An in-game configuration editor is strongly recommended.

### Memories | [Settings](Memories/Memories.cs)

Have a strong preference for how zoomed in or out your camera is when on foot, piloting a ship, or in a saddle? This plugin will remember.

### Nag Messages | [Settings](NagMessages/NagMessages.cs)

Nags the player to change their Forsaken Power if their current power is not preferred and to eat if their stomach is empty. The player can also be notified when food benefits are wearing off before they wear off completely.

### No Lossy Cooking Stations | [Settings](NoLossyCookingStations/NoLossyCookingStations.cs)

Helps prevent cooking station network lag from eating your food by:
1. Forcefully taking network ownership of the cooking station
2. Limiting the rate at which items can be added
3. Dumping overflow back into the world

Also works with fermenters.

### No Unarmed Combat | [Settings](NoUnarmedCombat/NoUnarmedCombat.cs)

Vikings may love to fight and throwing hands is a great way to start a brawl, but actual weapons are considerably more effective at getting the point across. Rather than punching, sheathed equipment will be withdrawn and gear from the toolbar will be equipped instead of trying to cast fist like magic missile.

### Ping Tweaks | [Settings](PingTweaks/PingTweaks.cs)

PingTweaks improves the appearance and behavior of pings.

- Pings are not broadcast to other players without a modifier key
- Map marker pings show the marker text
- Pings are sent and map markers are created with proper elevation data
- Pings show distance in meters
- In-world ping text color may be changed
- Pings from other players may be pinned by double-clicking on them (persistence optional)

PingTweaks is client-side but works better when all clients have it.

### Restful Arrival | [Settings](RestfulArrival/RestfulArrival.cs)

Removes the delay between resting and rested upon joining a world.

### Super Ultrawide Support | [Settings](SuperUltrawideSupport/SuperUltrawideSupport.cs)

Do your monitors fulfill the Manifest Destiny of your desk? Does your cursor accumulate frequent flyer miles? Never skip neck day? Designed for a triple-monitor experience, SuperUltrawideSupport fits important HUD and GUI elements to an aspect ratio of your choice.

# Legal Stuff

This repo is licenced under [LGPL 3.0](LICENCE.md) unless where otherwise stated. Markdown-formatted licences are provided by [IQAndreas/markdown-licenses](https://github.com/IQAndreas/markdown-licenses).

CustomItemSlotLib is a fork of [nearbear](https://github.com/nearbear/ValheimBepinexMods/tree/main/CustomSlotItemLib)'s repo and remains licensed under the [MIT Licence](CustomSlotItemLib/LICENCE).

Package preview imagery is derived from content provided by https://clipart-library.com and is approved for "Non-Commercial Use" per their [custom license](http://clipart-library.com/terms.html).

Modification of Valheim is understood to be acceptable per [Iron Gate's official stance on mods](https://www.valheimgame.com/news/regarding-mods/). NOT AN OFFICIAL VALHEIM PRODUCT. NOT APPROVED BY OR ASSOCIATED WITH IRON GATE.
