using System.Linq;
using Content.IntegrationTests.Tests.Interaction;
using Content.Shared.Radio.Components;

namespace Content.IntegrationTests.Tests._Scp;

public sealed class ScpHeadsetEncryptionKeysTest : InteractionTest
{
    [Test]
    public async Task ScpHeadsetsRejectCommonKeys()
    {
        await SpawnTarget("ClothingHeadsetScientificService");
        var comp = Comp<EncryptionKeyHolderComponent>();

        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("ScientificService"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("ScientificService"));
        });

        await InteractUsing("EncryptionKeyCommon");

        Assert.Multiple(() =>
        {
            Assert.That(comp.KeyContainer.ContainedEntities, Has.Count.EqualTo(1));
            Assert.That(comp.DefaultChannel, Is.EqualTo("ScientificService"));
            Assert.That(comp.Channels, Has.Count.EqualTo(1));
            Assert.That(comp.Channels.First(), Is.EqualTo("ScientificService"));
        });
    }
}
