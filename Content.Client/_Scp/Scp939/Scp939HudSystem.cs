using System.Linq;
using Content.Client.Overlays;
using Content.Client.SSDIndicator;
using Content.Client.Stealth;
using Content.Shared._Scp.Scp939;
using Content.Shared.Examine;
using Content.Shared.Movement.Components;
using Content.Shared.StatusIcon.Components;
using Content.Shared.Verbs;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Client._Scp.Scp939;

public sealed class Scp939HudSystem : EquipmentHudSystem<Scp939Component>
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IPrototypeManager _prototypeManager = default!;

    private ShaderInstance _shaderInstance = default!;

    private List<ShaderInstance> _shaderInstances = new();

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<Scp939VisibilityComponent, MoveEvent>(OnMove);
        SubscribeLocalEvent<Scp939VisibilityComponent, BeforePostShaderRenderEvent>(BeforeRender);
        SubscribeLocalEvent<Scp939VisibilityComponent, GetStatusIconsEvent>(OnGetStatusIcons, after: new []{typeof(SSDIndicatorSystem)});
        SubscribeLocalEvent<Scp939VisibilityComponent, ExamineAttemptEvent>(OnExamine);

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
        {
            return;
        }

        var visibility = GetVisibility(ent);

        if (visibility < 0.2f)
        {
            args.Cancel();
        }
    }

    private void OnGetStatusIcons(Entity<Scp939VisibilityComponent> ent, ref GetStatusIconsEvent args)
    {
        var visibility = GetVisibility(ent);

        if (visibility <= 0.5f)
        {
            args.StatusIcons.Clear();
        }
    }

    protected override void DeactivateInternal()
    {
        base.DeactivateInternal();

        var query = EntityQueryEnumerator<Scp939VisibilityComponent, SpriteComponent>();

        while (query.MoveNext(out var entityUid, out var visibilityComponent, out var spriteComponent))
        {
            spriteComponent.PostShader = null;
        }
    }

    private void OnMove(Entity<Scp939VisibilityComponent> ent, ref MoveEvent args)
    {
        ent.Comp.VisibilityAcc = 0f;

        if (!TryComp<MovementSpeedModifierComponent>(ent, out var speedModifierComponent)
            || !TryComp<PhysicsComponent>(ent, out var physicsComponent))
        {
            return;
        }

        var currentVelocity = physicsComponent.LinearVelocity.Length();

        if(speedModifierComponent.BaseWalkSpeed > currentVelocity)
        {
            ent.Comp.VisibilityAcc = ent.Comp.HideTime / 2f;
        }
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!IsActive)
        {
            return;
        }

        var query = EntityQueryEnumerator<SpriteComponent, Scp939VisibilityComponent>();

        var shaderId = 0;

        while (query.MoveNext(out var entityUid, out var spriteComponent, out var visibilityComponent))
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
        {
            return 0;
        }

        return Math.Clamp(1f - (acc / ent.Comp.HideTime), 0f, 1f);
    }
}
