using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Network;
using Robust.Shared.Serialization;

namespace Content.Shared._Scp.Blinking.ReducedBlinking;

// TODO: Переделать на химикат и дать возможно варить, используя реагент 173 и нечто из синтезатора реагентов.
// TODO: Добавить звук закапывания капель.
public abstract class SharedReducedBlinkingSystem : EntitySystem
{
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly INetManager _net = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ReducedBlinkingComponent, AfterInteractEvent>(OnInteract);
        SubscribeLocalEvent<ReducedBlinkingComponent, EyeDropletsUsedDoAfterEvent>(OnSuccess);
    }

    private void OnInteract(Entity<ReducedBlinkingComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (!HasComp<BlinkableComponent>(args.Target))
            return;

        var doAfterArgs = new DoAfterArgs(EntityManager, args.User, ent.Comp.ApplicationTime, new EyeDropletsUsedDoAfterEvent(), ent, args.Target, ent)
        {
            NeedHand = true,
            BreakOnMove = true,
            BreakOnDamage = true,
        };

        args.Handled = _doAfter.TryStartDoAfter(doAfterArgs);
    }

    private void OnSuccess(Entity<ReducedBlinkingComponent> ent, ref EyeDropletsUsedDoAfterEvent args)
    {
        if (args.Target == null)
            return;

        var target = args.Target.Value;

        if (!TryComp<BlinkableComponent>(target, out var blinkable))
            return;

        if (_blinking.AreEyesClosedManually(target))
        {
            _popup.PopupPredicted(Loc.GetString("eye-droplets-failed", ("name", Name(target))), ent, ent);
            return;
        }

        blinkable.AdditionalBlinkingTime = ent.Comp.FirstBlinkingBonusTime;
        DirtyField(target, blinkable, nameof(BlinkableComponent.AdditionalBlinkingTime));
        _blinking.ResetBlink(target, predicted: false);
        _useDelay.TryResetDelay(ent);

        var comp = new ActiveReducedBlinkingUserComponent()
        {
            Duration = ent.Comp.OtherBlinkingBonusDuration,
            BlinkingBonusTime = ent.Comp.OtherBlinkingBonusTime,
        };

        AddComp(target, comp, true);
        Dirty(target, comp);

        if (ent.Comp.UseSound != null)
            _audio.PlayPvs(ent.Comp.UseSound, ent);

        _popup.PopupPredicted(Loc.GetString("eye-droplets-used", ("name", Name(target))), ent, ent);

        // Уменьшаем количество оставшихся использований
        ent.Comp.UsageCount--;

        // Удаляем предмет, если использований не осталось
        if (ent.Comp.UsageCount <= 0 && _net.IsServer)
            QueueDel(ent);
    }
}

[Serializable, NetSerializable]
public sealed partial class EyeDropletsUsedDoAfterEvent : SimpleDoAfterEvent;
