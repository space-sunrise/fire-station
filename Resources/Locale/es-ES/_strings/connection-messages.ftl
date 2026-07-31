whitelist-not-whitelisted = No estás en la lista blanca.
# proper handling for having a min/max or not
whitelist-playercount-invalid =
    { $min ->
        [0] La lista blanca de este servidor solo se aplica al número de jugadores por debajo de { $max }.
       *[other]
            La lista blanca de este servidor solo se aplica al número de jugadores por encima de { $min } { $max ->
                [2147483647] -> así que quizá puedas unirte más tarde.
               *[other] -> y menos de { $max } jugadores, así que quizá puedas unirte más tarde.
            }
    }
whitelist-not-whitelisted-rp = No estás en la lista blanca. Para ponerte en lista blanca, visita nuestro Discord (el enlace se puede encontrar en https://discord.station14.ru).
cmd-whitelistadd-desc = Añade un jugador a la lista blanca del servidor.
cmd-whitelistadd-help = Uso: whitelistadd <username>
cmd-whitelistadd-existing = { $username } ya está en la lista blanca!
cmd-whitelistadd-added = { $username } añadido a la lista blanca
cmd-whitelistadd-not-found = No se puede encontrar el jugador '{ $username }'
cmd-whitelistadd-arg-player = [player]
cmd-whitelistremove-desc = Elimina al jugador de la lista blanca del servidor.
cmd-whitelistremove-help = Uso: whitelistremove <username>
cmd-whitelistremove-existing = { $username } no está en la lista blanca.
cmd-whitelistremove-removed = { $username } eliminado de la lista blanca
cmd-whitelistremove-not-found = No se puede encontrar el jugador '{ $username }'
cmd-whitelistremove-arg-player = [player]
cmd-kicknonwhitelisted-desc = Expulsa a todos los jugadores que no estén en la lista blanca del servidor.
cmd-kicknonwhitelisted-help = Uso: kicknonwhitelisted
ban-banned-permanent = Esta prohibición solo puede ser apelada.
ban-banned-permanent-appeal = Esta prohibición solo puede ser apelada. Para ello, visita { $link }.
ban-expires = Has sido expulsado por { $duration } minutos, y expirará { $time } UTC (para la hora de Moscú, añadir 3 horas).
ban-banned-1 = Tú, ni ningún otro usuario de ese ordenador o conexión, no podéis jugar aquí.
ban-banned-2 = ID de ban: { $id }
ban-banned-3 = Motivo de la prohibición: "{ $reason }"
ban-banned-4 = Se registrarán los intentos de eludir esta prohibición, por ejemplo, creando una nueva cuenta.
soft-player-cap-full = ¡El servidor está lleno!
whitelist-playtime = No tienes suficiente tiempo de juego para iniciar sesión en este servidor. Necesitas al menos { $minutes } minutos de juego para unirte a este servidor.
whitelist-player-count = El servidor no acepta nuevos jugadores en este momento. Por favor, inténtalo de nuevo más tarde.
whitelist-notes = Tienes demasiadas notas de administrador para unirte a este servidor. Puedes consultar tus notas escribiendo /adminremarks en el chat.
whitelist-manual = No estás en la lista de permitidos en este servidor.
whitelist-blacklisted = Estás en la lista negra de este servidor.
whitelist-always-deny = No se te permite unirte a este servidor.
whitelist-fail-prefix = No está en la lista de permitidos: { $msg }
cmd-blacklistadd-desc = Añade un jugador con el nombre de usuario especificado a la lista negra del servidor.
cmd-blacklistadd-help = Uso: blacklistadd <username>
cmd-blacklistadd-existing = ¡{ $username } ya en la lista negra!
cmd-blacklistadd-added = { $username } añadido a la lista negra
cmd-blacklistadd-not-found = No pude encontrar '{ $username }'
cmd-blacklistadd-arg-player = [jugador]
cmd-blacklistremove-desc = Elimina al jugador con el nombre de usuario especificado de la lista negra del servidor.
cmd-blacklistremove-help = Uso: blacklistremove <username>
cmd-blacklistremove-existing = { $username } no está en la lista negra!
cmd-blacklistremove-removed = { $username } eliminado de la lista negra
cmd-blacklistremove-not-found = No pude encontrar '{ $username }'
cmd-blacklistremove-arg-player = [jugador]
panic-bunker-account-denied = Este servidor está en modo Búnker, a menudo usado como precaución contra incursiones. Las nuevas conexiones de cuentas que no cumplan ciertos requisitos serán temporalmente excluidas. Por favor, inténtalo de nuevo más tarde
panic-bunker-account-denied-reason = Este servidor está en modo "Búnker", usado a menudo como precaución contra raids. Nuevas conexiones de cuentas que no cumplen ciertos requisitos no son aceptadas temporalmente. Por favor, inténtalo de nuevo más adelante Motivo: "{ $reason }"
panic-bunker-account-reason-account = Tu cuenta de Space Station 14 es demasiado nueva. Debe de tener más de { $minutes } minutos
panic-bunker-account-reason-overall =
    El tiempo mínimo que has jugado en el servidor es { $minutes } { $minutes ->
        [one] minuto
        [few] Actas
       *[other] Actas
    }.
baby-jail-account-denied = Este servidor está diseñado para principiantes y para quienes quieran ayudarles. No se aceptarán nuevas conexiones de cuentas demasiado antiguas o que no estén en la lista blanca. Prueba otros servidores y mira qué más ofrece Space Station 14. ¡Suerte!
baby-jail-account-denied-reason = Este servidor está diseñado para principiantes y para quienes quieren ayudarles. No se aceptarán nuevas conexiones de cuentas demasiado antiguas o que no estén en la whitelist. Prueba con otros servidores y mira qué más tiene para ofrecer Space Station 14. ¡Buena suerte! Motivo: "{ $reason }"
baby-jail-account-reason-account = Tu cuenta de Space Station 14 es demasiado antigua. Debe ser menos de { $minutes } minutos.
baby-jail-account-reason-overall = El tiempo total de juego en el servidor debe ser inferior a { $minutes } minutos.
hwid-required = Su cliente se negó a enviar el ID del equipo. Por favor, contacte con el equipo administrativo para más ayuda.
generic-misconfigured = El servidor no está configurado correctamente y no acepta jugadores. Por favor, contacta con el propietario del servidor e inténtalo de nuevo más adelante.
ipintel-server-ratelimited = Este servidor utiliza un sistema de seguridad con verificación externa, se ha alcanzado el límite máximo de comprobaciones. Contacta con la administración del servidor para recibir ayuda y repite más tarde.
ipintel-unknown = Este servidor utiliza seguridad validada externamente, pero ha ocurrido un error. Contacta con la administración del servidor e inténtalo de nuevo más tarde.
ipintel-suspicious = Se ha detectado una conexión a través de un centro de datos o VPN. Según las normas del servidor, las conexiones VPN están prohibidas. Contacte con la administración si esto es un error.
