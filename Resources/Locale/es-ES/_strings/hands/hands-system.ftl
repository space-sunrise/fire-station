## HandsSystem

# Examine text after when they're holding something (in-hand)
comp-hands-examine = { CAPITALIZE(SUBJECT($user)) } sigue { $items }.
comp-hands-examine-empty = { CAPITALIZE(SUBJECT($user)) } no contiene nada.
comp-hands-examine-wrapper = [color=paleturquoise]{ $item }[/color]
hands-system-blocked-by = Las manos están ocupadas
