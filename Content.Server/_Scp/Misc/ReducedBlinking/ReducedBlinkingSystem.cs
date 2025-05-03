using Content.Server._Scp.Blinking;
using Content.Server.DoAfter;
using Content.Shared._Scp.Blinking;
using Content.Shared._Scp.Other.ReducedBlinking;
using Content.Shared.DoAfter;
using Content.Shared.Humanoid;
using Content.Shared.Interaction;
using Content.Shared.Timing;
using Robust.Server.Audio;

namespace Content.Server._Scp.Misc.ReducedBlinking;

public sealed class ReducedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly BlinkingSystem _blinking = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly AudioSystem _audio = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReducedBlinkingComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ReducedBlinkingComponent, EyeDropletsUsedDoAfterEvent>(OnSuccess);
    }

    private void OnInteract(Entity<ReducedBlinkingComponent> entity, ref AfterInteractEvent args)
    {
        if (!TryComp(entity.Owner, out UseDelayComponent? useDelay) || _useDelay.IsDelayed((entity.Owner, useDelay)))
            return;

        if (!HasComp<HumanoidAppearanceComponent>(args.Target))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, entity.Comp.ApplicationTime, new EyeDropletsUsedDoAfterEvent(), entity, args.Target, entity)
        {
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnSuccess(Entity<ReducedBlinkingComponent> entity, ref EyeDropletsUsedDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<BlinkableComponent>(target, out var blinkableComponent))
            return;

        blinkableComponent.AdditionalBlinkingTime = entity.Comp.BonusTime;
        _blinking.ResetBlink(target);
        _useDelay.TryResetDelay(entity);

        if (entity.Comp.UseSound != null)
            _audio.PlayPvs(entity.Comp.UseSound, entity);

        // TODO: Удалить логику ниже после переделки предмета на химикат

        // Уменьшаем количество оставшихся использований
        entity.Comp.UsageCount--;

        // Удаляем предмет, если использований не осталось
        if (entity.Comp.UsageCount <= 0)
            QueueDel(entity);
    }

}
