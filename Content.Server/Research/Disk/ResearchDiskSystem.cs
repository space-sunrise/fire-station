using System.Linq;
using Content.Shared.Interaction;
using Content.Server.Popups;
using Content.Shared.Research.Prototypes;
using Content.Server.Research.Systems;
using Content.Shared.Research;
using Content.Shared.Research.Components;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Disk
{
    public sealed class ResearchDiskSystem : EntitySystem
    {
        [Dependency] private readonly IPrototypeManager _prototype = default!;
        [Dependency] private readonly PopupSystem _popupSystem = default!;
        [Dependency] private readonly ResearchSystem _research = default!;
        public override void Initialize()
        {
            base.Initialize();
            SubscribeLocalEvent<ResearchDiskComponent, AfterInteractEvent>(OnAfterInteract);
            SubscribeLocalEvent<ResearchDiskComponent, MapInitEvent>(OnMapInit);
        }

        private void OnAfterInteract(EntityUid uid, ResearchDiskComponent component, AfterInteractEvent args)
        {
            if (!args.CanReach)
                return;

            if (!TryComp<ResearchServerComponent>(args.Target, out var server))
                return;

            foreach (var (pointPrototype, value) in component.Points)
            {
                _research.ModifyServerPoints(args.Target.Value, pointPrototype, value, server);
                _popupSystem.PopupEntity(Loc.GetString("research-disk-inserted", ("points", component.Points)), args.Target.Value, args.User);
            }

            EntityManager.QueueDeleteEntity(uid);
            args.Handled = true;
        }

        private void OnMapInit(EntityUid uid, ResearchDiskComponent component, MapInitEvent args)
        {
            if (!component.UnlockAllTech)
                return;

            var allTechValue = new Dictionary<ProtoId<ResearchPointPrototype>, int>();

            foreach (var prototype in _prototype.EnumeratePrototypes<TechnologyPrototype>())
            {
                foreach (var (pointType, value) in prototype.Cost)
                {
                    allTechValue.TryGetValue(pointType, out var totalPoints);
                    allTechValue[pointType] = totalPoints + value;
                }
            }

            component.Points = allTechValue;
        }
    }
}
