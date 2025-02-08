using Content.Shared.Examine;

namespace Content.Shared._Scp.LightFlicking.MalfunctionLight;

public sealed class MalfunctionLightSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MalfunctionLightComponent, ExaminedEvent>(OnExamined);
    }

    private void OnExamined(Entity<MalfunctionLightComponent> ent, ref ExaminedEvent args)
    {
        var tip = Loc.GetString("malfunction-light-tip", ("item", Name(ent)));
        args.PushMarkup(tip);
    }
}
