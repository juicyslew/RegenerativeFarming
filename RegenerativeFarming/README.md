# Reap What You Sow

This stardew valley mod expands upon the mechanics of farming by adding a concept of soil health on your farm.  In order to grow a plant, the soil it grows on must provide the nutrients it needs!


## Rationale

I created this to make the main gameplay loop of farming more interesting.  When playing the base game, it's often too easy to only farm the most lucrative crop for each season.  This mod aims to make each crop have it's own niche use, either for making money, or preparing your farm better for future money making.

## Quick Overview

Changes:
* Every tile on your farm has soil health
* There are 3 types of crops: Cash crops, Cover crops, and Converter Crops
	* Cash Crops are more profitable, but consume soil health
	* Cover Crops are unprofitable, but provide soil health
	* Converter crops provide small profits, and consume some set of nutrients to provide another set of nutrients.
* Small crops (normal and trellis crops) will always Deplete nutrients on the first night they grow, and Regenerate the first time they produce harvestables.
* Fruit Trees, and Bushes
	* These plants will perform their soil effects on all tiles within a radius (1 for bushes, 2 for trees)
	* These plants perform their soil effects **at the end of every week**.  Fruit Trees and Bushes will only Regenerate the soil once they are fully grown.
* If a crop ever doesn't have enough of a nutrient when growing, it will die overnight.
* "Prodding Stick" tool
	* Use it to survey soil.  This reveals the soil's health at the cost of energy.
	* When holding it you can see an overlay of soil health for all the locations you've surveyed.
	* Use the right and left arrows to change what nutrient you're viewing in the overlay
	* Use the up and down arrows to configure the opacity and intensity of the overlay

### Feedback and Suggestions
If you have feedback or suggestions you'd like to share.  Please either create an issue on this repo, or directly message me on discord.  My username is: juicyslew

### Acknowledgements
In this section I'd like to acknowledge all of the people that helped me make this!

- Entoarox - for suggestions and guidance when first considering this project
- 6480 and MizuJakkaru - For making the cornucopia mod, which was a huge inspiration for this mod.
- Pathoschild - For making the automate mod, which I took some code from and used as a template for creating the Health overlay.
- spacechase0 - For making the Spacecore framework, and the satchel mod, which I used as a template for creating the Soil Surveyor tool
- Various friends who helped with feedback and testing
- Many others (especially from the stardew valley discord - #making-mods-general channel)