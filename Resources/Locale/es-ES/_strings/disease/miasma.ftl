ammonia-smell = ¡Algo huele fuerte!
perishable-1 = [color=green]{ CAPITALIZE(OBJECT($target)) } cuerpo sigue pareciendo fresh.[/color]
perishable-2 = [color=orangered]{ CAPITALIZE(OBJECT($target)) } cuerpo no parece especialmente fresh.[/color]
perishable-3 = [color=red]{ CAPITALIZE(OBJECT($target)) } cuerpo no parece fresco en all.[/color]
perishable-1-nonmob = [color=green]{ CAPITALIZE(SUBJECT($target)) } sigue viéndose fresh.[/color]
perishable-2-nonmob = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } no parece especialmente fresh.[/color]
perishable-3-nonmob = [color=red]{ CAPITALIZE(SUBJECT($target)) } no parece especialmente fresh.[/color]
rotting-rotting = [color=orange]{ CAPITALIZE(SUBJECT($target)) } { GENDER($target) ->
        [male] Podredumbre
        [female] Podredumbre
        [epicene] Podredumbre
       *[neuter] Podredumbre
    }![/color]
rotting-bloated = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } { GENDER($target) ->
        [male] hinchada
        [female] hinchada
        [epicene] hinchada
       *[neuter] hinchada
    }![/color]
rotting-extremely-bloated = [color=red]{ CAPITALIZE(SUBJECT($target)) } fuerte { GENDER($target) ->
        [male] hinchada
        [female] hinchada
        [epicene] hinchada
       *[neuter] hinchada
    }![/color]
rotting-rotting-nonmob = [color=orange]{ CAPITALIZE(SUBJECT($target)) } se pudre[/color]
rotting-bloated-nonmob = [color=orangered]{ CAPITALIZE(SUBJECT($target)) } se hinchó[/color]
rotting-extremely-bloated-nonmob = ¡[color=red]{ CAPITALIZE(SUBJECT($target)) } se hinchó mucho[/color]
