using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Farhat.Core;
using Farhat.Data;
using Farhat.Story;
using Farhat.Systems;

namespace Farhat.Tests
{
    public class SystemTests
    {
        GameObject host;

        [SetUp]
        public void SetUp()
        {
            EventBusRegistry.ClearAll();
            ServiceLocator.Clear();
            ServiceLocator.Register(new UnlockManager());
            ServiceLocator.Register(new CheckpointStore());
            host = new GameObject("host");
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(host);
            EventBusRegistry.ClearAll();
            ServiceLocator.Clear();
        }

        [UnityTest]
        public IEnumerator Health_PublishesPlayerFailedAtZeroAndRestoresToFull()
        {
            HealthComponent health = host.AddComponent<HealthComponent>();
            FailureType? failed = null;
            EventBus<PlayerFailed>.Subscribe(e => failed = e.Type);
            yield return null;
            DamageInfo big = new DamageInfo();
            big.Amount = 99f;
            health.TakeDamage(big);
            Assert.AreEqual(0f, health.Hearts);
            Assert.AreEqual(FailureType.Health, failed);
            health.RefillAndRevive();
            Assert.AreEqual(health.MaxHearts, health.Hearts);
        }

        [UnityTest]
        public IEnumerator Shadow_CooldownCaptureAndAreaLockBlockAbilities()
        {
            UnlockManager unlocks = ServiceLocator.Get<UnlockManager>();
            unlocks.Grant("shadow.scout");
            ShadowController shadow = host.AddComponent<ShadowController>();
            yield return null;
            Assert.IsTrue(shadow.CanUse);
            Assert.IsTrue(shadow.TryUse(ShadowAbility.Slip));
            Assert.IsFalse(shadow.CanUse, "cooldown running");
            Assert.IsFalse(shadow.TryUse(ShadowAbility.Swap), "swap not unlocked and on cooldown");

            string snapshot = shadow.CaptureSnapshot();
            shadow.RestoreSnapshot(snapshot);
            shadow.SetState(ShadowState.Captured);
            Assert.IsFalse(shadow.CanUse);
            shadow.SetState(ShadowState.Available);
            shadow.LockForArea(60f);
            Assert.IsFalse(shadow.CanUse);
            shadow.RestoreSnapshot(snapshot);
            Assert.IsFalse(shadow.CanUse, "the restored cooldown is still running");
        }

        [UnityTest]
        public IEnumerator Alchemy_CraftsOnlyWhenUnlockedEnabledAndStocked()
        {
            UnlockManager unlocks = ServiceLocator.Get<UnlockManager>();
            PotionRecipe recipe = ScriptableObject.CreateInstance<PotionRecipe>();
            recipe.id = "potion.heal";
            recipe.family = PotionFamily.Heal;
            recipe.requiresUnlock = "potion.heal";
            recipe.magnitude = 1f;
            IngredientAmount herb = new IngredientAmount();
            herb.itemId = "herb.desert";
            herb.count = 2;
            recipe.ingredients.Add(herb);

            AlchemyInventory inventory = host.AddComponent<AlchemyInventory>();
            yield return null;
            inventory.Gather("herb.desert", 2);
            Assert.IsFalse(inventory.TryCraft(recipe), "recipe not unlocked yet");
            unlocks.Grant("potion.heal");
            inventory.SetCraftingEnabled(false);
            Assert.IsFalse(inventory.TryCraft(recipe), "crafting disabled by the story, as in chapter 13");
            inventory.SetCraftingEnabled(true);
            Assert.IsTrue(inventory.TryCraft(recipe));
            Assert.AreEqual(0, inventory.Count("herb.desert"));
            Assert.AreEqual(1, inventory.PotionCount("potion.heal"));

            inventory.Gather("herb.desert", 3);
            inventory.Gather("herb.jungle", 1);
            Assert.AreEqual(2, inventory.Lose(2));
            Assert.AreEqual(1, inventory.Count("herb.desert"));
            Object.DestroyImmediate(recipe);
        }

        [UnityTest]
        public IEnumerator Awareness_FullMeterPublishesStealthFailureOnce()
        {
            AwarenessMeter meter = host.AddComponent<AwarenessMeter>();
            int failures = 0;
            EventBus<PlayerFailed>.Subscribe(e => { if (e.Type == FailureType.Stealth) failures++; });
            float deadline = Time.realtimeSinceStartup + 5f;
            while (meter.Value < 1f && Time.realtimeSinceStartup < deadline)
            {
                meter.ReportPerceived(40f);
                yield return null;
            }
            yield return null;
            Assert.AreEqual(1, failures);
            EventBus<PlayerRestored>.Publish(new PlayerRestored("start", 1));
            meter.RestoreSnapshot(meter.CaptureSnapshot());
            Assert.AreEqual(0f, meter.Value);
        }

        [UnityTest]
        public IEnumerator ObjectiveTimer_ExpiryPublishesObjectiveFailure()
        {
            ObjectiveTimer timer = host.AddComponent<ObjectiveTimer>();
            SerializedFieldSet(timer, "seconds", 0.2f);
            bool failed = false;
            EventBus<PlayerFailed>.Subscribe(e => failed = e.Type == FailureType.Objective);
            timer.StartTimer();
            float deadline = Time.realtimeSinceStartup + 3f;
            while (!failed && Time.realtimeSinceStartup < deadline) yield return null;
            Assert.IsTrue(failed);
        }

        static void SerializedFieldSet(Object target, string field, float value)
        {
            System.Reflection.FieldInfo info = target.GetType().GetField(field, System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            info.SetValue(target, value);
        }
    }
}
