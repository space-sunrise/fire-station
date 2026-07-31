## Cuerdas para el comando grant_connect_bypass.

cmd-grant_connect_bypass-desc = Permite temporalmente al usuario saltarse las comprobaciones normales de conexión.
cmd-grant_connect_bypass-help =
    Uso: grant_connect_bypass <usuario> [duración en minutos]
    Proporciona temporalmente al usuario la capacidad de saltarse las restricciones normales de conexión.
    El desplazamiento solo se aplica a este servidor de juego y expira tras (por defecto) 1 hora.
    El usuario podrá conectarse, independientemente de la lista blanca, el pánico o el límite de jugadores.
cmd-grant_connect_bypass-arg-user = <usuario>
cmd-grant_connect_bypass-arg-duration = [duración en minutos]
cmd-grant_connect_bypass-invalid-args = Se esperaban 1 o 2 argumentos
cmd-grant_connect_bypass-unknown-user = No pude encontrar el usuario '{ $user }'
cmd-grant_connect_bypass-invalid-duration = Duración incorrecta de '{ $duration }'
cmd-grant_connect_bypass-success = Aprobado con éxito el permiso de rastreo para el usuario '{ $user }'
