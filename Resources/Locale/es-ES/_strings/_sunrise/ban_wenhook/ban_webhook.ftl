server-ban-string-infinity = Para siempre
server-ban-no-name = No encontrado. ({ $hwid })
server-time-ban =
    Prohibición temporal de { $mins } { $mins ->
        [one] minuto
        [few] Actas
       *[other] Actas
    }.
server-perma-ban = Baneo permanente.
server-role-ban =
    Prohibición temporal de trabajo para { $mins } { $mins ->
        [one] minuto
        [few] Actas
       *[other] Actas
    }.
server-perma-role-ban = Prohibición permanente de empleo.
server-time-ban-string =
    > **Administrador**
    > **Inicio de sesión:** ''{ $adminName }''
    > **Discord:** { $adminLink }
    
    > **Intruso**
    > **Inicio de sesión:** ''{ $targetName }''
    > **Discord:** { $targetLink }
    
    > **Emitido:** { $TimeNow }
    > **Caducará:** { $expiresString }
    
    > **Causa:** { $reason }
    
    > **Gravedad:** { $severity }
server-ban-footer = { $server } | Ronda: #{ $round }
server-perma-ban-string =
    > **Administrador**
    > **Inicio de sesión:** ''{ $adminName }''
    > **Discord:** { $adminLink }
    
    > **Intruso**
    > **Inicio de sesión:** ''{ $targetName }''
    > **Discord:** { $targetLink }
    
    > **Emitido:** { $TimeNow }
    
    > **Causa:** { $reason }
    
    > **Gravedad:** { $severity }
server-role-ban-string =
    > **Administrador**
    > **Inicio de sesión:** ''{ $adminName }''
    > **Discord:** { $adminLink }
    
    > **Intruso**
    > **Inicio de sesión:** ''{ $targetName }''
    > **Discord:** { $targetLink }
    
    > **Emitido:** { $TimeNow }
    > **Caducará:** { $expiresString }
    
    > **Roles:** { $roles }
    
    > **Causa:** { $reason }
    
    > **Gravedad:** { $severity }
server-perma-role-ban-string =
    > **Administrador**
    > **Inicio de sesión:** ''{ $adminName }''
    > **Discord:** { $adminLink }
    
    > **Intruso**
    > **Inicio de sesión:** ''{ $targetName }''
    > **Discord:** ''{ $targetLink }''
    
    > **Emitido:** { $TimeNow }
    
    > **Roles:** { $roles }
    
    > **Causa:** { $reason }
    
    > **Gravedad:** { $severity }