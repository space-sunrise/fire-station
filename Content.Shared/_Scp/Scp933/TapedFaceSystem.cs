using Content.Shared.Humanoid;

namespace Content.Shared._Scp.Scp933;

/// <summary>
/// Система для применения скотча на лицо.
/// Используется при взаимодействии с человеком - скрывает глаза и морду.
/// </summary>
public sealed class TapedFaceSystem : EntitySystem
{
    [Dependency] private readonly SharedHumanoidAppearanceSystem _humanoid = default!;

    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<TapedFaceComponent, ComponentStartup>(OnTapedStartup);
        SubscribeLocalEvent<TapedFaceComponent, ComponentShutdown>(OnTapedShutdown);
    }

    /// <summary>
    /// При добавлении компонента - скрыть слои лица.
    /// </summary>
    private void OnTapedStartup(EntityUid uid, TapedFaceComponent component, ComponentStartup args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out _))
            return;

        _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Eyes, false);
        _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Snout, false);
    }

    /// <summary>
    /// При удалении компонента - показать слои лица.
    /// </summary>
    private void OnTapedShutdown(EntityUid uid, TapedFaceComponent component, ComponentShutdown args)
    {
        if (!TryComp<HumanoidAppearanceComponent>(uid, out _))
            return;

        _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Eyes, true);
        _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Snout, true);
    }

    /// <summary>
    /// Применить скотч на цель.
    /// </summary>
    public void ApplyTape(EntityUid target)
    {
        // Проверить что цель имеет humanoid appearance
        if (!TryComp<HumanoidAppearanceComponent>(target, out _))
            return;

        // Проверить что цель еще не обмотана
        if (HasComp<TapedFaceComponent>(target))
            return;

        // Добавить компонент
        AddComp(target, new TapedFaceComponent());
    }

    /// <summary>
    /// Снять скотч с цели.
    /// </summary>
    public void RemoveTape(EntityUid target)
    {
        RemComp<TapedFaceComponent>(target);
    }
}
