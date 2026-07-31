entity-effect-guidebook-spawn-entity =
    { $chance ->
        [1] Crea
        *[other] crear
    } { $amount ->
        [1] {INDEFINITE($entname)}
        *[other] {$amount} {MAKEPLURAL($entname)}
    }

entity-effect-guidebook-destroy =
    { $chance ->
        [1] Destruye
        *[other] Destruir
    } Objeto

entity-effect-guidebook-break =
    { $chance ->
        [1] Pausas
        *[other] Pausa
    } Objeto

entity-effect-guidebook-explosion =
    { $chance ->
        [1] Causas
        *[other] Causa
    } explosión

entity-effect-guidebook-emp =
    { $chance ->
        [1] Causas
        *[other] Causa
    } pulso electromagnético

entity-effect-guidebook-flash =
    { $chance ->
        [1] Causas
        *[other] Causa
    } destello cegador

entity-effect-guidebook-foam-area =
    { $chance ->
        [1] Crea
        *[other] crear
    } gran cantidad de espuma

entity-effect-guidebook-smoke-area =
    { $chance ->
        [1] Crea
        *[other] crear
    } gran cantidad de humo

entity-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Apaga
        *[other] Apaga
    } { $relative ->
        [1] sed en valores medios
        *[other] sed a un ritmo {NATURALFIXED($relative, 3)}x de la media
    }

entity-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Apaga
        *[other] Apaga
    } { $relative ->
        [1] Hambre en valores medios
        *[other] hambre a un ritmo {NATURALFIXED($relative, 3)}x de la media
    }

entity-effect-guidebook-health-change =
    { $chance ->
        [1] { $healsordeals ->
                [heals] Heals
                [deals] Acuerdos
                *[both] Cambios de salud mediante
             }
        *[other] { $healsordeals ->
                    [heals] Sanar
                    [deals] Solicita
                    *[both] Cambiar la salud mediante
                 }
    } { $changes }

entity-effect-guidebook-even-health-change =
    { $chance ->
        [1] { $healsordeals ->
            [heals] Sana de forma uniforme
            [deals] Se aplica de forma uniforme
            *[both] Modifica la salud de manera uniforme mediante
        }
        *[other] { $healsordeals ->
            [heals] Curar de forma uniforme
            [deals] Aplica de forma uniforme
            *[both] Cambiar la salud de manera uniforme mediante
        }
    } { $changes }

entity-effect-guidebook-status-effect =
    { $type ->
        [update]{ $chance ->
                    [1] Causas
                     *[other] Causa
                 } {LOC($key)} al menos durante {NATURALFIXED($time, 3)} {MANY("second", $time)} sin guardar
        [add]   { $chance ->
                    [1] Causas
                    *[other] Causa
                } {LOC($key)} al menos {NATURALFIXED($time, 3)} {MANY("second", $time)} con ahorros
        [set]  { $chance ->
                    [1] Causas
                    *[other] Causa
                } {LOC($key)} en {NATURALFIXED($time, 3)} {MANY("second", $time)} sin guardar
        *[remove]{ $chance ->
                    [1] Eliminaciones
                    *[other] Eliminado
                } {NATURALFIXED($time, 3)} {MANY("second", $time)} efecto {LOC($key)}
    } { $delay ->
        [0] Inmediatamente
        *[other] tras un retraso {NATURALFIXED($delay, 3)} {MANY("second", $delay)}
    }

entity-effect-guidebook-status-effect-indef =
    { $type ->
        [update]{ $chance ->
                    [1] Causas
                    *[other] Causa
                 } efecto permanente {LOC($key)}
        [add]   { $chance ->
                    [1] Causas
                    *[other] Causa
                } efecto permanente {LOC($key)}
        [set]  { $chance ->
                    [1] Causas
                    *[other] Causa
                } efecto permanente {LOC($key)}
        *[remove]{ $chance ->
                    [1] Eliminaciones
                    *[other] Eliminado
                } {LOC($key)} efecto
    } { $delay ->
        [0] Inmediatamente
        *[other] tras un retraso {NATURALFIXED($delay, 3)} {MANY("second", $delay)}
    }

entity-effect-guidebook-knockdown =
    { $type ->
        [update]{ $chance ->
                    [1] Causas
                    *[other] Causa
                    } {LOC($key)} al menos durante {NATURALFIXED($time, 3)} {MANY("second", $time)} sin guardar
        [add]   { $chance ->
                    [1] Causas
                    *[other] Causa
                }volcando al menos para {NATURALFIXED($time, 3)} {MANY("second", $time)} con acumulación
        *[set]  { $chance ->
                    [1] Causas
                    *[other] Causa
                } inclinándose al menos durante {NATURALFIXED($time, 3)} {MANY("second", $time)} sin acumulación
        [remove]{ $chance ->
                    [1] Eliminaciones
                    *[other] Eliminado
                } Rollover {NATURALFIXED($time, 3)} {MANY("second", $time)}
    }

entity-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Instalaciones
        *[other] Instalación
    } la temperatura de la solución es exactamente {NATURALFIXED($temperature, 2)}k

entity-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1] { $deltasign ->
                [1] Añade
                *[-1] Limpias
            }
        *[other]
            { $deltasign ->
                [1] Añadir
                *[-1] Limpieza
            }
    } calor desde la solución hasta que la temperatura { $deltasign ->
                [1] no superará {NATURALFIXED($maxtemp, 2)}k
                *[-1] no caerá por debajo de {NATURALFIXED($mintemp, 2)}k
            }

entity-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1] { $deltasign ->
                [1] Añade
                *[-1] Eliminaciones
            }
        *[other]
            { $deltasign ->
                [1] Añadir
                *[-1] Eliminar
            }
    } {NATURALFIXED($amount, 2)}u reactivo {$reagent} { $deltasign ->
        [1] en
        *[-1] de
    } Solución

entity-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1] { $deltasign ->
                [1] Añade
                *[-1] Eliminaciones
            }
        *[other]
            { $deltasign ->
                [1] Añadir
                *[-1] Eliminar
            }
    } {NATURALFIXED($amount, 2)}u reactivos del grupo {$group} { $deltasign ->
            [1] en
            *[-1] de
        } Solución

entity-effect-guidebook-adjust-temperature =
    { $chance ->
        [1] { $deltasign ->
                [1] Añade
                *[-1] Limpias
            }
        *[other]
            { $deltasign ->
                [1] Añadir
                *[-1] Limpieza
            }
    } {POWERJOULES($amount)} calor { $deltasign ->
            [1] K
            *[-1] de
        } del cuerpo donde se encuentra

entity-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Causas
        *[other] Causa
    } enfermedad {$disease}

entity-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Causas
        *[other] Causa
    } enfermedades {$diseases}

entity-effect-guidebook-jittering =
    { $chance ->
        [1] Causas
        *[other] Causa
    } temblando

entity-effect-guidebook-clean-bloodstream =
    { $chance ->
        [1] Limpias
        *[other] Purificar
    } flujo sanguíneo de otras sustancias

entity-effect-guidebook-cure-disease =
    { $chance ->
        [1] Heals
        *[other] Treat
    } Enfermedades

entity-effect-guidebook-eye-damage =
    { $chance ->
        [1] { $deltasign ->
                [1] Acuerdos
                *[-1] Heals
            }
        *[other]
            { $deltasign ->
                [1] Solicita
                *[-1] Sanar
            }
    } daño ocular

entity-effect-guidebook-vomit =
    { $chance ->
        [1] Causas
        *[other] Causa
    } vómitos

entity-effect-guidebook-create-gas =
    { $chance ->
        [1] Crea
        *[other] crear
    } { $moles } { $moles ->
        [1] Polilla
        *[other] polillas
    } gas {$gas}

entity-effect-guidebook-drunk =
    { $chance ->
        [1] Causas
        *[other] Causa
    } Intoxicación

entity-effect-guidebook-electrocute =
    { $chance ->
        [1] Amortiguadores
        *[other] Electrocución
    } metabolizando en {NATURALFIXED($time, 3)} {MANY("second", $time)}

entity-effect-guidebook-emote =
    { $chance ->
        [1] Lo haré
        *[other] será forzado
    } metabolizando para realizar [bold][color=white]{$emote}[/color][/bold]

entity-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Extinguidos
        *[other] Extinguido
    } fuego

entity-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Incrementos
        *[other] Aumento
    } Inflamabilidad

entity-effect-guidebook-ignite =
    { $chance ->
        [1] Incendia
        *[other] Incendiado
    } Metabolización

entity-effect-guidebook-make-sentient =
    { $chance ->
        [1] ¿Lo hace
        *[other] do
    } metabolizando la inteligencia

entity-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Giros
        *[other] gira
    } metabolizando en {$entityname}

entity-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1] { $deltasign ->
                [1] Mejora
                *[-1] Debilita
            }
        *[other] { $deltasign ->
                    [1] Mejorar
                    *[-1] debilitar
                 }
    } sangrando

entity-effect-guidebook-modify-blood-level =
    { $chance ->
        [1] { $deltasign ->
                [1] Incrementos
                *[-1] Lowers
            }
        *[other] { $deltasign ->
                    [1] Aumento
                    *[-1] Inferior
                 }
    } nivel en sangre

entity-effect-guidebook-paralyze =
    { $chance ->
        [1] Paralizante
        *[other] paraliza
    } metabolizando al menos {NATURALFIXED($time, 3)} {MANY("second", $time)}

entity-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] Trampas
        *[other] Cambio
    } velocidad de movimiento {NATURALFIXED($sprintspeed, 3)}x al menos {NATURALFIXED($time, 3)} {MANY("second", $time)}

entity-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Se aleja temporalmente
        *[other] Expulsado temporalmente
    } narcolepsia

entity-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Enjuagues
        *[other] Lavar
    } tarta de crema de la cara

entity-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Heals
        *[other] Treat
    } infección zombi actual

entity-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Da
        *[other] da
    } infección zombi

entity-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Heals
        *[other] Treat
    } infección zombi actual y otorga inmunidad contra el futuro

entity-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Restauraciones
        *[other] se están restaurando
    } {NATURALFIXED($time, 3)} {MANY("second", $time)} decadencia

entity-effect-guidebook-area-reaction =
    { $chance ->
        [1] Causas
        *[other] Causa
    } la reacción del humo o la espuma a {NATURALFIXED($duration, 3)} {MANY("second", $duration)}

entity-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Causas
        *[other] Causa
    } añadiendo {$reagent} al contenedor interior de la solución

entity-effect-guidebook-artifact-unlock =
    { $chance ->
        [1] Ayuda
        *[other] Ayuda
        } Desbloquear un artefacto alienígena.

entity-effect-guidebook-artifact-durability-restore =
    Restaura {$restored} durabilidad en nodos activos de artefactos.

entity-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Regula
        *[other] Regular
    } {$attribute} en {$positive ->
    [true] [color=red]{$amount}[/color]
    *[false] [color=green]{$amount}[/color]
    }

entity-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Rejuvenece
        *[other] Rejuvenecer
    } planta dependiendo de su edad y tiempo de crecimiento

entity-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Restauraciones
        *[other] se están restaurando
    } viabilidad de la planta perdida debido a la mutación

entity-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Incrementos
        *[other] Aumento
    } Vida útil y/o salud base de la planta con un 10% de probabilidad para cada una

entity-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Incrementos
        *[other] Aumento
    } la potencia de la planta a {$increase} hasta el máximo {$limit}. La planta pierde semillas cuando alcanza el {$seedlesstreshold} de potencia. Intentar aumentar la potencia por encima de {$limit} puede reducir los rendimientos con un 10% de probabilidad

entity-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Retornos
        *[other] Regreso
    } Semillas de Planta

entity-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Eliminaciones
        *[other] Eliminar
    } Semillas de Planta

entity-effect-guidebook-plant-mutate-chemicals =
    { $chance ->
        [1] Mutantes
        *[other] Mutado
    } planta para producir {$name}
