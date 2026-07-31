# Shown when greeted with the Suspicion role
suspicion-role-greeting = ¡Te { $roleName }!
# Shown when greeted with the Suspicion role
suspicion-objective = Gol: { $objectiveText }
# Shown when greeted with the Suspicion role
suspicion-partners-in-crime =
    { $partnersCount ->
        [zero] Estás solo. ¡Suerte!
        [one] Tu aliado: { $partnerNames }.
       *[other] Tus aliados: { $partnerNames }.
    }
