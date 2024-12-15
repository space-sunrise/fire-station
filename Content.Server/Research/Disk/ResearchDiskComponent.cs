using Content.Shared.Research;
using Robust.Shared.Prototypes;

namespace Content.Server.Research.Disk
{
    [RegisterComponent]
    public sealed partial class ResearchDiskComponent : Component
    {
        [DataField("points"), ViewVariables(VVAccess.ReadWrite)]
        public Dictionary<ProtoId<ResearchPointPrototype>, int> Points = [];

        /// <summary>
        /// If true, the value of this disk will be set to the sum
        /// of all the technologies in the game.
        /// </summary>
        /// <remarks>
        /// This is for debug purposes only.
        /// </remarks>
        [DataField("unlockAllTech")]
        public bool UnlockAllTech = false;
    }
}
