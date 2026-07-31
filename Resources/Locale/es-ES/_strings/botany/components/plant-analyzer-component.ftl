plant-analyzer-component-no-seed = Planta no encontrada
plant-analyzer-component-health = Salud:
plant-analyzer-component-age = Edad:
plant-analyzer-component-water = Agua:
plant-analyzer-component-nutrition = Alimentos:
plant-analyzer-component-toxins = Toxinas:
plant-analyzer-component-pests = Plagas:
plant-analyzer-component-weeds = Malezas:
plant-analyzer-component-alive = [color=green]ALIVE[color]
plant-analyzer-component-dead = [color=red]DEAD[color]
plant-analyzer-component-unviable = [color=red]DEATH GENE[color]
plant-analyzer-component-mutating = [color=#00ff5f]MUTATES[color]
plant-analyzer-component-kudzu = [color=red]KUZU[color]
plant-analyzer-soil = Hay químicos no absorbidos en este { $holder }: [color=white]{ $chemicals }[/color].
plant-analyzer-soil-empty = No hay productos químicos no absorbidos en este { $holder }.
plant-analyzer-component-environemt = Este [color=green]{ $seedName }[/color] requiere una atmósfera con un nivel de presión [color=lightblue]{ $kpa }kPa ± { $kpaTolerance }kPa[/color], una temperatura de [color=lightsalmon]{ $temp }°C ± { $tempTolerance }°k[/color] y un nivel de iluminación [color=white]{ $lightLevel } ± { $lightTolerance }[/color].
plant-analyzer-component-environemt-void = Este [color=green]{ $seedName }[/color] debe cultivarse [bolditalic]in el vacío de space[/bolditalic] al nivel de la iluminación [color=white]{ $lightLevel } ± { $lightTolerance }[/color].
plant-analyzer-component-environemt-gas = Este [color=green]{ $seedName }[/color] requiere una atmósfera que contenga [bold]{ $gases }[/bold] a un nivel de presión de [color=lightblue]{ $kpa }kPa ± { $kpaTolerance }kPa[/color], una temperatura de [color=lightsalmon]{ $temp }°C ± { $tempTolerance }°K[/color] y un nivel de luz de [color=white]{ $lightLevel } ± { $lightTolerance }[/color].
plant-analyzer-produce-plural = { $thing }
plant-analyzer-output =
    { $yield ->
        [0]
            { $gasCount ->
                [0] Lo único que parece hacer es consumir agua y nutrientes.
               *[other] Lo único que parece hacer es convertir el agua y los nutrientes en [bold]{ $gases }[/bold].
            }
       *[other]
            Tiene [color=lightgreen]{ $yield } { $potency }[/color]{ $seedless ->
                [true] { " " }but [color=red]no seeds[/color]
               *[false] { $nothing }
            }{ " " }{ $yield ->
                [one] Flor
               *[other] Flores
            }{ " " }which{ $gasCount ->
                [0] { $nothing }
               *[other]
                    { $yield ->
                        [one] { " " }highlights
                       *[other] { " " }
                    }{ " " }[bold]{ $gases }[/bold] y
            }{ " " }will convertirse en { $yield ->
                [one] { " " }{ INDEFINITE($firstProduce) } [color=#a4885c]{ $produce }[/color]
               *[other] { " " }[color=#a4885c]{ $producePlural }[/color]
            }.{ $chemCount ->
                [0] { $nothing }
               *[other] { " " }Trace cantidades de [color=white]{ $chemicals }[/color] se encontraron en su tallo.
            }
    }
plant-analyzer-potency-tiny = Microscópico
plant-analyzer-potency-small = Pequeño
plant-analyzer-potency-below-average = Tamaño inferior a la media
plant-analyzer-potency-average = Tamaño medio
plant-analyzer-potency-above-average = Tamaño superior a la media
plant-analyzer-potency-large = Bastante grande
plant-analyzer-potency-huge = Enorme
plant-analyzer-potency-gigantic = gigantesco
plant-analyzer-potency-ludicrous = ridículamente grande
plant-analyzer-potency-immeasurable = inmensamente grande
plant-analyzer-print = Impresión
plant-analyzer-printout-missing = N/A
plant-analyzer-printout =
    {"[color=#9FED58][head=2]Plant Analizador Report[/head][/color]"}
    ──────────────────────────────
    {"[bullet/]"} Vista: {$seedName}
    {"    "}[bullet/] Idoneidad: {$viable ->
        [no][color=red]No[/color]
        [yes][color=green]Yes[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }
    {"    "}[bullet/] Resistencia: {$endurance}
    {"    "}[bullet/] Esperanza de vida: {$lifespan}
    {"    "}[bullet/] Producto: [color=#a4885c]{$produce}[/color]
    {"    "}[bullet/] Kudzu: {$kudzu ->
        [no][color=green]No[/color]
        [yes][color=red]Yes[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }
    {"[bullet/]"} Perfil de crecimiento:
    {"    "}[bullet/] Agua: [color=cyan]{$water}[/color]
    {"    "}[bullet/] Nutrientes: [color=orange]{$nutrients}[/color]
    {"    "}[bullet/] Toxinas: [color=yellowgreen]{$toxins}[/color]
    {"    "}[bullet/] Plagas: [color=magenta]{$pests}[/color]
    {"    "}[bullet/] Malezas: [color=red]{$weeds}[/color]
    {"[bullet/]"} Perfil Medioambiental:
    {"    "}[bullet/] Composición: [bold]{$gasesIn}[/bold]
    {"    "}[bullet/] Presión: [color=lightblue]{$kpa}kPa ± {$kpaTolerance}kPa[/color]
    {"    "}[bullet/] Temperatura: [color=lightsalmon]{$temp}°C ± {$tempTolerance}°C[/color]
    {"    "}[bullet/] Iluminación: [color=gray][bold]{$lightLevel} ± {$lightTolerance}[/bold][/color]
    {"[bullet/]"} Flores: {$yield ->
        [-1]{LOC("plant-analyzer-printout-missing")}
        [0][color=red]0[/color]
        *[other][color=lightgreen]{$yield} {$potency}[/color]
    }
    {"[bullet/]"} Semillas: {$seeds ->
        [no][color=red]No[/color]
        [yes][color=green]Yes[/color]
        *[other]{LOC("plant-analyzer-printout-missing")}
    }
    {"[bullet/]"} Químicos: [color=gray][bold]{$chemicals}[/bold][/color]
    {"[bullet/]"} Emisiones: [bold]{$gasesOut}[/bold]
