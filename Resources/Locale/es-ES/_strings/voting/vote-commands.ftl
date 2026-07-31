### Voting system related console commands


## 'createvote' command

cmd-createvote-desc = Crea una votación
cmd-createvote-help = Uso: createvote <'reiniciar'|'preset'|'mapa'>
cmd-createvote-cannot-call-vote-now = ¡No puedes empezar a votar ahora mismo!
cmd-createvote-invalid-vote-type = Tipo de voto inválido
cmd-createvote-arg-vote-type = <vote type>

## 'customvote' command

cmd-customvote-desc = Crea un voto personalizable
cmd-customvote-help = Uso: customvote <title> <option1> <option2> [opción3...]
cmd-customvote-on-finished-tie = ¡Un empate entre { $ties }!
cmd-customvote-on-finished-win = { $winner } gana!
cmd-customvote-arg-title = <title>
cmd-customvote-arg-option-n = <option{ $n }>

## 'vote' command

cmd-vote-desc = Votos en el voto activo
cmd-vote-help = Uso: voto <voteId> <option>
cmd-vote-cannot-call-vote-now = ¡No puedes empezar a votar ahora mismo!
cmd-vote-on-execute-error-must-be-player = Debe ser un jugador
cmd-vote-on-execute-error-invalid-vote-id = Identificación de voto inválida
cmd-vote-on-execute-error-invalid-vote-options = Parámetros de votación incorrectos
cmd-vote-on-execute-error-invalid-vote = Voto incorrecto
cmd-vote-on-execute-error-invalid-option = Parámetro inválido

## 'listvotes' command

cmd-listvotes-desc = Listas de votos activos
cmd-listvotes-help = Uso: votos de lista

## 'cancelvote' command

cmd-cancelvote-desc = Cancela la votación actual
cmd-cancelvote-help =
    Uso: cancelar voto <id>
    Puedes encontrar el ID usando el comando listvotes.
cmd-cancelvote-error-invalid-vote-id = Identificación de voto inválida
cmd-cancelvote-error-missing-vote-id = Identificación perdida
cmd-cancelvote-arg-id = <id>
