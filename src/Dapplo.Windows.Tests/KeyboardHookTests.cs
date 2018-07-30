//  Dapplo - building blocks for desktop applications
//  Copyright (C) 2017-2018  Dapplo
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

        [StaFact]
        private async Task TestKeyHandler_SingleCombination()
        {
            int pressCount = 0;
            var keyHandler = new KeyCombinationHandler(VirtualKeyCode.Back, VirtualKeyCode.RightShift);
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
                new KeyCombinationHandler(VirtualKeyCode.Print),
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA));

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
        private void TestKeyHandler_Repeat()
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
        private void TestKeyHandler_KeyUp_AfterCombination()
        {
            var keyCombinationHandler = new KeyCombinationHandler(VirtualKeyCode.Print);

            var keyPrintDown = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Print,
                IsKeyDown = true
            };

            var keyControlDown = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Control,
                IsKeyDown = true
            };

            var keyControlUp = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Control,
                IsKeyDown = false
            };

            var result = keyCombinationHandler.Handle(keyPrintDown);
            Assert.True(result);
            result = keyCombinationHandler.Handle(keyControlDown);
            Assert.False(result);
            result = keyCombinationHandler.Handle(keyControlUp);
            Assert.False(result);
        }

        [Fact]
        private void TestKeyHandler_Sequence()
        {
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print),
                new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA));

            var keyOneEvent = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Print,
                IsKeyDown = true
            };


            var keyTwoDownEvent = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Control,
                IsKeyDown = true,
                IsModifier = true
            };
            var keyTwoUpEvent = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Control,
                IsKeyDown = false,
                IsModifier = true
            };
            var keyThreeModifierEvent = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.Shift,
                IsKeyDown = true,
                IsModifier = true
            };
            var keyThreeEvent = new KeyboardHookEventArgs
            {
                Key = VirtualKeyCode.KeyA,
                IsKeyDown = true
            };

            var result = sequenceHandler.Handle(keyOneEvent);
            Assert.False(result);
            result = sequenceHandler.Handle(keyTwoDownEvent);
            Assert.False(result);
            result = sequenceHandler.Handle(keyTwoDownEvent);
            Assert.False(result);
            result = sequenceHandler.Handle(keyTwoUpEvent);
            Assert.False(result);
            result = sequenceHandler.Handle(keyThreeModifierEvent);
            Assert.False(result);
            result = sequenceHandler.Handle(keyThreeEvent);
            Assert.True(result);
        }

        [StaFact]
        private async Task TestKeyHandler_SequenceWithOptionalKeys()
        {
            int pressCount = 0;
            var sequenceHandler = new KeySequenceHandler(
                new KeyCombinationHandler(VirtualKeyCode.Print),
                new KeyOrCombinationHandler(
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyA),
                    new KeyCombinationHandler(VirtualKeyCode.Shift, VirtualKeyCode.KeyB))
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