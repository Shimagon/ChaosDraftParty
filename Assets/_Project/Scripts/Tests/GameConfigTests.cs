using NUnit.Framework;
using ChaosDraftParty.Core.Configs;
using UnityEngine;

namespace ChaosDraftParty.Tests
{
    /// <summary>
    /// GameConfig の凹関数スタック倍率が仕様通りか検証する EditMode テスト。
    /// Spec v9.5: 1.0 -> 1.20 -> 1.35 -> 1.45 (4+ で頭打ち)
    /// </summary>
    public class GameConfigTests
    {
        [Test]
        public void StackMultiplier_Zero_Returns100()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            Assert.That(c.GetStackMultiplier(0), Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void StackMultiplier_One_Returns100()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            Assert.That(c.GetStackMultiplier(1), Is.EqualTo(1.0f).Within(0.001f));
        }

        [Test]
        public void StackMultiplier_Two_Returns120()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            Assert.That(c.GetStackMultiplier(2), Is.EqualTo(1.20f).Within(0.001f));
        }

        [Test]
        public void StackMultiplier_Three_Returns135()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            Assert.That(c.GetStackMultiplier(3), Is.EqualTo(1.35f).Within(0.001f));
        }

        [Test]
        public void StackMultiplier_FourOrMore_CappedAt145()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            Assert.That(c.GetStackMultiplier(4), Is.EqualTo(1.45f).Within(0.001f));
            Assert.That(c.GetStackMultiplier(5), Is.EqualTo(1.45f).Within(0.001f));
            Assert.That(c.GetStackMultiplier(100), Is.EqualTo(1.45f).Within(0.001f));
        }

        [Test]
        public void GravityAcceleration_DefaultMultiplier_Returns9_81()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            Assert.That(c.GravityAcceleration, Is.EqualTo(-9.81f).Within(0.001f));
        }

        [Test]
        public void GravityAcceleration_LowGravity_Returns70Percent()
        {
            var c = ScriptableObject.CreateInstance<GameConfig>();
            c.gravityMultiplier = 0.7f;
            Assert.That(c.GravityAcceleration, Is.EqualTo(-6.867f).Within(0.001f));
        }
    }
}
