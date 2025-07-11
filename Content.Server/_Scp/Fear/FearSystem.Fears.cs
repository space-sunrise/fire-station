using Content.Shared.Humanoid;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Mobs;

namespace Content.Server._Scp.Fear;

public sealed partial class FearSystem
{
    private void InitializeFears()
    {
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (!HasComp<HumanoidAppearanceComponent>(ev.Target))
            return;

        var activated = ev.NewMobState == MobState.Dead;
        var toggleUsed = new ItemToggledEvent(false, activated, null);
        RaiseLocalEvent(ev.Target, ref toggleUsed);
    }
}
