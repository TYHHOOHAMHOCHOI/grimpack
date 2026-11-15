using System.Collections.Generic;
using NUnit.Framework;
using Project05.UI;
using Project05.UI.Hud;

namespace Project05.Tests.EditMode
{
    /// <summary>Rules behind the Combat screen buttons (design/ux/combat-screen-buttons.md).</summary>
    public class CombatScreenModelTests
    {
        private static CombatScreenModel Playing(float startedAt = 0f)
        {
            var m = new CombatScreenModel();
            Assert.IsTrue(m.RequestStart(startedAt));
            return m;
        }

        [Test]
        public void Start_FromPlanning_EntersPlayback()
        {
            var m = new CombatScreenModel();
            Assert.IsTrue(m.CanStart);
            Assert.IsTrue(m.RequestStart(0f));
            Assert.AreEqual(ScreenPhase.Playback, m.Phase);
        }

        [Test]
        public void Start_WhileItemHeld_IsRefused()
        {
            var m = new CombatScreenModel { ItemHeld = true };
            Assert.IsFalse(m.CanStart);
            Assert.IsFalse(m.RequestStart(0f));
            Assert.AreEqual(ScreenPhase.Planning, m.Phase);
        }

        [Test]
        public void Start_WhileMenuOpen_IsRefused()
        {
            var m = new CombatScreenModel();
            m.OpenMenu();
            Assert.IsFalse(m.RequestStart(0f));
        }

        [Test]
        public void Start_TwiceInPlayback_SecondIsRefused()
        {
            var m = Playing();
            Assert.IsFalse(m.RequestStart(1f));
        }

        [Test]
        public void SelectSpeed_InPlayback_ChangesAndCancelsSkip()
        {
            var m = Playing();
            Assert.IsTrue(m.ArmSkip(1f));
            Assert.IsTrue(m.SelectSpeed(2));
            Assert.AreEqual(2, m.SpeedIndex);
            Assert.IsFalse(m.SkipArmed);
        }

        [Test]
        public void SelectSpeed_InPlanning_IsIgnored()
        {
            var m = new CombatScreenModel();
            Assert.IsFalse(m.SelectSpeed(1));
            Assert.AreEqual(0, m.SpeedIndex);
        }

        [TestCase(-1)]
        [TestCase(3)]
        public void SelectSpeed_OutOfRange_IsRefused(int index)
        {
            var m = Playing();
            Assert.IsFalse(m.SelectSpeed(index));
        }

        [Test]
        public void Skip_InsideInputLock_IsRefusedNotBuffered()
        {
            var m = Playing(10f);
            Assert.IsFalse(m.ArmSkip(10f + CombatScreenModel.InputLock - 0.01f));
            Assert.IsFalse(m.SkipArmed);
            Assert.IsTrue(m.ArmSkip(10f + CombatScreenModel.InputLock));
        }

        [Test]
        public void Skip_WhilePaused_IsRefused()
        {
            var m = Playing();
            m.OpenMenu();
            Assert.IsFalse(m.ArmSkip(5f));
        }

        [Test]
        public void FinishPlayback_ReturnsToPlanning_KeepsSpeed_DisarmsSkip()
        {
            var m = Playing();
            m.SelectSpeed(1);
            m.ArmSkip(1f);
            m.FinishPlayback();
            Assert.AreEqual(ScreenPhase.Planning, m.Phase);
            Assert.AreEqual(1, m.SpeedIndex);
            Assert.IsFalse(m.SkipArmed);
        }

        [Test]
        public void RestoreSpeed_Clamps()
        {
            var m = new CombatScreenModel();
            m.RestoreSpeed(9);
            Assert.AreEqual(2, m.SpeedIndex);
            m.RestoreSpeed(-4);
            Assert.AreEqual(0, m.SpeedIndex);
        }

        [Test]
        public void Esc_WithNothingOpen_OpensMenu_AndPauses()
        {
            var m = new CombatScreenModel();
            Assert.AreEqual(EscResult.OpenedMenu, m.Escape());
            Assert.IsTrue(m.Paused);
        }

        [Test]
        public void Esc_Priority_ConfirmThenSettingsThenMenu()
        {
            var m = new CombatScreenModel();
            m.OpenMenu();
            m.OpenSettings();
            m.AskAbandon();
            Assert.AreEqual(EscResult.ClosedConfirm, m.Escape());
            Assert.AreEqual(EscResult.ClosedSettings, m.Escape());
            Assert.AreEqual(EscResult.ClosedMenu, m.Escape());
            Assert.IsFalse(m.Paused);
        }

        [Test]
        public void Esc_PopupBeforeHeldItemBeforeMenu()
        {
            var m = new CombatScreenModel { PopupOpen = true, ItemHeld = true };
            Assert.AreEqual(EscResult.ClosedPopup, m.Escape());
            m.PopupOpen = false;
            Assert.AreEqual(EscResult.CancelledHeldItem, m.Escape());
            m.ItemHeld = false;
            Assert.AreEqual(EscResult.OpenedMenu, m.Escape());
        }

        [Test]
        public void CloseMenu_AlsoClosesAnythingAboveIt()
        {
            var m = new CombatScreenModel();
            m.OpenMenu();
            m.OpenSettings();
            m.AskAbandon();
            m.CloseMenu();
            Assert.IsFalse(m.Paused);
        }
    }
}
