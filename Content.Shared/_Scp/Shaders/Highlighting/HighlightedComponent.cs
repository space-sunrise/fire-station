using Robust.Shared.GameStates;

namespace Content.Shared._Scp.Shaders.Highlighting;

/// <summary>
/// Компонент-маркер сущностей, которые будут подсвечиваться.
/// Добавление компонента заставляет спрайт использовать шейдер подсвечивания.
/// Удаление убирает подсвечивание
/// </summary>
/// TODO: Кеширование предыдущего шейдера сущности и возвращение после удаления подсвечивания.
[RegisterComponent, NetworkedComponent]
public sealed partial class HighlightedComponent : Component;
