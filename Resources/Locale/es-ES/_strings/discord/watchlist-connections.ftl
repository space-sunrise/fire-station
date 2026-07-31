discord-watchlist-connection-header =
    { $players ->
        [one] { $players } un jugador en la lista de seguimiento conectado a
       *[other] { $players } jugadores en la lista de seguimiento se han conectado
    } { $serverName }
discord-watchlist-connection-entry =
    - { $playerName } con el mensaje "{ $message }"{ $expiry ->
        [0] { "" }
       *[other] { " " }(expira <t:{ $expiry }:R>)
    }{ $otherWatchlists ->
        [0] { "" }
        [one] { " " }and { $otherWatchlists } otra lista de vigilancia
       *[other] { " " }and { $otherWatchlists } otras listas de vigilancia
    }
