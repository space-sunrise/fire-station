### UI

# Shown when a stack is examined in details range
comp-stack-examine-detail-count =
    En la pila [color={ $markupCountColor }]{ $count }[/color] { $count ->
        [one] Punto
        [few] Tema
       *[other] Elementos
    }.
# Stack status control
comp-stack-status = Cantidad: [color=white]{ $count }[/color]

### Interaction Messages

# Shown when attempting to add to a stack that is full
comp-stack-already-full = La pila ya está llena.
# Shown when a stack becomes full
comp-stack-becomes-full = La pila ya está llena.
# Text related to splitting a stack
comp-stack-split = Has partido un montón.
comp-stack-split-halve = Divide en dos
comp-stack-split-too-small = La pila es demasiado pequeña para dividirla.
