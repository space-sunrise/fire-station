# Commands


## Delay shuttle round end

emergency-shuttle-command-round-desc = Останавливает таймер окончания раунда, когда эвакуационный челнок покидает гиперпространство.
emergency-shuttle-command-round-yes = Раунд продлён.
emergency-shuttle-command-round-no = Невозможно продлить окончание раунда.

## Dock emergency shuttle

emergency-shuttle-command-dock-desc = Вызывает спасательный челнок и приземляет его возле комплекса... если это возможно.

## Launch emergency shuttle

emergency-shuttle-command-launch-desc = Досрочно запускает транспортный челнок, если это возможно.
# Emergency shuttle
emergency-shuttle-left = Эвакуационный челнок покинул комплекс. Расчётное время полёта челнока - { $transitTime } секунд.
emergency-shuttle-launch-time = Эвакуационный челнок взлетает через { $consoleAccumulator } секунд.
emergency-shuttle-docked = Эвакуационный челнок приземлился { $location }. Он улетит через { $time } секунд.
emergency-shuttle-good-luck = Эвакуационный челнок не может найти ближайшее место приземления. Удачи.
emergency-shuttle-nearby = Эвакуационный челнок не может найти подходящее место для приземления, поэтому приземляется недалеко от комплекса. Направление: { $direction }.
emergency-shuttle-extended = Время запуска { " " } было продлено из-за неудобных обстоятельств.
# Emergency shuttle console popup / announcement
emergency-shuttle-console-no-early-launches = Досрочный запуск отключён
# Emergency shuttle console popup / announcement
emergency-shuttle-console-auth-left =
    { $remaining } { $remaining ->
        [one] авторизация осталась
        [few] авторизации остались
       *[other] авторизации остались
    } для досрочного запуска челнока.
emergency-shuttle-console-auth-revoked =
    Авторизации на досрочный запуск челнока отозваны, { $remaining } { $remaining ->
        [one] авторизация необходима
        [few] авторизации необходимы
       *[other] авторизации необходимы
    }.
emergency-shuttle-console-denied = Доступ запрещён
# UI
emergency-shuttle-console-window-title = Консоль транспортного челнока
# UI
emergency-shuttle-ui-engines = ДВИГАТЕЛИ:
emergency-shuttle-ui-idle = Простой
emergency-shuttle-ui-repeal-all = Повторить всё
emergency-shuttle-ui-early-authorize = Разрешение на досрочный запуск
emergency-shuttle-ui-authorize = АВТОРИЗОВАТЬСЯ
emergency-shuttle-ui-repeal = ПОВТОРИТЬ
emergency-shuttle-ui-authorizations = Авторизации
emergency-shuttle-ui-remaining = Осталось: { $remaining }
# Map Misc.
map-name-centcomm = Штаб О4
map-name-terminal = Терминал прибытия
