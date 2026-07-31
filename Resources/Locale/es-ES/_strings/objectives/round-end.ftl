objectives-round-end-result =
    { $count ->
        [one] Hubo una { $agent }.
        [few] Era { $count } { $agent }.
       *[other] Era { $count } { $agent }.
    }
objectives-round-end-result-in-custody = { $custody } de { $count } { $agent } fueron arrestados.
objectives-player-user-named = [color=White]{ $name }[/color] ([color=gray]{ $user }[/color])
objectives-player-user = [color=gray]{ $user }[/color]
objectives-player-named = [color=White]{ $name }[/color]
objectives-no-objectives = { $custody }{ $title } – { $agent }.

objectives-with-objectives = { $custody }{ $title } – { $agent } con los siguientes objetivos:

objectives-objective-success = {$objective} | [color=green]Success![/color] ({TOSTRING($progress, "P0")})
objectives-objective-partial-success = {$objective} | ¡[color=yellow]Partial ¡Éxito![/color] ({TOSTRING($progress, "P0")})
objectives-objective-partial-failure = {$objective} | ¡[color=orange]Partial fracaso![/color] ({TOSTRING($progress, "P0")})
objectives-objective-fail = {$objective} | [color=red]Failure![/color] ({TOSTRING($progress, "P0")})

objectives-in-custody = [bold][color=red]| ARRESTADO | [/color][/bold]
