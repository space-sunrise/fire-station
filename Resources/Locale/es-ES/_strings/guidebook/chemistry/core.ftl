guidebook-reagent-effect-description =
    { $chance ->
        [1] { $effect }
       *[other] ¿Tiene { NATURALPERCENT($chance, 2) } posibilidad de { $effect }
    }{ $conditionCount ->
        [0] .
       *[other] { " " }, { $conditions } todavía.
    }
guidebook-reagent-name = [bold][color={ $color }]{ CAPITALIZE($name) }[/color][/bold]
guidebook-reagent-recipes-header = Receta
guidebook-reagent-recipes-reagent-display = [bold]{ $reagent }[/bold] \[{ $ratio }\]
guidebook-reagent-sources-header = Fuentes
guidebook-reagent-sources-ent-wrapper = [bold]{ $name }[/bold] \[1\]
guidebook-reagent-sources-gas-wrapper = [bold]{ $name } (gas)[/bold] \[1\]
guidebook-reagent-effects-header = Efectos
guidebook-reagent-effects-metabolism-group-rate = [bold]{ $group }[/bold] [color=gray]({ $rate } unidades por segundo)[/color]
guidebook-reagent-plant-metabolisms-header = Metabolismo de las plantas
guidebook-reagent-plant-metabolisms-rate = [bold]Plant Metabolism[/bold] [color=gray] (1 unidad cada 3 segundos normalmente)[/color]
guidebook-reagent-recipes-mix-info =
    { $minTemp ->
        [0]
            { $hasMax ->
                [true] { CAPITALIZE($verb) } abajo { $maxTemp }K
               *[false] { CAPITALIZE($verb) }
            }
       *[other]
            { CAPITALIZE($verb) } { $hasMax ->
                [true] entre { $minTemp }K y { $maxTemp }K
               *[false] por encima { $minTemp }K
            }
    }
guidebook-reagent-physical-description = [italic]The sustancia parece { $description }.[/italic].
