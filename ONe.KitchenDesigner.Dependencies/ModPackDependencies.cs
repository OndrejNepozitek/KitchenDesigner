using System.Collections.Generic;
using System.Reflection;
using KitchenMods;

namespace ONe.KitchenDesigner.Dependencies;

public class ModPackDependencies
{
    public AssemblyModPack ModPack { get; }
    
    public Assembly Assembly { get; }
    
    public List<AssemblyName> MissingDependencies { get; }

    public bool IsMissingDependencies => MissingDependencies.Count > 0;
    
    public ModPackDependencies(AssemblyModPack modPack, Assembly assembly, List<AssemblyName> missingDependencies)
    {
        ModPack = modPack;
        Assembly = assembly;
        MissingDependencies = missingDependencies;
    }
}