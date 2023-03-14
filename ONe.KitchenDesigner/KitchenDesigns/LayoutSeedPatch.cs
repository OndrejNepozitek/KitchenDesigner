using HarmonyLib;
using Kitchen;
using Unity.Entities;

namespace ONe.KitchenDesigner.KitchenDesigns;

[HarmonyPatch(typeof(LayoutSeed), nameof(LayoutSeed.GenerateMap))]
public static class LayoutSeedPatch_GenerateMap
{
    public static bool Prefix(ref Entity __result)
    {
        if (!KitchenDesignLoader.ShouldPatchLayoutSeed)
        {
            return true;
        }

        KitchenDesignLoader.ShouldPatchLayoutSeed = false;
        var entity = KitchenDesignLoader.LoadKitchenDesign(); 
        __result = entity; 
        
        return false;
    }
}