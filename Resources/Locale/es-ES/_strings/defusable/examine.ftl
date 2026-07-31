defusable-examine-defused = { CAPITALIZE($name) } [color=lime]neutralized[/color].
defusable-examine-live =
    { CAPITALIZE($name) } está haciendo tic tac [color=red][/color] y quedan [color=red]{ $time } { $time ->
        [one] Segundo
        [few] Segundos
       *[other] Segundos
    }.
defusable-examine-live-display-off = { CAPITALIZE($name) } [color=red]ticks[/color] y el temporizador parece estar apagado.
defusable-examine-inactive = { CAPITALIZE($name) } [color=lime]inactive[/color], pero aún puede explotar.
defusable-examine-bolts =
    Bolts { $down ->
        [true] [color=red]omitted[/color]
       *[false] [color=green]raised[/color]
    }.
