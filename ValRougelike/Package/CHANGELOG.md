  **0.7.1**
---
```
- Fixes divide by zero for rarity based loot rolls
- Adds harvest multipliers support to pickable items
```

  **0.7.0**
---
```
- Multiplayer Overhaul
- Death configuration is now managed by profiles
- Players on a server can choose which deathlink profile they want to use
- Selected choice and bonuses/penalties are visible in the compendium
- Death profiles can influence many aspects
	- Increase/decreased gathering yields
	- Drops from enemies
	- Skill gains/penalties
	- What happens to your items on death
	- How many items you can keep on death
	- How much food you keep on death
- Dependencies have been updated to current version of Jotun
```

  **0.5.5**
---
```
- Fixes equipped items not being saved to tombstone when they are removed
```

  **0.5.4**
---
```
- Prevent shuffling of items on death
- Fix some items appearing as duplicated ghosts
```

  **0.5.3**
---
```
- Refresh inventory state to prevent ghost items
```

  **0.5.2**
---
```
- Added localization and external localization support (Bepinex/config/Deathlink/localization)
```

  **0.5.1**
---
```
- Fixes an issue that would prevent saving any equipment
- Changes the default maximum equipment saved style to be an absolute value (this will not change existing configs)
```

  **0.5.0**
---
```
- Adds a system to allow configuring skill reduction rates on death
	- Skills can now be configured to be reduced by a percentage, like vanilla
	- Skills GAINS since the last death can also be reduced by a configurable percentage
	- Skills which can lose XP can be configured
		- Configure which skills do not loose gained XP
		- Configure which skills do not loose XP on death
- 
```

  **0.4.1**
---
```
- Fix minimap marker on death NPE
```

  **0.4.0**
---
```
- Added MaxPercentResourcesRetainedOnDeath which provides an alternative to MaximumEquipmentRetainedOnDeath and can the behavior between the two can be toggled with MaximumEquipmentRetainedStyle
	- This allows for scaling of the amount of equipment saved for a variety of different playstyles, and the option for more linear progression between the start of the game to the end of the game
- Fixes an edgecase for retaining food on death
- Fixes an edgecase for retaining items on death that would result in the player retaining less items than intended
- Added DeathSkillPercentageStyle which allows for scaling of saved items based on total player inventory size and not just items in the players inventory
- Lowered the skill floor for item loss, particularly impactful for extremely large inventories (80+)
- Increased base XP rate for all activities
```

  **0.3.4**
---
```
- Fix non-skill checked items being processed incorrectly
```

  **0.3.3**
---
```
- Update default frequency of skill gains
```

  **0.3.2**
---
```
- Fixes AzuExtendedInventory integration not shuffling saved equipment
- Fixes MaxItemsSaved resulting in the same items regularly being saved
```

  **0.3.1**
---
```
- Adds a max number of equipment items saved option
- Adds the option to save all items to the tomb stone
```

  **0.3.0**
---
```
- Fixes old binary being included
- Adds AzuEPI integration
	- Supports saving items from quickslots added by any mods that use AzuEPI
	- Saving of items is not ordered and may not result in the item staying in the slot (unless it is equipment)
- Logo fixes
```

  **0.2.0**
---
```
- Add configurable food clearing on death
- Add configurable food clearing based on skill level on death
- Added configuration to add or not add a map marker on death
```

  **0.1.0**
---
```
- Initial release
- Add death resistance skill
- Add configurable death item loss/destruction
- Add seperate configurable tier for items which are either dropped, or destroyed
- Add skill tracker and skill xp reduction control
```