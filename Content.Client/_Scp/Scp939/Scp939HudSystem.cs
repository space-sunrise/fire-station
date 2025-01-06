using System.Diagnostics.CodeAnalysis;
using Content.Client.Overlays;
using Content.Client.SSDIndicator;
using Content.Client.Stealth;
using Content.Shared._Scp.Scp939;
using Content.Shared._Scp.Scp939.Protection;
using Content.Shared.Examine;
using Content.Shared.Movement.Components;
using Content.Shared.Standing;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Throwing;
using Content.Shared.Weapons.Melee.Events;
using Content.Shared.Weapons.Ranged.Events;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Client._Scp.Scp939;

public sealed class Scp939HudSystem : EquipmentHudSystem<Scp939Component>
{
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;

    private ShaderInstance _shaderInstance = default!;

    // TODO: Выделить значения плохого зрения в отдельный компонент, не связанный с 939
    private Scp939Component? _scp939Component;

    private readonly List<ShaderInstance> _shaderInstances = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent((Entity<Scp939VisibilityComponent> ent, ref StartCollideEvent args) => OnCollide(ent, args.OtherEntity));
        SubscribeLocalEvent((Entity<Scp939VisibilityComponent> ent, ref EndCollideEvent args) => OnCollide(ent, args.OtherEntity));

        #region Visibility

        SubscribeLocalEvent<Scp939VisibilityComponent, MoveEvent>(OnMove);

        SubscribeLocalEvent<Scp939VisibilityComponent, ThrowEvent>(OnThrow);
        SubscribeLocalEvent<Scp939VisibilityComponent, StoodEvent>(OnStood);
        SubscribeLocalEvent<Scp939VisibilityComponent, MeleeAttackEvent>(OnMeleeAttack);

        #endregion

        SubscribeLocalEvent<Scp939VisibilityComponent, BeforePostShaderRenderEvent>(BeforeRender);
        SubscribeLocalEvent<Scp939VisibilityComponent, GetStatusIconsEvent>(OnGetStatusIcons, after: new []{typeof(SSDIndicatorSystem)});
        SubscribeLocalEvent<Scp939VisibilityComponent, ExamineAttemptEvent>(OnExamine);

        SubscribeLocalEvent<Scp939Component, PlayerAttachedEvent>(OnPlayerAttached);

        _shaderInstance = _prototypeManager.Index<ShaderPrototype>("Hide").Instance().Duplicate();

        for (int i = 0; i < 100; i++)
        {
            _shaderInstances.Add(_shaderInstance.Duplicate());
        }

        UpdatesAfter.Add(typeof(StealthSystem));
    }

    private void OnExamine(Entity<Scp939VisibilityComponent> ent, ref ExamineAttemptEvent args)
    {
        if (!IsActive)
            return;

        var visibility = GetVisibility(ent);

        if (visibility < 0.2f)
            args.Cancel();
    }

    private void OnGetStatusIcons(Entity<Scp939VisibilityComponent> ent, ref GetStatusIconsEvent args)
    {
        // Олежа чурка
        var playerEntity = _playerManager.LocalSession?.AttachedEntity;

        if (!HasComp<Scp939Component>(playerEntity))
            return;

        var visibility = GetVisibility(ent);

        if (visibility <= 0.5f)
            args.StatusIcons.Clear();
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        var query = EntityQueryEnumerator<Scp939VisibilityComponent, SpriteComponent>();

        while (query.MoveNext(out _, out _, out var spriteComponent))
        {
            spriteComponent.PostShader = null;
        }
    }

    #region Visibility

    private void OnCollide(Entity<Scp939VisibilityComponent> ent, EntityUid otherEntity)
    {
        if (!HasComp<Scp939Component>(otherEntity))
            return;

        MobDidSomething(ent);
    }

    private void OnThrow(Entity<Scp939VisibilityComponent> ent, ref ThrowEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnStood(Entity<Scp939VisibilityComponent> ent, ref StoodEvent args)
    {
        MobDidSomething(ent);
    }

    private void OnMeleeAttack(Entity<Scp939VisibilityComponent> ent, ref MeleeAttackEvent args)
    {
        MobDidSomething(ent);
    }

    private void MobDidSomething(Entity<Scp939VisibilityComponent> ent)
    {
        ent.Comp.VisibilityAcc = 0.001f;
        Dirty(ent);
    }

    private void OnMove(Entity<Scp939VisibilityComponent> ent, ref MoveEvent args)
    {
        // В зависимости от наличие защит или проблем со зрением у 939 изменяется то, насколько хорошо мы видим жертву
        if (ModifyAcc(ent.Comp, out var modifier)) // Если зрение затруднено
        {
            ent.Comp.VisibilityAcc *= modifier;
        }
        else if (HasComp<Scp939ProtectionComponent>(ent)) // Если имеется защита(тихое хождение)
        {
            return;
        }
        else // Если со зрением все ок
        {
            ent.Comp.VisibilityAcc = 0;
        }

        if (!TryComp<MovementSpeedModifierComponent>(ent, out var speedModifierComponent)
            || !TryComp<PhysicsComponent>(ent, out var physicsComponent))
        {
            return;
        }

        var currentVelocity = physicsComponent.LinearVelocity.Length();

        if (speedModifierComponent.BaseWalkSpeed > currentVelocity)
        {
            ent.Comp.VisibilityAcc = ent.Comp.HideTime / 2f;
        }
    }

    #endregion

    private void OnPlayerAttached(Entity<Scp939Component> ent, ref PlayerAttachedEvent args)
    {
        _scp939Component = ent.Comp;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!IsActive)
            return;

        var query = EntityQueryEnumerator<SpriteComponent, Scp939VisibilityComponent>();

        var shaderId = 0;

        while (query.MoveNext(out _, out var spriteComponent, out var visibilityComponent))
        {
            if (_shaderInstances.Count <= shaderId)
            {
                _shaderInstances.Add(_shaderInstance.Duplicate());
            }

            var shader = _shaderInstances[shaderId];

            UpdateVisibility(spriteComponent, shader);

            shaderId++;

            visibilityComponent.VisibilityAcc += frameTime;
        }
    }

    private void UpdateVisibility(SpriteComponent spriteComponent, ShaderInstance shader)
    {
        spriteComponent.Color = Color.White;
        spriteComponent.GetScreenTexture = true;
        spriteComponent.RaiseShaderEvent = true;

        spriteComponent.PostShader = shader;
    }

    private void BeforeRender(Entity<Scp939VisibilityComponent> ent, ref BeforePostShaderRenderEvent args)
    {
        var visibility = GetVisibility(ent);
        args.Sprite.PostShader?.SetParameter("visibility", visibility);
    }

    private float GetVisibility(Entity<Scp939VisibilityComponent> ent)
    {
        var acc = ent.Comp.VisibilityAcc;

        if (acc > ent.Comp.HideTime)
            return 0;

        return Math.Clamp(1f - (acc / ent.Comp.HideTime), 0f, 1f);
    }

    // TODO: Переделать под статус эффект и добавить его в панель статус эффектов, а то непонятно игруну
    /// <summary>
    /// Если вдруг собачка плохо видит
    /// </summary>
    private bool ModifyAcc(Scp939VisibilityComponent visibilityComponent, [NotNullWhen(true)] out int modifier)
    {
        // 1 = отсутствие модификатора
        modifier = 1;

        if (_scp939Component == null)
            return false;

        if (!_scp939Component.PoorEyesight)
            return false;

        modifier = _random.Next(visibilityComponent.MinValue, visibilityComponent.MaxValue);

        return true;
    }
}
