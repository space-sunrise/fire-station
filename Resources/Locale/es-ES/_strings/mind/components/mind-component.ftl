# MindComponent localization

comp-mind-ghosting-prevented = No puedes convertirte en fantasma ahora mismo.

## Messages displayed when a body is examined and in a certain state

comp-mind-examined-catatonic = { CAPITALIZE(SUBJECT($ent)) } en un estupor catatónico. El estrés de vivir en el espacio profundo debía de ser demasiado para { OBJECT($ent) }. La recuperación es poco probable.
comp-mind-examined-dead =
    { CAPITALIZE(SUBJECT($ent)) } { GENDER($ent) ->
        [male] Fallecido
        [female] Fallecido
        [epicene] Muerto
       *[neuter] Muerto
    }
comp-mind-examined-ssd = { CAPITALIZE(SUBJECT($ent)) } mira distraídamente al vacío y no reacciona a nada. { CAPITALIZE(SUBJECT($ent)) } pronto puede que vuelva en sí.
comp-mind-examined-dead-and-ssd = { CAPITALIZE(POSS-ADJ($ent)) } alma está inactiva y puede que pronto regrese.
comp-mind-examined-dead-and-irrecoverable = { CAPITALIZE(POSS-ADJ($ent)) } alma salió del cuerpo y desapareció. La recuperación es poco probable.
