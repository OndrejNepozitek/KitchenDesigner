using HarmonyLib;
using Kitchen;
using Unity.Entities;

namespace ONe.KitchenDesigner.KitchenDesigns;

[HarmonyPatch(typeof(CreateSeededRuns), nameof(CreateSeededRuns.GenerateMap))]
public static class CreateSeededRunsPatch_GenerateMap
{
    public static bool Prefix(ref Entity __result)
    {
        if (!KitchenDesignLoader.ShouldPatchCreateSeededRuns)
        {
            return true;
        }

        KitchenDesignLoader.ShouldPatchCreateSeededRuns = false;
        var entity = KitchenDesignLoader.LoadKitchenDesign(); 
        __result = entity; 
        
        return false;
    }
}