using Content.Shared.Body.Components;
using Content.Shared.Body.Events;
using Content.Shared.Body.Systems;
using Content.Shared.Damage;
using Content.Shared.Nutrition;
using Content.Shared.Tag;
using Content.Shared.Whitelist;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;
using Content.Shared._Scp.Holding;

namespace Content.Shared._Scp.Scp082;

// TODO: Анхардкод
public abstract class SharedScp082System : EntitySystem
{
    [Dependency] private readonly SharedBodySystem _body = default!;
    [Dependency] private readonly StomachSystem _stomach = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<Scp082Component, GetMeleeDamageEvent>(OnGetMeleeDamage);
        SubscribeLocalEvent<Scp082Component, BodyInitEvent>(OnBodyInit);
        SubscribeLocalEvent<Scp082Component, BeforeIngestedEvent>(OnBeforeIngested);
        SubscribeLocalEvent<Scp082Component, ScpHoldAttemptEvent>(OnScpHoldAttempt);
    }

    private void OnGetMeleeDamage(Entity<Scp082Component> entity, ref GetMeleeDamageEvent args)
    {
        args.Damage = DamageSpecifier.ApplyModifier(args.Damage, entity.Comp.DamageModifier, 1f);
    }

    private void OnBeforeIngested(Entity<Scp082Component> entity, ref BeforeIngestedEvent args)
    {
        if (args.Solution is not { } solution)
            return;

        args.Transfer = solution.Volume;
    }

    private void OnScpHoldAttempt(Entity<Scp082Component> ent, ref ScpHoldAttemptEvent args)
    {
        if (ent.Comp.Anger >= ent.Comp.AngerHoldDisable)
            args.Cancelled = true;
    }

    protected virtual void OnBodyInit(Entity<Scp082Component> entity, ref BodyInitEvent args)
    {
        UpdateDigestibility(entity);
    }

    protected void UpdateDigestibility(Entity<Scp082Component> entity)
    {
        if (!_body.TryGetBodyOrganEntityComps<StomachComponent>(entity.Owner, out var stomachs))
            return;

        EntityWhitelist whitelist;
        if (entity.Comp.Hunger < entity.Comp.HumanoidFoodHungerThreshold)
        {
            var tags = new List<ProtoId<TagPrototype>>();
            tags.Add("Meat");
            whitelist = new EntityWhitelist { Tags = tags };
        }
        else
        {
            whitelist = new EntityWhitelist { Components = new[] { "Edible" } };
        }

        foreach (var stomach in stomachs)
        {
            _stomach.SetSpecialDigestible(stomach.Comp1, whitelist);
        }
    }
}
