# Blood Cult Console Commands

# Add Target Command
bloodcult-addtarget-description = Añadir un objetivo de culto de sangre
bloodcult-addtarget-help = Añade un jugador específico como objetivo del Culto Sangriento para rastrear y posibles sacrificios.
bloodcult-addtarget-usage = Uso: bloodcult_addtarget <ckey>
bloodcult-addtarget-player-not-found = Jugador con ckey '{ $ckey }' no encontrado o no está en el juego.
bloodcult-addtarget-system-not-found = No se ha encontrado el sistema del Culto de la Sangre.
bloodcult-addtarget-rule-not-found = No se encontró ninguna regla activa del Culto Sangriento.
bloodcult-addtarget-already-target = La esencia ya es el objetivo del culto.
bloodcult-addtarget-success = El objetivo del culto { $name } añadido con éxito.

# Remove Target Command
bloodcult-removetarget-description = Eliminar objetivo del Culto Sangriento
bloodcult-removetarget-help = Elimina a un jugador específico de la lista de objetivos del Culto Sangriento, poniendo fin al seguimiento y la marca para el sacrificio.
bloodcult-removetarget-usage = Uso: bloodcult_removetarget <ckey>
bloodcult-removetarget-player-not-found = Jugador con ckey '{ $ckey }' no encontrado o no está en el juego.
bloodcult-removetarget-system-not-found = No se ha encontrado el sistema del Culto de la Sangre.
bloodcult-removetarget-rule-not-found = No se encontró ninguna regla activa del Culto Sangriento.
bloodcult-removetarget-not-target = La esencia no es el objetivo del culto.
bloodcult-removetarget-success = El objetivo del culto { $name } eliminado con éxito.

# List Targets Command
bloodcult-listtargets-description = Mostrar todos los objetivos actuales del Culto de la Sangre
bloodcult-listtargets-help = Muestra todos los objetivos actuales del Culto de la Sangre con su estado (vivos o sacrificados) e información de entidades.
bloodcult-listtargets-usage = Uso: bloodcult_listtargets
bloodcult-listtargets-system-not-found = No se ha encontrado el sistema del Culto de la Sangre.
bloodcult-listtargets-no-targets = No se han encontrado los objetivos de la secta.
bloodcult-listtargets-header = { $count ->
    [1] Objetivo actual del culto ({ $count }):
    [few] Objetivos actuales del culto ({ $count }):
    *[other] Objetivos actuales del culto ({ $count }):
}
bloodcult-listtargets-sacrificed = Sacrificados
bloodcult-listtargets-alive = Viva
bloodcult-listtargets-target = { $name } ({ $uid }) - { $status }
bloodcult-unknown-entity = Entidad desconocida

# Cult Device Alert
bloodcult-biocode-alert = El dispositivo pulsa con energía oscura, rechazando tu contacto. Solo quienes están atados por sangre pueden ejercer su poder.
