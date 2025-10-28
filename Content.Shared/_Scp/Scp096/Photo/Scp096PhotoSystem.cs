using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared._Scp.Scp096.Main.Systems;
using Content.Shared._Scp.ScpMask;
using Content.Shared._Sunrise.Helpers;
using Content.Shared.Examine;

namespace Content.Shared._Scp.Scp096.Photo;

public sealed class Scp096PhotoSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedScp096System _scp096 = default!;
    [Dependency] private readonly SharedSunriseHelpersSystem _helpers = default!;
    [Dependency] private readonly ScpMaskSystem _scpMask = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp096PhotoComponent, ComponentInit>(OnInit);
        SubscribeLocalEvent<Scp096PhotoComponent, ExaminedEvent>(OnExamined);
    }

    private void OnInit(Entity<Scp096PhotoComponent> photo, ref ComponentInit args)
    {
        // Меняем спрайт на проявленный
        _appearance.SetData(photo, Scp096PhotoVisualLayers.Base, true);
    }

    private void OnExamined(Entity<Scp096PhotoComponent> photo, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (!_helpers.TryGetFirst<Scp096Component>(out var scp096))
            return;

        if (!_scp096.TryAddTarget(scp096.Value, args.Examiner, true, true))
            return;

        _scpMask.TryTear(scp096.Value);
    }
}
