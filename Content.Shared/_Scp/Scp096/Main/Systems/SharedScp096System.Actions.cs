using System.Linq;
using Content.Shared._Scp.Blood;
using Content.Shared._Scp.Proximity;
using Content.Shared._Scp.Scp096.Main.Components;
using Content.Shared.Damage;
using Content.Shared.DoAfter;
using Content.Shared.IdentityManagement;
using Content.Shared.Jittering;
using Content.Shared.Mobs;
using Content.Shared.Standing;

namespace Content.Shared._Scp.Scp096.Main.Systems;

public abstract partial class SharedScp096System
{
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly ProximitySystem _proximity = default!;
    [Dependency] private readonly DamageableSystem _damageable = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly StandingStateSystem _standing = default!;

    private readonly Dictionary<EntityUid, TimeSpan> _pendingJitteringRemoval = new ();

    private void InitializeActions()
    {
        SubscribeLocalEvent<Scp096Component, Scp096CryOutEvent>(OnCryOut);

        SubscribeLocalEvent<Scp096Component, Scp096FaceSkinRipEvent>(OnFaceSkinRip);
        SubscribeLocalEvent<Scp096Component, Scp096FaceSkinRipStartDoAfterEvent>(OnFaceSkinRipDoAfter);
        SubscribeLocalEvent<Scp096FaceComponent, MobStateChangedEvent>(OnFaceMobStateChanged);

        SubscribeLocalEvent<Scp096Component, Scp096SitDownEvent>(OnSitDown);
    }

    private void UpdateActions()
    {
        if (_pendingJitteringRemoval.Count == 0)
            return;

        foreach (var (ent, end) in _pendingJitteringRemoval)
        {
            if (_timing.CurTime < end)
                continue;

            RemComp<JitteringComponent>(ent);
            _pendingJitteringRemoval.Remove(ent);
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

        _jittering.AddJitter(ent, -10, 100f);
        AddToPendingJittering(ent, _timing.CurTime + ent.Comp.CryOutJitterTime);

        args.Handled = true;
    }

    private void OnFaceSkinRip(Entity<Scp096Component> ent, ref Scp096FaceSkinRipEvent args)
    {
        if (args.Handled)
            return;

        if (!ent.Comp.FaceEntity.HasValue)
            return;

        if (_scpMask.HasScpMask(ent))
        {
            var message = Loc.GetString("scp096-mask-prevent-skin-rip");
            _popup.PopupClient(message, ent, ent);

            return;
        }

        if (!TryGetFace(ent.AsNullable(), out var face))
            return;

        if (_mobState.IsIncapacitated(face.Value))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager,
            ent,
            ent.Comp.FaceSkinRipDoAfterTime,
            new Scp096FaceSkinRipStartDoAfterEvent(),
            ent,
            ent,
            ent)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnDropItem = true,
            BreakOnHandChange = true,
        };

        if (!_doAfter.TryStartDoAfter(doAfterArgs))
            return;

        _jittering.AddJitter(ent, -10, 100f);
        AddToPendingJittering(ent, _timing.CurTime + ent.Comp.FaceSkinRipDoAfterTime);

        args.Handled = true;
    }

    private void OnFaceSkinRipDoAfter(Entity<Scp096Component> ent, ref Scp096FaceSkinRipStartDoAfterEvent args)
    {
        if (args.Handled)
            return;

        if (args.Cancelled)
        {
            RemComp<JitteringComponent>(ent);

            // Не убираем из _pendingJitteringRemoval, потому что затраты на поиск нужного кортежа будет больше,
            // чем от временного хранения лишних данных
            return;
        }

        if (HasComp<ActiveScp096HeatingUpComponent>(ent) || HasComp<ActiveScp096RageComponent>(ent))
            return;

        if (!ent.Comp.FaceEntity.HasValue)
        {
            Log.Error("Failed to get SCP-096 face entity!");
            return;
        }

        var damaged = _damageable.TryChangeDamage(ent.Comp.FaceEntity,
            ent.Comp.FaceSkinRipDamageToFace,
            ignoreResistances: true,
            origin: ent,
            useModifier: false,
            useVariance: false);

        if (damaged == null)
            return;

        var message = Loc.GetString("scp096-face-skin-rip", ("name", Identity.Name(ent, EntityManager)));
        _popup.PopupPredicted(message, ent, ent);

        _audio.PlayPredicted(ent.Comp.FaceSkinRipDamageToFaceSound, ent, ent);
        SpawnBlood(ent.Owner);

        args.Handled = true;
    }

    private void OnFaceMobStateChanged(Entity<Scp096FaceComponent> ent, ref MobStateChangedEvent args)
    {
        if (!ent.Comp.FaceOwner.HasValue)
        {
            Log.Error("Found SCP-096 face without reference to original SCP-096");
            return;
        }

        switch (args.NewMobState)
        {
            case MobState.Dead:
            case MobState.Critical:
                EnsureComp<ActiveScp096WithoutFaceComponent>(ent.Comp.FaceOwner.Value);
                break;
            case MobState.Invalid:
            case MobState.Alive:
                // Используем RemCompDeferred, чтобы избежать конфликта с уже запланированным удалением
                // (например, при воскрешении через OnRejuvenate)
                RemCompDeferred<ActiveScp096WithoutFaceComponent>(ent.Comp.FaceOwner.Value);
                break;
        }
    }

    private void OnSitDown(Entity<Scp096Component> ent, ref Scp096SitDownEvent args)
    {
        if (args.Handled)
            return;

        if (!_timing.IsFirstTimePredicted)
            return;

        if (HasComp<ActiveScp096HeatingUpComponent>(ent) || HasComp<ActiveScp096RageComponent>(ent))
            return;

        var sat = _standing.IsDown(ent.Owner);
        args.Handled = TryToggleSit(ent.AsNullable(), sat);
    }

    private bool TryToggleSit(Entity<Scp096Component?> ent, bool haveToStand, bool useAnimation = true)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        // Проверка на эквивалентность состояния.
        // Пример - мы хотим встать, но уже стоим -> выходим из метода.
        if (!_standing.IsDown(ent.Owner) == haveToStand)
            return false;

        var successful = haveToStand
            ? _standing.Stand(ent.Owner)
            : _standing.Down(ent.Owner, false);

        if (!successful)
            return false;

        ToggleMovement(ent, haveToStand, false);
        TryModifyTearsSpawnSpeed(ent, !haveToStand);

        if (useAnimation)
        {
            ent.Comp.AgroToDeadAnimation = !haveToStand;
            ent.Comp.DeadToIdleAnimation = haveToStand;
            Dirty(ent);

            AddToPendingAnimations((ent, ent.Comp), _timing.CurTime + ent.Comp.AnimationDuration);
        }

        return true;
    }

    private bool IsValidForCryOutDamage(Entity<Scp096Component> ent, EntityUid uid)
    {
        if (!_whitelist.IsWhitelistPassOrNull(ent.Comp.CryOutWhitelist, uid))
            return false;

        if (!_proximity.IsRightType(ent, uid, LineOfSightBlockerLevel.None, out _))
            return false;

        return true;
    }

    private void AddToPendingJittering(Entity<Scp096Component> ent, TimeSpan end)
    {
        if (_pendingJitteringRemoval.TryGetValue(ent, out var existingEnd))
            _pendingJitteringRemoval[ent] = TimeSpan.FromSeconds(Math.Max(end.TotalSeconds, existingEnd.TotalSeconds));
        else
            _pendingJitteringRemoval[ent] = end;
    }

    protected virtual void SpawnBlood(Entity<BloodSplattererComponent?> ent) { }
}
