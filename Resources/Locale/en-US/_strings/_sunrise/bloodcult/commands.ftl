# Blood Cult Console Commands

# Add Target Command
bloodcult-addtarget-description = Add a target for the Blood Cult
bloodcult-addtarget-help = Adds a specific player as a Blood Cult target for tracking and potential sacrifice.
bloodcult-addtarget-usage = Usage: bloodcult_addtarget <ckey>
bloodcult-addtarget-player-not-found = Player with ckey '{ $ckey }' not found or not currently in-game.
bloodcult-addtarget-system-not-found = Blood Cult system not found.
bloodcult-addtarget-rule-not-found = Active Blood Cult rule not found.
bloodcult-addtarget-already-target = The entity is already a cult target.
bloodcult-addtarget-success = Cult target { $name } successfully added.

# Remove Target Command
bloodcult-removetarget-description = Remove a target for the Blood Cult
bloodcult-removetarget-help = Removes a specific player from the Blood Cult target list, ceasing tracking and marking for sacrifice.
bloodcult-removetarget-usage = Usage: bloodcult_removetarget <ckey>
bloodcult-removetarget-player-not-found = Player with ckey '{ $ckey }' not found or not currently in-game.
bloodcult-removetarget-system-not-found = Blood Cult system not found.
bloodcult-removetarget-rule-not-found = Active Blood Cult rule not found.
bloodcult-removetarget-not-target = The entity is not a cult target.
bloodcult-removetarget-success = Cult target { $name } successfully removed. # List Targets Command
bloodcult-listtargets-description = Show all current Blood Cult targets
bloodcult-listtargets-help = Displays all current Blood Cult targets, including their status (alive or sacrificed) and entity information.
bloodcult-listtargets-usage = Usage: bloodcult_listtargets
bloodcult-listtargets-system-not-found = Blood Cult system not found.
bloodcult-listtargets-no-targets = No cult targets found.
bloodcult-listtargets-header = { $count ->
    [1] Current cult target ({ $count }):
    [few] Current cult targets ({ $count }):
    *[other] Current cult targets ({ $count }):
}
bloodcult-listtargets-sacrificed = Sacrificed
bloodcult-listtargets-alive = Alive
bloodcult-listtargets-target = { $name } ({ $uid }) - { $status }
bloodcult-unknown-entity = Unknown entity

# Cult Device Alert
bloodcult-biocode-alert = The device pulses with dark energy, rejecting your touch. Only those bound by blood may wield its power.
