using Content.Client.Power.APC.UI;
using Content.Shared.Access.Systems;
using Content.Shared.APC;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client.Power.APC
{
    [UsedImplicitly]
    public sealed class ApcBoundUserInterface : BoundUserInterface
    {
        [ViewVariables]
        private ApcMenu? _menu;

        public ApcBoundUserInterface(EntityUid owner, Enum uiKey) : base(owner, uiKey)
        {
        }

        protected override void Open()
        {
            base.Open();
            _menu = this.CreateWindow<ApcMenu>();
            _menu.SetEntity(Owner);
            _menu.OnBreaker += BreakerPressed;
            var playerManager = IoCManager.Resolve<IPlayerManager>();

            var hasAccess = false;
            if (playerManager.LocalEntity != null)
            {
                var accessReader = EntMan.System<AccessReaderSystem>();
                hasAccess = accessReader.IsAllowed((EntityUid)playerManager.LocalEntity, Owner);
            }
            _menu?.SetAccessEnabled(hasAccess);
        }

        protected override void UpdateState(BoundUserInterfaceState state)
        {
            base.UpdateState(state);

            var castState = (ApcBoundInterfaceState) state;
            _menu?.UpdateState(castState);
        }

        public void BreakerPressed()
        {
            SendMessage(new ApcToggleMainBreakerMessage());
        }
    }
}
