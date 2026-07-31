# ban
cmd-ban-desc = Banea a alguien
cmd-ban-help = Uso: baneo <nombre o ID de usuario> <reason> [duración en minutos, no especificado o 0 para permanente]
cmd-ban-player = No encontré a ningún jugador con ese nombre.
cmd-ban-invalid-minutes = { $minutes } no es un número aceptable de minutos.
cmd-ban-invalid-severity = { $severity } no es un peso aceptable!
cmd-ban-invalid-arguments = Número inválido de argumentos
cmd-ban-hint = <name/user ID>
cmd-ban-hint-reason = <reason>
cmd-ban-hint-severity = [severity]
cmd-ban-hint-duration = [duración]
cmd-ban-hint-duration-1 = Para siempre
cmd-ban-hint-duration-2 = 1 día
cmd-ban-hint-duration-3 = 3 días
cmd-ban-hint-duration-4 = 1 semana
cmd-ban-hint-duration-5 = 2 semanas
# ban panel
cmd-banpanel-desc = Panel de Prohibiciones Abiertas
cmd-banpanel-help = Uso: panel de baneos [nombre del jugador o guía]
cmd-banpanel-server = Esto no puede usarse desde la consola del servidor
cmd-banpanel-player-err = No se puede encontrar al jugador especificado
cmd-ban-hint-duration-6 = 1 mes
# listbans
cmd-banlist-desc = Lista de baneos de usuarios activos.
cmd-banlist-help = Uso: lista de prohibiciones <nombre o ID de usuario>
cmd-banlist-empty = No hay baneos activos para el usuario { $user }
cmd-banlistF-hint = <name/user ID>
cmd-ban_exemption_update-desc = Establece una excepción para los tipos de baneo de jugadores.
cmd-ban_exemption_update-help =
    Uso: ban_exemption_update <player> <flag> [<flag> [...]]
    Especifica múltiples banderas para dar al jugador una exención de varios tipos de ban.
    Para eliminar todas las excepciones, ejecuta este comando y pon la bandera única en "Ninguna".
cmd-ban_exemption_update-nargs = Se esperaban al menos 2 argumentos
cmd-ban_exemption_update-locate = No se puede encontrar el '{ $player }' del jugador.
cmd-ban_exemption_update-invalid-flag = Bandera inválida '{ $flag }'.
cmd-ban_exemption_update-success = Señales actualizadas de excepción de prohibición para '{ $player }' ({ $uid }).
cmd-ban_exemption_update-arg-player = <player>
cmd-ban_exemption_update-arg-flag = <flag>
cmd-ban_exemption_get-desc = Muestra excepciones de baneo para un jugador específico.
cmd-ban_exemption_get-help = Uso: ban_exemption_get <player>
cmd-ban_exemption_get-nargs = Se esperaba exactamente un argumento
cmd-ban_exemption_get-none = El usuario no tiene excepciones a los baneos.
cmd-ban_exemption_get-show = El usuario queda excluido de los baneos con las siguientes señales: { $flags }.
# Ban panel
ban-panel-title = Panel de Prohibición
ban-panel-player = Jugador
ban-panel-ip = IP
ban-panel-hwid = HWID
ban-panel-reason = Causa
ban-panel-last-conn = ¿Usar IP y HWID desde la última conexión?
ban-panel-submit = Prohibición
ban-panel-confirm = ¿Estás seguro?
ban-panel-tabs-basic = Información básica
ban-panel-tabs-reason = Causa
ban-panel-tabs-players = Lista de jugadores
ban-panel-tabs-role = Información sobre los baneos de roles
ban-panel-no-data = Especifica el usuario, IP o HWID a banear
ban-panel-invalid-ip = No se puede analizar la dirección IP. Inténtalo de nuevo
ban-panel-select = Seleccionar tipo
ban-panel-server = Baneo del servidor
ban-panel-role = Prohibición del cargo
ban-panel-minutes = Actas
ban-panel-hours = Horarios
ban-panel-days = Días
ban-panel-weeks = Semanas
ban-panel-months = Meses
ban-panel-years = Años
ban-panel-permanent = Constante
ban-panel-ip-hwid-tooltip = Deja la opción en blanco y marca la casilla de abajo para usar los datos de la última conexión
ban-panel-severity = Gravedad:
# Ban string
server-ban-string = { $admin } creado una prohibición en un servidor con un nivel de gravedad de { $severity } que expira { $expires } por [{ $name }, { $ip }, { $hwid }] con la razón: { $reason }, ronda: { $round }
ban-panel-erase = Borrar mensajes de chat y jugador de la ronda
server-ban-string-never = Nunca
server-ban-string-no-pii = { $admin } ha establecido un baneo de servidor { $severity } gravedad que expirará { $expires } { $name } con la razón: { $reason }, ronda: { $round }
server-ban-unknown-round = No se conoce
cmd-ban_exemption_get-arg-player = <player>
# Antag Bans
ban-panel-role-selection-antag = Antagonista
ban-panel-role-selection-antag-all-option = Todos
# Kick on ban
ban-kick-reason = You have been banned
