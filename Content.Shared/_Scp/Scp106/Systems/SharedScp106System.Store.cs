using Content.Shared._Scp.Scp106.Components;
using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract partial class SharedScp106System
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    private void InitializeStore()
    {
        // Abilities in that store - I love lambdas >:)

        // TODO: Проверка на хендхелд и кенселед
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106BoughtPhantomAction args) =>
            _actions.AddAction(ent, args.BoughtAction));
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106BoughtBareBladeAction args) =>
            _actions.AddAction(ent, args.BoughtAction));
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106BoughtCreatePortal args) =>
            _actions.AddAction(ent, args.BoughtAction));
        SubscribeLocalEvent((Entity<Scp106Component> ent, ref Scp106BoughtTerrify args) =>
            _actions.AddAction(ent, args.BoughtAction));

        SubscribeLocalEvent<Scp106Component, Scp106OnUpgradePhantomAction>(OnUpgradePhantomAction);

        SubscribeLocalEvent<Scp106Component, Scp106BareBladeAction>(OnScp106BareBladeAction);
    }

    private void OnUpgradePhantomAction(Entity<Scp106Component> ent, ref Scp106OnUpgradePhantomAction args)
    {
        ent.Comp.PhantomCoolDown -= args.CooldownReduce;
        Dirty(ent);
    }

    private void OnScp106BareBladeAction(Entity<Scp106Component> ent, ref Scp106BareBladeAction args)
    {
        TryToggleBlade(ent, ref args);
    }

    private bool TryToggleBlade(Entity<Scp106Component> ent, ref Scp106BareBladeAction args, bool force = false)
    {
        if (args.Handled)
            return false;

        // Клинок можно использовать только в карманном измерении или форсированно через код
        if (!IsInDimension(ent) && !force)
        {
            var message = Loc.GetString("scp106-bare-blade-action-invalid");
            _popup.PopupEntity(message, ent, ent);

            return false;
        }

        ToggleBlade(ent, args.Prototype);

        args.Handled = true;
        return true;
    }

    protected virtual void ToggleBlade(Entity<Scp106Component> ent, EntProtoId blade) { }
}
