using Content.Shared._Scp.Blinking;
using Content.Shared.Actions;
using Content.Shared.Eye.Blinding.Components;
using Content.Shared.Flash.Components;

namespace Content.Shared.Eye.Blinding.Systems;

// TODO: Почемы ты параша не являешься кор системой для моргания
// Fire edit - переименовал в SharedEyeClosingSystem и добавил наследников на сервере и клиенте
public abstract class SharedEyeClosingSystem : EntitySystem
{
    [Dependency] private readonly BlindableSystem _blindableSystem = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly SharedBlinkingSystem _blinking = default!; // Fire

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<EyeClosingComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<EyeClosingComponent, ComponentShutdown>(OnShutdown);
        SubscribeLocalEvent<EyeClosingComponent, ToggleEyesActionEvent>(OnToggleAction);
        SubscribeLocalEvent<EyeClosingComponent, CanSeeAttemptEvent>(OnTrySee);
        SubscribeLocalEvent<EyeClosingComponent, AfterAutoHandleStateEvent>(OnHandleState);
    }

    private void OnMapInit(Entity<EyeClosingComponent> eyelids, ref MapInitEvent args)
    {
        _actionsSystem.AddAction(eyelids, ref eyelids.Comp.EyeToggleActionEntity, eyelids.Comp.EyeToggleAction);
        Dirty(eyelids);
    }

    private void OnShutdown(Entity<EyeClosingComponent> eyelids, ref ComponentShutdown args)
    {
        _actionsSystem.RemoveAction(eyelids, eyelids.Comp.EyeToggleActionEntity);

        SetEyelids((eyelids.Owner, eyelids.Comp), false);
    }

    private void OnToggleAction(Entity<EyeClosingComponent> eyelids, ref ToggleEyesActionEvent args)
    {
        if (args.Handled)
            return;

        // Fire edit start
        if (HasComp<FlashedComponent>(eyelids))
            return;
        // Fire edit end

        args.Handled = true;
        SetEyelids((eyelids.Owner, eyelids.Comp), !eyelids.Comp.EyesClosed);
    }

    private void OnHandleState(Entity<EyeClosingComponent> eyelids, ref AfterAutoHandleStateEvent args)
    {
    }

    private void OnTrySee(Entity<EyeClosingComponent> eyelids, ref CanSeeAttemptEvent args)
    {
        if (eyelids.Comp.EyesClosed)
            args.Cancel();
    }

    /// <summary>
    /// Checks whether or not the entity's eyelids are closed.
    /// </summary>
    /// <param name="eyelids">The entity that contains an EyeClosingComponent</param>
    /// <returns>Exactly what this function says on the tin. True if eyes are closed, false if they're open.</returns>
    public bool AreEyesClosed(Entity<EyeClosingComponent?> eyelids)
    {
        return Resolve(eyelids, ref eyelids.Comp, false) && eyelids.Comp.EyesClosed;
    }

    /// <summary>
    /// Sets whether or not the entity's eyelids are closed.
    /// </summary>
    /// <param name="eyelids">The entity that contains an EyeClosingComponent</param>
    /// <param name="value">Set to true to close the entity's eyes. Set to false to open them</param>
    public void SetEyelids(Entity<EyeClosingComponent?> eyelids, bool value)
    {
        if (!Resolve(eyelids, ref eyelids.Comp))
            return;

        if (eyelids.Comp.EyesClosed == value)
            return;

        // Fire edit start.
        if (!_blinking.CanCloseEyes(eyelids.Owner))
            return;
        // Fire edit end.

        eyelids.Comp.EyesClosed = value;
        Dirty(eyelids);

        if (eyelids.Comp.EyeToggleActionEntity != null)
            _actionsSystem.SetToggled(eyelids.Comp.EyeToggleActionEntity, eyelids.Comp.EyesClosed);

        _blindableSystem.UpdateIsBlind(eyelids.Owner);

        // Fire edit start.
        if (value)
        {
            _blinking.ResetBlink(eyelids.Owner);
        }
        // Fire edit end.
    }

    public void UpdateEyesClosable(Entity<BlindableComponent?> blindable)
    {
        if (!Resolve(blindable, ref blindable.Comp, false))
            return;

        var ev = new GetBlurEvent(blindable.Comp.EyeDamage);
        RaiseLocalEvent(blindable.Owner, ev);

        if (_entityManager.TryGetComponent<EyeClosingComponent>(blindable, out var eyelids) && !eyelids.NaturallyCreated)
            return;

        if (ev.Blur < BlurryVisionComponent.MaxMagnitude || ev.Blur >= blindable.Comp.MaxDamage)
        {
            RemCompDeferred<EyeClosingComponent>(blindable);
            return;
        }

        var naturalEyelids = EnsureComp<EyeClosingComponent>(blindable);
        naturalEyelids.NaturallyCreated = true;
        Dirty(blindable);
    }
}

public sealed partial class ToggleEyesActionEvent : InstantActionEvent
{
}
