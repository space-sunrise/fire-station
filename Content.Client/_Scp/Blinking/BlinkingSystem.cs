using Content.Shared._Scp.Blinking;
using Robust.Client.Audio;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Blinking;

public sealed class BlinkingSystem : SharedBlinkingSystem
{
    [Dependency] private readonly AudioSystem _audio = default!;
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IOverlayManager _overlayMan = default!;
    [Dependency] private readonly IGameTiming _timing = default!;

    private static readonly SoundSpecifier EyeOpenSound = new SoundCollectionSpecifier("EyeOpen");
    private static readonly SoundSpecifier EyeCloseSound = new SoundCollectionSpecifier("EyeClose");

    private static readonly SoundSpecifier BlinkSound = new SoundPathSpecifier("/Audio/_Scp/Effects/Blinking/blink.ogg");
    private static readonly SoundSpecifier SpawnBlindSound = new SoundCollectionSpecifier("BlinkingSpawnSound", AudioParams.Default.WithVolume(-5));

    private BlinkingOverlay _overlay = default!;
    private const float DefaultAnimationDuration = 0.4f;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BlinkableComponent, LocalPlayerAttachedEvent>(OnAttached);
        SubscribeLocalEvent<BlinkableComponent, LocalPlayerDetachedEvent>(OnDetached);

        SubscribeNetworkEvent<EntityEyesStateChanged>(OnEyesStateChanged);
        SubscribeNetworkEvent<PlayerOpenEyesAnimation>(OnOpenEyesAnimation);

        _overlay = new BlinkingOverlay();

        SetDefaultAnimationDuration();
        _overlay.OnAnimationFinished += SetDefaultAnimationDuration;
    }

    protected override void OnOpenedEyes(Entity<BlinkableComponent> ent, ref EntityOpenedEyesEvent args)
    {
        base.OnOpenedEyes(ent, ref args);

        OpenEyes(ent, args.Manual, args.UseEffects);
    }

    protected override void OnClosedEyes(Entity<BlinkableComponent> ent, ref EntityClosedEyesEvent args)
    {
        base.OnClosedEyes(ent, ref args);

        CloseEyes(ent, args.Manual, args.UseEffects);
    }

    private void OnAttached(Entity<BlinkableComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (_overlayMan.HasOverlay<BlinkingOverlay>())
            return;

        _overlayMan.AddOverlay(_overlay);
    }

    private void OnDetached(Entity<BlinkableComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (!_overlayMan.HasOverlay<BlinkingOverlay>())
            return;

        _overlayMan.RemoveOverlay(_overlay);
    }

    /// <summary>
    /// Метод, обрабатывающий сетевой ивент смены состояния глаз.
    /// Используется для не предугадываемых со стороны клиента изменений состояний глаз, требующих эффектов.
    /// Закрывает или открывает глаза на экране в зависимости от содержимого ивента.
    /// </summary>
    private void OnEyesStateChanged(EntityEyesStateChanged ev)
    {
        if (!ev.NetEntity.HasValue)
            return;

        var ent = GetEntity(ev.NetEntity);

        if (!TryComp<BlinkableComponent>(ent, out var blinkable))
            return;

        if (ev.NewState == EyesState.Closed)
            CloseEyes((ent.Value, blinkable), ev.Manual, ev.UseEffects);
        else
            OpenEyes((ent.Value, blinkable), ev.Manual, ev.UseEffects);
    }

    private void OnOpenEyesAnimation(PlayerOpenEyesAnimation ev)
    {
        var ent = GetEntity(ev.NetEntity);

        if (_player.LocalEntity != ent)
            return;

        _overlay.AnimationDuration = 0.01f;
        _overlay.OnAnimationFinished += AnimationOpenEyes;
        _overlay.CloseEyes();
    }

    /// <summary>
    /// Открывает глаза персонажа, проигрывает специфичный звук открытия глаз.
    /// Сама анимация открытия происходит в оверлее.
    /// </summary>
    /// <param name="ent">Сущность, которой будут открыты глаза</param>
    /// <param name="manual">Глаза открыты вручную?</param>
    /// <param name="useEffects">Требуется ли использовать эффекты?</param>
    private void OpenEyes(Entity<BlinkableComponent> ent, bool manual = false, bool useEffects = false)
    {
        if (!TryEyes(ent, manual, useEffects))
            return;

        if (!_overlay.AreEyesClosed())
            return;

        _overlay.OpenEyes();
        _audio.PlayGlobal(EyeOpenSound, ent);
    }

    /// <summary>
    /// Закрывает глаза персонажа, проигрывает специфичный звук закрытия глаз.
    /// Сама анимация закрытия происходит в оверлее.
    /// </summary>
    /// <param name="ent">Сущность, которой будут закрыты глаза</param>
    /// <param name="manual">Глаза закрыты вручную?</param>
    /// <param name="useEffects">Требуется ли использовать эффекты?</param>
    private void CloseEyes(Entity<BlinkableComponent> ent, bool manual = false, bool useEffects = false)
    {
        if (!TryEyes(ent, manual, useEffects))
            return;

        // Основная проверка, которая определяет наличие эффектов.
        // Если ничего из этого не выполняется, значит эффекты не нужны
        if (!manual && !IsScpNearby(ent) && !useEffects)
            return;

        _overlay.CloseEyes();
        _audio.PlayGlobal(EyeCloseSound, ent);
    }

    /// <summary>
    /// Клиентский метод проверки на возможность включить эффекты смены закрытия или открытия глаз.
    /// Содержит общие одинаковые проверки для закрытия и открытия глаз.
    /// </summary>
    /// <param name="ent">Сущность, для которой делается проверка.</param>
    /// <param name="manual">Глаза открыты/закрыты вручную?</param>
    /// <param name="useEffects">Требуется ли использовать эффекты?</param>
    /// <returns></returns>
    private bool TryEyes(Entity<BlinkableComponent> ent, bool manual = false, bool useEffects = false)
    {
        if (!_timing.IsFirstTimePredicted)
            return false;

        if (_player.LocalEntity != ent)
            return false;

        // Странная конструкция, которая исправляет странную проблему.
        // Почему-то ивенты смены состояния глаз, закрытия, открытия на одного ентити вызываются несколько раз несколько тиков подряд.
        // Это не перекрывается IsFirstTimePredicted, так как все происходит в разных тиках. Как на сервере, так и на клиенте.
        // Причины этого я понять не смог, поэтому сделал этот костыль.
        // Он предотвращает спам звуками и анимацией при смене состояния глаз. Эта логика(пока) используется только на клиенте.
        // В других местах проблем с этим не возникает.
        if (ent.Comp.LastClientSideVisualsAttemptTick >= _timing.CurTick - 1)
        {
            ent.Comp.LastClientSideVisualsAttemptTick = _timing.CurTick;
            return false;
        }

        ent.Comp.LastClientSideVisualsAttemptTick = _timing.CurTick;

        return true;
    }

    private void AnimationOpenEyes()
    {
        _overlay.OnAnimationFinished -= AnimationOpenEyes;

        if (_player.LocalSession == null)
            return;

        _overlay.AnimationDuration = 4f;
        _overlay.OpenEyes();

        _audio.PlayGlobal(SpawnBlindSound, _player.LocalSession);
    }

    private void SetDefaultAnimationDuration()
    {
        _overlay.AnimationDuration = DefaultAnimationDuration;
    }
}
