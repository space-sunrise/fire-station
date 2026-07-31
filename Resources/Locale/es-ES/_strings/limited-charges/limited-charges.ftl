limited-charges-charges-remaining =
    Hay { $charges } { $charges ->
        [one] Carga
        [few] Carga
       *[other] Cargos
    }
limited-charges-max-charges = Tiene [color=green]maximum[/color] cargos.
limited-charges-recharging =
    { $seconds ->
        [one] Quedan [color=yellow]{ $seconds }[/color] segundos antes de la nueva carga.
        [few] Quedan [color=yellow]{ $seconds }[/color] segundos antes de la nueva carga.
       *[other] Quedan [color=yellow]{ $seconds }[/color] segundos para una nueva carga.
    }
