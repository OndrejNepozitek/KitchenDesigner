using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ONe.KitchenDesigner.Dependencies;

public class MissingDependenciesGUIManager : MonoBehaviour
{
    private bool _show;
    private static readonly int WindowId = nameof(KitchenDesignerDependencies).GetHashCode();
    private ModDependencies _modDependencies;
    private readonly Dictionary<string, string> _knownDependencies = new Dictionary<string, string>()
    {
        { "0Harmony", "Harmony" },
        { "KitchenLib-Workshop", "KitchenLib" },
    };

    public static float Scale { get; private set; } = 1f;

    public void Show(ModDependencies modDependencies)
    {
        _show = true;
        _modDependencies = modDependencies;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.H))
        {
            _show = false;
        }
    }
    
    private Rect CalculateWindowRect()
    {
        Scale = Mathf.Max(1f, 0.4f * Screen.height / 250);
        
        var width = Mathf.Min(Screen.width, 500);
        var height = Mathf.Min(Screen.height, 250);
        var offsetX = Mathf.RoundToInt(20);
        var offsetY = Mathf.RoundToInt(20);
        
        return new Rect(offsetX, offsetY, width, height);
    }

    private void OnGUI()
    {
        if (_show)
        {
            var windowRect = CalculateWindowRect();

            GUIUtility.ScaleAroundPivot(new Vector2(Scale, Scale), windowRect.min);
            
            var backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false); 
            backgroundTexture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 1));
            backgroundTexture.Apply();
            
            var guiStyle = GUI.skin.window;
            guiStyle.normal.background = backgroundTexture;
            
            GUI.Box(windowRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = backgroundTexture } });
            GUILayout.Window(WindowId, windowRect, Window, "Press H to hide");
            GUI.FocusWindow(WindowId);
        }
    }
    
    private void Window(int windowID)
    {
        var headerStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
            stretchWidth = true,
            fontSize = 20
        };
        GUILayout.Label("!! Kitchen Designer is missing dependencies !!", headerStyle);
        
        GUILayout.Label("The Kitchen Designer workshop mod detected that some of its Workshop dependencies are missing. This mod will not work without them.");
        GUILayout.Space(10);
        GUILayout.Label("Please subscribe to the following workshop mods and then restart your game:");

        var listStyle = new GUIStyle(GUI.skin.label)
        {
            wordWrap = true,
            fontSize = 18
        };

        var missingDependencies = _modDependencies.ModPacks
            .SelectMany(x => x.MissingDependencies)
            .Distinct()
            .ToList();

        foreach (var assemblyName in missingDependencies)
        {
            if (_knownDependencies.TryGetValue(assemblyName.Name, out var knownDependency))
            {
                GUILayout.Label(knownDependency, listStyle);
            }
            else
            {
                GUILayout.Label($"Unknown workshop mod: {assemblyName.Name}", listStyle);
            }
        }
        
        GUILayout.FlexibleSpace();
        
        GUILayout.Label("Note: Since January 2023, Kitchen Designer depends on the Harmony workshop mod. If you have problems with installing, uninstalling or updating workshop mods, please try to 'Verify integrity of game files' in Steam.");
    }
}