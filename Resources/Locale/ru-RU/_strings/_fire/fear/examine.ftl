examine-fear-state-anxiety = [color=lightblue]{ CAPITALIZE(gender-based-third-form) } выглядит встреженно[/color]
examine-fear-state-fear = [color=lightblue]{ CAPITALIZE(gender-based-third-form-case) } глаза выглядят напуганными[/color]
examine-fear-state-terror = [color=lightblue]{ CAPITALIZE(gender-based-third-form) } кажется не в себе![/color]

gender-based-third-form = { GENDER($target) ->
    [male] он
    [female] она
    [epicene] они
    *[neuter] оно
}

gender-based-third-form-case = { GENDER($target) ->
    [male] его
    [female] её
    [epicene] их
    *[neuter] этого
}

