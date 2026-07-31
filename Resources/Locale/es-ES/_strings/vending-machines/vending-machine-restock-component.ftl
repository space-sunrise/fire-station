vending-machine-restock-invalid-inventory = { CAPITALIZE($this) } no es adecuado para reponer { $target }.
vending-machine-restock-needs-panel-open = El panel técnico { CAPITALIZE($target) } debe estar abierto.
vending-machine-restock-start = { $user } empieza a reponer { $target }.
vending-machine-restock-done =
    { $user } { GENDER($user) ->
        [male] Graduado
        [female] Graduado
        [epicene] Terminado
       *[neuter] Terminado
    } reponer { $target }.
