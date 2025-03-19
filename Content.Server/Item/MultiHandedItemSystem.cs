using Content.Server.Inventory;
using Content.Server.Wieldable;
using Content.Shared.Hands;
using Content.Shared.Item;
using Content.Shared.Wieldable;
using Content.Shared.Wieldable.Components;

namespace Content.Server.Item;

public sealed class MultiHandedItemSystem : SharedMultiHandedItemSystem
{
    [Dependency] private readonly VirtualItemSystem _virtualItem = default!;
    [Dependency] private readonly WieldableSystem _wieldable = default!; // Fire

    protected override void OnEquipped(EntityUid uid, MultiHandedItemComponent component, GotEquippedHandEvent args)
    {
        // Fire edit start - чтобы спрайт брался в 2 руки мне пришлось сделать это, суки. Почему раньше не придумали
        if (TryComp<WieldableComponent>(uid, out var wieldableComponent) && component.HandsNeeded == 2)
        {
            _wieldable.TryWield(uid, wieldableComponent, args.User);
        }
        else
        {
            for (var i = 0; i < component.HandsNeeded - 1; i++)
            {
                _virtualItem.TrySpawnVirtualItemInHand(uid, args.User);
            }
        }
        // Fire edit end
    }

    protected override void OnUnequipped(EntityUid uid, MultiHandedItemComponent component, GotUnequippedHandEvent args)
    {
        // Fire added start - система двуруча сама все сделает
        if (HasComp<WieldableComponent>(uid) && component.HandsNeeded == 2)
            return;
        // Fire added end

        _virtualItem.DeleteInHandsMatching(args.User, uid);
    }
}
