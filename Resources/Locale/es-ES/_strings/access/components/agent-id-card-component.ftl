agent-id-no-new = { CAPITALIZE($card) } no dieron nuevo acceso.
agent-id-new-1 = { CAPITALIZE($card) } le dio acceso a uno nuevo.
agent-id-new =
    { CAPITALIZE($card) } dado { $number } { $number ->
        [one] Nuevo Acceso
        [few] Nuevos accesos
       *[other] Nuevos accesos
    }.
agent-id-card-current-name = Nombre:
agent-id-card-current-job = Posición:
agent-id-card-job-icon-label = Icono:
agent-id-menu-title = Tarjeta de identificación del agente
