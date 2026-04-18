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
        if (!TryComp<Scp933TargetComponent>(uid, out var targetComp) || !targetComp.CanWearTape)
            return;

        // Получаем настройки визуальных эффектов или используем дефолт
        if (!TryComp<Scp933VisualEffectsComponent>(uid, out var visualComp))
        {
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Eyes, false);
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Snout, false);
            return;
        }

        if (visualComp.HideEyes)
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Eyes, false);
        if (visualComp.HideSnout)
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Snout, false);
    }

    /// <summary>
    /// При удалении компонента - показать слои лица.
    /// </summary>
    private void OnTapedShutdown(EntityUid uid, TapedFaceComponent component, ComponentShutdown args)
    {
        if (!TryComp<Scp933TargetComponent>(uid, out var targetComp) || !targetComp.CanWearTape)
            return;

        if (HasComp<Scp933FaceTornComponent>(uid) || HasComp<Scp933MasterComponent>(uid))
            return;

        // Получаем настройки визуальных эффектов или используем дефолт
        if (!TryComp<Scp933VisualEffectsComponent>(uid, out var visualComp))
        {
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Eyes, true);
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Snout, true);
            return;
        }

        if (visualComp.HideEyes)
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Eyes, true);
        if (visualComp.HideSnout)
            _humanoid.SetLayerVisibility(uid, HumanoidVisualLayers.Snout, true);
    }

    /// <summary>
    /// Применить скотч на цель.
    /// </summary>
    public void ApplyTape(EntityUid target)
    {
        // Проверить что цель является валидной целью SCP-933
        if (!TryComp<Scp933TargetComponent>(target, out var targetComp) || !targetComp.CanWearTape)
            return;

        // Проверить что цель еще не обмотана
        if (HasComp<TapedFaceComponent>(target))
            return;

        EnsureComp<TapedFaceComponent>(target);
    }
}
