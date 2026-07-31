scp-radio-cycle-channel = Canal de comunicación por conmutación
scp-radio-toggle-radio = Encendido/apagado
scp-radio-current-channel = Canal de comunicación actual: { $name }
scp-radio-microphone =
    Microphone { $value ->
        [true] Habilitado
       *[false] Fuera
    }
scp-radio-radio-status =
    Radio: { $value ->
        [true] [bold]included[/bold]
       *[false] [bold]off[/bold]
    }
scp-radio-microphone-status =
    Micrófono: { $value ->
        [true] [bold]included[/bold]
       *[false] [bold]Off[/bold]
    }
scp-radio-not-enough-charge = No hay suficiente carga
scp-radio-toggle-message =
    { $name } { $value ->
        [true] Incluye
       *[false] se apaga
    }
