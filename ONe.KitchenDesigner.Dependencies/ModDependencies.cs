using System.Collections.Generic;
using System.Linq;
using KitchenMods;

namespace ONe.KitchenDesigner.Dependencies;

public class ModDependencies
{
    public Mod Mod { get; }
    
    public List<ModPackDependencies> ModPacks { get; }

    public bool IsMissingDependencies { get; }
    
    public bool IsFromWorkshop { get; }
    
    public ModDependencies(Mod mod, List<ModPackDependencies> modPacks)
    {
        Mod = mod;
        ModPacks = modPacks;
        IsMissingDependencies = modPacks.Any(x => x.IsMissingDependencies);
        IsFromWorkshop = int.TryParse(Mod.Name, out _);
    }
}