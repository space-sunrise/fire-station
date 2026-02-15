# Правило: Архитектурный паттерн OnEvent -> TryDo -> CanDo -> Do

Это правило описывает мандаторный архитектурный паттерн для реализации действий и взаимодействий в кодовой базе Space Station 14. Следование этому паттерну обеспечивает предсказуемость, переиспользуемость и чистоту кода.

Структура: `OnEvent()` -> `TryDoSomething()` -> (проверка) `if (!CanDoSomething()) return` -> `DoSomething()`

## 📝 Общая схема

Логика разбивается на три уровня ответственности:

1.  **Event Handler (`OnEvent`)**: Точка входа. Принимает событие, распаковывает данные и вызывает `Try`-метод.
2.  **Public API (`TryDo`)**: "Публичный интерфейс" действия. Оркестрирует проверку (`CanDo`) и исполнение. Возвращает успех/неудачу.
3.  **Check (`CanDo`)**: Чистая проверка условий. Определяет, *можно* ли совершить действие, но *не совершает* его.

---

## 🔍 Компоненты паттерна

### 1. Обработчик событий (`OnEvent`)
Метод, подписанный на событие (`SubscribeLocalEvent`).
*   **Задача**: Перенаправить поток исполнения в публичный API.
*   **Логика**: Минимальная. Только проверка валидности события (например, `args.Handled`) и вызов `Try...`.
*   **Название**: `On[EventName]`, `On[Action]`.
*   **Сигнатура**: `OnEvent(Entity<T> ent, ref Event args)` (используем `Entity<T>`).

### 2. Попытка действия (`TryDoSomething`)
Публичный метод, доступный для вызова из других систем (API).
*   **Сигнатура**: `public bool TryAction(Entity<T> ent, ...)`
*   **Задача**:
    1.  Вызвать `CanDoSomething`. Если вернул `false` — вернуть `false`.
    2.  Если проверки пройдены — выполнить действие (изменить компонент, вызвать событие, проиграть звук и т.д.).
    3.  Вернуть `true` при успехе.
*   **Важно**: Если действие требует специфических аргументов (например, `user`), они должны быть переданы сюда.

### 3. Проверка возможности (`CanDoSomething`)
Метод, содержащий условия выполнения.
*   **Сигнатура**: `public bool CanAction(Entity<T> ent, ..., bool quiet = false)`
*   **Задача**: Проверить все условия (дистанция, наличие инструмента, статус компонента).
*   **Side Effects**:
    *   ❌ **ЗАПРЕЩЕНО** менять состояние сущностей (компонентов).
    *   ✅ **РАЗРЕШЕНО** отправлять сообщения игроку (Popups), если аргумент `quiet` равен `false`.

---

## ✅ Пример (Система взаимодействия с предметами)

Обрати внимание на четкое разделение ответственности и использование `Entity<T>`.

```csharp
// 1. Event Handler
// Получает событие использования предмета в руке.
// Использование Entity<T> обеспечивает авто-резолв компонента.
private void OnUseInHand(Entity<WieldableComponent> ent, ref UseInHandEvent args)
{
    if (args.Handled)
        return;

    // Вызов публичного API
    // Передаем ent (структуру Entity<T>), содержащую UID и Component.
    if (TryWield(ent, args.User))
        args.Handled = true;
}

// 2. Public API (TryDo)
// Публичный метод принимает Entity<T>, избегая лишних Resolve() внутри.
public bool TryWield(Entity<WieldableComponent> ent, EntityUid user)
{
    // Шаг 1: Проверка (Early Return)
    if (!CanWield(ent, user))
        return false;

    // Шаг 2: Исполнение (Do)
    // Здесь мы уже уверены, что действие валидно.
    var (uid, component) = ent;

    // Логика изменения состояния
    SetWielded(ent, true);

    // Визуальные и звуковые эффекты
    if (component.WieldSound != null)
        _audio.PlayPredicted(component.WieldSound, uid, user);

    // События
    var ev = new ItemWieldedEvent(user);
    RaiseLocalEvent(uid, ref ev);

    // Popup об успехе
    var message = Loc.GetString("wieldable-component-successful-wield", ("item", uid));
    _popup.PopupPredicted(message, user, user);

    return true;
}

// 3. Check (CanDo)
// Чистая функция проверки. Принимает Entity<T>.
public bool CanWield(Entity<WieldableComponent> ent, EntityUid user, bool quiet = false)
{
    var (uid, component) = ent;

    // Проверка 1: Есть ли руки?
    if (!TryComp<HandsComponent>(user, out var hands))
    {
        if (!quiet)
            _popup.PopupClient(Loc.GetString("wieldable-component-no-hands"), user, user);
        return false;
    }

    // Проверка 2: Предмет в руках?
    if (!_hands.IsHolding((user, hands), uid, out _))
    {
        if (!quiet)
            _popup.PopupClient(Loc.GetString("wieldable-component-not-in-hands", ("item", uid)), user, user);
        return false;
    }

    // Проверка 3: Достаточно ли свободных рук?
    if (_hands.CountFreeableHands((user, hands), except: uid) < component.FreeHandsRequired)
    {
        if (!quiet)
            _popup.PopupClient(Loc.GetString("wieldable-component-not-enough-free-hands"), user, user);
        return false;
    }

    return true;
}
```

---

## ❌ Анти-паттерны (Чего избегать)

### Использование `EntityUid` + `Component` вместо `Entity<T>`
Устаревший стиль сигнатур.
*   **Плохо**: `public bool TryAction(EntityUid uid, MyComponent comp, ...)`
*   **Хорошо**: `public bool TryAction(Entity<MyComponent> ent, ...)`

### "Толстый" Event Handler
Вся логика находится внутри `OnEvent`.
*   **Плохо**:
    ```csharp
    private void OnUse(Entity<Comp> ent, ref UseEvent args) {
        if (!Condition) return;
        PerformAction();
    }
    ```

### Side-effects в `CanDo`
Метод `Can` изменяет данные компонента.
*   **Плохо**:
    ```csharp
    public bool CanShoot(Entity<GunComponent> ent) {
        ent.Comp.Ammo--; // ❌ НИКОГДА так не делай в проверке!
        return ent.Comp.Ammo >= 0;
    }
    ```

### "Слепой" `TryDo`
Метод `Try` не вызывает `Can`, а полагается на то, что вызывающий уже всё проверил.

### Возврат строки вместо bool в `CanDo`
*   **Совет**: Используй `out string? reason`, если нужно вернуть причину отказа, но сам метод должен возвращать `bool`.
