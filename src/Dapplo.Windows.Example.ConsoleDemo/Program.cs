// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
using System;
using Dapplo.Windows.Input.Enums;
using Dapplo.Windows.Input.Keyboard;
using Dapplo.Windows.Messages;

namespace Dapplo.Windows.Example.ConsoleDemo
{
    internal static class Program
    {
        [STAThread]
        private static void Main(string[] args)
        {
            var key = new KeyCombinationHandler(VirtualKeyCode.KeyA);
            using (KeyboardHook.KeyboardEvents.Where(key).Subscribe(e => Hit()))
            {
                MessageLoop.ProcessMessages();
            }
        }

        private static void Hit()
        {
            Console.WriteLine("Hit");
        }
    }
}
