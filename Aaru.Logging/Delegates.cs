using System;

namespace Aaru.Logging;

/// <summary>
///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
///     the standard output console using the specified format information.
/// </summary>
/// <param name="format">A composite format string.</param>
/// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
public delegate void WriteLineDelegate(string format, params object[] arg);

/// <summary>
///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
///     the error output console using the specified format information.
/// </summary>
/// <param name="format">A composite format string.</param>
/// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
public delegate void ErrorDelegate(string format, params object[] arg);

/// <summary>
///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
///     the verbose output console using the specified format information.
/// </summary>
/// <param name="format">A composite format string.</param>
/// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
public delegate void VerboseDelegate(string format, params object[] arg);

/// <summary>
///     Writes the text representation of the specified array of objects, to the standard output console using the
///     specified format information.
/// </summary>
/// <param name="format">A composite format string.</param>
/// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
public delegate void WriteDelegate(string format, params object[] arg);

/// <summary>
///     Writes the text representation of the specified array of objects, followed by the current line terminator, to
///     the debug output console using the specified format information.
/// </summary>
/// <param name="module">Description of the module writing to the debug console</param>
/// <param name="format">A composite format string.</param>
/// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
public delegate void DebugDelegate(string module, string format, params object[] arg);

/// <summary>
///     Writes the exception to the debug output console.
/// </summary>
/// <param name="ex">Exception.</param>
public delegate void ExceptionDelegate(Exception ex, string message, params object[] arg);

/// <summary>
///     Writes the text representation of the specified array of objects, to the standard output console using the
///     specified format information.
/// </summary>
/// <param name="format">A composite format string.</param>
/// <param name="arg">An array of objects to write using <paramref name="format" />.</param>
public delegate void InformationDelegate(string format, params object[] arg);

