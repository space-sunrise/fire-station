examine-fear-state-anxiety = [color=lightblue]{ CAPITALIZE(gender-based-third-form) } parece mostrar ansiedad[/color]
examine-fear-state-fear = [color=lightblue]{ CAPITALIZE(gender-based-third-form-case) } ojos parecen reflejar miedo[/color]
examine-fear-state-terror = ¡[color=lightblue]{ CAPITALIZE(gender-based-third-form) } parece haber perdido el control[/color]
examine-fear-state-none-dead = [color=lightblue]{ CAPITALIZE(gender-based-third-form) } parece en calma, como si la muerte hubiera llegado de forma inesperada[/color]
examine-fear-state-anxiety-dead = [color=lightblue]En { gender-based-third-form-case } ojos apagados permanece congelada una última mirada de ansiedad[/color]
examine-fear-state-fear-dead = [color=lightblue]En { gender-based-third-form-case } ojos completamente abiertos quedó fijado un instante de lucidez: el último[/color]
examine-fear-state-terror-dead = [color=lightblue]{ CAPITALIZE(gender-based-third-form-case) } ojos permanecen fijos en un vacío que nadie debería haber contemplado; su boca quedó congelada en un grito silencioso[/color]
gender-based-third-form =
    { GENDER($target) ->
        [male] Él
        [female] Ella
        [epicene] La persona
       *[neuter] Eso
    }
gender-based-third-form-case =
    { GENDER($target) ->
        [male] sus
        [female] sus
        [epicene] sus
       *[neuter] sus
    }
