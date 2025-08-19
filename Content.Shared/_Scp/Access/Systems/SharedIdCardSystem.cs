using Content.Shared.Access.Components;
using Content.Shared.Examine;

namespace Content.Shared.Access.Systems;

public abstract partial class SharedIdCardSystem
{
    private void InitializeScp()
    {
        SubscribeLocalEvent<IdCardComponent, ExaminedEvent>(OnExamine);
    }

    private void OnExamine(Entity<IdCardComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
            return;

        if (ent.Comp.FullName == null && ent.Comp.LocalizedJobTitle == null)
            return;

        var notFoundMessage = Loc.GetString("generic-not-available-shorthand");

        using (args.PushGroup(nameof(IdCardComponent)))
        {
            args.PushMarkup(Loc.GetString("id-card-examine",
                ("name", ent.Comp.FullName ?? notFoundMessage),
                ("job", ent.Comp.LocalizedJobTitle ?? notFoundMessage)));
        }
    }
}
