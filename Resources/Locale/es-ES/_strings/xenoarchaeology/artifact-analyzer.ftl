analysis-console-menu-title = Consola analítica de espectro ancho Mark 3
analysis-console-server-list-button = Servidor
analysis-console-extract-button = Extraer O.I.
analysis-console-info-no-scanner = ¡El analizador no está conectado! Por favor, conéctalo con una multiherramienta.
analysis-console-info-no-artifact = ¡Artefacto no encontrado! Coloca el artefacto en la plataforma.
analysis-console-info-ready = Todos los sistemas están funcionando.
analysis-console-no-node = Selecciona un nodo para ver
analysis-console-info-id = [font="Monospace" size=11]ID[/font]
analysis-console-info-id-value = [font="Monospace" size=11][color=yellow]{ $id }[/color][/font]
analysis-console-info-class = [font="Monospace" size=11]Classification[/font]
analysis-console-info-class-value = [font="Monospace" size=11]{ $class }[/font]
analysis-console-info-locked = [font="Monospace" size=11]Status[/font]
analysis-console-info-locked-value = [font="Monospace" size=11][color={ $state ->
        [0] rojo] bloqueado
        [1] cal] comercializable
       *[2] plum]Activo
    }[/color][/font]
analysis-console-info-durability = [font="Monospace" size=11]Charges[/font]
analysis-console-info-durability-value = [font="Monospace" size=11][color={ $color }]{ $current }/{ $max }[/color][/font]
analysis-console-info-effect = [font="Monospace" size=11]Effect:[/font]
analysis-console-info-effect-value = [font="Monospace" size=11][color=gray]{ $state ->
        [true] { $info }
       *[false] Desbloquea nodos para obtener información
    }[/color][/font]
analysis-console-info-trigger = [font="Monospace" size=11]Triggers:[/font]
analysis-console-info-triggered-value = [font="Monospace" size=11][color=gray]{ $triggers }[/color][/font]
analysis-console-info-scanner = Escaneando...
analysis-console-info-scanner-paused = Paused.
analysis-console-progress-text =
    { $seconds ->
        [one] T-{ $seconds } segundo
       *[other] T-{ $seconds } segundos
    }
analysis-console-extract-value = [font="Monospace" size=11][color=orange]Node { $id } (+{ $value })[/color][/font]
analysis-console-extract-none = [font="Monospace" size=11][color=orange] Nodos desbloqueados no tienen puntos por extraer. [/color][/font]
analysis-console-extract-sum = [font="Monospace" size=11][color=orange]Total O.I.: { $value }[/color][/font]
analyzer-artifact-extract-popup = ¡La superficie del artefacto brilla con energía!
