using System.Collections.Generic;
using System.Diagnostics;
using Kitchen;
using ONe.KitchenDesigner.KitchenDesigns;
using Unity.Entities;
using UnityEngine;

namespace ONe.KitchenDesigner;

public static class KitchenDesignerWindow
{
    private static string _kitchenDesignValue = "";
    private static State _state = State.Initial;
    private static readonly Stopwatch LayoutGeneratedStopwatch = new();
    private static KitchenDesign _kitchenDesign;
    
    private static string _kitchenDesignMessage;
    private static bool _useRandomSeed = true;
    private static string _fixedSeed;
    public static bool LargeLayoutSupport { get; private set; }

    private static readonly List<State> ErrorStates = new()
    {
        State.NoHeadquarters, State.NoSeededRuns, State.NoDesignProvided, State.LayoutNotLoaded, State.LayoutGeneratingFailure
    };
    private static readonly List<State> ReadyToGenerateStates = new()
    {
        State.LayoutLoaded, State.LayoutGeneratingFailure, State.LayoutGeneratingSuccess
    };

    private static GUIStyle _headerStyle;
    private static Vector2 _scrollPos;

    private static void DoStateTransition(ref bool hasChanges, bool buttonClicked)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var isInsideHeadquarters = !entityManager.CreateEntityQuery(typeof(SSelectedLayoutPedestal)).IsEmpty;
        var hasSeededRuns = !entityManager.CreateEntityQuery((ComponentType)typeof(CSeededRunInfo)).IsEmpty;
        var isDesignProvided = !string.IsNullOrWhiteSpace(_kitchenDesignValue);
        
        // If not in error state and not inside headquarters, transition to NoHeadquarters
        if (!IsInErrorState() && !isInsideHeadquarters)
        {
            _state = State.NoHeadquarters;
            return;
        }

        // If in NoHeadquarters but now is inside, transition to Initial
        if (_state == State.NoHeadquarters && isInsideHeadquarters)
        {
            _state = State.Initial;
            return;
        }

        // If not in error state and no seeded runs available, transition to NoSeededRuns
        if (!IsInErrorState() && !hasSeededRuns)
        {
            _state = State.NoSeededRuns;
            return;
        }

        // If in NoSeededRuns but seeded runs are now available, transition to Initial
        if (_state == State.NoSeededRuns && hasSeededRuns)
        {
            _state = State.Initial;
            return;
        }

        // If not in error state and no design provided, transition to NoDesignProvided
        if (!IsInErrorState() && !isDesignProvided)
        {
            _state = State.NoDesignProvided;
            return;
        }

        // If in NoDesignProvided and design is provided, transition to Initial
        if (_state == State.NoDesignProvided && isDesignProvided)
        {
            _state = State.Initial;
            return;
        }
        
        // If there are some changes in the design we transition back to the Initial state
        // It is important to set the hasChanges var to false because we do not want it to be
        // consumed more than once
        if (hasChanges)
        {
            hasChanges = false;
            _state = State.Initial;
            return;
        }

        // If the state is Initial when this is checked, all the basic error states were already handled
        // We can transition to DesignProvided
        if (_state == State.Initial)
        {
            _state = State.DesignProvided;
            return;
        }

        // If in DesignProvided, try to decode the design
        // Based on the result, transition either to LayoutLoaded or LayoutNotLoaded
        if (_state == State.DesignProvided)
        {
            if (KitchenDesignDecoder.TryDecode(_kitchenDesignValue, out _kitchenDesign, out _kitchenDesignMessage))
            {
                _state = State.LayoutLoaded;
            }
            else
            {
                _state = State.LayoutNotLoaded;
            }

            return;
        }

        // If in LayoutLoaded and button was clicked, start generating layout and transition to LayoutGenerating
        if (IsReadyToGenerateState() && buttonClicked)
        {
            // buttonClicked = false;
            _state = State.LayoutGenerating;
            
            // Decode the design again in order to get a different randomization of settings
            KitchenDesignDecoder.TryDecode(_kitchenDesignValue, out var kitchenDesignNew, out _);
            var kitchenDesign = kitchenDesignNew ?? _kitchenDesign;
            var seed = _useRandomSeed ? null : _fixedSeed;
            KitchenDesignLoader.LoadKitchenDesign(kitchenDesign, seed);

            return;
        }
        
        // If in LayoutGenerating and the layout is no longer generating, transition to Success or Failure
        // Also start a stopwatch so that we can reset the state after some time after a successful generation
        // The main reason is to not show the success message if a user already played to layout
        if (_state == State.LayoutGenerating && !KitchenDesignLoader.IsGenerating)
        {
            LayoutGeneratedStopwatch.Restart();
            _state = KitchenDesignLoader.WasLastGenerationSuccessful
                ? State.LayoutGeneratingSuccess
                : State.LayoutGeneratingFailure;
            return;
        }

        // Go back to Initial 5 seconds after a successful generation attempt
        if (_state == State.LayoutGeneratingSuccess && LayoutGeneratedStopwatch.ElapsedMilliseconds > 5000)
        {
            _state = State.Initial;
            return;
        }
    }

    private static string GetStatusText()
    {
        switch (_state)
        {
            case State.Initial:
                return $"Unexpected state ({State.Initial})";
            case State.NoHeadquarters:
                return "You must be inside the headquarters and be the host of the game.";
            case State.NoSeededRuns:
                return "Seeded runs not available (you need to be at least level 6).";
            case State.NoDesignProvided:
                return "No custom design provided.";
            case State.DesignProvided:
                return $"Unexpected state ({State.DesignProvided})";
            case State.LayoutNotLoaded:
                return "Kitchen Design could not be loaded. Please check that you copied it correctly. Also check the log.\n\nError: " + _kitchenDesignMessage;;
            case State.LayoutLoaded:
                return "Kitchen Design loaded, you can click the button below.";
            case State.LayoutGenerating:
                return "Layout is being generated";
            case State.LayoutGeneratingSuccess:
                return "Layout generated, you can close this window and play.";
            case State.LayoutGeneratingFailure:
                return $"Layout not generated, please see the log for errors. Possible reason: {GetLayoutGeneratingFailureReason()}";
            default:
                return "Unexpected state";
        }
    }

    private static string GetLayoutGeneratingFailureReason()
    {
        switch (KitchenDesignLoader.LastGenerationError)
        {
            case DesignLoadError.CannotDecorateKitchen:
                return "Could not decorate kitchen. Please make sure the kitchen is large enough for all the required appliances.";
            
            case DesignLoadError.CannotDecorateDining:
                return "Could not decorate dining room. Please make sure the dining is large enough for all the tables.";
            
            case DesignLoadError.NorthPole:
                return "North Pole maps are harder to design! 1) You need a room (garden) on the left side 2) You need a room (garden) on the right side (must be a different room) 3) The room on the left should be connected to both the kitchen and the dining room 4) Have hatches between the outside rooms and the kitchen and dining room 5) Experiment!";
            
            default:
                return "Unknown";
        }
    }

    public static void Draw()
    {
        GUILayout.BeginVertical();

        _headerStyle ??= new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            wordWrap = true,
            stretchWidth = true,
            fontSize = 20
        };
        GUILayout.Label($"Kitchen Designer v{KitchenDesigner.Version}", _headerStyle, GUILayout.ExpandWidth(true));
        
        _scrollPos = GUILayout.BeginScrollView(_scrollPos, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

        GUILayout.Label("Copy your Exported Kitchen Design Description in the text area below:");
        
        var newDesignValue = GUILayout.TextArea(_kitchenDesignValue, GUILayout.Height(100));
        var hasChanges = newDesignValue != _kitchenDesignValue;
        _kitchenDesignValue = newDesignValue;

        var previousState = _state;
        while (true)
        {
            DoStateTransition(ref hasChanges, false);
            
            if (previousState == _state)
            {
                break;
            }

            previousState = _state;
        }
        
        var style = new GUIStyle(GUI.skin.label);

        if (IsInErrorState())
        {
            style.normal.textColor = new Color(1f, 0.7f, 0);
        } 
        else if (_state == State.LayoutGeneratingSuccess)
        {
            style.normal.textColor = Color.green;
        }
        
        GUILayout.BeginHorizontal();
        GUILayout.Label($"Status: ", GUILayout.Width(75));
        GUILayout.Label(GetStatusText(), style, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        
        if (IsReadyToGenerateState())
        {
            if (GUILayout.Button("Generate kitchen layout"))
            {
                DoStateTransition(ref hasChanges, true);
            }
        }
        else
        {
            GUILayout.Button("Cannot generate kitchen layout - see status above");
        }
        
        GUILayout.FlexibleSpace();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Fixed seed", GUILayout.Width(350));
        _fixedSeed = GUILayout.TextField(_fixedSeed, GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Use random seed", GUILayout.Width(350));
        _useRandomSeed = GUILayout.Toggle(_useRandomSeed, _useRandomSeed ? "Enabled" : "Disabled", GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        
        GUILayout.BeginHorizontal();
        GUILayout.Label("Large layout support (experimental)", GUILayout.Width(350));
        LargeLayoutSupport = GUILayout.Toggle(LargeLayoutSupport, LargeLayoutSupport ? "Enabled" : "Disabled", GUILayout.ExpandWidth(true));
        GUILayout.EndHorizontal();
        GUILayout.Label("Note: The checkbox above should not be needed anymore as large layouts should be detected automatically now.");
        
        GUILayout.EndScrollView();
        
        GUILayout.EndVertical();
    }

    private static bool IsInErrorState()
    {
        return ErrorStates.Contains(_state);
    }

    private static bool IsReadyToGenerateState()
    {
        return ReadyToGenerateStates.Contains(_state);
    }

    private enum State
    {
        Initial,
        NoHeadquarters,
        NoSeededRuns,
        NoDesignProvided,
        DesignProvided,
        LayoutNotLoaded,
        LayoutLoaded,
        LayoutGenerating,
        LayoutGeneratingSuccess,
        LayoutGeneratingFailure,
    }
}