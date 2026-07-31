## Rev Head

roles-antag-rev-head-name = El Jefe de la Revolución
roles-antag-rev-head-objective = Tu tarea es capturar la estación persuadiendo a los miembros de la tripulación para que se unan a tu lado y destruyendo todo el personal de mando de la estación.
head-rev-role-greeting =
    Eres el jefe de la revolución.
    Tienes la tarea de eliminar a todo el personal de mando de la estación mediante asesinato, destierro o arresto.
    El sindicato te ha patrocinado con un flash especial que convierte a los miembros de la tripulación a tu bando.
    Ten cuidado, no funcionará con el personal de seguridad, los miembros del mando y quienes llevan gafas de sol.
    Viva la revolución!
head-rev-briefing =
    Usa flashes para convertir a los miembros de la tripulación a tu bando.
    Elimina todos los capítulos para apoderarse de la estación.
head-rev-break-mindshield = ¡El escudo mental ha sido destruido!

## Rev

roles-antag-rev-name = Revolucionario
roles-antag-rev-objective = Tu tarea es proteger y cumplir las órdenes de los líderes de la revolución, así como deshacerte de todo el personal de mando de la estación.
rev-break-control =
    { $name } { GENDER($name) ->
        [male] recordado a quién fue fiel
        [female] recordó a quién fue fiel
        [epicene] recordados a quién regresaron
       *[neuter] recordado a quién era fiel
    } ¡De verdad!
rev-role-greeting =
    Eres un revolucionario.
    Tienes la tarea de capturar la estación y proteger a los líderes de la revolución.
    Deshacerse de todo el personal de mando de la estación.
    Viva la revolución!
rev-briefing = Ayuda a los líderes de la revolución a deshacerse del mando de la estación para capturarla.
rev-banned = You have been converted but are unable to play due to a ban for this role.

## General

rev-title = Revolucionarios
rev-description = Los revolucionarios están entre nosotros.
rev-not-enough-ready-players = ¡No hay suficientes jugadores listos para jugar! { $readyPlayersCount } jugadores de la { $minimumPlayers } requerida están listos. No se puede ejecutar el preajuste de Revolucionarios.
rev-no-one-ready = ¡No hay jugadores listos! No puedes ejecutar el preset de Revolucionarios.
rev-no-heads = No hay candidatos para el puesto de jefe de la revolución. No puedes ejecutar el preajuste de los Revolucionarios.
rev-won = Los líderes de la revolución sobrevivieron y destruyeron todo el personal de mando de la estación.
rev-headrev-count =
    { $initialCount ->
        [one] Solo hubo un líder de la revolución:
       *[other] Hubo { $initialCount } cabezas de la revolución:
    }
rev-lost = Miembros del personal de mando de la estación sobrevivieron y destruyeron a todos los líderes de la revolución.
rev-stalemate = Los jefes de la revolución y el estado mayor de la estación fueron asesinados. Esto es un empate.
rev-headrev-name-user = [color=#5e9cff]{ $name }[/color] ([color=gray]{ $username }[/color]) convertido { $count } { $count ->
        [one] Miembro
        [few] Miembro
       *[other] Miembros
    } Tripulación
rev-headrev-name = [color=#5e9cff]{ $name }[/color] convertido { $count } { $count ->
        [one] Miembro
        [few] Miembro
       *[other] Miembros
    } Tripulación
rev-reverse-stalemate = Los líderes de la revolución y el estado mayor de mando de la estación sobrevivieron.
rev-deconverted-title = ¡Convertido!
rev-deconverted-text =
    Con la muerte del último jefe de la revolución, la revolución termina.
    
    Ya no eres un revolucionario, así que compórtate.
rev-deconverted-confirm = Confirmar
