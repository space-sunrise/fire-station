using System.Linq;
using Content.Server._Scp.GameTicking.Rules.Components;
using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.GameTicking.Rules.Components;
using Content.Server.Mind;
using Content.Server.Roles.RoleCodeword;
using Content.Shared._Scp.Chaos;
using Content.Shared.Roles.RoleCodeword;
using Content.Shared.Speech;
using Content.Shared.Speech.Components;
using Robust.Shared.Player;

namespace Content.Server._Scp.GameTicking.Rules;

public sealed class ChaosSleepSpyRuleSystem : GameRuleSystem<ChaosSleepSpyRuleComponent>
{
    [Dependency] private readonly RoleCodewordSystem _roleCodeword = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChaosSleepSpyRuleComponent, AfterAntagEntitySelectedEvent>(AfterEntitySelected);
        SubscribeLocalEvent<ChaosSleepSpyMobComponent, ListenEvent>(OnListen);
    }

    private void AfterEntitySelected(Entity<ChaosSleepSpyRuleComponent> ent, ref AfterAntagEntitySelectedEvent args)
    {
        if (ent.Comp.CodeWords == null)
            return;

        EnsureComp<ActiveListenerComponent>(args.EntityUid);

        if (!_mind.TryGetMind(args.EntityUid, out var mindId, out _))
            return;

        var codewordComp = EnsureComp<RoleCodewordComponent>(mindId);
        _roleCodeword.SetRoleCodewords((mindId, codewordComp), "spy", ent.Comp.CodeWords.ToList(), ent.Comp.CodeWordColor);

        var sleepSpyMobComp = EnsureComp<ChaosSleepSpyMobComponent>(args.EntityUid);
        sleepSpyMobComp.CodeWords = ent.Comp.CodeWords;
    }

    private void OnListen(Entity<ChaosSleepSpyMobComponent> ent, ref ListenEvent args)
    {
        if (ent.Comp.CodeWords == null)
            return;

        foreach (var word in ent.Comp.CodeWords)
        {
            if (args.Message.Contains(word, StringComparison.OrdinalIgnoreCase))
            {
                TryUnsleepSpy(ent);
                return;
            }
        }
    }

    private void TryUnsleepSpy(Entity<ChaosSleepSpyMobComponent> ent)
    {
        if (!TryComp<ActorComponent>(ent, out var actorComp))
            return;

        _antag.ForceMakeAntag<ChaosSpyRuleComponent>(actorComp.PlayerSession, ent.Comp.DefaultChaosSpyRule);
        RemCompDeferred<ActiveListenerComponent>(ent);
        RemCompDeferred<ChaosSleepSpyMobComponent>(ent);
    }
}
