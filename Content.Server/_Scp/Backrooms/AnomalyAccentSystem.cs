using Content.Server.Speech;
using Robust.Shared.Random;

namespace Content.Server._Scp.Backrooms;

public sealed class AnomalyAccentSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;

    // Значения сдвига по юникоду
    private const int ShiftMin = 5;
    private const int ShiftMax = 200;

    public override void Initialize()
    {
        SubscribeLocalEvent<AnomalyAccentComponent, AccentGetEvent>(OnAccent);
    }

    private void OnAccent(EntityUid uid, AnomalyAccentComponent component, AccentGetEvent args)
    {
        args.Message = Accentuate(args.Message);
    }

    private string Accentuate(string message)
    {
        var speechArray = message.ToCharArray();

        for (var i = 0; i < speechArray.Length; i++)
        {
            // Случайно решаем, заменять ли символ
            if (_random.Next(0, 2) == 0) // 50% вероятность
            {
                // Сдвиг символа по алфавиту юникода
                speechArray[i] = (char)(speechArray[i] + _random.Next(ShiftMin, ShiftMax));
            }
        }

        return new string(speechArray);
    }


}
