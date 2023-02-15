using HarmonyLib;
using Kitchen;
using Unity.Entities;

namespace ONe.KitchenDesigner.KitchenDesigns;

[HarmonyPatch(typeof(SetSeededRunOverride), "OnUpdate")]
public class SetSeededRunOverridePatch_OnUpdate
{
    public static void Postfix()
    {
        if (KitchenDesignLoader.IsWaitingForSetSeededRunUpdate)
        {
            KitchenDesignLoader.IsWaitingForSetSeededRunUpdate = false;
            KitchenDesignLoader.SetSeededRunUpdated();
        }
    }
}