using System.Numerics;
using Content.Shared._Scp.Proximity;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._Scp.Shaders.Bloom;

/// <summary>
/// Система, управляющая эффектом свечения.
/// Высчитывает, какие сущности будут иметь эффект и передает в оверлеи.
/// </summary>
public sealed class LightingOverlaySystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayManager = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly SpriteSystem _sprite = default!;

    private EntityQuery<EyeComponent> _eyeQuery;
    private EntityQuery<TransformComponent> _transformQuery;

    private MaskLightingOverlay _mask = default!;
    private PointLightingOverlay _point = default!;

    /// <summary>
    /// Главный список для хранения сущностей для рендеринга эффекта.
    /// Создается в системе и передается в оба оверлея.
    /// </summary>
    private readonly List<(TransformComponent xform, Matrix3x2 matrix, Vector2 worldPos, Color color)> _entities = [];

    public override void Initialize()
    {
        base.Initialize();

        _mask = new MaskLightingOverlay(_prototypeManager, _sprite);
        _overlayManager.AddOverlay(_mask);

        _point = new PointLightingOverlay(_prototypeManager, _sprite);
        _overlayManager.AddOverlay(_point);

        _transformQuery = GetEntityQuery<TransformComponent>();
        _eyeQuery = GetEntityQuery<EyeComponent>();
    }

    public override void FrameUpdate(float frameTime)
    {
        base.FrameUpdate(frameTime);

        if (_player.LocalEntity == null)
            return;

        _entities.Clear();

        var drawFov = _eyeQuery.TryComp(_player.LocalEntity.Value, out var eye) && eye.DrawFov;

        // Просчитываем, какие сущности будут иметь эффект свечения и будет ли это видно игроку.
        // Если сущность проходит проверки -> добавляем ее в список и отправляем список в оверлеи.
        var query = EntityQueryEnumerator<BloomOverlayVisualsComponent, PointLightComponent, TransformComponent>();
        while (query.MoveNext(out var light, out _, out var pointLight, out var xform))
        {
            if (!pointLight.Enabled)
                continue;

            // TODO: Сделать настраиваемым
            if (drawFov && !_proximity.IsRightType(_player.LocalEntity.Value, light, LineOfSightBlockerLevel.Transparent, out _))
                continue;

            var (worldPos, _, worldMatrix) = _transform.GetWorldPositionRotationMatrix(xform, _transformQuery);

            _entities.Add((xform, worldMatrix, worldPos, pointLight.Color));
        }

        _mask.Entities = _entities;
        _point.Entities = _entities;
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay(_mask);
        _mask.Dispose();

        _overlayManager.RemoveOverlay(_point);
        _point.Dispose();
    }
}
