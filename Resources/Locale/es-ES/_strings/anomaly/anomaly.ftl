anomaly-component-contact-damage = ¡La anomalía te está arrancando la piel!
anomaly-vessel-component-anomaly-assigned = La anomalía se asigna a la nave.
anomaly-vessel-component-not-assigned = A esta nave no se le ha asignado ninguna anomalía. Prueba con un escáner en ella.
anomaly-vessel-component-assigned = Esta nave ya ha sido asignada a una anomalía.
anomaly-particles-delta = Partículas delta
anomaly-particles-epsilon = Partículas épsilon
anomaly-particles-zeta = Partículas zeta
anomaly-particles-omega = Partículas Omega
anomaly-particles-sigma = Partículas sigma
anomaly-scanner-component-scan-complete = ¡Escaneo completado!
anomaly-scanner-ui-title = Escáner de anomalías
anomaly-scanner-no-anomaly = No hay anomalías escaneadas.
anomaly-scanner-severity-percentage = Peligro actual: [color=gray]{ $percent }[/color]
anomaly-scanner-severity-percentage-unknown = Peligro actual: [color=red]BUG[/color]
anomaly-scanner-stability-low = Estado actual de la anomalía: [color=gold]Decay[/color]
anomaly-scanner-stability-medium = Estado actual de la anomalía: [color=forestgreen]Stable[/color]
anomaly-scanner-stability-high = Estado actual de la anomalía: [color=crimson]Growth[/color]
anomaly-scanner-stability-unknown = Estado actual de la anomalía: [color=red]ERROR[/color]
anomaly-scanner-point-output = Generación pasiva de puntuación: [color=gray]{ $point }[/color]
anomaly-scanner-point-output-unknown = Generación pasiva de puntuación: [color=red]ERROR[/color]
anomaly-scanner-particle-readout = Análisis de reacciones de partículas:
anomaly-scanner-particle-danger = - [color=crimson]Dangerous tipo:[/color] { $type }
anomaly-scanner-particle-unstable = - [color=plum]Unstable tipo:[/color] { $type }
anomaly-scanner-particle-containment = - [color=goldenrod]Deterrent Tipo:[/color] { $type }
anomaly-scanner-particle-transformation = - [color=#6b75fa]Transforming Tipo:[/color] { $type }
anomaly-scanner-particle-danger-unknown = - [color=crimson]Dangerous tipo:[/color] [color=red]ERROR[/color]
anomaly-scanner-particle-unstable-unknown = - [color=plum]Unstable tipo:[/color] [color=red]ERROR[/color]
anomaly-scanner-particle-containment-unknown = - [color=goldenrod]Deterrent Tipo:[/color] [color=red]ERROR[/color]
anomaly-scanner-particle-transformation-unknown = - [color=#6b75fa]Transforming Tipo:[/color] [color=red]ERROR[/color]
anomaly-scanner-pulse-timer = Hora del siguiente pulso: [color=gray]{ $time }[/color]
anomaly-gorilla-core-slot-name = Núcleo de Anomalía
anomaly-gorilla-charge-none = No hay [bold]anomaly core[/bold] dentro.
anomaly-gorilla-charge-limit =
    { $count ->
        [one] Izquierda
       *[other] Izquierda
    } [color={ $count ->
        [3] green
        [2] yellow
        [1] orange
        [0] red
       *[other] purple
    }]{ $count } { $count ->
        [one] Carga
        [few] Carga
       *[other] Cargos
    }[/color].
anomaly-gorilla-charge-infinite = Quedan [color=gold]infinite charges[/color] más. [italic]For now...[/italic]
anomaly-sync-connected = Anomalía Enlazada con Éxito
anomaly-sync-disconnected = ¡La conexión con la anomalía se ha perdido!
anomaly-sync-no-anomaly = No hay anomalía dentro del rango.
anomaly-sync-examine-connected = Está [color=darkgreen]attached[/color] de la anomalía.
anomaly-sync-examine-not-connected = Está [color=darkred]not attached[/color] de la anomalía.
anomaly-sync-connect-verb-text = Adjuntar anomalía
anomaly-sync-connect-verb-message = Adjunta una anomalía cercana a { $machine }.
anomaly-generator-ui-title = Generador de anomalías
anomaly-generator-fuel-display = Combustible:
anomaly-generator-cooldown = Tiempo de recarga: [color=gray]{ $time }[/color]
anomaly-generator-no-cooldown = Tiempo de recarga: [color=gray]Completed[/color]
anomaly-generator-yes-fire = Estado: [color=forestgreen]Ready[/color]
anomaly-generator-no-fire = Estado: [color=crimson]Not Ready[/color]
anomaly-generator-generate = Crear anomalía
anomaly-generator-charges =
    { $charges ->
        [one] { $charges } carga
        [few] { $charges } carga
       *[other] { $charges } cargos
    }
anomaly-generator-announcement = ¡Se ha creado una anomalía!
anomaly-command-pulse = Activa un pulso de anomalía
anomaly-command-supercritical = La anomalía del objetivo entra en un estado supercrítico
# Flavor text on the footer
anomaly-generator-flavor-left = Puede ocurrir una anomalía dentro del operador.
anomaly-generator-flavor-right = v1.1
anomaly-behavior-unknown = [color=red]MISTAKE. Imposible de count.[/color]
anomaly-behavior-title = Análisis de desviaciones conductuales:
anomaly-behavior-point = [color=gold]Anomaly genera un { $mod }% de points[/color]
anomaly-behavior-safe = [color=forestgreen]The anomalía es extremadamente estable. Extremadamente rara pulses.[/color]
anomaly-behavior-slow = [color=forestgreen]The frecuencia de pulsos es significativamente reduced.[/color]
anomaly-behavior-light = [color=forestgreen]Pulse potencia es significativamente reduced.[/color]
anomaly-behavior-balanced = No se detectaron desviaciones de comportamiento.
anomaly-behavior-delayed-force = La frecuencia de las pulsaciones se reduce significativamente, pero su intensidad aumenta.
anomaly-behavior-rapid = La frecuencia de las pulsaciones aumenta significativamente, pero su intensidad se reduce.
anomaly-behavior-reflect = Se ha detectado un recubrimiento protector.
anomaly-behavior-nonsensivity = Se ha detectado una reacción débil a las partículas.
anomaly-behavior-sensivity = Se ha detectado una reacción fuerte a las partículas.
anomaly-behavior-invisibility = Se detectó distorsión del flujo de luz.
anomaly-behavior-secret = Se detectó interferencia. Algunos datos no pueden leerse
anomaly-behavior-inconstancy = [color=crimson]Impermanence ha sido detectado. Con el tiempo, los tipos de partículas pueden change.[/color]
anomaly-behavior-fast = [color=crimson]The frecuencia de pulsos es significativamente increased.[/color]
anomaly-behavior-strenght = [color=crimson]The potencia de los pulsos es significativamente increased.[/color]
anomaly-behavior-moving = [color=crimson]Coordinate inestabilidad detected.[/color]
