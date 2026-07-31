second = { $count ->
    [one] Segundo
    [few] Segundos
    [many] Segundos
   *[other] Segundos
}

dead = { $count ->
    [one] Fallecido
    [few] de los muertos
    [many] de los muertos
   *[other] de los muertos
}
-create-3rd-person =
    { $chance ->
        [1] Crea
       *[other] crear
    }
-cause-3rd-person =
    { $chance ->
        [1] Causas
       *[other] Causa
    }
-satiate-3rd-person =
    { $chance ->
        [1] Saturados
       *[other] saturar
    }
reagent-effect-guidebook-create-entity-reaction-effect =
    { $chance ->
        [1] Crea
       *[other] crear
    } { $amount ->
        [1] { $entname }
       *[other] { $amount } { $entname }
    }
reagent-effect-guidebook-explosion-reaction-effect =
    { $chance ->
        [1] Causas
       *[other] Causa
    } explosión
reagent-effect-guidebook-emp-reaction-effect =
    { $chance ->
        [1] Causas
       *[other] Causa
    } pulso electromagnético
reagent-effect-guidebook-flash-reaction-effect =
    { $chance ->
        [1] Causas
       *[other] causada
    } destello cegador
reagent-effect-guidebook-foam-area-reaction-effect =
    { $chance ->
        [1] Crea
       *[other] crear
    } gran cantidad de espuma
reagent-effect-guidebook-smoke-area-reaction-effect =
    { $chance ->
        [1] Crea
       *[other] crear
    } gran cantidad de humo
reagent-effect-guidebook-satiate-thirst =
    { $chance ->
        [1] Apaga
       *[other] Apaga
    } { $relative ->
        [1] La sed es normal
       *[other] sedienta de { NATURALFIXED($relative, 3) }x de lo ordinario
    }
reagent-effect-guidebook-satiate-hunger =
    { $chance ->
        [1] Saturados
       *[other] saturar
    } { $relative ->
        [1] El hambre es normal
       *[other] hambre de { NATURALFIXED($relative, 3) }x de lo habitual
    }
reagent-effect-guidebook-health-change =
    { $chance ->
        [1]
            { $healsordeals ->
                [heals] Heals
                [deals] Acuerdos
               *[both] Cambios de salud mediante
            }
       *[other]
            { $healsordeals ->
                [heals] Cura
                [deals] Solicita
               *[both] Cambiar la salud mediante
            }
    } { $changes }
reagent-effect-guidebook-status-effect =
    { $type ->
        [add]
            { $chance ->
                [1] Causas
               *[other] Causa
            } { LOC($key) } al menos para { NATURALFIXED($time, 3) }, el efecto se acumula
       *[set]
            { $chance ->
                [1] Causas
               *[other] Causa
            } { LOC($key) } al menos para { NATURALFIXED($time, 3) }, el efecto no se acumula
        [remove]
            { $chance ->
                [1] Eliminaciones
               *[other] Eliminar
            } { NATURALFIXED($time, 3) } de { LOC($key) }
    }
reagent-effect-guidebook-activate-artifact =
    { $chance ->
        [1] Ensayos
       *[other] están intentando
    } activar un artefacto
reagent-effect-guidebook-set-solution-temperature-effect =
    { $chance ->
        [1] Instalaciones
       *[other] Instalación
    } la temperatura de la solución es exactamente { NATURALFIXED($temperature, 2) }k
reagent-effect-guidebook-adjust-solution-temperature-effect =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Añade
               *[-1] Eliminaciones
            }
       *[other]
            { $deltasign ->
                [1] Añadir
               *[-1] Eliminar
            }
    } calor desde la solución hasta que la temperatura alcanza { $deltasign ->
        [1] No más de { NATURALFIXED($maxtemp, 2) }k
       *[-1] al menos { NATURALFIXED($mintemp, 2) }k
    }
reagent-effect-guidebook-adjust-reagent-reagent =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Añadir
               *[-1] Eliminaciones
            }
       *[other]
            { $deltasign ->
                [1] Añadir
               *[-1] Eliminar
            }
    } { NATURALFIXED($amount, 2) }unit de { $reagent } { $deltasign ->
        [1] K
       *[-1] de
    } Solución
reagent-effect-guidebook-adjust-reagent-group =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Añade
               *[-1] Eliminaciones
            }
       *[other]
            { $deltasign ->
                [1] Añadir
               *[-1] Eliminar
            }
    } { NATURALFIXED($amount, 2) } unidades de reactivos en el grupo { $group } { $deltasign ->
        [1] K
       *[-1] de
    } Solución
reagent-effect-guidebook-adjust-temperature =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Añadir
               *[-1] Eliminar
            }
       *[other]
            { $deltasign ->
                [1] Añadir
               *[-1] Eliminar
            }
    } { POWERJOULES($amount) } calor { $deltasign ->
        [1] al cuerpo
       *[-1] del cuerpo
    } en el que se metaboliza
reagent-effect-guidebook-chem-cause-disease =
    { $chance ->
        [1] Causas
       *[other] Causa
    } enfermedad { $disease }
reagent-effect-guidebook-chem-cause-random-disease =
    { $chance ->
        [1] Causas
       *[other] Causa
    } enfermedad { $diseases }
reagent-effect-guidebook-jittering =
    { $chance ->
        [1] Causas
       *[other] Causa
    } temblando
reagent-effect-guidebook-chem-clean-bloodstream =
    { $chance ->
        [1] Limpias
       *[other] Purificar
    } el sistema circulatorio de otras sustancias
reagent-effect-guidebook-cure-disease =
    { $chance ->
        [1] Heals
       *[other] Cura
    } enfermedad
reagent-effect-guidebook-cure-eye-damage =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Acuerdos
               *[-1] Heals
            }
       *[other]
            { $deltasign ->
                [1] Solicita
               *[-1] Cura
            }
    } lesiones oculares
reagent-effect-guidebook-chem-vomit =
    { $chance ->
        [1] Causas
       *[other] Causa
    } vómitos
reagent-effect-guidebook-create-gas =
    { $chance ->
        [1] Crea
       *[other] crear
    } { $moles } { $moles ->
        [1] Polilla
       *[other] Polilla
    } gas { $gas }
reagent-effect-guidebook-drunk =
    { $chance ->
        [1] Causas
       *[other] Causa
    } Intoxicación
reagent-effect-guidebook-electrocute =
    { $chance ->
        [1] Descarga eléctrica
       *[other] Descargas eléctricas
    } que ha consumido dentro de { NATURALFIXED($time, 3) }
reagent-effect-guidebook-extinguish-reaction =
    { $chance ->
        [1] Apaga
       *[other] extinguir
    } fuego
reagent-effect-guidebook-flammable-reaction =
    { $chance ->
        [1] Incrementos
       *[other] Aumento
    } Inflamabilidad
reagent-effect-guidebook-ignite =
    { $chance ->
        [1] Incendia
       *[other] Incendiado
    } del usuario
reagent-effect-guidebook-make-sentient =
    { $chance ->
        [1] ¿Lo hace
       *[other] do
    } que lo usó con sensatez
reagent-effect-guidebook-make-polymorph =
    { $chance ->
        [1] Giros
       *[other] gira
    } que se usaba en { $entityname }
reagent-effect-guidebook-modify-bleed-amount =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Mejora
               *[-1] Debilita
            }
       *[other]
            { $deltasign ->
                [1] Mejorar
               *[-1] debilitar
            }
    } sangrando
reagent-effect-guidebook-modify-blood-level =
    { $chance ->
        [1]
            { $deltasign ->
                [1] Incrementos
               *[-1] Lowers
            }
       *[other]
            { $deltasign ->
                [1] Aumento
               *[-1] Inferior
            }
    } nivel sanguíneo en el cuerpo
reagent-effect-guidebook-paralyze =
    { $chance ->
        [1] Paralizante
       *[other] paraliza
    } que ha consumido al menos { NATURALFIXED($time, 3) }
reagent-effect-guidebook-movespeed-modifier =
    { $chance ->
        [1] ¿Lo hace
       *[other] do
    } velocidad de movimiento { NATURALFIXED($walkspeed, 3) }x del mínimo estándar para { NATURALFIXED($time, 3) }
reagent-effect-guidebook-reset-narcolepsy =
    { $chance ->
        [1] Previene
       *[other] Prevenir
    } Ataques de narcolepsia
reagent-effect-guidebook-wash-cream-pie-reaction =
    { $chance ->
        [1] Enjuagues
       *[other] Lavar
    } tarta de crema de la cara
reagent-effect-guidebook-cure-zombie-infection =
    { $chance ->
        [1] Heals
       *[other] Treat
    } virus zombi
reagent-effect-guidebook-cause-zombie-infection =
    { $chance ->
        [1] Infecta
       *[other] infectar
    } un virus zombi humano
reagent-effect-guidebook-reduce-rotting =
    { $chance ->
        [1] Regeneración
       *[other] regenera
    } { NATURALFIXED($time, 3) } { MANY("second", $time) } decadencia
reagent-effect-guidebook-innoculate-zombie-infection =
    { $chance ->
        [1] Heals
       *[other] Treat
    } y proporciona inmunidad contra él en el futuro
reagent-effect-guidebook-area-reaction =
    { $chance ->
        [1] Causas
       *[other] Causa
    } reacción de humo o espuma a { NATURALFIXED($duration, 3) } { MANY("second", $duration) }
reagent-effect-guidebook-add-to-solution-reaction =
    { $chance ->
        [1] Marcas
       *[other] Fuerza
    } los productos químicos aplicados al objeto se añadirán al recipiente interno de la solución de dicho objeto
reagent-effect-guidebook-plant-attribute =
    { $chance ->
        [1] Trampas
       *[other] Cambio
    } { $attribute } para [color={ $colorName }]{ $amount }[/color]
reagent-effect-guidebook-plant-cryoxadone =
    { $chance ->
        [1] Rejuvenece
       *[other] Rejuvenecer
    } planta, dependiendo de la edad de la planta y del momento de su crecimiento
reagent-effect-guidebook-plant-phalanximine =
    { $chance ->
        [1] Restauraciones
       *[other] se están restaurando
    } viabilidad de una planta que se ha vuelto inviable como resultado de una mutación
reagent-effect-guidebook-plant-diethylamine =
    { $chance ->
        [1] Incrementos
       *[other] Aumento
    } Vida útil y/o salud base de la planta con un 10% de probabilidad por unidad.
reagent-effect-guidebook-plant-robust-harvest =
    { $chance ->
        [1] Incrementos
       *[other] Aumento
    } la potencia de la planta { $increase } hasta el máximo en la { $limit }. Hace que la planta pierda sus semillas cuando la potencia alcanza { $seedlesstreshold }. Intentar aumentar la potencia más allá de { $limit } puede causar un 10% de probabilidad de reducir los rendimientos.
reagent-effect-guidebook-plant-seeds-add =
    { $chance ->
        [1] Restores the
       *[other] restore the
    } seeds of the plant
reagent-effect-guidebook-plant-seeds-remove =
    { $chance ->
        [1] Removes the
       *[other] remove the
    } seeds of the plant
reagent-effect-guidebook-cause-flesh-cultist-infection =
    { $chance ->
        [1] Causas
       *[other] Causa
    } la infección carnal de un cultista
