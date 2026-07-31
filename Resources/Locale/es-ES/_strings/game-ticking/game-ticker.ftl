game-ticker-restart-round = Reiniciando la ronda...
game-ticker-start-round = Comienza la ronda...
game-ticker-start-round-cannot-start-game-mode-fallback = ¡No se ha conseguido iniciar { $failedGameMode } modo! Arranca { $fallbackMode }...
game-ticker-start-round-cannot-start-game-mode-restart = ¡No se ha podido iniciar el modo { $failedGameMode }! Reiniciar la ronda...
game-ticker-start-round-invalid-map = El mapa seleccionado { $map } no es adecuado para el modo de juego { $mode }. El modo de juego puede no funcionar como se espera...
game-ticker-unknown-role = No se conoce
game-ticker-delay-start = El inicio de la ronda se retrasó { $seconds } segundos.
game-ticker-pause-start = El inicio de la ronda fue suspendido.
game-ticker-pause-start-resumed = Se ha reanudado la cuenta atrás para el inicio de la ronda.
game-ticker-player-join-game-message = ¡Bienvenido a la Fundación SCP desde Project Fire Station! Si juegas por primera vez, asegúrate de pulsar ESC en el teclado y leer las reglas del juego, y no dudes en pedir ayuda a la Ayuda Administrativa.
game-ticker-get-info-text =
    Ronda actual: [color=white]#{ $roundId }[/color]
    Jugadores actuales: [color=white]{ $playerCount }[/color]
    Mapa actual: [color=white]{ $mapName }[/color]
    Modo de juego actual: [color=white]{ $gmTitle }[/color]
    >[color=yellow]{ $desc }[/color]
game-ticker-get-info-preround-text =
    Ronda actual: [color=white]#{ $roundId }[/color]
    Jugadores actuales: [color=white]{ $playerCount }[/color] ([color=white]{ $readyCount }[/color] { $readyCount ->
        [one] Listo
       *[other] Listo
    })
    Mapa actual: [color=white]{ $mapName }[/color]
    Modo de juego actual: [color=white]{ $gmTitle }[/color]
    >[color=yellow]{ $desc }[/color]
game-ticker-no-map-selected = ¡[color=red]The mapa aún no ha sido seleccionado[/color]
game-ticker-player-no-jobs-available-when-joining = Al intentar unirme al juego, no había roles disponibles.
# Displayed in chat to admins when a player joins
player-join-message = ¡El jugador { $name } dentro!
player-first-join-message = El jugador { $name } conectado al servidor por primera vez.
# Displayed in chat to admins when a player leaves
player-leave-message = ¡Jugador { $name } fuera!
latejoin-arrival-announcement =
    { $character } ({ $job }) { $gender ->
        [male] Llegó
        [female] Llegó
        [epicene] Beneficio
       *[neuter] Llegó
    } al Complejo!
latejoin-arrival-announcement-special = { $job } { $character } en el complejo!
latejoin-arrival-sender = Complejo
latejoin-arrivals-direction = Pronto llegará un autobús lanzadera para llevarte a la estación.
latejoin-arrivals-direction-time = El transbordador que te llevará a la estación llegará en { $time }.
latejoin-arrivals-dumped-from-shuttle = Una fuerza misteriosa te impide salir en la lanzadera de llegada.
latejoin-arrivals-teleport-to-spawn = Una fuerza misteriosa te teletransportará desde la lanzadera de llegada. ¡Feliz cambio!
preset-not-enough-ready-players = No se ha iniciado el preajuste de { $presetName }. Requiere { $minimumPlayers } jugadores, pero solo { $readyPlayersCount } están listos.
preset-not-enough-ready-command-staff = No se ha iniciado { $presetName } preset. Requiere { $minimumCommandStaff } miembros del equipo, pero solo se puede { $readyCommandStaffCount }.
preset-no-one-ready = No se ha podido iniciar { $presetName } modo. No hay jugadores listos.
game-run-level-PreRoundLobby = Lobby antes del inicio de la ronda
game-run-level-InRound = En la ronda
game-run-level-PostRound = Después de la ronda
