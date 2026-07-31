# Loc strings for various entity state & client-side PVS related commands

cmd-reset-ent-help = Uso: reseteador <UID de entidad>
cmd-reset-ent-desc = Reinicia la entidad al último estado que recibió del servidor. Esto también reiniciará las entidades que fueron eliminadas a espacio nulo.
cmd-reset-all-ents-help = Uso: resetallents
cmd-reset-all-ents-desc = Reinicia todas las entidades al último estado recibido del servidor. Esto solo afecta a las entidades que no han sido eliminadas en el espacio nulo.
cmd-detach-ent-help = Uso: desprendido <UID de entidad>
cmd-detach-ent-desc = Elimina la entidad en el espacio nulo como si hubiera salido del rango PVS.
cmd-local-delete-help = Uso: localdelete <UID de entidad>
cmd-local-delete-desc = Elimina la entidad. A diferencia del comando de eliminación normal, este comando es del lado del cliente (LADO CLIENTE). Si la entidad no está del lado del cliente, es probable que provoque errores.
cmd-full-state-reset-help = Uso: fullstatereset
cmd-full-state-reset-desc = Reinicia toda la información sobre el estado de la entidad y consulta al servidor para obtener el estado completo.
