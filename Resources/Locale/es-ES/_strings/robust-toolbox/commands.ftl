### Localization for engine console commands


## generic command errors

cmd-invalid-arg-number-error = Un número inválido de argumentos.
cmd-parse-failure-integer = { $arg } no es un entero válido.
cmd-parse-failure-float = { $arg } no es un float válido.
cmd-parse-failure-bool = { $arg } no es un error válido.
cmd-parse-failure-uid = { $arg } no es una entidad válida UID.
cmd-parse-failure-mapid = { $arg } no es un MapID válido.
cmd-parse-failure-entity-exist = El UID { $arg } no coincide con una entidad existente.
cmd-error-file-not-found = No se puede encontrar el archivo: { $file }.
cmd-error-dir-not-found = No se pudo encontrar directorio: { $dir }.
cmd-failure-no-attached-entity = No hay ninguna entidad vinculada a esta carcasa.

## 'help' command

cmd-help-desc = Muestra ayuda general o específica de un mando
cmd-help-help =
    Uso: ayuda [nombre del equipo]
    Si no especificas un nombre de comando, se muestra ayuda general. Si se proporciona un nombre de comando, se muestra el comando.
cmd-help-no-args = Para obtener ayuda para un comando específico, usa 'ayuda<command>'. Para obtener una lista de todos los comandos disponibles, usa 'lista'. Para buscar por comando, usa 'lista<filter>'.
cmd-help-unknown = Equipo desconocido: { $command }
cmd-help-top = { $command } - { $description }
cmd-help-invalid-args = Un número inválido de argumentos.
cmd-help-arg-cmdname = [nombre del equipo]

## 'cvar' command

cmd-cvar-desc = Recibe o instala CVar.
cmd-cvar-help =
    Uso: cvar <nombre | ?> [valor]
    Si se proporciona un valor, se analizará y almacenará como un nuevo valor CVar.
    Si no, se mostrará el valor actual de CVar.
    Usa 'CVAR?' para obtener una lista de todos los CVar registrados.
cmd-cvar-invalid-args = Deben presentarse exactamente uno o dos argumentos.
cmd-cvar-not-registered = El CVar '{ $cvar }' no está registrado. Usa 'cvar?' para obtener una lista de todos los CVars registrados.
cmd-cvar-parse-error = El valor de entrada está en el formato incorrecto para el tipo de { $type }
cmd-cvar-compl-list = Lista de CVars disponibles
cmd-cvar-arg-name = <name | ?>
cmd-cvar-value-hidden = <value hidden>

## 'list' command

cmd-list-desc = Muestra una lista de comandos disponibles con un filtro de búsqueda opcional
cmd-list-help =
    Uso: lista [filtro]
    Lista todos los comandos disponibles. Si se ha proporcionado un argumento, se usará para filtrar los comandos por nombre.
cmd-list-heading = SIDE NAME            DESC{ "\u000A" }-------------------------{ "\u000A" }
cmd-list-arg-filter = [filtro]

## '>' command, aka remote exec

cmd-remoteexec-desc = Ejecuta un comando en el lado del servidor
cmd-remoteexec-help =
    Uso: > <command> [arg] [arg] [arg...]
    Ejecuta el comando desde el lado del servidor. Esto es necesario si hay un comando con el mismo nombre en el cliente, porque una simple ejecución de comando ejecutará primero el comando en el cliente.

## 'gc' command

cmd-gc-desc = Comienza GC (Recolector de basura)
cmd-gc-help =
    Uso: gc [generación]
    Usa GC. Recoger() para activar la recogida de basura.
    Si se ha proporcionado un argumento, se analizará como el número de generación del GC y será utilizado por el GC. Collect(int).
    Usa el comando 'gfc' para realizar la recogida de basura, con LOH-compacting.
cmd-gc-failed-parse = No fue posible descifrar el argumento.
cmd-gc-arg-generation = [generación]

## 'gcf' command

cmd-gcf-desc = Arranca GC, completa, con compactación LOH y todo.
cmd-gcf-help =
    Uso: gcf
    Ejecuta el GC completo. Recoger(2, GCCollectionMode.Forzado, verdadero, verdadero) mientras comprime el 'gran montón de objetos' del LOH.
    Lo más probable es que esto provoque una congelación de cientos de milisegundos, tenérselo en cuenta.

## 'gc_mode' command

cmd-gc_mode-desc = Cambia/muestra el modo de retardo de la GC
cmd-gc_mode-help =
    Uso: gc_mode [Tipo]
    Si no se ha presentado ningún argumento, el modo de retardo actual de la GC volverá a funcionar.
    Si el argumento se omitió, se analizará como GCLatencyMode y se establecerá como GC Delay Mode.
cmd-gc_mode-current = Modo de retardo actual de GC: { $prevMode }
cmd-gc_mode-possible = Modos posibles:
cmd-gc_mode-option = - { $mode }
cmd-gc_mode-unknown = Modo de retardo desconocido de GC: { $arg }
cmd-gc_mode-attempt = Intento de cambiar el modo de retardo de GC: { $prevMode } -> { $mode }
cmd-gc_mode-result = Modo de retardo GC resultante: { $mode }
cmd-gc_mode-arg-type = [tip]

## 'mem' command

cmd-mem-desc = Muestra información de memoria gestionada
cmd-mem-help = Uso: mem
cmd-mem-report =
    Tamaño del montón: { TOSTRING($heapSize, "N0") }
    Total distribuido: { TOSTRING($totalAllocated, "N0") }

## 'physics' command

cmd-physics-overlay = { $overlay } no es una superposición reconocida

## 'lsasm' command

cmd-lsasm-desc = Lista ensamblajes cargados por contexto de arranque
cmd-lsasm-help = Uso: LSASM

## 'exec' command

cmd-exec-desc = Ejecuta un archivo de script a partir de los datos de usuario del juego que se están escribiendo
cmd-exec-help =
    Uso: exec <fileName>
    Cada línea del archivo se ejecuta como un solo comando, a menos que comience con un #
cmd-exec-arg-filename = <fileName>

## 'dump_net_comps' command

cmd-dump_net_comps-desc = Muestra una tabla de componentes de red.
cmd-dump_net_comps-help = Uso: dump_net-comps
cmd-dump_net_comps-error-writeable = El registro sigue siendo grabable y no se han generado identificaciones en línea.
cmd-dump_net_comps-header = Registros de componentes de red:

## 'dump_event_tables' command

cmd-dump_event_tables-desc = Muestra tablas de eventos dirigidos para una entidad.
cmd-dump_event_tables-help = Uso: dump_event_tables <entityUid>
cmd-dump_event_tables-missing-arg-entity = Argumento de entidad faltante
cmd-dump_event_tables-error-entity = Entidad inválida
cmd-dump_event_tables-arg-entity = <entityUid>

## 'monitor' command

cmd-monitor-desc = Activa el monitor de depuración en el menú F3.
cmd-monitor-help =
    Uso: monitor <name>
    Monitores posibles: { $monitors }
    También puedes usar los valores especiales "-all" y "+all" para ocultar o mostrar todos los monitores respectivamente.
cmd-monitor-arg-monitor = <monitor>
cmd-monitor-invalid-name = Nombre del monitor inválido
cmd-monitor-arg-count = Argumento del monitor ausente
cmd-monitor-minus-all-hint = Oculta todos los monitores
cmd-monitor-plus-all-hint = Muestra todos los monitores

## Mapping commands

cmd-set-ambient-light-desc = Permite configurar la iluminación ambiental para la tarjeta especificada, en formato SRGB.
cmd-set-ambient-light-help = Uso: setambientlight [mapid] [r g b a]
cmd-set-ambient-light-parse = No podía analizar los argumentos como valores de bytes de colores.
cmd-savemap-desc = Serializa la tarjeta al disco. No guarda la tarjeta tras la inicialización a menos que se le forceje.
cmd-savemap-help = Uso: mapa de <MapID> <Path> guardado [forza]
cmd-savemap-not-exist = El mapa objetivo no existe.
cmd-savemap-init-warning = Intenté guardar el mapa tras inicializarlo sin que me obligaran a guardar.
cmd-savemap-attempt = Un intento de salvar el mapa { $mapId } a { $path }.
cmd-savemap-success = El mapa ha sido guardado con éxito.
cmd-hint-savemap-id = <MapID>
cmd-hint-savemap-path = <Path>
cmd-hint-savemap-force = [bool]
cmd-loadmap-desc = Carga un mapa desde el disco dentro del juego.
cmd-loadmap-help = Uso: mapa de carga <MapID> <Path> [x] [y] [rotación] [uids consistentes]
cmd-loadmap-nullspace = No se puede cargar 0 en el mapa.
cmd-loadmap-exists = El mapa { $mapId } ya existe.
cmd-loadmap-success = El mapa { $mapId } se descargó de { $path }.
cmd-loadmap-error = Ocurrió un error al cargar el mapa desde { $path }.
cmd-hint-loadmap-x-position = [x-position]
cmd-hint-loadmap-y-position = [y-position]
cmd-hint-loadmap-rotation = [rotation]
cmd-hint-loadmap-uids = [float]
cmd-hint-savebp-id = <Grid EntityID>

## 'flushcookies' command


# Nota: el comando flushcookies está tomado de Robust.Client.WebView, no está en el código del motor principal.

cmd-flushcookies-desc = Restablecer el almacenamiento de cookies CEF en disco
cmd-flushcookies-help =
    Esto garantiza que las cookies se almacenen correctamente en el disco en caso de un apagado descuidado.
    Ten en cuenta que la operación real es asincrónica.
cmd-ldrsc-desc = Precachea el recurso.
cmd-guidump-desc = Descarga el árbol de interfaz gráfica para /guidump.txt en los datos del usuario.
cmd-guidump-help = Uso: guidump
cmd-uitest-desc = Se abre la ventana de prueba de la interfaz.
cmd-uitest-help = Uso: uitest
cmd-uitest2-desc = Se abre la ventana de Control de Prueba de la Interfaz.
cmd-uitest2-help = Uso: uitest2 <tab>
cmd-uitest2-arg-tab = <tab>
cmd-uitest2-error-args = Se esperaba un máximo de un argumento.
cmd-uitest2-error-tab = Pestaña inválida: '{ $value }'
cmd-uitest2-title = UITest2
cmd-setclipboard-desc = Configura el portapapeles del sistema.
cmd-setclipboard-help = Uso: portapapeles <text>
cmd-getclipboard-desc = Recupera la carpeta del sistema.
cmd-getclipboard-help = Uso: GetClipboard
cmd-togglelight-desc = Activa el renderizado de la iluminación.
cmd-togglelight-help = Uso: togglelight
cmd-togglefov-desc = Cambia el campo de visión para el cliente.
cmd-togglefov-help = Uso: togglefov
cmd-togglehardfov-desc = Desactiva el FOV duro para el cliente. (para depurar space-station-14#2353)
cmd-togglehardfov-help = Uso: togglehardfov
cmd-toggleshadows-desc = Activa el renderizado de sombras.
cmd-toggleshadows-help = Uso: toggleshadows
cmd-togglelightbuf-desc = Activa el renderizado de la iluminación. Esto incluye las sombras, pero no el campo de visión.
cmd-togglelightbuf-help = Uso: togglelightbuf
cmd-chunkinfo-desc = Recupera la información del fragmento bajo tu cursor.
cmd-chunkinfo-help = Uso: chunkinfo
cmd-rldshader-desc = Recarga todos los shaders.
cmd-rldshader-help = Uso: rldshader
cmd-cldbglyr-desc = Activa la depuración del campo de visión y las capas de iluminación.
cmd-cldbglyr-help =
    Uso: cldbglyr <layer>: Toggle <layer>
    cldbglyr: Desactiva todas las capas
cmd-key-info-desc = Muestra información sobre la llave.
cmd-key-info-help = Uso: keyinfo <Key>
cmd-bind-desc = Asigna un acceso directo de teclado a un comando de entrada.
cmd-bind-help =
    Uso: asignar { cmd-bind-arg-key } { cmd-bind-arg-mode } { cmd-bind-arg-command }
    Ten en cuenta que esto NO guarda los enlaces automáticamente. Usa el comando 'svbind' para guardar la configuración de los enlaces.
cmd-bind-arg-key = <KeyName>
cmd-bind-arg-mode = <BindMode>
cmd-bind-arg-command = <InputCommand>
cmd-net-draw-interp-desc = Desactiva el mapeo de depuración por interpolación de red.
cmd-net-draw-interp-help = Uso: net_draw_interp
cmd-net-watch-ent-desc = Imprime todas las actualizaciones de red del EntityID en la consola.
cmd-net-watch-ent-help = Uso: net_watchent <0|EntityUid>
cmd-net-refresh-desc = Consulta el estado completo del servidor.
cmd-net-refresh-help = Uso: net_refresh
cmd-net-entity-report-desc = Activa el panel de Informes de Entidad de Red.
cmd-net-entity-report-help = Uso: net_entityreport
cmd-fill-desc = Llena la consola para la depuración.
cmd-fill-help = Llena la consola de tonterías para depurar.
cmd-cls-desc = Limpia la consola.
cmd-cls-help = Limpia la consola de todos los mensajes.
cmd-sendgarbage-desc = Envía basura al servidor.
cmd-sendgarbage-help = El camarero responderá con "no u"
cmd-loadgrid-desc = Carga la cuadrícula de un archivo a un mapa existente.
cmd-loadgrid-help = Uso: cuadrícula de carga <MapID> <Path> [x y] [rotación] [almacena Uids]
cmd-loc-desc = Muestra la ubicación absoluta de la entidad del jugador en la consola.
cmd-loc-help = Uso: loc
cmd-tpgrid-desc = Teletransporta la red a una nueva ubicación.
cmd-tpgrid-help = Uso: tpgrid <gridId> <X> <Y> [<MapId>]
cmd-rmgrid-desc = Elimina una cuadrícula del mapa. No puedes borrar una cuadrícula estándar.
cmd-rmgrid-help = Uso: rmgrid <gridId>
cmd-mapinit-desc = Empieza a inicializar el mapa en el mapa.
cmd-mapinit-help = Uso: mapinit <mapID>
cmd-lsmap-desc = Enumera las cartas.
cmd-lsmap-help = Uso: LSMAP
cmd-lsgrid-desc = Enumera las cuadrículas.
cmd-lsgrid-help = Uso: lsgrid
cmd-addmap-desc = Añade un nuevo mapa en blanco a una ronda. Si el mapID ya existe, este comando no hace nada.
cmd-addmap-help = Uso: addmap <mapID> [inicializar]
cmd-rmmap-desc = Elimina el mapa del mundo. No se puede borrar el espacio nulo.
cmd-rmmap-help = Uso: rmmap <mapId>
cmd-savegrid-desc = Guarda la malla en el disco.
cmd-savegrid-help = Uso: cuadrícula <gridID> <Path>de guardado
cmd-testbed-desc = Carga el campo de prueba de física en el mapa especificado.
cmd-testbed-help = Uso: banco <mapid> <test>de pruebas
cmd-saveconfig-desc = Guarda la configuración del cliente en un archivo de configuración.
cmd-saveconfig-help = Uso: saveconfig
cmd-addcomp-desc = Añade un componente a una entidad.
cmd-addcomp-help = Uso: comp de adversiones <uid> <componentName>
cmd-addcompc-desc = Añade un componente a una entidad en el cliente.
cmd-addcompc-help = Uso: addcompc <uid> <componentName>
cmd-rmcomp-desc = Elimina un componente de una entidad.
cmd-rmcomp-help = Uso: rmcomp <uid> <componentName>
cmd-rmcompc-desc = Elimina un componente de una entidad en el cliente.
cmd-rmcompc-help = Uso: rmcompc <uid> <componentName>
cmd-addview-desc = Te permite suscribirte a la pantalla de la entidad para depurar.
cmd-addview-help = Uso: addview <entityUid>
cmd-addviewc-desc = Te permite suscribirte para mostrar una entidad en el cliente para la depuración.
cmd-addviewc-help = Uso: addview <entityUid>
cmd-removeview-desc = Te permite darte de baja de mostrar una entidad para depuración.
cmd-removeview-help = Uso: removeview <entityUid>
cmd-loglevel-desc = Cambia el nivel de registro para el aserradero especificado.
cmd-loglevel-help =
    Uso: loglevel <sawmill> <level>
    Aserradero: La etiqueta que precede a los mensajes de registro. Para la que estableces el nivel.
    nivel: Nivel logarítmico. Debe coincidir con uno de los valores de la enumeración LogLevel.
cmd-testlog-desc = Escribe un registro de pruebas en el aserradero.
cmd-testlog-help =
    Uso: testlog <sawmill> <level> <message>
    Aserradero: La etiqueta que precede al mensaje registrado.
    nivel: Nivel logarítmico. Debe coincidir con uno de los valores de la enumeración LogLevel.
    mensaje: El mensaje que se registrará. Envuélvelo entre comillas dobles si quieres usar espacios.
cmd-vv-desc = Abre las variables de vista.
cmd-vv-help = Uso: vv <entity ID|nombre de interfaz IoC|nombre de interfaz SIoC>
cmd-showvelocities-desc = Muestra tus velocidades angulares y lineales.
cmd-showvelocities-help = Uso: showvelocities
cmd-setinputcontext-desc = Establece el contexto de entrada activo.
cmd-setinputcontext-help = Uso: setinputcontext <context>
cmd-forall-desc = Ejecuta un comando para todas las entidades con un componente dado.
cmd-forall-help = Uso: para todas las consultas <bql> ¿<comando...>
cmd-delete-desc = Elimina una entidad con el ID especificado.
cmd-delete-help = Uso: eliminar <entity UID>
# System commands
cmd-showtime-desc = Muestra la hora del servidor.
cmd-showtime-help = Uso: hora del espectáculo
cmd-restart-desc = Reinicia cuidadosamente el servidor (no solo la ronda).
cmd-restart-help = Uso: reiniciar
cmd-shutdown-desc = Cierra el servidor de forma ordenada.
cmd-shutdown-help = Uso: cierre
cmd-netaudit-desc = Muestra la información de seguridad de NetMsg.
cmd-netaudit-help = Uso: netaudit
# Player commands
cmd-tp-desc = Teletransporta al jugador a cualquier lugar de la ronda.
cmd-tp-help = Uso: tp <x> <y> [<mapID>]
cmd-tpto-desc = Teletransporta al jugador actual o a los jugadores/entidades especificados a la ubicación del primer jugador/entidad.
cmd-tpto-help = Uso: tpto <nombre de usuario|uid> [nombre de usuario|uid]...
cmd-tpto-destination-hint = Destino (UID o nombre de usuario)
cmd-tpto-victim-hint = Entidad para teletransporte (UID o nombre de usuario)
cmd-tpto-parse-error = No se pudo encontrar entidad o jugador: { $str }
cmd-listplayers-desc = Lista a todos los jugadores que están actualmente conectados.
cmd-listplayers-help = Uso: listplayers
cmd-kick-desc = Expulsa a un jugador conectado del servidor, desconectándolo.
cmd-kick-help = Uso: patada <PlayerIndex> [<Reason>]
# Spin command
cmd-spin-desc = Hace que la entidad gire. Por defecto, la entidad es la madre del jugador conectado.
cmd-spin-help = Uso: velocidad de giro [arrastre] [entityUid]
# Localization command
cmd-rldloc-desc = Recarga la localización (cliente y servidor).
cmd-rldloc-help = Uso: rldloc
# Debug entity controls
cmd-spawn-desc = Crea una entidad del tipo especificado.
cmd-spawn-help = Uso: aparición <prototype> O aparición <prototype> <ID de entidad relativa> O aparición <prototype> <x> <y>
cmd-cspawn-desc = Crea una entidad cliente del tipo especificado a tus pies.
cmd-cspawn-help = Uso: cspawn <tipo de entidad>
cmd-scale-desc = Aumenta o disminuye el tamaño de una entidad.
cmd-scale-help = Uso: escala <entityUid> <float>
cmd-dumpentities-desc = Muestra una lista de entidades.
cmd-dumpentities-help = Muestra una lista de entidades con su UID y prototipo.
cmd-getcomponentregistration-desc = Recupera información sobre el registro del componente.
cmd-getcomponentregistration-help = Uso: getcomponentregistration <componentName>
cmd-showrays-desc = Permite la depuración de rayos físicos. Debes especificar un entero para <raylifetime>.
cmd-showrays-help = Uso: rayas <raylifetime>de exhibición
cmd-disconnect-desc = Se desconecta inmediatamente del servidor y vuelve al menú principal.
cmd-disconnect-help = Uso: desconexión
cmd-entfo-desc = Muestra diagnósticos detallados de la entidad.
cmd-entfo-help =
    Uso: entfo <entityuid>
    El UID del objeto puede precederse con 'c' para convertirlo en el UID del objeto cliente.
cmd-fuck-desc = Tira una excepción
cmd-fuck-help = Tira una excepción
cmd-showpos-desc = Permite la depuración de todas las posiciones de las entidades en el juego.
cmd-showpos-help = Uso: showpos
cmd-sggcell-desc = Muestra las entidades en una celda de cuadra.
cmd-sggcell-help = Uso: sggcell <gridID> <vector2i>\nEste parámetro vector2i tiene la forma <int>x,y<int>.
cmd-overrideplayername-desc = Cambia el nombre que se usa cuando intentas conectarte al servidor.
cmd-overrideplayername-help = Uso: overrideplayername <name>
cmd-showanchored-desc = Muestra entidades fijadas en una casilla específica.
cmd-showanchored-help = Uso: showanchored
cmd-dmetamem-desc = Genera los miembros del tipo en un formato apropiado para el archivo de configuración sandbox.
cmd-dmetamem-help = Uso: dmetamem <type>
cmd-launchauth-desc = Descarga tokens de autenticación de los datos del lanzador para ayudar a probar servidores en funciones.
cmd-launchauth-help = Uso: launchauth <nombre de cuenta>
cmd-lightbb-desc = Permite la visualización de las cajas delimitadoras de la luz.
cmd-lightbb-help = Uso: lightbb
cmd-monitorinfo-desc = Monitorización de la información
cmd-monitorinfo-help = Uso: monitorinfo <id>
cmd-setmonitor-desc = Instala el monitor
cmd-setmonitor-help = Uso: monitor <id>de ajuste
cmd-physics-desc = Muestra la superposición física de depuración. El argumento especifica la superposición.
cmd-physics-help = Uso: física <aabbs/com/contactnormals/contactpoints/distance/joints/shapeinfo/shapes>
cmd-hardquit-desc = Cierra inmediatamente el cliente del juego.
cmd-hardquit-help = Cierra inmediatamente el cliente del juego, sin dejar rastro. Sin despedirse del servidor.
cmd-quit-desc = Cierra correctamente el cliente del juego.
cmd-quit-help = Cierra correctamente el cliente del juego, notifica al servidor conectado, etc.
cmd-csi-desc = Abre la consola interactiva de C#.
cmd-csi-help = Uso: csi
cmd-scsi-desc = Abre la consola interactiva de C# en el servidor.
cmd-scsi-help = Uso: scsi
cmd-watch-desc = Se abre la ventana de monitorización variable.
cmd-watch-help = Uso: reloj
cmd-showspritebb-desc = Activa o desactiva la visualización de los bordes de sprites.
cmd-showspritebb-help = Uso: showspritebb
cmd-togglelookup-desc = Muestra/oculta los límites de la búsqueda de entidades a través de la superposición.
cmd-togglelookup-help = Uso: togglelookup
cmd-net_entityreport-desc = Activa o desactiva el panel de Reporte de Entidad de Red.
cmd-net_entityreport-help = Uso: net_entityreport
cmd-net_refresh-desc = Consulta el estado completo del servidor.
cmd-net_refresh-help = Uso: net_refresh
cmd-net_graph-desc = Activa o desactiva el panel de Estadísticas de Red.
cmd-net_graph-help = Uso: net_graph
cmd-net_watchent-desc = Imprime todas las actualizaciones de red del EntityID en la consola.
cmd-net_watchent-help = Uso: net_watchent <0|EntityUid>
cmd-net_draw_interp-desc = Permite o desactiva el mapeo de depuración por interpolación de red.
cmd-net_draw_interp-help = Uso: net_draw_interp <0|EntityUid>
cmd-vram-desc = Muestra estadísticas sobre el uso de memoria de vídeo del juego.
cmd-vram-help = Uso: vram
cmd-showislands-desc = Muestra los cuerpos físicos actuales involucrados en cada isla física.
cmd-showislands-help = Uso: islas de exhibición
cmd-showgridnodes-desc = Muestra los nodos para dividir la malla.
cmd-showgridnodes-help = Uso: showgridnodes
cmd-profsnap-desc = Crea una instantánea de corrección.
cmd-profsnap-help = Uso: profsnap
cmd-devwindow-desc = Ventana de desarrollo
cmd-devwindow-help = Uso: devwindow
cmd-scene-desc = Cambia inmediatamente la escena/estado de la interfaz.
cmd-scene-help = Uso: escena <className>
cmd-szr_stats-desc = Informe estadístico de Serializer.
cmd-szr_stats-help = Uso: szr_stats
cmd-hwid-desc = Devuelve el HWID (ID de hardware) actual.
cmd-hwid-help = Uso: hwid
cmd-vvread-desc = Recupera el valor del camino usando VV (Variables de Vista).
cmd-vvwrite-desc = Cambia el valor del camino usando VV (Ver variables).
cmd-vvwrite-help = Uso: vvwrite <path>
cmd-vvinvoke-desc = Llama/Llama al camino con argumentos usando VV.
cmd-vvinvoke-help = Uso: vvinvoke <path> [argumentos...]
cmd-dump_dependency_injectors-desc = Genera la caché de los inyectores de dependencia de IoCManager.
cmd-dump_dependency_injectors-help = Uso: dump_dependency_injectors
cmd-dump_dependency_injectors-total-count = Total: { $total }
cmd-dump_netserializer_type_map-desc = Imprime el tipo y el hash del serializador NetSerializer.
cmd-dump_netserializer_type_map-help = Uso: dump_netserializer_type_map
cmd-hub_advertise_now-desc = Inmediatamente se anuncia en el servidor principal del hub.
cmd-hub_advertise_now-help = Uso: hub_advertise_now
cmd-echo-desc = Devuelve los argumentos a la consola.
cmd-echo-help = Uso: echo "<message>"
cmd-vfs_ls-desc = Enumera el contenido de un directorio en VFS.
cmd-vfs_ls-help =
    Uso: vfs_list <path>
    Ejemplo:
    vfs_list /Assemblies
cmd-vfs_ls-err-args = Se requiere exactamente un argumento.
cmd-vfs_ls-hint-path = <path>
