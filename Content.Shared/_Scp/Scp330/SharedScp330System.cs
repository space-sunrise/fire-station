using Content.Shared.Examine;
using Content.Shared.Storage;
using Robust.Shared.Containers;

namespace Content.Shared._Scp.Scp330;

public abstract class SharedScp330System : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;

    private const int ExaminePriority = -50;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp330BowlComponent, ExaminedEvent>(OnExamined);
    }

    #region Event handlers

    private void OnExamined(Entity<Scp330BowlComponent> ent, ref ExaminedEvent args)
    {
        if (!args.IsInDetailsRange)
        {
            AddMessage(ent, ref args, Loc.GetString("scp330-cant-see-count"));
            return;
        }

        var container = _container.EnsureContainer<Container>(ent, StorageComponent.ContainerId);
        var message = Loc.GetString("scp330-see-count", ("count", container.Count));

        var tacked = ent.Comp.ThiefCounter.GetValueOrDefault(args.Examiner);
        var canTake = Math.Max(0, ent.Comp.PunishmentAfter - tacked);
        var message2 = canTake != 0
            ? Loc.GetString("scp330-can-take", ("count", canTake))
            : Loc.GetString("scp330-can-not-take");

        AddMessage(ent, ref args, message, message2);
    }

    #endregion

    #region Helpers

    private void AddMessage(Entity<Scp330BowlComponent> ent, ref ExaminedEvent args, params string[] messages)
    {
        using (args.PushGroup(nameof(Scp330BowlComponent)))
        {
            foreach (var message in messages)
            {
                args.PushMarkup(GetColoredMessage(ent, message), ExaminePriority);
            }
        }
    }

    private string GetColoredMessage(Entity<Scp330BowlComponent> ent, string message)
    {
        return $"[color={ent.Comp.ExamineMessageColor.ToHex()}]{message}[/color]";
    }

    #endregion
}
