using HarmonyLib;
using Kitchen;
using UnityEngine;

namespace ONe.KitchenDesigner;

[HarmonyPatch(typeof(LayoutView), nameof(LayoutView.UpdateNavmesh))]
public static class LayoutViewNavMeshPatch
{
    [HarmonyPrefix]
    public static void Postfix(LayoutView __instance)
    {
        var builder = __instance.Builder;
        var bounds = builder.Blueprint.GetBounds();
        var isLargeLayout = bounds.size.x > 19 || bounds.size.y > 13;
        
        if (KitchenDesignerWindow.LargeLayoutSupport || isLargeLayout)
        {
            Debug.Log("[KitchenDesigner] Fixing pathfinding by making the navmesh larger");
            var boxCollider = __instance.gameObject.GetComponent<BoxCollider>();
            boxCollider.size = new Vector3(300f, 0.01f, 300f);
        }
    }
}