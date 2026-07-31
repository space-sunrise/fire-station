parse-minutes-fail = No supe descifrar '{ $minutes }' como minutos
parse-session-fail = No se ha encontrado ninguna sesión para '{ $username }'

## Role Timer Commands

# - playtime_addoverall
cmd-playtime_addoverall-desc = Añade el número de minutos especificado al tiempo total de juego del jugador
cmd-playtime_addoverall-help = Uso: { $command } <nombre de usuario> <minutes>
cmd-playtime_addoverall-succeed = El tiempo total de juego { $username } aumentado en { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_addoverall-arg-user = <user name>
cmd-playtime_addoverall-arg-minutes = <minutes>
cmd-playtime_addoverall-error-args = Se esperan exactamente dos argumentos
# - playtime_addrole
cmd-playtime_addrole-desc = Añade un número específico de minutos al tiempo que el jugador tiene en un rol específico
cmd-playtime_addrole-help = Uso: { $command } <nombre de usuario> <role> <minutes>
cmd-playtime_addrole-succeed = El tiempo de juego para el { $username } / \'{ $role }\' se ha incrementado en { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_addrole-arg-user = <user name>
cmd-playtime_addrole-arg-role = <role>
cmd-playtime_addrole-arg-minutes = <minutes>
cmd-playtime_addrole-error-args = Se esperan exactamente tres argumentos
# - playtime_getoverall
cmd-playtime_getoverall-desc = Obtén el tiempo total de juego del jugador en minutos
cmd-playtime_getoverall-help = Uso: { $command } <nombre de usuario>
cmd-playtime_getoverall-success = El tiempo total de juego de { $username } es { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_getoverall-arg-user = <user name>
cmd-playtime_getoverall-error-args = Se espera exactamente un argumento
# - GetRoleTimer
cmd-playtime_getrole-desc = Recibe el temporizador de todos o uno de los roles del jugador
cmd-playtime_getrole-help = Uso: { $command } <nombre de usuario> [rol]
cmd-playtime_getrole-no = No se han encontrado temporizadores de rol
cmd-playtime_getrole-role = Roles: { $role }, tiempo de juego: { $time }
cmd-playtime_getrole-overall = Tiempo total de juego { $time }
cmd-playtime_getrole-succeed = La duración del { $username } es: { TOSTRING($time, "dddd\\:hh\\:mm") }.
cmd-playtime_getrole-arg-user = <user name>
cmd-playtime_getrole-arg-role = <role|'Overall'>
cmd-playtime_getrole-error-args = Se esperan exactamente uno o dos argumentos
# - playtime_save
cmd-playtime_save-desc = Guardar el tiempo de juego del jugador en la base de datos
cmd-playtime_save-help = Uso: { $command } <nombre de usuario>
cmd-playtime_save-succeed = Tiempo { $username } salvado
cmd-playtime_save-arg-user = <user name>
cmd-playtime_save-error-args = Se espera exactamente un argumento

## 'playtime_flush' command'

cmd-playtime_flush-desc = Registra los rastreadores activos en el almacenamiento de tiempo del juego.
cmd-playtime_flush-help =
    Uso: { $command } [nombre de usuario]
    Esto solo provoca escrituras en el almacenamiento de back-end y no escribe inmediatamente en la base de datos.
    Si un usuario es transferido, solo ese usuario será procesado.
cmd-playtime_flush-error-args = Se espera cero o uno de argumentos
cmd-playtime_flush-arg-user = [user name]
