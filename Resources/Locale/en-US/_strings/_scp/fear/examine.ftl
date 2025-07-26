examine-fear-state-anxiety = [color=lightblue]{ CAPITALIZE(gender-based-third-form) } looks anxious[/color]
examine-fear-state-fear = [color=lightblue]{ CAPITALIZE(gender-based-third-form-case) }'s eyes look fearful[/color]
examine-fear-state-terror = [color=lightblue]{ CAPITALIZE(gender-based-third-form) } seems insane![/color]
gender-based-third-form =
    { GENDER($target) ->
        [male] he
        [female] she
        [epicene] they
       *[neuter] it
    }
gender-based-third-form-case =
    { GENDER($target) ->
        [male] his
        [female] her
        [epicene] their
       *[neuter] its
    }
