// /***************************************************************************
// Aaru Data Preservation Suite
// ----------------------------------------------------------------------------
//
// Filename       : AaruLogging.cs
// Author(s)      : Natalia Portillo <claunia@claunia.com>
//
// Component      : Console.
//
// --[ Description ] ----------------------------------------------------------
//
//     Handlers for normal, verbose and debug consoles.
//
// --[ License ] --------------------------------------------------------------
//
//     This library is free software; you can redistribute it and/or modify
//     it under the terms of the GNU Lesser General Public License as
//     published by the Free Software Foundation; either version 2.1 of the
//     License, or (at your option) any later version.
//
//     This library is distributed in the hope that it will be useful, but
//     WITHOUT ANY WARRANTY; without even the implied warranty of
//     MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//     Lesser General Public License for more details.
//
//     You should have received a copy of the GNU Lesser General Public
//     License along with this library; if not, see <http://www.gnu.org/licenses/>.
//
// ----------------------------------------------------------------------------
// Copyright © 2011-2025 Natalia Portillo
// ****************************************************************************/

using System;
using System.Diagnostics.CodeAnalysis;

namespace Aaru.Logging;

/// <summary>
///     Implements a console abstraction that defines four level of messages that can be routed to different consoles:
///     standard, error, verbose and debug.
/// </summary>
[SuppressMessage("ReSharper", "UnusedMember.Global")]
public static class AaruLogging
{
    /// <summary>Event to receive writings to the standard output console that should be followed by a line termination.</summary>
    public static event WriteLineDelegate WriteLineEvent;

    /// <summary>Event to receive writings to the error output console that should be followed by a line termination.</summary>
    public static event ErrorDelegate ErrorEvent;

    /// <summary>Event to receive writings to the verbose output console that should be followed by a line termination.</summary>
    public static event VerboseDelegate VerboseEvent;

    /// <summary>Event to receive line terminations to the debug output console.</summary>
    public static event DebugDelegate DebugEvent;

    /// <summary>Event to receive writings to the standard output console.</summary>
    public static event WriteDelegate WriteEvent;

    /// <summary>Event to receive exceptions to write to the debug output console.</summary>
    public static event ExceptionDelegate WriteExceptionEvent;

    /// <summary>
    ///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
    ///     the standard output console using the specified format information.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
    public static void WriteLine(string format, params object[] arg) => WriteLineEvent?.Invoke(format, arg);

    /// <summary>
    ///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
    ///     the error output console using the specified format information.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
    public static void Error(string format, params object[] arg) => ErrorEvent?.Invoke(format, arg);

    /// <summary>
    ///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
    ///     the verbose output console using the specified format information.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
    public static void Verbose(string format, params object[] arg) => VerboseEvent?.Invoke(format, arg);

    /// <summary>
    ///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
    ///     the debug output console using the specified format information.
    /// </summary>
    /// <param name="module">Description of the module writing to the debug console</param>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
    public static void Debug(string module, string format, params object[] arg) =>
        DebugEvent?.Invoke(module, format, arg);

    /// <summary>Writes the current line terminator to the standard output console.</summary>
    public static void WriteLine() => WriteLineEvent?.Invoke("", null);

    /// <summary>
    ///     Writes the text representation of the specified array of objects to the standard output console using the
    ///     specified format information.
    /// </summary>
    /// <param name="format">A composite format string.</param>
    /// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
    public static void Write(string format, params object[] arg) => WriteEvent?.Invoke(format, arg);

    /// <summary>
    ///     Writes the exception to the debug output console.
    /// </summary>
    /// <param name="ex">Exception.</param>
    public static void Exception(Exception ex) => WriteExceptionEvent?.Invoke(ex);
}