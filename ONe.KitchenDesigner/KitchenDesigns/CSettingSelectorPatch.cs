using HarmonyLib;
using Kitchen;
using Unity.Entities;

namespace ONe.KitchenDesigner.KitchenDesigns;

[HarmonyPatch(typeof(CSettingSelector), nameof(CSettingSelector.IDFromQuery))]
public static class CSettingSelectorPatch_IDFromQuery
{
    public static bool Prefix(ref int __result)
    {
        if (KitchenDesignLoader.LastGeneratedMapItem == Entity.Null)
        {
            return true;
        }
        
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

        if (!entityManager.Exists(KitchenDesignLoader.LastGeneratedMapItem))
        {
            KitchenDesignLoader.LastGeneratedMapItem = Entity.Null;
            return true;
        }

        __result = KitchenDesignLoader.LastGeneratedSetting.ID;
        return false;
    }
}