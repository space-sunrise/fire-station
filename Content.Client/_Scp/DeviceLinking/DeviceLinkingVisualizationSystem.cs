using Content.Client._Scp.DeviceLinking.Overlays;
using Content.Shared._Scp.DeviceLinking;
using Robust.Client.Graphics;
using Robust.Shared.Random;

namespace Content.Client._Scp.DeviceLinking;

public sealed class DeviceLinkingVisualizationSystem : EntitySystem
{
    [Dependency] private readonly IOverlayManager _overlayMan = default!;

    private Dictionary<EntityUid, List<EntityUid>>? _rays;
    private Dictionary<EntityUid, Color>? _sourceColors;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<DeviceLinkOverlayData>(OnDebugOverlayData);
        SubscribeNetworkEvent<DeviceLinkOverlayToggledEvent>(OnOverlayToggled);
    }

    public override void Shutdown()
    {
        base.Shutdown();

        RemoveOverlay();
    }

    private void OnOverlayToggled(DeviceLinkOverlayToggledEvent args)
    {
        if (args.IsEnabled)
            _overlayMan.AddOverlay(new DeviceLinkDebugOverlay());
        else
            RemoveOverlay();
    }

    public Dictionary<EntityUid, List<EntityUid>>? GetConnectionRays() => this._rays;
    public Dictionary<EntityUid, Color>? GetSourceColors() => this._sourceColors;

    private void RemoveOverlay()
    {
        _overlayMan.RemoveOverlay<DeviceLinkDebugOverlay>();

        _rays = null;
        _sourceColors = null;
    }

    private void OnDebugOverlayData(DeviceLinkOverlayData args)
    {
        if (!_overlayMan.TryGetOverlay(out DeviceLinkDebugOverlay? overlay))
            return;

        _rays = new();
        _sourceColors = new();

        foreach (var ray in args.Rays)
        {
            List<EntityUid> entities = new();

            var source = GetEntity(ray.Source);

            if (!source.Valid || Transform(source).MapUid is null)
                continue;

            bool isInvalidConnection = false; // Проверка на то является ли сущность видной клиентом
            foreach (var connection in ray.Connections)
            {
                var entity = GetEntity(connection);

                if (!entity.Valid || Transform(entity).MapUid is null)
                {
                    isInvalidConnection = true;
                    break;
                }

                entities.Add(entity);
            }

            if (isInvalidConnection)
                continue;

            if (!_rays.ContainsKey(source))
                _rays.Add(source, entities);

            var random = new Random(ray.Source.Id);
            var rayColor = new Color(random.NextFloat(0, 1), random.NextFloat(0, 1), random.NextFloat(0, 1));

            _sourceColors.Add(source, rayColor);
        }
    }
}
