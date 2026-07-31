### Localization for role ban command

cmd-roleban-desc = Impide que el usuario interprete roles
cmd-roleban-help = Uso: roleban <nombre o ID de usuario> <job> <reason> [duración en minutos, no especificar o 0 por siempre]

## Completion result hints

cmd-roleban-hint-1 = <name or user ID>
cmd-roleban-hint-2 = <job>
cmd-roleban-hint-3 = <reason>
cmd-roleban-hint-4 = [duración en minutos, no especificado, o 0 para siempre]
cmd-roleban-hint-5 = [severity]
cmd-roleban-hint-duration-1 = Para siempre
cmd-roleban-hint-duration-2 = 1 día
cmd-roleban-hint-duration-3 = 3 días
cmd-roleban-hint-duration-4 = 1 semana
cmd-roleban-hint-duration-5 = 2 semanas
cmd-roleban-hint-duration-6 = 1 mes

### Localization for role unban command

cmd-roleunban-desc = Devuelve al usuario la capacidad de interpretar roles
cmd-roleunban-help = Uso: roleunban <id de rol ban>

## Completion result hints

cmd-roleunban-hint-1 = <role ban id>

### Localization for roleban list command

cmd-rolebanlist-desc = Lista de prohibiciones de roles de jugadores
cmd-rolebanlist-help = Uso: <nombre o ID de usuario> [incluir unbaned]

## Completion result hints

cmd-rolebanlist-hint-1 = <name or user ID>
cmd-rolebanlist-hint-2 = [include unbanned]
cmd-roleban-minutes-parse = { $time } es un número inaceptable de minutos.\n{ $help }
cmd-roleban-severity-parse = { severity } no es un nivel de gravedad aceptable n{ $help }.
cmd-roleban-arg-count = Un número inválido de argumentos.
cmd-roleban-job-parse = No existe { $job } trabajo.
cmd-roleban-name-parse = Es imposible encontrar un jugador con este nombre.
cmd-roleban-existing = { $target } ya tiene una prohibición en el papel de { $role }.
cmd-roleban-success = { $target } tienen prohibido interpretar los papeles de { $role } debido a { $reason } { $length }.
cmd-roleban-inf = Para siempre
cmd-roleban-until = hasta { $expires }
# Department bans
cmd-departmentban-desc = Evita que el usuario desempeñe roles que forman parte del departamento
cmd-departmentban-help = Uso: departmentban <nombre o ID de usuario> <department> <reason> [duración en minutos, no especificar o 0 por siempre]
