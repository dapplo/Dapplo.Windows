//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2019  Dapplo
// 
//  For more information see: http://dapplo.net/
//  Dapplo repositories are hosted on GitHub: https://github.com/dapplo
// 
//  This file is part of Dapplo.Windows
// 
//  Dapplo.Windows is free software: you can redistribute it and/or modify
//  it under the terms of the GNU Lesser General Public License as published by
//  the Free Software Foundation, either version 3 of the License, or
//  (at your option) any later version.
// 
//  Dapplo.Windows is distributed in the hope that it will be useful,
//  but WITHOUT ANY WARRANTY; without even the implied warranty of
//  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//  GNU Lesser General Public License for more details.
// 
//  You should have a copy of the GNU Lesser General Public License
//  along with Dapplo.Windows. If not, see <http://www.gnu.org/licenses/lgpl.txt>.

#region using
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Dapplo.Log;
using Dapplo.Log.XUnit;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Xunit;
using Xunit.Abstractions;

#endregion

namespace Dapplo.Windows.Tests
{
    public class KeyboardHookTests
    {
        public KeyboardHookTests(ITestOutputHelper testOutputHelper)
        {
            LogSettings.RegisterDefaultLogger<XUnitLogger>(LogLevels.Verbose, testOutputHelper);
        }

        private static KeyboardHookEventArgs KeyDown(VirtualKeyCode virtualKeyCode)
        {
            return new KeyboardHookEventArgs
            {
                Key = virtualKeyCode,
                IsKeyDown = true,
                IsModifier = virtualKeyCode.IsModifier()
            };
        }

        private static KeyboardHookEventArgs KeyUp(VirtualKeyCode virtualKeyCode)
        {
            return new KeyboardHookEventArgs
            {
                Key = virtualKeyCode,
                IsKeyDown = false,
                IsModifier = virtualKeyCode.IsModifier()
            };
        }

        [StaFact]
        private async Task TestKeyHandler_SingleCombination()
        {
            int pressCount = 0;
            var keyHandler = new KeyCombinationHandler(VirtualKeyCode.Back, VirtualKeyCode.RightShift) { IgnoreInjected = false };
            using (KeyboardHook.KeyboardEvents.Where(keyHandler).Subscribe(keyboardHookEventArgs => pressCount++))
            {
                await Task.Delay(20);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
                await Task.Delay(20);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
                await Task.Delay(20);
            }
            Assert.True(pressCount == 2);
            KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
            await Task.Delay(20);
            Assert.True(pressCount == 2);
        }

        [StaFact]
        private async Task TestKeyHandler_Sequence_InputGenerator()
        {
            int pressCount = 0;
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print) { IgnoreInjected = false },
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false });


            using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).Subscribe(keyboardHookEventArgs => pressCount++))
            {
                await Task.Delay(20);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
                await Task.Delay(20);
                Assert.True(pressCount == 0);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyB);
                await Task.Delay(20);
                Assert.True(pressCount == 0);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
                await Task.Delay(20);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
                await Task.Delay(20);
                Assert.True(pressCount == 1);
            }
        }

        [Fact]
        private void TestKeyHandler_KeySequenceHandler_Wrong_Right()
        {
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print),
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA))
            {
                // Prevent debug issues, we are not testing the timeout here!
                Timeout = null
            };

            var result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Print));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Shift));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.KeyB));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.KeyB));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Shift));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Print));
            Assert.False(result);

            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Shift));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.KeyA));
            Assert.True(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.KeyA));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Shift));
            Assert.False(result);
            Assert.False(sequenceHandler.HasKeysPressed);
        }

        [Fact]
        private void TestKeyHandler_KeySequenceHandler_Right()
        {
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print),
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA))
            {
                Timeout = null
            };

            var result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Print));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Print));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Control));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Control));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Control));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Shift));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.KeyA));
            Assert.True(result);
        }

        [Fact]
        private void TestKeyHandler_KeyCombinationHandler_Repeat()
        {
            var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print);

            var keyPrintDown = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Print,
                IsKeyDown = true
            };

            var keyPrintUp = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Print,
                IsKeyDown = false
            };

            var result = keyCombinationHandler.Handle(keyPrintDown);
            Assert.True(result);
            result = keyCombinationHandler.Handle(keyPrintDown);
            Assert.False(result);

            // Key up again
            result = keyCombinationHandler.Handle(keyPrintUp);
            Assert.False(result);
            result = keyCombinationHandler.Handle(keyPrintDown);
            Assert.True(result);
        }

        /// <summary>
        /// Test that after a key down, having a not matching key down & up we should not have a "hit"
        /// </summary>
        [Fact]
        private void TestKeyHandler_KeyCombinationHandler_KeyUp()
        {
            var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print);

            var result = keyCombinationHandler.Handle(KeyDown(VirtualKeyCode.Print));
            Assert.True(result);
            result = keyCombinationHandler.Handle(KeyDown(VirtualKeyCode.Control));
            Assert.False(result);
            result = keyCombinationHandler.Handle(KeyUp(VirtualKeyCode.Control));
            Assert.False(result);
        }

        [StaFact]
        private async Task TestKeyHandler_SequenceWithOptionalKeys_KeyboardInputGenerator()
        {
            int pressCount = 0;
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print) {IgnoreInjected = false},
                new KeyOrCombinationHandler(
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA) { IgnoreInjected = false },
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB) { IgnoreInjected = false })
            )
            {
                // Timeout for test
                Timeout = TimeSpan.FromMilliseconds(200)
            };

            using (KeyboardHook.KeyboardEvents.Where(sequenceHandler).Subscribe(keyboardHookEventArgs => pressCount++))
            {
                await Task.Delay(20);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
                await Task.Delay(20);
                Assert.Equal(0, pressCount);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyB);
                await Task.Delay(20);
                Assert.Equal(1, pressCount);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
                await Task.Delay(20);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
                await Task.Delay(20);
                Assert.Equal(2, pressCount);

                // Test with timeout, waiting to long
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Print);
                await Task.Delay(400);
                KeyboardInputGenerator.KeyCombinationPress(VirtualKeyCode.Shift, VirtualKeyCode.KeyA);
                await Task.Delay(20);
                Assert.Equal(2, pressCount);
            }
        }

        [Fact]
        private async Task TestKeyHandler_SequenceWithOptionalKeys_Timeout()
        {
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print),
                new KeyOrCombinationHandler(
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA),
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB))
            )
            {
                Timeout = TimeSpan.FromMilliseconds(200)
            };

            bool result;
            // Print key
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Print));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Print));
            Assert.False(result);

            // Shift KeyB
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Shift));
            Assert.True(sequenceHandler.HasKeysPressed);
            Assert.False(result);
            await Task.Delay(400);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.KeyB));
            Assert.True(sequenceHandler.HasKeysPressed);
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.KeyB));
            Assert.True(sequenceHandler.HasKeysPressed);
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Shift));
            Assert.False(sequenceHandler.HasKeysPressed);
            Assert.False(result);
        }

        [Fact]
        private void TestKeyHandler_SequenceWithOptionalKeys_OneTry()
        {
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print),
                new KeyOrCombinationHandler(
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA),
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB))
            )
            {
                Timeout = null
            };

            bool result;

            // Print key
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Print));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Print));
            Assert.False(result);

            // Shift KeyB
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.Shift));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyDown(VirtualKeyCode.KeyB));
            Assert.True(result);
            // Shift KeyB
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.KeyB));
            Assert.False(result);
            result = sequenceHandler.Handle(KeyUp(VirtualKeyCode.Shift));
            Assert.False(result);
        }

        [Fact]
        private void TestKeyHelper_VirtualKeyCodesFromString()
        {
            const string testKeys = "ctrl + shift + A";
            var virtualKeyCodes = KeyHelper.VirtualKeyCodesFromString(testKeys).ToList();
            Assert.NotEmpty(virtualKeyCodes);
            Assert.Contains(VirtualKeyCode.Shift,virtualKeyCodes);
            Assert.Contains(VirtualKeyCode.Control, virtualKeyCodes);
            Assert.Contains(VirtualKeyCode.KeyA, virtualKeyCodes);
        }

        [Fact]
        private void TestKeyHelper_VirtualCodeToLocaleDisplayText()
        {
            var keyCombination = string.Join(" + ", new[] {VirtualKeyCode.LeftShift, VirtualKeyCode.KeyA}.Select(vk => KeyHelper.VirtualCodeToLocaleDisplayText(vk, false)));

            Assert.NotEmpty(keyCombination);
            Assert.Contains("+ A", keyCombination);
        }

        //[StaFact]
        private async Task TestHandlingKeyAsync()
        {
            await KeyboardHook.KeyboardEvents.Where(args => args.IsWindows && args.IsShift && args.IsControl && args.IsAlt)
                .Select(args =>
                {
                    args.Handled = true;
                    return args;
                })
                .FirstAsync();
        }

        //[StaFact]
        private async Task TestMappingAsync()
        {
            await KeyboardHook.KeyboardEvents.FirstAsync(info => info.IsLeftShift && info.IsKeyDown);
        }

        //[StaFact]
        private async Task TestSuppressVolumeAsync()
        {
            await KeyboardHook.KeyboardEvents.Where(args =>
                {
                    if (args.Key != VirtualKeyCode.VolumeUp)
                    {
                        return true;
                    }
                    args.Handled = true;
                    return false;
                })
                .FirstAsync();
        }
    }
}