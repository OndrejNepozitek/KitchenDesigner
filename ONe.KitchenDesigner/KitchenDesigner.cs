using HarmonyLib;
using KitchenMods;
using UnityEngine;

namespace ONe.KitchenDesigner
{
    internal class KitchenDesigner : IModInitializer
    {
        public const string Version = "1.2.6";

        private static GameObject GameObject { get; set; }
        
        public static KitchenDesignerGUIManager KitchenDesignerGUIManager { get; private set; }
        
        public void PostActivate(Mod mod) 
        {
            Debug.Log($"[KitchenDesigner] Initializing KitchenDesigner v{Version}"); 
            var harmony = new Harmony("ONe.KitchenDesigner");
            harmony.PatchAll(GetType().Assembly);
        }

        public void PreInject()
        {
            GameObject = new GameObject("Kitchen Designer");
            KitchenDesignerGUIManager = GameObject.AddComponent<KitchenDesignerGUIManager>();
        }

        public void PostInject()
        {

        }
    }
}