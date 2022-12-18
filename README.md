# Kitchen Designer

Kitchen Designer is a PlateUp! mod that lets you play on custom kitchen designs. There is a dedicated website (https://plateuptools.com/kitchen-designer) that you can visit to learn more.

> **Note:** While the PlateUp Workshop is not fully public, the [plateuptools.com/kitchen-designer](https://plateuptools.com/kitchen-designer) website contains outdated installation instructions that are meant for the 3rd party BepInEx mod loader.

> This repository contains the Workshop version of the Kitchen Designer mod. For the legacy BepInEx version, please visit [OndrejNepozitek/PlateUpMods](https://github.com/OndrejNepozitek/PlateUpMods) repository. The legacy version is no longer maintained. 

## How to install

This mod can be downloaded in the PlateUp! Workshop on Steam. Right now, the workshop is in public beta. If you want to try the Workshop, you should join the **[Unofficial PlateUp Modding](https://discord.gg/GR2gAG4x5p)** discord and read one of the latest announcements. There, you will find information regarding how to join the beta of the workshop.

If you have access to the workshop, you can find the mod on [this link](https://steamcommunity.com/sharedfiles/filedetails/?id=2901012380).

## How to use

Please head to the [https://plateuptools.com/kitchen-designer](https://plateuptools.com/kitchen-designer) where you can find more information the mod.

Just keep in mind that while the Workshop is still in beta, the website refers to the Legacy BepInEx version which you had to install manually with a 3rd party mod loader.

Moreover, in the Workshop version, you can access the in-game Kitchen Designer menu by pressing Escape while inside headquarters and there you should see the **Kitchen Designer** item in the menu.

## For developers

This mod is based on my [PlateUp Mod Props](https://github.com/OndrejNepozitek/PlateUpModProps) Nuget package.

### Configure PlateUp! path

After you clone this repository, you need to configure where you have the PlateUp! game files. If you have the game installed in `C:\Program Files (x86)\Steam\steamapps\common\PlateUp\PlateUp\` you can skip this step. Otherwise, create a file named `Local.Build.props` in the `ONe.KitchenDesigner` directory. Open the file a put the following inside:

```xml
<Project>
    <PropertyGroup>
        <PlateUpDir>CHANGE THIS</PlateUpDir>
    </PropertyGroup>
</Project>
```

Change the `PlateUpDir` property to point to the folder where you have PlateUp installed. It is the folder that has `PlateUp.exe` inside. For me, the file looks like this:

```xml
<Project>
    <PropertyGroup>
        <PlateUpDir>C:\Personal\PlateUp</PlateUpDir>
    </PropertyGroup>
</Project>
```

### How to build

After you clone the repo and configure the PlateUp! path, you can build the `ONe.KitchenDesigner` project. If the build succeeds, the binaries should be automatically copied to `<PlateUpDir>\Mods\ONe.KitchenDesigner\Content`. After that, the game should automatically start and the mod should be loaded.

### How to publish a new version

First, make sure that there is a `plateup_mod_metadata.json` file next to the content folder. If there is no such file, create it with the following content:

```json
{
    "steamWorkshopItemID": "2901012380"
}
```

Next, make sure to increment the mod version in the `KitchenDesigner` class.

Finally, run the `ModUploader` tool to submit the mod to the Steam Workshop.