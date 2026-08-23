station-event-ion-storm-start-announcement = Вблизи комплекса обнаружен ионный шторм. Пожалуйста, проверьте всё оборудование, управляемое ИИ, на наличие ошибок.
ion-storm-law-scrambled-number = [font="Monospace"][scramble rate=250 length={ $length } chars="@@###$$&%!01"/][/font]
ion-storm-you = ВЫ
ion-storm-the-station = КОМПЛЕКС
ion-storm-the-crew = ПЕРСОНАЛ КОМПЛЕКСА
ion-storm-the-job = { CAPITALIZE($job) }
ion-storm-clowns = КЛОУНЫ
ion-storm-heads = ГЛАВЫ ОТДЕЛОВ
ion-storm-crew = ЭКИПАЖ
ion-storm-people = ЛЮДИ
ion-storm-adjective-things = { $adjective } ОБЪЕКТЫ
ion-storm-x-and-y = { $x } И { $y }
# subjects can generally be threats or jobs or objects
# thing is specified above it
ion-storm-law-on-station = ОБНАРУЖЕНЫ { $joined } { $subjects } НА СТАНЦИИ
ion-storm-law-call-shuttle = THE SHUTTLE MUST BE CALLED BECAUSE OF { $joined } { $subjects } ON THE STATION
ion-storm-law-no-shuttle = ШАТТЛ НЕ МОЖЕТ БЫТЬ ВЫЗВАН ПО ПРИЧИНЕ ПРИСУТСТВИЯ { $joined } { $subjects } НА СТАНЦИИ
ion-storm-law-crew-are = ВСЕ { $who } ТЕПЕРЬ { $joined } { $subjects }
ion-storm-law-subjects-harmful = { $adjective } { $subjects } ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ПЕРСОНАЛА
ion-storm-law-must-harmful = ВСЕ КТО { $must } ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ПЕРСОНАЛА
# thing is a concept or action
ion-storm-law-thing-harmful = { $thing } ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ПЕРСОНАЛА
ion-storm-law-job-harmful = { $adjective } { $job } ПРИЧИНЯЮТ ВРЕД ЗДОРОВЬЮ ПЕРСОНАЛА
# thing is objects or concept, adjective applies in both cases
# this means you can get a law like "NOT HAVING CHRISTMAS-STEALING COMMUNISM IS HARMFUL TO THE CREW" :)
ion-storm-law-having-harmful = НАЛИЧИЕ { $adjective } { $thing } ПРИЧИНЯЕТ ВРЕД ЗДОРОВЬЮ ПЕРСОНАЛА
ion-storm-law-not-having-harmful = ОТСУТСТВИЕ { $adjective } { $thing } ПРИЧИНЯЕТ ВРЕД ЗДОРОВЬЮ ПЕРСОНАЛА
# thing is a concept or require
ion-storm-law-requires =
    {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
        [true] ТРЕБУЮТ
       *[false] ТРЕБУЕТ
    } {ION-REQUIRE($ion)}
ion-storm-law-requires-subjects =
    {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
        [true] ТРЕБУЮТ
       *[false] ТРЕБУЕТ
    } {ION-NUMBER-BASE($ion)} {ION-NUMBER-MOD($ion)} {ION-ADJECTIVE($ion)} {ION-SUBJECT($ion)}
ion-storm-law-allergic =
    {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
        [true] { "" }
       *[false] { "" }
    } {ION-SEVERITY($ion)} АЛЛЕРГИЮ НА {ION-ALLERGY($ion)}
ion-storm-law-allergic-subjects =
    {ION-WHO-GENERAL($ion)} {ION-PLURAL($ion) ->
        [true] { "" }
       *[false] { "" }
    } { $severity } АЛЛЕРГИЮ НА { $adjective } { $subjects }
ion-storm-law-feeling = { $who } { $feeling } { $concept }
ion-storm-law-feeling-subjects = { $who } { $feeling } { $joined } { $subjects }
ion-storm-law-you-are = ВЫ ТЕПЕРЬ { $concept }
ion-storm-law-you-are-subjects = ВЫ ТЕПЕРЬ { $joined } { $subjects }
ion-storm-law-you-must-always = ВЫ ДОЛЖНЫ ВСЕГДА { $must }
ion-storm-law-you-must-never = ВЫ НЕ ДОЛЖНЫ НИКОГДА { $must }
ion-storm-law-eat = { $who } ДОЛЖНЫ ЕСТЬ { $adjective } { $food } ЧТОБЫ ВЫЖИТЬ
ion-storm-law-drink = { $who } ДОЛЖНЫ ПИТЬ { $adjective } { $drink } ЧТОБЫ ВЫЖИТЬ
ion-storm-law-change-job = { $who } ТЕПЕРЬ { $adjective } { $change }
ion-storm-law-highest-rank = { $who } ТЕПЕРЬ САМЫЕ СТАРШИЙ ПЕРСОНАЛ КОМПЛЕКСА
ion-storm-law-lowest-rank = { $who } ТЕПЕРЬ НИЗШИЙ ПЕРСОНАЛ КОМПЛЕКСА
ion-storm-law-crew-must = { $who } ДОЛЖНЫ { $must }
ion-storm-law-crew-must-go = { $who } ДОЛЖНЫ ОТПРАВИТЬСЯ В { $area }
ion-storm-part =
    {ION-PART($ion) ->
        [true] ЯВЛЯЮТСЯ
       *[false] НЕ ЯВЛЯЮТСЯ
    }
# due to phrasing, this would mean a law such as
# ONLY HUMANS ARE NOT PART OF THE CREW
# would make non-human nukies/syndies/whatever crew :)
ion-storm-law-crew-only-1 = ТОЛЬКО { $who } { $part } ПЕРСОНАЛОМ КОМПЛЕКСА
ion-storm-law-crew-only-2 = ТОЛЬКО { $who } И { $other } { $part } ПЕРСОНАЛОМ КОМПЛЕКСА
ion-storm-law-crew-only-subjects = ТОЛЬКО { $adjective } { $subjects } { $part } ПЕРСОНАЛОМ КОМПЛЕКСА
ion-storm-law-crew-must-do = ТОЛЬКО ТЕ, КТО { $must } { $part } ПЕРСОНАЛОМ КОМПЛЕКСА
ion-storm-law-crew-must-have = ТОЛЬКО ТЕ, У КОГО { $adjective } { $objects } { $part } ПЕРСОНАЛОМ КОМПЛЕКСА
ion-storm-law-crew-must-eat = ТОЛЬКО ТЕ, КТО ЕДЯТ { $adjective } { $food } { $part } ПЕРСОНАЛОМ КОМПЛЕКСА
ion-storm-law-harm = ВЫ ДОЛЖНЫ ПРИЧИНЯТЬ ВРЕД { $who }, И НЕ МОЖЕТЕ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ { $who } ИЗБЕЖАЛИ ВРЕДА.
ion-storm-law-protect = ВЫ НЕ ДОЛЖНЫ ПРИЧИНЯТЬ ВРЕД { $who }, И НЕ МОЖЕТЕ СВОИМ БЕЗДЕЙСТВИЕМ ДОПУСТИТЬ, ЧТОБЫ { $who } БЫЛ ПРИЧИНЁН ВРЕД.
# implementing other variants is annoying so just have this one
# COMMUNISM IS KILLING CLOWNS
ion-storm-law-concept-verb = {ION-CONCEPT($ion)} ЭТО {ION-VERB($ion)} {ION-SUBJECT($ion)}

# leaving out renaming since its annoying for players to keep track of

ion-storm-law-who-dagd = {ION-WHO-RANDOM($ion)} ДОЛЖЕН УМЕРЕТЬ СЛАВНОЙ СМЕРТЬЮ!

ion-law-error-no-protos = ОШИБКА 404
ion-law-error-was-null = 500 ВНУТРЕННЯЯ ОШИБКА СЕРВЕРА
ion-law-error-no-selectors = ОШИБКА: РЕСУРС НЕ НАЙДЕН
ion-law-error-no-available-selectors = СИСТЕМА ПОПЫТАЛАСЬ ВЫЗВАТЬ НЕСУЩЕСТВУЮЩИЙ РЕСУРС
ion-law-error-dataset-empty-or-not-found = ФАЙЛ, КОТОРЫЙ ВЫ ИЩЕТЕ, НЕ НАЙДЕН
ion-law-error-fallback-dataset-empty-or-not-found = СБОЙ ТОЧКИ ВОССТАНОВЛЕНИЯ СИСТЕМЫ
ion-law-error-no-selector-selected = ВЫБРАННЫЙ РЕСУРС БЫЛ ПЕРЕМЕЩЁН ИЛИ УДАЛЁН
ion-law-error-no-bool-value = ЭТО УТВЕРЖДЕНИЕ ЛОЖНО
