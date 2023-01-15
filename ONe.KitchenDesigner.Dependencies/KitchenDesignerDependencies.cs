using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using KitchenMods;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace ONe.KitchenDesigner.Dependencies;

public class KitchenDesignerDependencies : IModInitializer
{
    public MissingDependenciesGUIManager GUIManager { get; private set; }
    
    private static readonly FieldInfo AsmField = typeof(AssemblyModPack).GetField("Asm", BindingFlags.Instance | BindingFlags.NonPublic);
    
    public void PostActivate(Mod mod)
    {
        SceneManager.sceneLoaded += (_, _) => PostActivateDelayed(mod);
    }

    private void PostActivateDelayed(Mod mod)
    {
        Debug.Log("[KitchenDesigner] Checking KitchenDesigner dependencies");

        var modDependencies = GetKitchenDesignerDependencies(mod);

        if (modDependencies.IsMissingDependencies)
        {
            Debug.Log("[KitchenDesigner] KitchenDesigner is missing dependencies, showing info window");
            var gameObject = new GameObject("Kitchen Designer Missing Dependencies");
            GUIManager = gameObject.AddComponent<MissingDependenciesGUIManager>();
            GUIManager.Show(modDependencies);
        }
        else
        {
            Debug.Log("[KitchenDesigner] KitchenDesigner dependencies should be alright");
        }
    }

    public void PreInject()
    {
 
    }
    
    public void PostInject()
    {

    }

    private ModDependencies GetKitchenDesignerDependencies(Mod kitchenDesignerMod)
    {
        var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
        var modDependencies = GetMissingDependencies(kitchenDesignerMod, loadedAssemblies);

        return modDependencies;
    }

    // private void CheckMissingDependencies()
    // {
    //     var loadedAssemblies = AppDomain.CurrentDomain.GetAssemblies().ToList();
    //
    //     foreach (var mod in ModPreload.Mods)
    //     {
    //         var dependencies = GetMissingDependencies(mod, loadedAssemblies);
    //         Debug.Log($"Mod: {mod.Name}");
    //
    //         if (dependencies.IsMissingDependencies)
    //         {
    //             Debug.Log($"Missing dependencies:");
    //
    //             foreach (var modPack in dependencies.ModPacks.Where(x => x.IsMissingDependencies))
    //             {
    //                 Debug.Log($"ModPack: {modPack.Assembly.GetName().Name}");
    //
    //                 foreach (var assemblyName in modPack.MissingDependencies)
    //                 {
    //                     Debug.Log(assemblyName.FullName);
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             Debug.Log($"No missing dependencies:");
    //         }
    //     }
    // }

    private ModDependencies GetMissingDependencies(Mod mod, List<Assembly> loadedAssemblies)
    {
        var modPacks = mod.GetPacks<AssemblyModPack>();
        var modPacksDependencies = new List<ModPackDependencies>();

        foreach (var modPack in modPacks)
        {
            var assembly = (Assembly) AsmField.GetValue(modPack);
            var missingDependencies = GetMissingDependencies(assembly, loadedAssemblies);
            modPacksDependencies.Add(new ModPackDependencies(modPack, assembly, missingDependencies));
        }

        return new ModDependencies(mod, modPacksDependencies);
    }

    private List<AssemblyName> GetMissingDependencies(Assembly assembly, List<Assembly> loadedAssemblies)
    {
        var result = new List<AssemblyName>();
        
        foreach (var assemblyName in assembly.GetReferencedAssemblies())
        {
            var isLoaded = loadedAssemblies.Any(x => AssemblyName.ReferenceMatchesDefinition(assemblyName, x.GetName()));
            
            if (!isLoaded)
            {
                result.Add(assemblyName); 
            }
        }

        return result;
    }
}