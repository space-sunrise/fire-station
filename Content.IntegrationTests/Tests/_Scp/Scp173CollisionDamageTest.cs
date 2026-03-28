using Content.Shared._Scp.Other.DamageOnCollide;
using Content.Shared.Damage.Components;
using Content.Shared.FixedPoint;
using Robust.Shared.GameObjects;

namespace Content.IntegrationTests.Tests._Scp;

[TestFixture]
public sealed class Scp173CollisionDamageTest
{
    private const string StorageTargetId = "Scp173CollisionStorageTarget";
    private const string VendingTargetId = "Scp173CollisionVendingTarget";
    private const string NeutralTargetId = "Scp173CollisionNeutralTarget";
    private const string TestInventoryId = "Scp173CollisionTestInventory";

    [TestPrototypes]
    private const string Prototypes = $@"
- type: entity
  id: {StorageTargetId}
  name: storage target
  components:
  - type: EntityStorage
  - type: Damageable
    damageContainer: Inorganic

- type: vendingMachineInventory
  id: {TestInventoryId}
  startingInventory:
    Crowbar: 1

- type: entity
  id: {VendingTargetId}
  parent: VendingMachine
  components:
  - type: VendingMachine
    pack: {TestInventoryId}
    ejectDelay: 0
  - type: Sprite
    sprite: error.rsi

- type: entity
  id: {NeutralTargetId}
  name: neutral target
  components:
  - type: Damageable
    damageContainer: Inorganic
";

    [Test]
    public async Task Scp173CollisionDamagesEntityStorageAndVendingMachines()
    {
        await using var pair = await PoolManager.GetServerClient();
        var server = pair.Server;
        var entMan = server.EntMan;
        var damageOnCollide = server.System<ScpDamageOnCollideSystem>();
        var map = await pair.CreateTestMap();

        EntityUid scp = default;
        EntityUid storageTarget = default;
        EntityUid vendingTarget = default;
        EntityUid neutralTarget = default;

        await server.WaitPost(() =>
        {
            scp = entMan.SpawnEntity("Scp173", map.GridCoords);
            storageTarget = entMan.SpawnEntity(StorageTargetId, map.GridCoords);
            vendingTarget = entMan.SpawnEntity(VendingTargetId, map.GridCoords);
            neutralTarget = entMan.SpawnEntity(NeutralTargetId, map.GridCoords);
        });

        await server.WaitAssertion(() =>
        {
            var storageDamageable = entMan.GetComponent<DamageableComponent>(storageTarget);
            var vendingDamageable = entMan.GetComponent<DamageableComponent>(vendingTarget);
            var neutralDamageable = entMan.GetComponent<DamageableComponent>(neutralTarget);

            Assert.Multiple(() =>
            {
                Assert.That(storageDamageable.Damage.GetTotal(), Is.EqualTo(FixedPoint2.Zero));
                Assert.That(vendingDamageable.Damage.GetTotal(), Is.EqualTo(FixedPoint2.Zero));
                Assert.That(neutralDamageable.Damage.GetTotal(), Is.EqualTo(FixedPoint2.Zero));
            });

            Assert.That(damageOnCollide.TryApplyDamage(scp, storageTarget, requireVelocity: false), Is.True);
            Assert.That(damageOnCollide.TryApplyDamage(scp, vendingTarget, requireVelocity: false), Is.True);
            Assert.That(damageOnCollide.TryApplyDamage(scp, neutralTarget, requireVelocity: false), Is.False);

            Assert.Multiple(() =>
            {
                Assert.That(storageDamageable.Damage.GetTotal(), Is.GreaterThan(FixedPoint2.Zero));
                Assert.That(vendingDamageable.Damage.GetTotal(), Is.GreaterThan(FixedPoint2.Zero));
                Assert.That(neutralDamageable.Damage.GetTotal(), Is.EqualTo(FixedPoint2.Zero));
            });
        });

        await pair.CleanReturnAsync();
    }
}
