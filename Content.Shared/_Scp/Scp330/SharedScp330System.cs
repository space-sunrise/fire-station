using Content.Shared.Examine;
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

        var container = _container.EnsureContainer<Container>(ent, ent.Comp.ContainerId);
        var message = Loc.GetString("scp330-see-count", ("count", container.Count));
        AddMessage(ent, ref args, message);
    }

    #endregion

    #region Helpers

    private void AddMessage(Entity<Scp330BowlComponent> ent, ref ExaminedEvent args, string message)
    {
        using (args.PushGroup(nameof(Scp330BowlComponent)))
        {
            args.PushMarkup(GetColoredMessage(ent, message), ExaminePriority);
        }
    }

    private string GetColoredMessage(Entity<Scp330BowlComponent> ent, string message)
    {
        return $"[color={ent.Comp.ExamineMessageColor.ToHex()}]{message}[/color]";
    }

    #endregion
}
