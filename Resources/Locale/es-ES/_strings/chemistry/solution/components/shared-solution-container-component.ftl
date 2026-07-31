shared-solution-container-component-on-examine-main-text = Contiene [color={$color}]{$desc}[/color] { $chemCount ->
    [1] sustancia.
   *[other] mezcla de sustancias.
    }

examinable-solution-has-recognizable-chemicals = Reconocerás en la composición: {$recognizedString}.
examinable-solution-recognized = [color={$color}]{$chemical}[/color]

examinable-solution-on-examine-volume = Container { $fillLevel ->
    [exact] Contiene [color=white]{$current}/{$max}u[/color]
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}

examinable-solution-on-examine-volume-no-max = In container { $fillLevel ->
    [exact] contiene [color=white]{$current}u[/color].
   *[other] [bold]{ -solution-vague-fill-level(fillLevel: $fillLevel) }[/bold].
}

examinable-solution-on-examine-volume-puddle = Puddle { $fillLevel ->
    [exact] [color=white]{$current}u[/color].
    [full] ¡Enormes y brillantes!
    [mostlyfull] ¡Enormes y brillantes!
    [halffull] profundo y extendido.
    [halfempty] Muy profundo.
   *[mostlyempty] se acumula en algunos puntos.
    [empty] se desintegró en pequeñas gotas.
}

-solution-vague-fill-level =
    { $fillLevel ->
        [full] [color=white]complete[/color]
        [mostlyfull] [color=#DFDFDF]almost complete[/color]
        [halffull] [color=#C8C8C8]half full[/color]
        [halfempty] [color=#C8C8C8]half empty[/color]
        [mostlyempty] [color=#A4A4A4]almost empty[/color]
       *[empty] [color=gray]empty[/color]
    } 