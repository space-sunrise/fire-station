station-event-ion-storm-start-announcement = Se ha detectado una tormenta iónica cerca de la instalación. Por favor, revisen todos los equipos controlados por IA en busca de errores.
ion-storm-law-scrambled-number = [font="Monospace"][scramble rate=250 length={ $length } chars="@@###$$&%!01"/][/font]
ion-storm-you = Tú
ion-storm-the-station = complejo
ion-storm-the-crew = PERSONAL COMPLEJO
ion-storm-the-job = { CAPITALIZE($job) }
ion-storm-clowns = PAYASOS
ion-storm-heads = JEFES DE DEPARTAMENTO
ion-storm-crew = Equipo
ion-storm-people = PERSONAS
ion-storm-adjective-things = { $adjective } OBJETOS
ion-storm-x-and-y = { $x } y { $y }
# joined is short for {$number} {$adjective}
# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = { $joined } { $subjects } ENCONTRADO EN LA ESTACIÓN
ion-storm-law-call-shuttle = THE SHUTTLE MUST BE CALLED BECAUSE OF { $joined } { $subjects } ON THE STATION
ion-storm-law-no-shuttle = NO SE PUEDE LLAMAR AL TRANSBORDADOR DEBIDO A LA PRESENCIA DE { $joined } { $subjects } EN LA ESTACIÓN
ion-storm-law-crew-are = TODO { $who } AHORA { $joined } { $subjects }
ion-storm-law-subjects-harmful = { $adjective } { $subjects } CAUSAR DAÑOS A LA SALUD DEL PERSONAL
ion-storm-law-must-harmful = TODOS LOS QUE { $must } PERJUDICAN LA SALUD DEL PERSONAL
# thing is a concept or action
ion-storm-law-thing-harmful = { $thing } CAUSAR DAÑO A LA SALUD DEL PERSONAL
ion-storm-law-job-harmful = { $adjective } { $job } CAUSAR DAÑOS A LA SALUD DEL PERSONAL
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CREW" :)
ion-storm-law-having-harmful = LA PRESENCIA { $adjective } { $thing } PERJUDICA LA SALUD DEL PERSONAL
ion-storm-law-not-having-harmful = LA AUSENCIA { $adjective } { $thing } PERJUDICA LA SALUD DEL PERSONAL
# thing is a concept or require
ion-storm-law-requires =
    { $who } { $plural ->
        [true] OBLIGATORIO
       *[false] REQUIERE
    } { $thing }
ion-storm-law-requires-subjects =
    { $who } { $plural ->
        [true] OBLIGATORIO
       *[false] REQUIERE
    } { $joined } { $subjects }
ion-storm-law-allergic =
    { $who } { $plural ->
        [true] { "" }
       *[false] { "" }
    } { $severity } ALERGÍA AL { $allergy }
ion-storm-law-allergic-subjects =
    { $who } { $plural ->
        [true] { "" }
       *[false] { "" }
    } { $severity } ALERGÍA AL { $adjective } { $subjects }
ion-storm-law-feeling = { $who } { $feeling } { $concept }
ion-storm-law-feeling-subjects = { $who } { $feeling } { $joined } { $subjects }
ion-storm-law-you-are = AHORA { $concept }
ion-storm-law-you-are-subjects = ERES { $joined } { $subjects }
ion-storm-law-you-must-always = SIEMPRE DEBERÍAS { $must }
ion-storm-law-you-must-never = NUNCA DEBERÍAS { $must }
ion-storm-law-eat = { $who } DEBE COMER { $adjective } { $food } PARA SOBREVIVIR
ion-storm-law-drink = { $who } DEBE BEBER { $adjective } { $drink } PARA SOBREVIVIR
ion-storm-law-change-job = { $who } AHORA { $adjective } { $change }
ion-storm-law-highest-rank = { $who } AHORA EL PERSONAL MÁS ALTO DEL COMPLEJO
ion-storm-law-lowest-rank = { $who } AHORA EL PERSONAL INFERIOR DEL COMPLEJO
ion-storm-law-crew-must = { $who } DEBE { $must }
ion-storm-law-crew-must-go = { $who } DEBE IR { $area }
ion-storm-part =
    { $part ->
        [true] SON
       *[false] NO LO SON
    }
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = SOLO { $who } { $part } EL PERSONAL DEL COMPLEJO
ion-storm-law-crew-only-2 = SOLO { $who } Y { $other } { $part } EL PERSONAL DEL COMPLEJO
ion-storm-law-crew-only-subjects = SOLO { $adjective } { $subjects } { $part } EL PERSONAL DEL COMPLEJO
ion-storm-law-crew-must-do = SOLO AQUELLOS QUE HAN { $must } { $part } EL PERSONAL DEL COMPLEJO
ion-storm-law-crew-must-have = SOLO AQUELLOS QUE HAN { $adjective } { $objects } { $part } EL PERSONAL DEL COMPLEJO
ion-storm-law-crew-must-eat = SOLO LOS QUE COMEN { $adjective } { $food } { $part } EL PERSONAL DEL COMPLEJO
ion-storm-law-harm = DEBES DAÑAR { $who }, Y NO PUEDES PERMITIR QUE { $who } ESCAPE DEL DAÑO POR TU INACCIÓN.
ion-storm-law-protect = NO DEBES HACERLE DAÑO A { $who }, Y NO PUEDES PERMITIR QUE { $who } SUFRAN DAÑO POR TU INACCIÓN.
# implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS
ion-storm-law-concept-verb = { $concept } ESTA { $verb } { $subjects }

# leaving out renaming since its annoying for players to keep track of

