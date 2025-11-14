using System.Linq;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Actions;
using Content.Shared.Damage;
using Content.Shared.Jittering;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;

    private readonly List<(EntityUid uid, TimeSpan end)> _pendingJitteringRemoval = [];

    private void InitializeActions()
    {
        SubscribeLocalEvent<Scp096Component, Scp096CryOutEvent>(OnCryOut);
    }

    private void UpdateActions()
    {
        if (_pendingJitteringRemoval.Count == 0)
            return;

        for (var i = 0; i < _pendingJitteringRemoval.Count; i++)
        {
            var (uid, end) = _pendingJitteringRemoval[i];

            if (_timing.CurTime < end)
                continue;

            RemComp<JitteringComponent>(uid);
            _pendingJitteringRemoval.RemoveAt(i);
        }
    }

    private void OnCryOut(Entity<Scp096Component> ent, ref Scp096CryOutEvent args)
    {
        if (args.Handled)
            return;

        var targets =
            _lookup.GetEntitiesInRange<DamageableComponent>(Transform(ent).Coordinates,
                    ent.Comp.CryOutRange,
                    LookupFlags.Static)
                .Where(e => IsValidForCryOutDamage(ent, e));

        var damagedAny = false;
        foreach (var target in targets)
        {
            if (_damageable.TryChangeDamage(target, ent.Comp.CryOutDamage, origin: ent, canHeal: false) != null)
                damagedAny = true;
        }

        // Никому не нанесли урон?
        if (!damagedAny)
            return;

        _audio.PlayPredicted(ent.Comp.CryOutSound, ent, ent);
        _actions.SetUseDelay(args.Action.AsNullable(), ent.Comp.CryOutCooldown);

        _jittering.AddJitter(ent, -10, 100f);
        _pendingJitteringRemoval.Add((ent, _timing.CurTime + ent.Comp.CryOutJitterTime));

        args.Handled = true;
    }

    private bool IsValidForCryOutDamage(Entity<Scp096Component> ent, EntityUid uid)
    {
        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.CryOutWhitelist, uid))
            return false;

        if (!_proximity.IsRightType(ent, uid, LineOfSightBlockerLevel.None, out _))
            return false;

        return true;
    }
}
