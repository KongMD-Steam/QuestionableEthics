# Questionable Ethics Enhanced
 Questionable Ethics Enhanced (QEE) is a content mod that adds cloning and pawn manipulation features to Rimworld. I created this mod to fix bugs and add functionality to the game. See the Change Notes tab in Steam Workshop or the [Github repository](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced) for info on the latest updates.

## Features

* **Grow organs** in vats!
* **Clone** animals and humanoids! Clones have the traits and passions of their "parent".
* **Scan brains** to preserve the skill levels of a pawn! Apply any brain template to your clones, to get them up-to-speed fast.
* **Recruit** hostile pawns instantly with nerve stapling!
* **Balance** the mod as you wish, with the Mod Settings menu (see preview image above)
* **Enhancements** to the original Questionable Ethics mod

![Cloning](https://i.imgur.com/Sr8GRm1.jpg)
![Brain Templating](https://i.imgur.com/7HhYJby.jpg)

## Changes to Questionable Ethics mechanics

Questionable Ethics Enhanced is a successor to the Questionable Ethics mod created by ChJees. At release, there are 14 bugs fixed, 15 new enhancements, and a few new compatibility patches that were not in QE. [Full list here](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/issues?page=1&q=is%3Aissue+is%3Aclosed). Here are the big changes:

* The [Life Support System](https://steamcommunity.com/sharedfiles/filedetails/?id=1778018794) & [Crude Bionics](https://steamcommunity.com/sharedfiles/filedetails/?id=1785162951) have been moved to standalone mods, to improve mod compatibility.
* All descriptions have been re-written with an emphasis on **discoverability** (how do I use this item and where do I obtain it) and **correct grammar**.
* **Failed cloning no longer causes Cloning Vat ingredient loading to break**. This was a high-impact bug in QE and many people asked for a fix.
* Nutrient Solution and Protein Mash recipes, output, and costs have been rebalanced across the board. See mod preview image for details.
* Cloning vats now display how many days remain until clone is fully grown, instead of a percentage of completion.
* The Apply Brain Templating toils have been re-written and many bugs have been fixed. Get [feedback messages during pawn selection](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/blob/master/Languages/English/Keyed/QuestionableEthics.xml), if it's an invalid target.
* Family relationships will no longer be generated for clones.
* The Organ Vat now uses Bills, like a workbench, instead of the Start Growing gizmo.

## Can I use this on an existing save?
Yes. 

If you have a save with Questionable Ethics enabled and want to switch to this mod instead, [please follow these steps](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/blob/master/Docs/QE_Legacy_Save_Instructions.md) to keep your research/items intact.

## Compatibility
This mod patches in changes to vanilla organ defs, but does not remove anything from the vanilla game. All defNames in the mod are unique and should not conflict with other mods. Any mods determined to be incompatible will throw a custom error message upon loading the game, when QEE and that mod are both enabled. **Consider this mod compatible with your modlist, unless specified below:**

**Enhanced Functionality**
* Bionic Icons - Textures from this mod will be used for the organs. Highly recommended upgrade over vanilla textures!
* Evolved Organs - Has exciting changes to its mechanics when QEE is enabled. There are too many to list, but [go here]([https://github.com/Xahkarias/Evolved-Organs/blob/master/About/Patch%20Notes.txt) for more details.
* Humanoid Alien Races 2.0 - Race mods built with this framework will be able to to be cloned in the Cloning Vat.
* Rah's Bionics and Surgery Expansion (RBSE)
  * Organ installation requires RBSE research pre-req 'Organ Transplantation'
  * Organs need to be refrigerated
  * Organ Rejection hediff added when organs are implanted

**Fully Compatible**
* Bioreactor
* Combat Extended - no patch required
* EPOE - Duplicated natural organ defs (two 'Eye' organs with different descriptions, for example). Outside of this, you should have no issues running with both mods enabled.
* Harvest Organs Post-Mortem (HOPM) - All natural organs from QEE have a chance to spawn after autopsy

**Partially Compatible**
* Races from Lord of the Rims by Jecrell - Races can be cloned, but won't have their racial bonus. Workaround - Add the racial hediff to your save file for the pawn. Looking into adding this information to genome templates in a future patch.

**Incompatible**
* Multiplayer
* Questionable Ethics

**Investigating...**
* Harvest Everything (a lot of duplicate functionality)
* Pawnmorpher

If you run into errors when adding this mod to your modlist, please let me know in the comments. This mod includes a Debug Mode toggle in the Mod Settings, which I would recommend setting to Enabled, if you have problems. [Post an issue on Github for maximum visibility](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/issues). The more information you include, the easier it will be for me to fix it.

## Harmony Patches
* MedicalRecipesUtility.SpawnNaturalPartIfClean() - Postfix
* Recipe_InstallNaturalBodyPart.ApplyOnPawn() - Postfix
* BillUtility.GetWorkgiver() - Prefix

## Links
[Source code](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced)

[Bug Report or feature request](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/issues)

[Full list of changes](https://github.com/KongMD-Steam/QuestionableEthicsEnhanced/issues?page=1&q=is%3Aissue+is%3Aclosed)

## Translations
This mod has been translated to the following languages:
* Japanese

## Credits
* KongMD - XML, C#, Preview art
* Proxyer - Japanese Translation
* EvaineQ - Cloning vat art

## Special Thanks
* erdelf, Mehni, Bar0th and Jamaican Castle for answering my modding questions
* Everyone in the Rimworld Discord that helps keep the Rimworld mod scene vibrant.

## Questionable Ethics Credits ##
* ChJees - Concept, XML & C#
* Shotgunfrenzy - Art, Vats and associated items
* Shinzy - Art, Organs
* Edmund - Art, Organ