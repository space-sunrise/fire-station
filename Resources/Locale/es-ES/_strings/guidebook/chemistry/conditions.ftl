reagent-effect-condition-guidebook-total-damage =
    { $max ->
        [2147483648] El cuerpo tiene al menos { NATURALFIXED($min, 2) } daño total
       *[other]
            { $min ->
                [0] no tiene más de { NATURALFIXED($max, 2) } daño total
               *[other] tiene entre { NATURALFIXED($min, 2) } y { NATURALFIXED($max, 2) } daño total
            }
    }
reagent-effect-condition-guidebook-total-hunger =
    { $max ->
        [2147483648] El objetivo tiene al menos { NATURALFIXED($min, 2) } hambre total
       *[other]
            { $min ->
                [0] El objetivo no tiene más de { NATURALFIXED($max, 2) } hambre total
               *[other] El objetivo está entre { NATURALFIXED($min, 2) } y { NATURALFIXED($max, 2) } de hambre general
            }
    }
reagent-effect-condition-guidebook-reagent-threshold =
    { $max ->
        [2147483648] El sistema circulatorio contiene al menos { NATURALFIXED($min, 2) } units{ $reagent }
       *[other]
            { $min ->
                [0] no hay más de { NATURALFIXED($max, 2) } { $reagent }
               *[other] tiene entre { NATURALFIXED($min, 2) } y { NATURALFIXED($max, 2) } { $reagent }
            }
    }
reagent-effect-condition-guidebook-mob-state-condition = Paciente en { $state }
reagent-effect-condition-guidebook-job-condition = La posición del objetivo es { $job }
reagent-effect-condition-guidebook-solution-temperature =
    La temperatura de la solución es { $max ->
        [2147483648] al menos { NATURALFIXED($min, 2) }k
       *[other]
            { $min ->
                [0] No más de { NATURALFIXED($max, 2) }k
               *[other] entre { NATURALFIXED($min, 2) }K y { NATURALFIXED($max, 2) }K
            }
    }
reagent-effect-condition-guidebook-body-temperature =
    La temperatura corporal es { $max ->
        [2147483648] al menos { NATURALFIXED($min, 2) }k
       *[other]
            { $min ->
                [0] No más de { NATURALFIXED($max, 2) }k
               *[other] entre { NATURALFIXED($min, 2) }K y { NATURALFIXED($max, 2) }K
            }
    }
reagent-effect-condition-guidebook-organ-type =
    metabolizando órgano { $shouldhave ->
        [true] Estos son
       *[false] No lo es
    } { $name } órgano
reagent-effect-condition-guidebook-has-tag =
    Objetivo { $invert ->
        [true] no tiene
       *[false] tiene
    } etiqueta { $tag }
reagent-effect-condition-guidebook-this-reagent = Este reactivo
