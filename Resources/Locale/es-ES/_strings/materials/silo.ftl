ore-silo-ui-title = Almacenamiento de materiales
ore-silo-ui-label-clients = Máquinas
ore-silo-ui-label-mats = Materiales
ore-silo-ui-itemlist-entry =
    { $linked ->
        [true] { "[Conectado]" }
       *[False] { "" }
    } { $name } ({ $beacon }) { $inRange ->
        [true] { "" }
       *[false] (Fuera de alcance)
    }
