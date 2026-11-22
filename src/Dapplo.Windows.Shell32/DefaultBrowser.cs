// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using Dapplo.Windows.Shell32.Enums;

namespace Dapplo.Windows.Shell32;

/// <summary>
///     Starts the default browser, the application which handles the https protocol, for an address or a local page.
/// </summary>
/// <remarks>
///     Opening a local .html file with ShellExecute starts the application of the .html association, which can be an editor.
///     This reads the command of the https protocol (AssocQueryString with ASSOCF_IS_PROTOCOL, verb "open") and starts it with the target,
///     a local path is passed as file:/// address.
/// </remarks>
public static class DefaultBrowser
{
    /// <summary>
    ///     The switch with which Chromium based browsers (Edge, Chrome) register their command: <c>--single-argument %1</c>.
    ///     Everything after it is taken literally, quotes included, so the target must not be quoted there.
    /// </summary>
    public const string SingleArgumentSwitch = "--single-argument";

    /// <summary>
    ///     The command line with which the default browser opens an address, the "open" command of the https protocol,
    ///     e.g. <c>"C:\Program Files (x86)\Microsoft\Edge\Application\msedge.exe" --single-argument %1</c>
    /// </summary>
    /// <returns>string, null when there is no association for https</returns>
    public static string GetCommand() => ShlwapiApi.GetAssociationString(AssocStr.Command, "https", "open", AssocF.IsProtocol);

    /// <summary>
    ///     Opens the target in the default browser, see <see cref="CreateStartInfo(string)"/>
    /// </summary>
    /// <param name="target">string with an address (https://...) or a rooted local path</param>
    /// <exception cref="ArgumentException">the target is null or empty</exception>
    /// <exception cref="System.ComponentModel.Win32Exception">the browser couldn't be started</exception>
    public static void Open(string target)
    {
        using var process = Process.Start(CreateStartInfo(target));
    }

    /// <summary>
    ///     Creates the ProcessStartInfo which opens the target in the default browser:
    ///     the command of the https protocol with the target (a rooted local path becomes a file:/// address).
    ///     When there is no command, or it can't be parsed, the target is opened with ShellExecute.
    /// </summary>
    /// <param name="target">string with an address (https://...) or a rooted local path</param>
    /// <returns>ProcessStartInfo</returns>
    /// <exception cref="ArgumentException">the target is null or empty</exception>
    public static ProcessStartInfo CreateStartInfo(string target)
    {
        if (string.IsNullOrEmpty(target))
        {
            throw new ArgumentException("The target must not be empty.", nameof(target));
        }
        return CreateStartInfo(GetCommand(), target) ?? new ProcessStartInfo(target)
        {
            UseShellExecute = true
        };
    }

    /// <summary>
    ///     Creates the ProcessStartInfo for a browser command (as registered for a protocol) and a target.
    ///     The program is the quoted part of the command, or the part up to the first space, the rest are the arguments, in which:
    ///     <list type="bullet">
    ///         <item><description><c>"%1"</c> and <c>%1</c> (and <c>%L</c>) are replaced with the quoted target,</description></item>
    ///         <item><description>after <see cref="SingleArgumentSwitch"/> they are replaced with the target without quotes, as everything after that switch is taken literally,</description></item>
    ///         <item><description>without a placeholder the quoted target is appended.</description></item>
    ///     </list>
    ///     A rooted local path is passed as file:/// address, see <see cref="ToAddress"/>.
    /// </summary>
    /// <param name="command">string with the command, e.g. <c>"C:\...\firefox.exe" -osint -url "%1"</c></param>
    /// <param name="target">string with an address or a rooted local path</param>
    /// <returns>ProcessStartInfo which starts the program directly, null when the command is empty or has an unterminated quote</returns>
    public static ProcessStartInfo CreateStartInfo(string command, string target)
    {
        if (string.IsNullOrEmpty(target) || !TrySplitCommand(command, out var program, out var arguments))
        {
            return null;
        }
        return new ProcessStartInfo(program, ReplacePlaceholders(arguments, ToAddress(target)))
        {
            UseShellExecute = false
        };
    }

    /// <summary>
    ///     Returns the address to pass to a browser: a rooted local path (C:\..., \\server\share\...) becomes a file:/// address,
    ///     e.g. <c>C:\Temp\my page.html</c> becomes <c>file:///C:/Temp/my%20page.html</c>, everything else is returned unchanged.
    /// </summary>
    /// <param name="target">string with an address or a path</param>
    /// <returns>string with the address</returns>
    public static string ToAddress(string target)
    {
        if (string.IsNullOrEmpty(target) || target.IndexOf("://", StringComparison.Ordinal) > 0)
        {
            return target;
        }
        try
        {
            if (Path.IsPathRooted(target) && Uri.TryCreate(Path.GetFullPath(target), UriKind.Absolute, out var uri) && uri.IsFile)
            {
                return uri.AbsoluteUri;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException or System.Security.SecurityException)
        {
            // Not a valid path, pass it as it is
        }
        return target;
    }

    /// <summary>
    ///     Splits a command line into the program and the arguments
    /// </summary>
    private static bool TrySplitCommand(string command, out string program, out string arguments)
    {
        program = null;
        arguments = null;
        if (string.IsNullOrWhiteSpace(command))
        {
            return false;
        }
        command = command.Trim();
        int argumentsStart;
        if (command[0] == '"')
        {
            int closingQuote = command.IndexOf('"', 1);
            if (closingQuote < 0)
            {
                return false;
            }
            program = command.Substring(1, closingQuote - 1);
            argumentsStart = closingQuote + 1;
        }
        else
        {
            int space = command.IndexOf(' ');
            program = space < 0 ? command : command.Substring(0, space);
            argumentsStart = space < 0 ? command.Length : space + 1;
        }
        if (program.Length == 0)
        {
            return false;
        }
        program = Environment.ExpandEnvironmentVariables(program);
        arguments = command.Substring(argumentsStart).Trim();
        return true;
    }

    /// <summary>
    ///     Replaces the %1 placeholders in one pass, so a %1 in the target (e.g. an encoded character) isn't replaced again
    /// </summary>
    private static string ReplacePlaceholders(string arguments, string target)
    {
        var quotedTarget = QuoteArgument(target);
        var result = new StringBuilder(arguments.Length + target.Length + 3);
        bool literal = false;
        bool replaced = false;
        int index = 0;
        while (index < arguments.Length)
        {
            if (!literal && IsSwitchAt(arguments, index))
            {
                literal = true;
                result.Append(SingleArgumentSwitch);
                index += SingleArgumentSwitch.Length;
                continue;
            }
            if (IsPlaceholderAt(arguments, index, true))
            {
                result.Append(literal ? target : quotedTarget);
                index += 4;
                replaced = true;
                continue;
            }
            if (IsPlaceholderAt(arguments, index, false))
            {
                result.Append(literal ? target : quotedTarget);
                index += 2;
                replaced = true;
                continue;
            }
            result.Append(arguments[index]);
            index++;
        }
        if (!replaced)
        {
            if (result.Length > 0)
            {
                result.Append(' ');
            }
            result.Append(literal ? target : quotedTarget);
        }
        return result.ToString();
    }

    private static bool IsSwitchAt(string arguments, int index) =>
        string.CompareOrdinal(arguments, index, SingleArgumentSwitch, 0, SingleArgumentSwitch.Length) == 0
        && (index == 0 || char.IsWhiteSpace(arguments[index - 1]))
        && (index + SingleArgumentSwitch.Length == arguments.Length || char.IsWhiteSpace(arguments[index + SingleArgumentSwitch.Length]));

    private static bool IsPlaceholderAt(string arguments, int index, bool quoted)
    {
        if (quoted)
        {
            return index + 4 <= arguments.Length && arguments[index] == '"' && arguments[index + 3] == '"' && IsPlaceholderAt(arguments, index + 1, false);
        }
        return index + 2 <= arguments.Length && arguments[index] == '%' && (arguments[index + 1] == '1' || arguments[index + 1] == 'L' || arguments[index + 1] == 'l');
    }

    /// <summary>
    ///     Quotes an argument for CommandLineToArgvW: backslashes before a quote are doubled, quotes are escaped
    /// </summary>
    private static string QuoteArgument(string argument)
    {
        var result = new StringBuilder(argument.Length + 2);
        result.Append('"');
        int backslashes = 0;
        foreach (char c in argument)
        {
            if (c == '\\')
            {
                backslashes++;
                continue;
            }
            if (c == '"')
            {
                result.Append('\\', backslashes * 2 + 1);
            }
            else
            {
                result.Append('\\', backslashes);
            }
            backslashes = 0;
            result.Append(c);
        }
        result.Append('\\', backslashes * 2);
        result.Append('"');
        return result.ToString();
    }
}
