using System.Linq;
using Content.Server.Speech;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._Scp.Backrooms.DogAccent;

public sealed class DogAccentSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    private readonly TimeSpan _cooldown = TimeSpan.FromSeconds(10);
    private TimeSpan _lastTriggered = TimeSpan.Zero;

    private HashSet<string> _replacements = new HashSet<string>
    {
        "anomaly-accent-words-dog-1",
        "anomaly-accent-words-dog-2",
        "anomaly-accent-words-dog-3",
        "anomaly-accent-words-dog-4",
        "anomaly-accent-words-dog-5",
    };

    public override void Initialize()
    {
        SubscribeLocalEvent<DogAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, DogAccentComponent component, AccentGetEvent args)
    {
        if (!(_timing.CurTime - _lastTriggered >= _cooldown))
            return;

        args.Message = ReplaceRandomWords(args.Message);
        _lastTriggered = _timing.CurTime;
    }

    private string ReplaceRandomWords(string message)
    {
        // Разбиваем строку на слова
        var words = message.Split(new[] { ' ', ',', '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);

        var count = _random.Next(1, words.Length + 1);

        // Получаем случайные индексы для замены
        var indicesToReplace = Enumerable.Range(0, words.Length)
            .OrderBy(x => _random.Next())
            .Take(count)
            .ToList();

        foreach (var index in indicesToReplace)
        {
            var randomReplacement = _replacements.ElementAt(_random.Next(_replacements.Count));
            words[index] = Loc.GetString(randomReplacement);
        }

        return string.Join(" ", words);
    }
}
