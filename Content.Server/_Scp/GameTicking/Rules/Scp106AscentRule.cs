using System.Linq;
using Content.Server._Scp.GameTicking.Rules.Components;
using Content.Server._Scp.Scp106.Components;
using Content.Server.AlertLevel;
using Content.Server.Chat.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Rules;
using Content.Server.Ghost;
using Content.Server.Jittering;
using Content.Server.Speech.EntitySystems;
using Content.Server.Stunnable;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Content.Shared.StatusEffect;
using Robust.Server.Audio;
using Robust.Shared.Audio;
using Robust.Shared.Player;

namespace Content.Server._Scp.GameTicking.Rules;

public sealed class Scp106AscentRule : GameRuleSystem<Scp106AscentRuleComponent>
{
    [Dependency] private readonly JitteringSystem _jittering = default!;
    [Dependency] private readonly StutteringSystem _stuttering = default!;
    [Dependency] private readonly StunSystem _stun = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly AlertLevelSystem _alertLevel = default!;
    [Dependency] private readonly GhostSystem _ghost = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;

    private static readonly TimeSpan AscentStunTime = TimeSpan.FromSeconds(5f);
    private static readonly TimeSpan AscentJitterTime = TimeSpan.FromSeconds(15f);
    private static readonly TimeSpan AscentStutterTime = TimeSpan.FromSeconds(30f);

    // В теории несколько событий сразу без сранья педалями быть не должно, поэтому для удобства оно будет тут сохранено
    private EntityUid _ruleUid;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp106PortalSpawnerComponent, ComponentShutdown>(OnSpawnerShutdown);
    }

    protected override void Started(EntityUid uid, Scp106AscentRuleComponent component, GameRuleComponent gameRule, GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        // TODO: Разделить на два события, сначала страшное предупреждение, потом пизделово

        var statusEffectQuery = EntityQueryEnumerator<HumanoidAppearanceComponent, StatusEffectsComponent>();
        while (statusEffectQuery.MoveNext(out var human, out _, out _))
        {
            _stun.TryParalyze(human, AscentStunTime, true);
            _jittering.DoJitter(human, AscentJitterTime, true);
            _stuttering.DoStutter(human, AscentStutterTime, true);

            _ghost.DoGhostBooEvent(human);
        }

        var message = Loc.GetString("scp106-many-humans-in-backrooms-alarm-announcement");
        _chat.DispatchGlobalAnnouncement(message, colorOverride: Color.Red);

        _audio.PlayGlobal(
            "/Audio/_Sunrise/stab.ogg",
            Filter.Broadcast(),
            false,
            new AudioParams().WithVolume(-10));

        // TODO: Амбиент страшный пиздец

        if (!TryGetRandomStation(out var station))
            return;

        _alertLevel.SetLevel(station.Value, "gamma", true, true);

        _gameTicker.StartGameRule(component.SpawnPortalsRule);
        _ruleUid = uid;
    }

    private void OnSpawnerShutdown(Entity<Scp106PortalSpawnerComponent> ent, ref ComponentShutdown args)
    {
        var allPortals = EntityQuery<Scp106PortalSpawnerComponent>();

        if (!allPortals.Any())
            return;

        // Как только все порталы уничтожены завершает события вторжения
        _gameTicker.EndGameRule(_ruleUid);
    }
}
