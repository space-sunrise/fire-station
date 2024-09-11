using System.Linq;
using Content.Server.Cuffs;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Shared.Cuffs.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.NPC.Components;
using Content.Shared.NPC.Systems;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Server._Scp.GameRules.ScpSl;

/*
 * Конец раунда начнется только если будут сделаны следующие условия -
Победа Мобильно-Оперативной Группы

-Все SCP-Объекты нейтрализованы
-Сбежало 1-2 ученых
-Сбежало 0 Д-Класса

Победа Класса-Д

-Сбежало 1-2 и т.д Д-Класса
-Сбежало 0 Ученых
-1 Объект СЦП Нейтрализован
-Охрана комплекса и МОГ уничтожены

Победа SCP-Объектов

-Сбежало 0 Ученых
-Сбежало 0 Д-Класса
-Охрана комплекса и МОГ уничтожены
-Остались только SCP и Хаос (или Только SCP)
-Нейтрализовано 0 SCP

Ничья

-Сбежал 1 Д-Класс
-Сбежал 1 Ученый
-Убито 1-2 SCP-Объектов
 */

public sealed class ScpSlGameRuleSystem : GameRuleSystem<ScpSlGameRuleComponent>
{
    [Dependency] private readonly NpcFactionSystem _npcFactionSystem = default!;
    [Dependency] private readonly CuffableSystem _cuffableSystem = default!;

    protected override string SawmillName => "ScpSl";

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ScpSlEscapeZoneComponent, StartCollideEvent>(OnEscapeZoneCollide);
    }

    private void OnEscapeZoneCollide(Entity<ScpSlEscapeZoneComponent> ent, ref StartCollideEvent args)
    {
        var ruleQuery = this.QueryActiveRules();

        var collidedEntity = args.OtherEntity;

        if (_npcFactionSystem.IsMember(collidedEntity, "ClassDFaction"))
        {
            if (TryComp<CuffableComponent>(collidedEntity, out var cuffed)
                && cuffed.CuffedHandCount > 0
                && _npcFactionSystem.GetNearbyHostiles(collidedEntity, 5f).Any())
            {
                //Become MOG
                Escaped(EscapedType.Scientist);
                return;
            }

            //Become Chaos
            Escaped(EscapedType.DClass);
            return;
        }

        if (_npcFactionSystem.IsMember(collidedEntity, "ScientistsFaction"))
        {

            if (TryComp<CuffableComponent>(collidedEntity, out var cuffed)
                && cuffed.CuffedHandCount > 0
                && _npcFactionSystem.GetNearbyHostiles(collidedEntity, 5f).Any())
            {
                //Become Chaos
                Escaped(EscapedType.DClass);
                return;
            }

            //Become MOG
            Escaped(EscapedType.Scientist);
            return;
        }
    }

    private void OnMobStateChanged(MobStateChangedEvent ev)
    {
        if (ev.NewMobState != MobState.Dead)
        {
            ShouldRoundEnd();
        }
    }

    protected override void Started(EntityUid uid, ScpSlGameRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);
    }

    protected override void Added(EntityUid uid, ScpSlGameRuleComponent component, GameRuleComponent gameRule, GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);
    }

    protected override void Ended(EntityUid uid, ScpSlGameRuleComponent component, GameRuleComponent gameRule, GameRuleEndedEvent args)
    {
        base.Ended(uid, component, gameRule, args);
    }

    protected override void AppendRoundEndText(EntityUid uid,
        ScpSlGameRuleComponent component,
        GameRuleComponent gameRule,
        ref RoundEndTextAppendEvent args)
    {
        base.AppendRoundEndText(uid, component, gameRule, ref args);
    }

    private bool ShouldRoundEnd()
    {
        var scpsQuery = EntityQueryEnumerator<ScpMarkerComponent, MobStateComponent, TransformComponent>();
        var humansQuery = EntityQueryEnumerator<NpcFactionMemberComponent, MobStateComponent, ActorComponent>();

        while (scpsQuery.MoveNext(out var scpUid, out var _, out var mobStateComponent, out var xformComponent))
        {

        }

        while (humansQuery.MoveNext(out var humanUid, out var npcFactionComponent, out var mobStateComponent, out var actorComponent))
        {

        }

        return false;
    }

    private void Escaped(EscapedType escapedType)
    {
        var rules = QueryActiveRules();

        while (rules.MoveNext(out var ruleUid, out var scpRuleComponent, out var ruleComponent))
        {
            if (escapedType == EscapedType.Scientist)
            {
                scpRuleComponent.EscapedScientists++;
                return;
            }

            scpRuleComponent.EscapedDClass++;
        }
    }

    private enum EscapedType
    {
        Scientist,
        DClass
    }
}
