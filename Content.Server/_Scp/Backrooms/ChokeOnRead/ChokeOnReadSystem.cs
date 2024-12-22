using Content.Server.Body.Components;

namespace Content.Server._Scp.Backrooms.ChokeOnRead;

public sealed class ChokeOnReadSystem : EntitySystem
{
    // Бекап максимальной насыщенности. Да, мне похуй, что оно может отличаться. У людей 5
    private const float HumanMaxSaturation = 5.0f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ChokeOnReadComponent, BoundUIOpenedEvent>(OnRead);
        SubscribeLocalEvent<ChokeOnReadComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnRead(Entity<ChokeOnReadComponent> paper, ref BoundUIOpenedEvent args)
    {
        var user = args.Actor;

        if (!TryComp<RespiratorComponent>(user, out var respiratorComponent))
            return;

        var cursedSet = paper.Comp.Cursed;

        if (cursedSet.Contains(user))
            return;

        // Изменяем максимально возможную насыщенность кислородом, чтобы заставить игрока задыхаться
        respiratorComponent.MaxSaturation = respiratorComponent.MinSaturation;

        // Добавляем игрока в список проклятых
        cursedSet.Add(user);
    }

    private void OnShutdown(Entity<ChokeOnReadComponent> paper, ref ComponentShutdown args)
    {
        // Проходимся по всем существам из списка проклятых
        foreach (var uid in paper.Comp.Cursed)
        {
            if (!TryComp<RespiratorComponent>(uid, out var respiratorComponent))
                continue;

            // Снимаем проклятье
            respiratorComponent.MaxSaturation = HumanMaxSaturation;
        }
    }
}
