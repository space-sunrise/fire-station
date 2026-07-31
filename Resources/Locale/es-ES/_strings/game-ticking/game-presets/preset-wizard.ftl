## Survivor

roles-antag-survivor-name = Superviviente
# It's a Halo reference
roles-antag-survivor-objective = Objetivo actual: Sobrevivir
survivor-role-greeting =
    Eres un Superviviente.
    Primero que nada, tienes que volver vivo a Centcom.
    Reúne todo el poder de fuego que necesites para garantizar tu supervivencia.
    No confíes en nadie.
survivor-round-end-dead-count =
    { $deadCount ->
        [one] [color=red]{ $deadCount }[/color] superviviente murió.
        [few] [color=red]{ $deadCount }[/color] supervivientes murieron.
       *[other] [color=red]{ $deadCount }[/color] supervivientes murieron.
    }
survivor-round-end-alive-count =
    { $aliveCount ->
        [one] [color=yellow]{ $aliveCount }[/color] superviviente permaneció en la estación.
        [few] [color=yellow]{ $aliveCount }[/color] supervivientes permanecieron en la estación.
       *[other] [color=yellow]{ $aliveCount }[/color] supervivientes permanecieron en la estación.
    }
survivor-round-end-alive-on-shuttle-count =
    { $aliveCount ->
        [one] [color=green]{ $aliveCount }[/color] superviviente escapó.
        [few] [color=green]{ $aliveCount }[/color] supervivientes sobrevivieron.
       *[other] [color=green]{ $aliveCount }[/color] supervivientes fueron salvados.
    }

## Wizard

objective-issuer-swf = [color=turquoise]Federation de la Mages[/color] espacial
wizard-title = Mago
wizard-description = ¡En la estación Magus! No sé qué puede hacer.
roles-antag-wizard-name = Mago
roles-antag-wizard-objective = Dales una lección que nunca olvidarán.
wizard-role-greeting =
    ¡ERES UN MAGO!
    Surgieron tensiones entre la Federación de Magos Espaciales y NanoTrasen.
    Por eso has sido elegido por la Federación de Magos Espaciales para visitar la estación.
    Muéstrales tus habilidades.
    Depende de ti decidir qué hacer, solo recuerda que los Magos Espaciales quieren que sigas vivo.
wizard-round-end-name = Mago

## TODO: Wizard Apprentice (Coming sometime post-wizard release)

