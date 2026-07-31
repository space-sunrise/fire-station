point-scoreboard-winner = ¡El ganador es [color=lime]{ $player }![/color]
point-scoreboard-header = [bold]Results table[/bold]
point-scoreboard-list =
    { $place }. [bold][color=cyan]{ $name }[/color][/bold] está ganando [color=yellow]{ $points ->
        [one] { $points } punto
       *[other] { $points } puntos
    }.[/color]
