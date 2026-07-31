gun-selected-mode-examine = El modo fuego [color={ $color }]{ $mode }[/color] seleccionado.
gun-fire-rate-examine = La cadencia de fuego [color={ $color }]{ $fireRate }[/color] por segundo.
gun-selector-verb = Cambio a { $mode }
gun-selected-mode = Seleccionados { $mode }
gun-disabled = ¡No puedes usar armas!
gun-clumsy = ¡Las armas te explotan en la cara!
gun-set-fire-mode = Se selecciona el modo { $mode }
gun-alert-level-condition = ¡El código de la estación es demasiado bajo para este modo de disparo!
gun-magazine-whitelist-fail = ¡No cabe en un arma!
gun-magazine-fired-empty = ¡No quedan cartuchos!
gun-Insulated-gloves = ¡Tus dedos son demasiado gruesos para apretar el gatillo!
# SelectiveFire
gun-SemiAuto = Semiautomático
gun-Burst = Cola
gun-FullAuto = AUTOMÁTICO
# BallisticAmmoProvider
gun-ballistic-cycle = Twitch
gun-ballistic-cycled = Boca abajo
gun-ballistic-cycled-empty = Baja
gun-ballistic-transfer-invalid = ¡{ CAPITALIZE($ammoEntity) } no se puede colocar en { $targetEntity }!
gun-ballistic-transfer-empty = La { CAPITALIZE($entity) } está vacía.
gun-ballistic-transfer-target-full = { CAPITALIZE($entity) } ya está completamente cargada.
# CartridgeAmmo
gun-cartridge-spent = Es [color=red]spent[/color].
gun-cartridge-unspent = Es [color=lime]not spent[/color].
# BatteryAmmoProvider
gun-battery-examine =
    Carga suficiente para [color={ $color }]{ $count }[/color] { $count ->
        [one] Disparo
        [few] Disparo
       *[other] Disparos
    }.
# CartridgeAmmoProvider
gun-chamber-bolt-ammo = El obturador no está cerrado
gun-chamber-bolt = El cerrojo [color={ $color }]{ $bolt }[/color].
gun-chamber-bolt-closed = El obturador está cerrado
gun-chamber-bolt-opened = La persiana está abierta
gun-chamber-bolt-close = Cierra la persiana
gun-chamber-bolt-open = Abre la persiana
gun-chamber-bolt-closed-state = Abierto
gun-chamber-bolt-open-state = Cerrado
gun-chamber-rack = Tira del cerrojo
# MagazineAmmoProvider
gun-magazine-examine =
    Aquí [color={ $color }]{ $count }[/color] { $count ->
        [one] Disparo
        [few] Disparo
       *[other] Disparos
    }.
# 🌟Starlight - Start🌟
gun-magazine-ammo-type = Contiene [color={$color}]{$type}[/color].
gun-magazine-empty = La tienda está vacía.
# 🌟Starlight - End🌟

# RevolverAmmoProvider
gun-revolver-empty = Descarga el revólver
gun-revolver-full = El revólver está completamente cargado
gun-revolver-insert = Cargado
gun-revolver-spin = Haz girar el carrete
gun-revolver-spun = El tambor gira
gun-speedloader-empty = El Speedloader está vacío
examine-weapon-dismantle-on-shoot = Las armas pueden deshacerse al dispararse ([color=yellow]{ $chance }%[/color]).
