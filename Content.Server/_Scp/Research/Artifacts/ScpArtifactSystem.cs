using Content.Server.Research.Systems;
using Content.Server.Xenoarchaeology.XenoArtifacts;
using Content.Server.Xenoarchaeology.XenoArtifacts.Events;
using Content.Shared._Scp.Mobs.Components;

namespace Content.Server._Scp.Research.Artifacts;

public sealed class ScpArtifactSystem : EntitySystem
{
    [Dependency] private readonly ArtifactSystem _artifact= default!;
    [Dependency] private readonly ResearchSystem _research = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ScpComponent, ArtifactActivatedEvent>(OnTrigger);
    }

    /// <summary>
    /// Метод, который выдает очки исследований после активации эффекта сцп.
    /// Каждый СЦП должен выдавать очки сразу после триггера, вместо стандартного метода через анализатор артефактов.
    /// </summary>
    /// <param name="scp">Активировавшийся сцп</param>
    /// <param name="args">Ивент активации артефакта</param>
    private void OnTrigger(Entity<ScpComponent> scp, ref ArtifactActivatedEvent args)
    {
        var pointsValue = _artifact.GetResearchPointValue(scp);

        if (pointsValue.Count == 0)
            return;

        if (!_research.TryGetClientServer(scp, out var server, out _))
            return;

        foreach (var (pointType, pointValue) in pointsValue)
        {
            _research.ModifyServerPoints(server.Value, pointType, pointValue);
            _artifact.AdjustConsumedPoints(scp, pointValue);
        }
    }

}
