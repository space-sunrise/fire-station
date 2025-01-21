using Content.Shared.Humanoid;
using Content.Shared.Mobs;
using Content.Shared.Whitelist;
using Robust.Server.Audio;
using Robust.Server.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._Scp.Misc.KillGlobalSound;

/// <summary>
/// Система, созданная для проигрывания отдаленного звука убийства сущности специально для сцп
/// Проигрывает некий звук "отдаленного убийства" сущностям, что находятся далеко от места убийства, но не сильно
/// Нужна для создания атмосферы, типо ебать там кошмар происходит арараахавыа аврыарааа
/// </summary>
/// <remarks>
/// TODO: Рассмотреть вариант реализации на видимость убитой сущности и убившего сцп вместо радиусов
/// Это будет менее производительно, но тут это допустимо.
/// </remarks>
public sealed class KillGlobalSoundSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly TransformSystem _transform = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private const float ExceptRange = 16f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<HumanoidAppearanceComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<HumanoidAppearanceComponent> ent, ref MobStateChangedEvent args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        if (!TryComp<KillGlobalSoundComponent>(args.Origin, out var killSoundComponent))
            return;

        if (_entityWhitelist.IsWhitelistFailOrNull(killSoundComponent.OriginWhitelist, args.Origin.Value))
            return;

        if (!_random.Prob(killSoundComponent.Chance))
            return;

        var xform = Transform(ent);

        if (!xform.GridUid.HasValue)
            return;

        // Нам нужны сущности, находящиеся в отдалении
        // Поэтому мы берем сначала большой кружок максимального радиуса
        // А потом убираем из него маленький кружок ближайших сущностей
        // В итоге получается как раз те сущности, что находятся в отдалении, но не далеко
        var coords = _transform.GetMapCoordinates(ent);
        var filter = Filter.BroadcastGrid(xform.GridUid.Value)
            .AddInRange(coords, killSoundComponent.MaxRadius)
            .RemoveInRange(coords, ExceptRange);

        _audio.PlayGlobal(killSoundComponent.Sound, filter, true);
    }
}
