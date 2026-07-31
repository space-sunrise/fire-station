# Loading Screen

replay-loading = Cargando ({ $cur }/{ $total })
replay-loading-reading = Lectura de archivos
replay-loading-processing = Procesamiento de archivos
replay-loading-spawning = Entidades que aparecen
replay-loading-initializing = Inicialización de entidades
replay-loading-starting = Creación de entidades
replay-loading-failed =
    No se pudo cargar la repetición. Error:
    { $reason }
replay-loading-retry = Intenta cargar con una mayor tolerancia a las excepciones: ¡PUEDE CAUSAR ERRORES!
replay-loading-cancel = Rechazar
# Main Menu
replay-menu-subtext = Repeticiones
replay-menu-load = Descarga la repetición seleccionada
replay-menu-select = Repetición selectiva
replay-menu-open = Abrir la carpeta de repeticiones
replay-menu-none = No se han encontrado repeticiones.
# Main Menu Info Box
replay-info-title = Información de la repetición
replay-info-none-selected = Repetición no seleccionada
replay-info-invalid = [color=red]Invalid repetición selected[/color]
replay-info-info =
    { "[" }color=gris]Seleccionado:[/color] { $name } ({ $file })
    { "[" }color=gris]Tiempo:[/color] { $time }
    { "[" }color=gris]ID de la ronda:[/color] { $roundId }
    { "[" }color=gris]Duración:[/color] { $duration }
    { "[" }color=gray]ForkId:[/color]   { $forkId }
    { "[" }color=gray]Version:[/color]   { $version }
    { "[" }color=gray]Engine:[/color]   { $engVersion }
    { "[" }color=gray]Type Hash:[/color]   { $hash }
    { "[" }color=gray]Comp Hash:[/color]   { $compHash }
# Replay selection window
replay-menu-select-title = Repetición selectiva
# Replay related verbs
replay-verb-spectate = Observa
# command
cmd-replay-spectate-help = Uso: replay_spectate [Entidad (Opcional)]
cmd-replay-spectate-desc = Adjunta o despina a un jugador local a un uid de entidad determinada.
cmd-replay-spectate-hint = EntidadUid Opcional
cmd-replay-toggleui-desc = Desactiva la interfaz de control de reproducción.
