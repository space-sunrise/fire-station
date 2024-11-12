using System.Diagnostics.CodeAnalysis;
using Content.Server.Power.EntitySystems;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Interaction;
using Content.Shared.Kitchen;
using Content.Shared.Popups;
using Robust.Shared.Audio;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Containers;
using Robust.Shared.Timing;
using Content.Server.Jittering;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.Jittering;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._Scp.Research.ReagentSynthesizer;

public sealed class ReagentSynthesizerSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainersSystem = default!;
    [Dependency] private readonly ItemSlotsSystem _itemSlotsSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedAudioSystem _audioSystem = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly JitteringSystem _jitter = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ActiveReagentSynthesizerComponent, ComponentStartup>(OnActiveGrinderStart);
        SubscribeLocalEvent<ActiveReagentSynthesizerComponent, ComponentRemove>(OnActiveGrinderRemove);
        SubscribeLocalEvent<ReagentSynthesizerComponent, InteractUsingEvent>(OnInteractUsing);

        SubscribeLocalEvent<ReagentSynthesizerComponent, EntInsertedIntoContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ReagentSynthesizerComponent, EntRemovedFromContainerMessage>(OnContainerModified);
        SubscribeLocalEvent<ReagentSynthesizerComponent, ContainerIsRemovingAttemptEvent>(OnEntRemoveAttempt);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ActiveReagentSynthesizerComponent, ReagentSynthesizerComponent>();
        while (query.MoveNext(out var uid, out var active, out var synthesizer))
        {
            if (active.EndTime > _timing.CurTime)
                continue;

            synthesizer.AudioStream = _audioSystem.Stop(synthesizer.AudioStream);
            RemCompDeferred<ActiveReagentSynthesizerComponent>(uid);

            var container = _itemSlotsSystem.GetItemOrNull(uid, SharedReagentGrinder.BeakerSlotId);

            if (!container.HasValue)
                continue;

            if (!IsSynthesisable(container.Value, out var solutionEntity))
                continue;

            SynthesizeSolution(synthesizer, solutionEntity.Value);
        }
    }

    private void OnEntRemoveAttempt(Entity<ReagentSynthesizerComponent> entity, ref ContainerIsRemovingAttemptEvent args)
    {
        if (HasComp<ActiveReagentSynthesizerComponent>(entity))
            args.Cancel();
    }

    private void OnContainerModified(EntityUid uid, ReagentSynthesizerComponent reagentGrinder, ContainerModifiedMessage args)
    {
        if (HasComp<ActiveReagentSynthesizerComponent>(uid))
            return;

        if (!this.IsPowered(uid, EntityManager))
            return;

        DoWork((uid, reagentGrinder));
    }

    private void OnInteractUsing(Entity<ReagentSynthesizerComponent> entity, ref InteractUsingEvent args)
    {
        var heldEnt = args.Used;
        var inputContainer = _containerSystem.EnsureContainer<ContainerSlot>(entity.Owner, SharedReagentGrinder.BeakerSlotId);

        if (!HasComp<FitsInDispenserComponent>(heldEnt))
        {
            _popupSystem.PopupEntity(Loc.GetString("reagent-grinder-component-cannot-put-entity-message"), entity.Owner, args.User);

            return;
        }

        if (args.Handled)
            return;

        if (!_containerSystem.Insert(heldEnt, inputContainer))
            return;

        args.Handled = true;
    }

    private void DoWork(Entity<ReagentSynthesizerComponent> synthesizer)
    {
        var container = _containerSystem.EnsureContainer<ContainerSlot>(synthesizer, SharedReagentGrinder.BeakerSlotId);
        var containerUid = _itemSlotsSystem.GetItemOrNull(synthesizer, SharedReagentGrinder.BeakerSlotId);

        if (container.ContainedEntities.Count <= 0)
            return;

        if (!HasComp<FitsInDispenserComponent>(containerUid))
            return;

        if (!IsSynthesisable(containerUid.Value, out _))
            return;

        var active = EnsureComp<ActiveReagentSynthesizerComponent>(synthesizer);
        active.EndTime = _timing.CurTime + synthesizer.Comp.WorkTime;

        synthesizer.Comp.AudioStream = _audioSystem.PlayPvs(synthesizer.Comp.ActiveSound,
            synthesizer,
            AudioParams.Default.WithPitchScale(0.5f))?.Entity;
    }

    private void SynthesizeSolution(ReagentSynthesizerComponent synthesizer, Entity<SolutionComponent> solution)
    {
        var reagent = _random.Pick(synthesizer.Reagents);

        var volume = solution.Comp.Solution.Volume;

        _solutionContainersSystem.RemoveEachReagent(solution, volume);
        _solutionContainersSystem.TryAddReagent(solution, reagent, volume, out _);
    }

    #region Jitter

    private void OnActiveGrinderStart(Entity<ActiveReagentSynthesizerComponent> ent, ref ComponentStartup args)
    {
        _jitter.AddJitter(ent, -10, 100);
    }

    private void OnActiveGrinderRemove(Entity<ActiveReagentSynthesizerComponent> ent, ref ComponentRemove args)
    {
        RemComp<JitteringComponent>(ent);
    }

    #endregion

    #region Helpers

    private void ClickSound(Entity<ReagentSynthesizerComponent> reagentGrinder)
    {
        _audioSystem.PlayPvs(reagentGrinder.Comp.ClickSound, reagentGrinder.Owner, AudioParams.Default.WithVolume(-2f));
    }

    private bool IsSynthesisable(EntityUid containerUid,
        [NotNullWhen(true)] out Entity<SolutionComponent>? outSolutionEntity)
    {
        outSolutionEntity = null;

        if (!_solutionContainersSystem.TryGetFitsInDispenser(containerUid, out var solutionEntity, out var solution))
            return false;

        outSolutionEntity = solutionEntity;

        foreach (var reagent in solution.Contents)
        {
            if (!_prototype.TryIndex<ReagentPrototype>(reagent.Reagent.Prototype, out var reagentPrototype))
                continue;

            if (reagentPrototype.Synthesisable)
                return true;
        }

        return false;
    }

    #endregion

}
