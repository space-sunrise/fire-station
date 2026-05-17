execution-verb-name = Execute
execution-verb-message = Use your weapon to execute someone.

# All the below localisation strings have access to the following variables
# attacker (the person committing the execution)
# victim (the person being executed)
# weapon (the weapon used for the execution)

execution-popup-melee-initial-internal = You ready {THE($weapon)} against {THE($victim)}'s throat.
execution-popup-melee-initial-external = { CAPITALIZE(THE($attacker)) } readies {POSS-ADJ($attacker)} {$weapon} against the throat of {THE($victim)}.
execution-popup-melee-complete-internal = You slit the throat of {THE($victim)}!
execution-popup-melee-complete-external = { CAPITALIZE(THE($attacker)) } slits the throat of {THE($victim)}!

execution-popup-self-initial-internal = You ready {THE($weapon)} against your own throat.
execution-popup-self-initial-external = { CAPITALIZE(THE($attacker)) } readies {POSS-ADJ($attacker)} {$weapon} against their own throat.
execution-popup-self-complete-internal = You slit your own throat!
execution-popup-self-complete-external = { CAPITALIZE(THE($attacker)) } slits their own throat!

execution-popup-gun-initial-internal = You aim the barrel of the { $weapon } at { $victim }'s head.
execution-popup-gun-initial-external = { $attacker } aims the barrel of the { $weapon } at { $victim }'s head.
execution-popup-gun-complete-internal = You shoot { $victim } in the head!
execution-popup-gun-complete-external = { $attacker } shoots { $victim } in the head!
execution-popup-gun-clumsy-internal = You miss { $victim }'s head and shoot yourself in the leg instead!
execution-popup-gun-clumsy-external = { $attacker } misses { $victim } and shoots themselves in the leg instead!
execution-popup-gun-empty = The { $weapon } clicks. There's no ammo!
execution-popup-ammo-empty = The { $weapon } clicks. The round is spent!
suicide-popup-gun-initial-internal = You place the barrel of the { $weapon } in your mouth.
suicide-popup-gun-initial-external = { $attacker } places the barrel of the { $weapon } in their mouth.
suicide-popup-gun-complete-internal = You shoot yourself in the head!
suicide-popup-gun-complete-external = { $attacker } shoots themselves in the head!
