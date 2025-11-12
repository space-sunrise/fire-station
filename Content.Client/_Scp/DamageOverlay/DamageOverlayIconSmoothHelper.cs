using Content.Client.Damage;
using Content.Client.IconSmoothing;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;

namespace Content.Client._Scp.DamageOverlay;

/// <summary>
///     Helper methods for working with IconSmooth format damage overlays.
/// </summary>
public static class DamageOverlayIconSmoothHelper
{
    /// <summary>
    ///     Corner layer enum values matching IconSmoothSystem.CornerLayers
    /// </summary>
    private enum CornerLayers : byte
    {
        SE = 0,
        NE = 1,
        NW = 2,
        SW = 3,
    }

    /// <summary>
    ///     Gets the current corner index (0-7) for a given corner layer from IconSmoothComponent.
    ///     The corner index represents the combination of adjacent tiles.
    /// </summary>
    public static int? GetCornerIndex(EntityUid uid, byte cornerLayer, SpriteComponent sprite, IEntityManager entityManager)
    {
        var spriteSystem = entityManager.System<SpriteSystem>();
        var cornerKey = (CornerLayers)cornerLayer;

        // Находим правильный ИНДЕКС слоя, используя его КЛЮЧ
        if (!spriteSystem.LayerMapTryGet((uid, sprite), cornerKey, out var layerIndex))
            return null;

        // Теперь, имея правильный индекс, получаем сам слой
        if (!sprite.TryGetLayer(layerIndex, out var layer))
            return null;

        var state = layer.State;
        if (state == null)
            return null;

        var stateName = state.Name;
        if (string.IsNullOrEmpty(stateName))
            return null;

        // IconSmooth states are in format: {StateBase}{cornerIndex}
        // We need to extract the cornerIndex by removing StateBase prefix
        if (!entityManager.TryGetComponent<IconSmoothComponent>(uid, out var iconSmooth))
            return null;

        var stateBase = iconSmooth.StateBase;
        if (string.IsNullOrEmpty(stateBase) || !stateName.StartsWith(stateBase))
        {
            // Fallback: try to parse the last character as corner index
            if (stateName.Length > 0)
            {
                var lastChar = stateName[^1];
                if (char.IsDigit(lastChar) && int.TryParse(lastChar.ToString(), out var cornerIndex) && cornerIndex >= 0 && cornerIndex <= 7)
                {
                    return cornerIndex;
                }
            }
            return null;
        }

        // Remove StateBase prefix and parse the remaining part as corner index
        var cornerIndexStr = stateName.Substring(stateBase.Length);
        if (int.TryParse(cornerIndexStr, out var cornerIndexValue) && cornerIndexValue >= 0 && cornerIndexValue <= 7)
        {
            return cornerIndexValue;
        }

        return null;
    }

    /// <summary>
    ///     Gets all corner indices for the four corners (SE, NE, NW, SW) from IconSmoothComponent.
    ///     Returns null if IconSmoothComponent is not present or not in Corners mode.
    /// </summary>
    public static (int? se, int? ne, int? nw, int? sw)? GetCornerIndices(EntityUid uid, SpriteComponent sprite, IEntityManager entityManager)
    {
        if (!entityManager.TryGetComponent<IconSmoothComponent>(uid, out var iconSmooth) || iconSmooth.Mode != IconSmoothingMode.Corners)
            return null;

        var se = GetCornerIndex(uid, (byte)CornerLayers.SE, sprite, entityManager);
        var ne = GetCornerIndex(uid, (byte)CornerLayers.NE, sprite, entityManager);
        var nw = GetCornerIndex(uid, (byte)CornerLayers.NW, sprite, entityManager);
        var sw = GetCornerIndex(uid, (byte)CornerLayers.SW, sprite, entityManager);

        return (se, ne, nw, sw);
    }

    /// <summary>
    ///     Checks if an entity should use IconSmooth format for damage overlays.
    ///     Returns true if IconSmoothComponent is present with Corners mode,
    ///     or if UseIconSmoothFormat is explicitly set to true.
    /// </summary>
    public static bool ShouldUseIconSmoothFormat(EntityUid uid, DamageVisualsComponent damageVisComp, IEntityManager entityManager)
    {
        // If explicitly set, use that value
        if (damageVisComp.UseIconSmoothFormat.HasValue)
            return damageVisComp.UseIconSmoothFormat.Value;

        // Auto-detect: check if IconSmoothComponent exists with Corners mode
        if (entityManager.TryGetComponent<IconSmoothComponent>(uid, out var iconSmooth) && iconSmooth.Mode == IconSmoothingMode.Corners)
            return true;

        return false;
    }
}

