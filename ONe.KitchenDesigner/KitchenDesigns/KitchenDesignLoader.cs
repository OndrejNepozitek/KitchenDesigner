using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Kitchen;
using Kitchen.Layouts;
using Kitchen.Layouts.Features;
using KitchenData;
using Unity.Entities;
using UnityEngine;
using Random = UnityEngine.Random;

namespace ONe.KitchenDesigner.KitchenDesigns;

public static class KitchenDesignLoader
{
    /// <summary>
    /// Signals whether the <see cref="KitchenDecoratorPatch"/> patch should run.
    /// </summary>
    public static bool ShouldPatchKitchenDecorations { get; internal set; }
    
    /// <summary>
    /// Signals whether the <see cref="DiningDecoratorPatch"/> patch should run.
    /// </summary>
    public static bool ShouldPatchDiningDecorations { get; internal set; }
    
    /// <summary>
    /// Signals whether the <see cref="LayoutSeedPatch_GenerateMap"/> patch should run.
    /// </summary>
    public static bool ShouldPatchLayoutSeed { get; internal set; }
    
    /// <summary>
    /// Signals whether the <see cref="SetSeededRunOverridePatch_OnUpdate"/> patch should run.
    /// </summary>
    public static bool IsWaitingForSetSeededRunUpdate { get; internal set; }
    
    /// <summary>
    /// Signals whether a layout is currently being generated.
    /// </summary>
    public static bool IsGenerating { get; private set; }
    
    /// <summary>
    /// Whether the last generation attempt was successful or not.
    /// </summary>
    public static bool WasLastGenerationSuccessful { get; private set; }
    
    /// <summary>
    /// The reason why the last generation attempt failed (if it failed)-
    /// </summary>
    public static DesignLoadError LastGenerationError { get; private set; }
    
    public static Entity LastGeneratedMapItem { get; internal set; } = Entity.Null;
    
    public static RestaurantSetting LastGeneratedSetting { get; private set; }

    private static KitchenDesign _kitchenDesign;
    private static Seed _seed;
    
    /// <summary>
    /// This method is the entry point for spawning kitchen designs in the game.
    /// </summary>
    /// <remarks>
    /// This method is asynchronous - no layout is generated immediately.
    /// </remarks>
    /// <param name="kitchenDesign">Kitchen designs to be loaded.</param>
    /// <param name="seed">Seed to be used. If empty, a random seed will be generated.</param>
    public static void LoadKitchenDesign(KitchenDesign kitchenDesign, string seed)
    {
        try
        {
            IsGenerating = true;
            ShouldPatchKitchenDecorations = false;
            ShouldPatchDiningDecorations = false;
            ShouldPatchLayoutSeed = false;
            IsWaitingForSetSeededRunUpdate = false;

            _kitchenDesign = kitchenDesign;
            PostProcessDesign(kitchenDesign);

            _seed = string.IsNullOrWhiteSpace(seed)
                ? Seed.Generate(new System.Random().Next())
                : new Seed(seed.ToLower());
            
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
            var query = entityManager.CreateEntityQuery((ComponentType)typeof(CSeededRunInfo));

            if (query.IsEmpty)
            {
                IsGenerating = false;
                WasLastGenerationSuccessful = false;
            }
            else
            {
                var seededRunInfoEntity = query.First();
                var seededRunInfo = entityManager.GetComponentData<CSeededRunInfo>(seededRunInfoEntity);
                seededRunInfo.IsSeedOverride = false;
                seededRunInfo.FixedSeed = new Seed();
                entityManager.SetComponentData(seededRunInfoEntity, seededRunInfo);
                IsWaitingForSetSeededRunUpdate = true;
            }
        }
        catch (Exception e)
        {
            LogHelper.LogError(e.Message);
            IsGenerating = false;
            WasLastGenerationSuccessful = false;
        }
    }

    internal static void SetSeededRunUpdated()
    {
        UpdateCSeededRunInfo(true, Seed.Generate(new System.Random().Next()));
        ShouldPatchKitchenDecorations = true;
        ShouldPatchDiningDecorations = true;
        ShouldPatchLayoutSeed = true;
    }

    private static void UpdateCSeededRunInfo(bool isSeedOverride, Seed fixedSeed)
    {
        var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;
        var query = entityManager.CreateEntityQuery((ComponentType)typeof(CSeededRunInfo));
        var seededRunInfoEntity = query.First();
        var seededRunInfo = entityManager.GetComponentData<CSeededRunInfo>(seededRunInfoEntity);
        seededRunInfo.IsSeedOverride = isSeedOverride;
        seededRunInfo.FixedSeed = fixedSeed;
        entityManager.SetComponentData(seededRunInfoEntity, seededRunInfo);
    }

    internal static Entity LoadKitchenDesign()
    {
        var seed = _seed;
        UpdateCSeededRunInfo(true, _seed);

        try
        {
            var entityManager = World.DefaultGameObjectInjectionWorld.EntityManager;

            var state = Random.state;
            Random.InitState(seed.IntValue);

            var layoutEntity = ConstructLayout(
                entityManager,
                _kitchenDesign.Blueprint,
                _kitchenDesign.Profile,
                _kitchenDesign.Setting,
                out var errors
            );
            var isValid = layoutEntity != Entity.Null;
            var result = Entity.Null;

            if (isValid)
            {
                var mapItem = CreateMapItem(layoutEntity, _kitchenDesign.Setting, seed);
                WasLastGenerationSuccessful = true;
                LastGeneratedMapItem = mapItem;
                LastGeneratedSetting = _kitchenDesign.Setting;
                result = mapItem;
            }
            else
            {
                var highestReasonCount = errors.Values.Max();
                var mostCommonReason = errors.First(x => x.Value == highestReasonCount).Key;

                LastGenerationError = mostCommonReason;
                WasLastGenerationSuccessful = false;

                throw new InvalidOperationException("Could not generate custom kitchen design. If you see this message, there should be a human-friendly error message in the Kitchen Designer window in-game.");
            }

            Random.state = state;
            IsGenerating = false;
            return result;
        }
        catch (Exception)
        {
            // Reset the state if something goes wrong
            ShouldPatchKitchenDecorations = false;
            ShouldPatchDiningDecorations = false;
            ShouldPatchLayoutSeed = false;
            IsGenerating = false;
            throw;
        }
        finally
        {
            if (!WasLastGenerationSuccessful)
            {
                UpdateCSeededRunInfo(false, _seed);
            }
        }
    }

    private static Entity ConstructLayout(
        EntityManager em,
        LayoutBlueprint layoutBlueprintFixed,
        LayoutProfile profile,
        RestaurantSetting setting,
        out Dictionary<DesignLoadError, int> errors)
    {
        var entity = em.CreateEntity((ComponentType)typeof(CStartingItem), (ComponentType)typeof(CLayoutRoomTile),
            (ComponentType)typeof(CLayoutFeature), (ComponentType)typeof(CLayoutAppliancePlacement));
        var layoutDecorator = (LayoutDecorator)null;
        var layoutBlueprint = (LayoutBlueprint)null;
        var flag = false;
        errors = new Dictionary<DesignLoadError, int>();
        
        for (int index = 0; index < CreateLayoutHelper.MapAttemptsMax; ++index)
        {
            LogHelper.Log($"Loading kitchen design, attempt {index + 1}");
            
            try
            {
                layoutBlueprint = layoutBlueprintFixed;
                layoutDecorator = new LayoutDecorator(layoutBlueprint, profile, setting);
                layoutDecorator.AttemptDecoration();
                flag = true;
                break;
            }
            catch (LayoutFailureException ex)
            {
                LogHelper.Log(ex.Message);
                LogLoadingError(ex, errors);
            }
        }

        if (!flag || layoutDecorator?.Decorations == null)
        {
            LogHelper.Log($"Giving up after {(object)CreateLayoutHelper.MapAttemptsMax} attempts");
            em.DestroyEntity(entity);
            return new Entity();
        }

        layoutBlueprint.ToEntity(em, entity);
        DynamicBuffer<CLayoutAppliancePlacement> buffer = em.GetBuffer<CLayoutAppliancePlacement>(entity);
        foreach (CLayoutAppliancePlacement decoration in layoutDecorator.Decorations)
            buffer.Add(decoration);

        return entity;
    }

    private static void LogLoadingError(LayoutFailureException exception, Dictionary<DesignLoadError, int> errors)
    {
        var error = DesignLoadError.Generic;

        if (exception.Message == "Not enough spaces to place kitchen equipment")
        {
            error = DesignLoadError.CannotDecorateKitchen;
        }
        else if (exception.Message == "Failed to decorate dining room")
        {
            error = DesignLoadError.CannotDecorateDining;
        }
        else if (exception.Message.StartsWith("Failed to apply decorator Kitchen.ConveyorDecorator"))
        {
            error = DesignLoadError.NorthPole;
        }

        if (errors.TryGetValue(error, out var count))
        {
            errors[error] = count + 1;
        }
        else
        {
            errors[error] = 1;
        }
    }

    private static void PostProcessDesign(KitchenDesign design)
    {
        CenterLayout(design.Blueprint);
    }

    /// <summary>
    /// Creates a map item entity from a given layout, setting and seed.
    /// 
    /// This is the same function as found in:
    /// <see cref="Kitchen.CreateSeededRuns" />
    /// <see cref="Kitchen.HandleLayoutRequests" />
    /// <see cref="Kitchen.SetSeededRunOverride" />
    ///
    /// No modifications were needed. Could be also called via reflection.
    /// </summary>
    private static Entity CreateMapItem(Entity layout, RestaurantSetting setting, Seed seed)
    {
        var em = World.DefaultGameObjectInjectionWorld.EntityManager;

        Entity entity = em.CreateEntity((ComponentType) typeof (CCreateItem), (ComponentType) typeof (CHeldBy));
        em.SetComponentData<CCreateItem>(entity, new CCreateItem()
        {
            ID = AssetReference.MapItem
        });
        em.AddComponentData<CItemLayoutMap>(entity, new CItemLayoutMap()
        {
            Layout = layout
        });
        em.AddComponentData<CSetting>(entity, new CSetting()
        {
            RestaurantSetting = setting.ID,
            FixedSeed = seed
        });
        
        return entity;
    }

    /// <summary>
    /// Re-centers a given layout blueprint.
    /// 
    /// This is the same function as found in:
    /// <see cref="Kitchen.Layouts.Modules.RecentreLayout" />
    ///
    /// No modifications were needed (only code format was applied). Could be also called via reflection.
    /// </summary>
    private static void CenterLayout(LayoutBlueprint blueprint)
    {
        if (blueprint.Tiles.Count == 0)
            return;

        Dictionary<LayoutPosition, Room> tiles = blueprint.Tiles;
        Vector2 min = new Vector2(
            (float)tiles
                .Select<KeyValuePair<LayoutPosition, Room>, int>(
                    (Func<KeyValuePair<LayoutPosition, Room>, int>)(r => r.Key.x)).Min(),
            (float)tiles
                .Select<KeyValuePair<LayoutPosition, Room>, int>(
                    (Func<KeyValuePair<LayoutPosition, Room>, int>)(r => r.Key.y)).Min());
        Vector2 new_min = -(new Vector2(
            (float)tiles
                .Select<KeyValuePair<LayoutPosition, Room>, int>(
                    (Func<KeyValuePair<LayoutPosition, Room>, int>)(r => r.Key.x)).Max(),
            (float)tiles
                .Select<KeyValuePair<LayoutPosition, Room>, int>(
                    (Func<KeyValuePair<LayoutPosition, Room>, int>)(r => r.Key.y)).Max()) - min) / 2f;
        new_min = new Vector2((float)Mathf.FloorToInt(new_min.x), (float)Mathf.FloorToInt(new_min.y));
        blueprint.Tiles = tiles.ToDictionary<KeyValuePair<LayoutPosition, Room>, LayoutPosition, Room>(
            (Func<KeyValuePair<LayoutPosition, Room>, LayoutPosition>)(r => translate((Vector2)r.Key)),
            (Func<KeyValuePair<LayoutPosition, Room>, Room>)(r => r.Value));
        blueprint.Features = blueprint.Features
            .Select<Feature, Feature>((Func<Feature, Feature>)(f =>
                new Feature(translate((Vector2)f.Tile1), translate((Vector2)f.Tile2), f.Type))).ToList<Feature>();

        LayoutPosition translate(Vector2 input) => (LayoutPosition)(input - min + new_min);
    }
}