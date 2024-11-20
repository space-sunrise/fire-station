using Content.Shared._Scp.Scp096.Photo;
using Content.Shared.Examine;

namespace Content.Server._Scp.Scp096.Photo;

public sealed class Scp096PhotoSystem : EntitySystem
{
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly Scp096System _scp096 = default!;

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

        _scp096.TryAddTarget(args.Examiner, true, true);
    }
}
