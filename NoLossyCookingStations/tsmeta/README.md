Helps prevent cooking station and fermenter network lag from eating your food by:
1. Forcefully taking network ownership of the interactable object
2. Limiting the rate at which items can be added
3. Dumping overflow back into the world

Also works for "Smelter" interactables like blast furnaces and windmills. Clients without this plugin still benefit when the network owner of these interactable object does have this plugin.

## Changelog

1.1.2

- Fix InvalidCastException when using the fermenter

1.1.1

- Update for 1.0.7 (Deep North)

1.1.0

- Update for 0.219.14 (Bog Witch)
- Apply the same logic to anything that counts as a Smelter

1.0.1

- Update for 0.217.46 (pre-Ashlands)

1.0.0

- Initial release
