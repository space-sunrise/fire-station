using Content.Shared.Humanoid;
using Content.Shared.Implants;

namespace Content.Shared._Scp.Scp049.Scp049Protection.Implant;

// TODO: ЗАБИРАТЬ ЗАЩИТУ ПРИ УБИРАНИИ ИМППЛАНТА, КОГДА СТАНЕТ НЕ ВПАДЛУ ТРОГАТЬ ЭТО СНОВА

public sealed class Scp049ProtectionImplantSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, AddImplantAttemptEvent>(OnInsert);
    }

    private void OnInsert(Entity<HumanoidAppearanceComponent> ent, ref AddImplantAttemptEvent args)
    {
        if (args.Cancelled)
            return;

        if (!HasComp<Scp049ProtectionImplantComponent>(args.Implant))
            return;

        EnsureComp<Scp049ProtectionComponent>(args.Target);
    }
}
