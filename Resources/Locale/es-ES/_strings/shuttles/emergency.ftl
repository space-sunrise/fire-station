# Commands


## Delay shuttle round end

emergency-shuttle-command-round-desc = Detiene el temporizador final de la ronda cuando la lanzadera de escape sale del hiperespacio.
emergency-shuttle-command-round-yes = La ronda se ha extendido.
emergency-shuttle-command-round-no = No es posible alargar el final de la ronda.

## Dock emergency shuttle

emergency-shuttle-command-dock-desc = Invoca una lanzadera de escape y la aterriza cerca de la instalación... si es posible.

## Launch emergency shuttle

emergency-shuttle-command-launch-desc = Arranca el transporte lanzadera antes de lo previsto, si es posible.
# Emergency shuttle
emergency-shuttle-left = El transbordador de evacuación salió del complejo. El tiempo estimado de vuelo del transbordador es de { $transitTime } segundos.
emergency-shuttle-launch-time = El transbordador de evacuación despega en { $consoleAccumulator } segundos.
emergency-shuttle-docked = La lanzadera de evacuación aterrizó { $location }. Volará en { $time } segundos.
emergency-shuttle-good-luck = La lanzadera de evacuación no encuentra el lugar de aterrizaje más cercano. Buena suerte.
emergency-shuttle-nearby = La lanzadera de evacuación no encuentra un lugar adecuado para aterrizar, así que aterriza cerca del complejo. Dirección: { $direction }.
emergency-shuttle-extended = El tiempo de lanzamiento del { " " } se alargó debido a circunstancias incómodas.
# Emergency shuttle console popup / announcement
emergency-shuttle-console-no-early-launches = Inicio temprano incapacitado
# Emergency shuttle console popup / announcement
emergency-shuttle-console-auth-left =
    { $remaining } { $remaining ->
        [one] Permanecen las autorizaciones
        [few] Las autorizaciones permanecen
       *[other] Las autorizaciones permanecen
    } para el lanzamiento anticipado del transbordador.
emergency-shuttle-console-auth-revoked =
    Se han revocado las autorizaciones para el lanzamiento anticipado del transbordador, { $remaining } { $remaining ->
        [one] Se requiere autorización
        [few] Se requieren autorizaciones
       *[other] Se requieren autorizaciones
    }.
emergency-shuttle-console-denied = Acceso denegado
# UI
emergency-shuttle-console-window-title = Consola del Transbordador de Transporte
# UI
emergency-shuttle-ui-engines = MOTORES:
emergency-shuttle-ui-idle = Sencillo
emergency-shuttle-ui-repeal-all = Repite todo
emergency-shuttle-ui-early-authorize = Autorización de lanzamiento anticipado
emergency-shuttle-ui-authorize = INICIAR SESIÓN
emergency-shuttle-ui-repeal = Repito
emergency-shuttle-ui-authorizations = Autorizaciones
emergency-shuttle-ui-remaining = Izquierda: { $remaining }
# Map Misc.
map-name-centcomm = Sede O4
map-name-terminal = Terminal de llegadas
