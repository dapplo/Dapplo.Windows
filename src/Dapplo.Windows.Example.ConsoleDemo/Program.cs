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
