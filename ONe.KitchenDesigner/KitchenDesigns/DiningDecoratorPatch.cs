using System;
using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using Kitchen;
using Kitchen.Layouts;
using KitchenData;
using UnityEngine;

namespace ONe.KitchenDesigner.KitchenDesigns;

[HarmonyPatch(typeof(DiningDecorator), nameof(KitchenDecorator.Decorate))]
public static class DiningDecoratorPatch
{
  [HarmonyPostfix]
  public static void Decorate(Room room, LayoutProfile ___Profile, LayoutBlueprint ___Blueprint, List<CLayoutAppliancePlacement> ___Decorations, ref bool __result)
  {
    if (!KitchenDesignLoader.ShouldPatchDiningDecorations)
    {
      return;
    }

    KitchenDesignLoader.ShouldPatchDiningDecorations = false;

    // Return early if we got a valid decoration from the original decorator
    if (__result)
    {
      return;
    }
    
    List<CLayoutAppliancePlacement> appliancePlacementList = new List<CLayoutAppliancePlacement>();
    List<Vector2> vector2List = new List<Vector2>();
    List<LayoutPosition> list = ___Blueprint.TilesOfRoom(room).OrderBy(r => UnityEngine.Random.value).ToList();
    int num = 0;
    foreach (LayoutPosition tile1 in list)
    {
      if (___Blueprint.IsTileOpenSpace(tile1))
      {
        bool flag = true;
        foreach (LayoutPosition direction in LayoutHelpers.Directions)
        {
          LayoutPosition tile2 = direction + tile1;
          
          // Do not check for features
          if (vector2List.Contains(tile2)/* || ___Blueprint.HasFeature(tile2)*/)
          {
            flag = false;
            break;
          }
        }
        if (flag)
        {
          appliancePlacementList.Add(new CLayoutAppliancePlacement()
          {
            Position = tile1,
            Appliance = ___Profile.Table.ID,
            Rotation = Quaternion.identity
          });
          ++num;
          if (num < ___Profile.MaximumTables)
          {
            foreach (LayoutPosition layoutPosition1 in LayoutHelpers.AllNearby)
            {
              LayoutPosition layoutPosition2 = layoutPosition1 + tile1;
              vector2List.Add(layoutPosition2);
            }
          }
          else
            break;
        }
      }
    }

    if (num != ___Profile.MaximumTables)
    {
      __result = false;
      return;
    }
    
    appliancePlacementList.ForEach(___Decorations.Add);
    __result = true;
  }
}
