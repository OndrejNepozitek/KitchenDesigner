using System.Reflection;
using HarmonyLib;
using Kitchen;

namespace ONe.KitchenDesigner;

[HarmonyPatch]
[HarmonyPatch(typeof (MainMenu), "Setup")]
internal class MainMenuPatch
{
    private static MethodInfo _addButtonMethod;
    private static MethodInfo _requestMenuAction;

    [HarmonyPrefix]
    private static bool Prefix(MainMenu __instance)
    {
        if (_addButtonMethod == null)
        {
            _addButtonMethod = AccessTools.Method(typeof(MainMenu), "AddButton");
            _requestMenuAction = AccessTools.Method(typeof(MainMenu), "RequestAction");
        }
        
        _addButtonMethod.Invoke(__instance, new object[]
        {
            "Kitchen Designer",
            (int playerId) =>
            {
                _requestMenuAction.Invoke(__instance, new object[] { new MenuAction(PauseMenuAction.CloseMenu) });
                KitchenDesigner.KitchenDesignerGUIManager.Show();
            },
            0, // arg
            1, // scale
            0.2f, // padding
        });

        return true;
    }
}