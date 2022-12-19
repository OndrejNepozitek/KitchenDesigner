using UnityEngine;

namespace ONe.KitchenDesigner;

public class KitchenDesignerGUIManager : MonoBehaviour
{
    private bool _show;
    private static readonly int WindowId = nameof(KitchenDesigner).GetHashCode();

    public void Show()
    {
        _show = true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            _show = false;
        }
    }
    
    private Rect CalculateWindowRect()
    {
        var width = Mathf.Min(Screen.width, 650);
        var height = Mathf.Min(Screen.height, 350);
        var offsetX = Mathf.RoundToInt((Screen.width - width) / 2f);
        var offsetY = Mathf.RoundToInt((Screen.height - height) / 2f);
        return new Rect(offsetX, offsetY, width, height);
    }

    private void OnGUI()
    {
        if (_show)
        {
            var windowRect = CalculateWindowRect();
            
            var backgroundTexture = new Texture2D(1, 1, TextureFormat.RGBAFloat, false); 
            backgroundTexture.SetPixel(0, 0, new Color(0.15f, 0.15f, 0.15f, 1));
            backgroundTexture.Apply();
            
            var guiStyle = GUI.skin.window;
            guiStyle.normal.background = backgroundTexture;
            
            GUI.Box(windowRect, GUIContent.none, new GUIStyle { normal = new GUIStyleState { background = backgroundTexture } });
            GUILayout.Window(WindowId, windowRect, Window, "Kitchen Designer configuration");
            GUI.FocusWindow(WindowId);
        }
    }
    
    private static void Window(int windowID)
    {
        KitchenDesignerWindow.Draw();
    }
}