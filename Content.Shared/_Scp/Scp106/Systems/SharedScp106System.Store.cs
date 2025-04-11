using Content.Shared._Scp.Scp106.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._Scp.Scp106.Systems;

public abstract partial class SharedScp106System
{
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
        if (!CheckIsInDimension(ent) && !force)
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
