### for technical and/or system messages


## General

shell-command-success = La orden se ejecuta.
shell-invalid-command = Orden equivocada.
shell-invalid-command-specific = Orden equivocado { $commandName }.
shell-cannot-run-command-from-server = No puedes ejecutar este comando desde el servidor.
shell-only-players-can-run-this-command = Solo los jugadores pueden ejecutar esta orden.
shell-must-be-attached-to-entity = Para ejecutar esta orden, debes estar conectado a una entidad.

## Arguments

shell-need-exactly-one-argument = Se necesita exactamente un argumento.
shell-wrong-arguments-number-need-specific =
    Tienes que { $properAmount } { $properAmount ->
        [one] Argumento
        [few] Argumento
       *[other] Argumentos
    }, fue { $currentAmount } { $currentAmount ->
        [one] Argumento
        [few] Argumento
       *[other] Argumentos
    }.
shell-argument-must-be-number = El argumento debe ser un número.
shell-argument-must-be-boolean = El argumento debe ser booleano.
shell-wrong-arguments-number = Número incorrecto de argumentos.
shell-need-between-arguments = ¡Necesitas de { $lower } a { $upper } argumentos!
shell-need-minimum-arguments = ¡Necesitas al menos { $minimum } argumentos!
shell-need-minimum-one-argument = ¡Al menos hace falta un argumento!
shell-argument-uid = EntityUid

## Guards

shell-entity-is-not-mob = ¡La entidad objetivo no es una multitud!
shell-invalid-entity-id = ID de entidad inválido.
shell-invalid-grid-id = ID de malla inválido.
shell-invalid-map-id = Identificación de tarjeta inválida.
shell-invalid-entity-uid = { $uid } no es un UID válido.
shell-invalid-bool = Booleano incorrecto.
shell-entity-uid-must-be-number = EntityUid debe ser un número.
shell-could-not-find-entity = No se pudo encontrar la esencia de la { $entity }.
shell-could-not-find-entity-with-uid = No he encontrado ninguna entidad con { $uid } UUD.
shell-entity-with-uid-lacks-component = Una entidad con un { $uid } uid no tiene un componente { $componentName }.
shell-invalid-color-hex = ¡Color HEX inválido!
shell-target-player-does-not-exist = ¡El jugador objetivo no existe!
shell-target-entity-does-not-have-message = ¡La entidad objetivo no tiene { $missing }!
shell-timespan-minutes-must-be-correct = { $span } no es un lapso de tiempo válido en minutos.
shell-argument-must-be-prototype = El argumento { $index } debería ser { prototypeName } dólares!
shell-argument-number-must-be-between = ¡El argumento { $index } debe ser un número entre { $lower } y { $upper }!
shell-argument-station-id-invalid = El argumento { $index } debe ser un ID de estación válido.
shell-argument-map-id-invalid = El argumento { $index } debe ser un ID de mapa válido.
shell-argument-number-invalid = ¡El argumento { $index } debe ser un número válido!
# Hints
shell-argument-username-hint = <username>
shell-argument-username-optional-hint = [username]
