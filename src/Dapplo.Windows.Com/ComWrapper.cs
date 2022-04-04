﻿// Copyright (c) Dapplo and contributors. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
#if NETFRAMEWORK

using Dapplo.Log;
using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Remoting;
using System.Runtime.Remoting.Messaging;
using System.Runtime.Remoting.Proxies;

namespace Dapplo.Windows.Com
{
    /// <summary>
    ///     Wraps a late-bound COM server.
    /// </summary>
    public sealed class ComWrapper : RealProxy, IDisposable, IRemotingTypeInfo
    {
        private const int MK_E_UNAVAILABLE = -2147221021;
        private const int CO_E_CLASSSTRING = -2147221005;
        /// <summary>
        /// This pretty much means that the COM message was rejected by the receiving application
        /// </summary>
        public const int RPC_E_CALL_REJECTED = unchecked((int) 0x80010001);

        /// <summary>
        /// This is a more hard error, but we are processing this like RPC_E_CALL_REJECTED
        /// </summary>
        public const int RPC_E_FAIL = unchecked((int) 0x80004005);
        private static readonly LogSource Log = new LogSource();

        /// <summary>
        ///     Implementation for the interface IRemotingTypeInfo
        ///     This makes it possible to cast the COMWrapper
        /// </summary>
        /// <param name="toType">Type to cast to</param>
        /// <param name="o">object to cast</param>
        /// <returns></returns>
        public bool CanCastTo(Type toType, object o)
        {
            var returnValue = _interceptType.IsAssignableFrom(toType);
            return returnValue;
        }

        /// <summary>
        ///     Implementation for the interface IRemotingTypeInfo
        /// </summary>
        public string TypeName
        {
            get => throw new NotSupportedException();
            set => throw new NotSupportedException();
        }

        [DllImport("ole32.dll")]
        private static extern int ProgIDFromCLSID([In] ref Guid clsid, [MarshalAs(UnmanagedType.LPWStr)] out string lplpszProgId);

        // Converts failure HRESULTs to exceptions:
        [DllImport("oleaut32", PreserveSig = false)]
        private static extern void GetActiveObject(ref Guid rclsid, IntPtr pvReserved, [MarshalAs(UnmanagedType.IUnknown)] out object ppunk);

        /// <summary>
        ///     Intercept method calls
        /// </summary>
        /// <param name="myMessage">
        ///     Contains information about the method being called
        /// </param>
        /// <returns>
        ///     A <see cref="ReturnMessage" />.
        /// </returns>
        public override IMessage Invoke(IMessage myMessage)
        {
            if (myMessage is not IMethodCallMessage callMessage)
            {
                Log.Debug().WriteLine("Message type not implemented: {0}", myMessage.GetType());
                return null;
            }

            var method = callMessage.MethodBase as MethodInfo;
            if (null == method)
            {
                Log.Debug().WriteLine("Unrecognized Invoke call: {0}", callMessage.MethodBase);
                return null;
            }

            object returnValue = null;
            object[] outArgs = null;
            var outArgsCount = 0;

            var methodName = method.Name;
            var returnType = method.ReturnType;
            var flags = BindingFlags.InvokeMethod;
            var argCount = callMessage.ArgCount;

            ParameterModifier[] argModifiers = null;
            ParameterInfo[] parameters = null;

            if ("Dispose" == methodName && 0 == argCount && typeof(void) == returnType)
            {
                Dispose();
            }
            else if ("ToString" == methodName && 0 == argCount && typeof(string) == returnType)
            {
                returnValue = ToString();
            }
            else if ("GetType" == methodName && 0 == argCount && typeof(Type) == returnType)
            {
                returnValue = _interceptType;
            }
            else if ("GetHashCode" == methodName && 0 == argCount && typeof(int) == returnType)
            {
                returnValue = GetHashCode();
            }
            else if ("Equals" == methodName && 1 == argCount && typeof(bool) == returnType)
            {
                returnValue = Equals(callMessage.Args[0]);
            }
            else if (1 == argCount && typeof(void) == returnType && (methodName.StartsWith("add_") || methodName.StartsWith("remove_")))
            {
                var removeHandler = methodName.StartsWith("remove_");
                methodName = methodName.Substring(removeHandler ? 7 : 4);
                // TODO: Something is missing here
                if (callMessage.InArgs[0] is not Delegate handler)
                {
                    return new ReturnMessage(new ArgumentNullException(nameof(handler)), callMessage);
                }
            }
            else
            {
                var invokeObject = _comObject;
                var invokeType = _comType;

                object[] args;
                ParameterInfo parameter;
                if (methodName.StartsWith("get_"))
                {
                    // Property Get
                    methodName = methodName.Substring(4);
                    flags = BindingFlags.GetProperty;
                    args = callMessage.InArgs;
                }
                else if (methodName.StartsWith("set_"))
                {
                    // Property Set
                    methodName = methodName.Substring(4);
                    flags = BindingFlags.SetProperty;
                    args = callMessage.InArgs;
                }
                else
                {
                    args = callMessage.Args;
                    if (null != args && 0 != args.Length)
                    {
                        // Modifiers for ref / out parameters
                        argModifiers = new ParameterModifier[1];
                        argModifiers[0] = new ParameterModifier(args.Length);

                        parameters = method.GetParameters();
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            parameter = parameters[i];
                            if (parameter.IsOut || parameter.ParameterType.IsByRef)
                            {
                                argModifiers[0][i] = true;
                                outArgsCount++;
                            }
                        }

                        if (0 == outArgsCount)
                        {
                            argModifiers = null;
                        }
                    }
                }

                // Un-wrap wrapped COM objects before passing to the method
                Type byValType;
                ComWrapper wrapper;
                ComWrapper[] originalArgs;
                if (null == args || 0 == args.Length)
                {
                    originalArgs = null;
                }
                else
                {
                    originalArgs = new ComWrapper[args.Length];
                    for (var i = 0; i < args.Length; i++)
                    {
                        if (null != args[i] && RemotingServices.IsTransparentProxy(args[i]))
                        {
                            wrapper = RemotingServices.GetRealProxy(args[i]) as ComWrapper;
                            if (null != wrapper)
                            {
                                originalArgs[i] = wrapper;
                                args[i] = wrapper._comObject;
                            }
                        }
                        else if (0 != outArgsCount && argModifiers[0][i])
                        {
                            byValType = GetByValType(parameters[i].ParameterType);
                            if (byValType.IsInterface)
                            {
                                // If we're passing a COM object by reference, and
                                // the parameter is null, we need to pass a
                                // DispatchWrapper to avoid a type mismatch exception.
                                if (null == args[i])
                                {
                                    args[i] = new DispatchWrapper(null);
                                }
                            }
                            else if (typeof(decimal) == byValType)
                            {
                                // If we're passing a decimal value by reference,
                                // we need to pass a CurrencyWrapper to avoid a 
                                // type mismatch exception.
                                // http://support.microsoft.com/?kbid=837378
                                args[i] = new CurrencyWrapper(args[i]);
                            }
                        }
                    }
                }

                do
                {
                    try
                    {
                        returnValue = invokeType.InvokeMember(methodName, flags, null, invokeObject, args, argModifiers, null, null);
                        break;
                    }
                    catch (InvalidComObjectException icoEx)
                    {
                        // Should assist BUG-1616 and others
                        Log.Warn().WriteLine(
                            "COM object {0} has been separated from its underlying RCW cannot be used. The COM object was released while it was still in use on another thread.",
                            _interceptType.FullName);
                        return new ReturnMessage(icoEx, callMessage);
                    }
                    catch (Exception ex)
                    {
                        // Test for rejected
                        var comEx = ex as COMException ?? ex.InnerException as COMException;
                        if (comEx != null && (comEx.ErrorCode == RPC_E_CALL_REJECTED || comEx.ErrorCode == RPC_E_FAIL))
                        {
                            var destinationName = _targetName;
                            // Try to find a "catchy" name for the rejecting application
                            if (destinationName != null && destinationName.Contains("."))
                            {
                                destinationName = destinationName.Substring(0, destinationName.IndexOf(".", StringComparison.Ordinal));
                            }
                            if (destinationName == null)
                            {
                                destinationName = _interceptType.FullName;
                            }

                            // TODO: Log destinationName and rejected information
                            Log.Error().WriteLine("Error while creating {0}: {1}", destinationName, comEx.ErrorCode);
                        }
                        // Not rejected OR pressed cancel
                        return new ReturnMessage(ex, callMessage);
                    }
                } while (true);

                // Handle enum and interface return types
                if (null != returnValue)
                {
                    if (returnType.IsInterface)
                    {
                        // Wrap the returned value in an intercepting COM wrapper
                        if (Marshal.IsComObject(returnValue))
                        {
                            returnValue = Wrap(returnValue, returnType, _targetName);
                        }
                    }
                    else if (returnType.IsEnum)
                    {
                        // Convert to proper Enum type
                        returnValue = Enum.Parse(returnType, returnValue.ToString());
                    }
                }

                // Handle out args
                if (0 != outArgsCount)
                {
                    outArgs = new object[args.Length];
                    for (var i = 0; i < parameters.Length; i++)
                    {
                        if (!argModifiers[0][i])
                        {
                            continue;
                        }

                        var arg = args[i];
                        if (null == arg)
                        {
                            continue;
                        }

                        parameter = parameters[i];
                        wrapper = null;

                        byValType = GetByValType(parameter.ParameterType);
                        if (typeof(decimal) == byValType)
                        {
                            if (arg is CurrencyWrapper currencyWrapper)
                            {
                                arg = currencyWrapper.WrappedObject;
                            }
                        }
                        else if (byValType.IsEnum)
                        {
                            arg = Enum.Parse(byValType, arg.ToString());
                        }
                        else if (byValType.IsInterface && Marshal.IsComObject(arg))
                        {
                            wrapper = originalArgs[i];
                            if (null != wrapper && wrapper._comObject != arg)
                            {
                                wrapper.Dispose();
                                wrapper = null;
                            }

                            if (null == wrapper)
                            {
                                wrapper = new ComWrapper(arg, byValType, _targetName);
                            }
                            arg = wrapper.GetTransparentProxy();
                        }
                        outArgs[i] = arg;
                    }
                }
            }

            return new ReturnMessage(returnValue, outArgs, outArgsCount, callMessage.LogicalCallContext, callMessage);
        }

        /// <summary>
        ///     Holds reference to the actual COM object which is wrapped by this proxy
        /// </summary>
        private readonly object _comObject;

        /// <summary>
        ///     Type of the COM object, set on constructor after getting the COM reference
        /// </summary>
        private readonly Type _comType;

        /// <summary>
        ///     The type of which method calls are intercepted and executed on the COM object.
        /// </summary>
        private readonly Type _interceptType;

        /// <summary>
        ///     The humanly readable target name
        /// </summary>
        private readonly string _targetName;

        /// <summary>
        ///     Gets a COM object and returns the transparent proxy which intercepts all calls to the object
        /// </summary>
        /// <typeparam name="T">Interface which defines the method and properties to intercept</typeparam>
        /// <returns>Transparent proxy to the real proxy for the object</returns>
        /// <remarks>T must be an interface decorated with the <see cref="ComProgIdAttribute" />attribute.</remarks>
        public static T GetInstance<T>()
        {
            var type = typeof(T);
            if (null == type)
            {
                throw new ArgumentNullException(nameof(T));
            }
            if (!type.IsInterface)
            {
                throw new ArgumentException("The specified type must be an interface.", nameof(T));
            }

            var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
            if (string.IsNullOrEmpty(progIdAttribute?.Value))
            {
                throw new ArgumentException("The specified type must define a ComProgId attribute.", nameof(T));
            }
            var progId = progIdAttribute.Value;

            object comObject = null;

            // Convert from clsid to Prog ID, if needed
            if (progId.StartsWith("clsid:"))
            {
                var guid = new Guid(progId.Substring(6));
                var result = ProgIDFromCLSID(ref guid, out progId);
                if (result != 0)
                {
                    // Restore progId, as it's overwritten
                    progId = progIdAttribute.Value;

                    try
                    {
                        GetActiveObject(ref guid, IntPtr.Zero, out comObject);
                    }
                    catch (Exception)
                    {
                        Log.Warn().WriteLine("Error {0} getting instance for class id {1}", result, progIdAttribute.Value);
                    }
                    if (comObject == null)
                    {
                        Log.Warn().WriteLine("Error {0} getting progId {1}", result, progIdAttribute.Value);
                    }
                }
                else
                {
                    Log.Info().WriteLine("Mapped {0} to progId {1}", progIdAttribute.Value, progId);
                }
            }

            if (comObject == null)
            {
                try
                {
                    comObject = Marshal.GetActiveObject(progId);
                }
                catch (COMException comE)
                {
                    if (comE.ErrorCode == MK_E_UNAVAILABLE)
                    {
                        Log.Debug().WriteLine("No current instance of {0} object available.", progId);
                    }
                    else if (comE.ErrorCode == CO_E_CLASSSTRING)
                    {
                        Log.Warn().WriteLine("Unknown progId {0}", progId);
                    }
                    else
                    {
                        Log.Warn().WriteLine(comE, "Error getting active object for " + progIdAttribute.Value);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn().WriteLine(e, "Error getting active object for " + progIdAttribute.Value);
                }
            }

            if (comObject != null)
            {
                if (comObject is IDispatch)
                {
                    var wrapper = new ComWrapper(comObject, type, progIdAttribute.Value);
                    return (T) wrapper.GetTransparentProxy();
                }
                return (T) comObject;
            }
            return default;
        }

        /// <summary>
        ///     A simple create instance, doesn't create a wrapper!!
        /// </summary>
        /// <typeparam name="TCom">Type of the COM</typeparam>
        /// <returns>T</returns>
        public static TCom CreateInstance<TCom>()
        {
            var type = typeof(TCom);
            if (null == type)
            {
                throw new ArgumentNullException(nameof(TCom));
            }
            if (!type.IsInterface)
            {
                throw new ArgumentException("The specified type must be an interface.", nameof(TCom));
            }

            var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
            if (string.IsNullOrEmpty(progIdAttribute?.Value))
            {
                throw new ArgumentException("The specified type must define a ComProgId attribute.", nameof(TCom));
            }
            var progId = progIdAttribute.Value;
            Type comType = null;
            if (progId.StartsWith("clsid:"))
            {
                var guid = new Guid(progId.Substring(6));
                try
                {
                    comType = Type.GetTypeFromCLSID(guid);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine("Error {1} type for {0}", progId, ex.Message);
                }
            }
            else
            {
                try
                {
                    comType = Type.GetTypeFromProgID(progId, true);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine("Error {1} type for {0}", progId, ex.Message);
                }
            }
            object comObject = null;
            if (comType != null)
            {
                try
                {
                    comObject = Activator.CreateInstance(comType);
                    if (comObject != null)
                    {
                        Log.Debug().WriteLine("Created new instance of {0} object.", progId);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn().WriteLine("Error {1} creating object for {0}", progId, e.Message);
                    throw;
                }
            }
            if (comObject != null)
            {
                return (TCom) comObject;
            }
            return default;
        }

        /// <summary>
        ///     Gets or creates a COM object and returns the transparent proxy which intercepts all calls to the object
        ///     The ComProgId can be a normal ComProgId or a GUID prefixed with "clsid:"
        /// </summary>
        /// <typeparam name="T">Interface which defines the method and properties to intercept</typeparam>
        /// <returns>Transparent proxy to the real proxy for the object</returns>
        /// <remarks>T must be an interface decorated with the <see cref="ComProgIdAttribute" />attribute.</remarks>
        public static T GetOrCreateInstance<T>()
        {
            var type = typeof(T);
            if (null == type)
            {
                throw new ArgumentNullException(nameof(T));
            }
            if (!type.IsInterface)
            {
                throw new ArgumentException("The specified type must be an interface.", nameof(T));
            }

            var progIdAttribute = ComProgIdAttribute.GetAttribute(type);
            if (string.IsNullOrEmpty(progIdAttribute?.Value))
            {
                throw new ArgumentException("The specified type must define a ComProgId attribute.", nameof(T));
            }

            object comObject = null;
            Type comType = null;
            var progId = progIdAttribute.Value;
            var guid = Guid.Empty;

            // Convert from clsid to Prog ID, if needed
            if (progId.StartsWith("clsid:"))
            {
                guid = new Guid(progId.Substring(6));
                var result = ProgIDFromCLSID(ref guid, out progId);
                if (result != 0)
                {
                    // Restore progId, as it's overwritten
                    progId = progIdAttribute.Value;
                    try
                    {
                        GetActiveObject(ref guid, IntPtr.Zero, out comObject);
                    }
                    catch (Exception)
                    {
                        Log.Warn().WriteLine("Error {0} getting instance for class id {1}", result, progIdAttribute.Value);
                    }
                    if (comObject == null)
                    {
                        Log.Warn().WriteLine("Error {0} getting progId {1}", result, progIdAttribute.Value);
                    }
                }
                else
                {
                    Log.Info().WriteLine("Mapped {0} to progId {1}", progIdAttribute.Value, progId);
                }
            }

            if (comObject == null && !progId.StartsWith("clsid:"))
            {
                try
                {
                    comObject = Marshal.GetActiveObject(progId);
                }
                catch (COMException comE)
                {
                    if (comE.ErrorCode == MK_E_UNAVAILABLE)
                    {
                        Log.Debug().WriteLine("No current instance of {0} object available.", progId);
                    }
                    else if (comE.ErrorCode == CO_E_CLASSSTRING)
                    {
                        Log.Warn().WriteLine("Unknown progId {0} (application not installed)", progId);
                        return default;
                    }
                    else
                    {
                        Log.Warn().WriteLine(comE, "Error getting active object for " + progId);
                    }
                }
                catch (Exception e)
                {
                    Log.Warn().WriteLine(e, "Error getting active object for " + progId);
                }
            }

            // Did we get the current instance? If not, try to create a new
            if (comObject == null)
            {
                try
                {
                    comType = Type.GetTypeFromProgID(progId, true);
                }
                catch (Exception ex)
                {
                    if (Guid.Empty != guid)
                    {
                        comType = Type.GetTypeFromCLSID(guid);
                    }
                    else
                    {
                        Log.Warn().WriteLine(ex, "Error type for " + progId);
                    }
                }

                if (comType != null)
                {
                    try
                    {
                        comObject = Activator.CreateInstance(comType);
                        if (comObject != null)
                        {
                            Log.Debug().WriteLine("Created new instance of {0} object.", progId);
                        }
                    }
                    catch (Exception e)
                    {
                        Log.Warn().WriteLine(e, "Error creating object for " + progId);
                    }
                }
            }
            if (comObject != null)
            {
                if (comObject is IDispatch)
                {
                    var wrapper = new ComWrapper(comObject, type, progIdAttribute.Value);
                    return (T) wrapper.GetTransparentProxy();
                }
                return (T) comObject;
            }
            return default;
        }

        /// <summary>
        ///     Wrap a com object as COMWrapper
        /// </summary>
        /// <typeparam name="T">Interface which defines the method and properties to intercept</typeparam>
        /// <param name="comObject">An object to intercept</param>
        /// <returns>Transparent proxy to the real proxy for the object</returns>
        public static T Wrap<T>(object comObject)
        {
            var type = typeof(T);
            if (null == comObject)
            {
                throw new ArgumentNullException(nameof(comObject));
            }
            if (null == type)
            {
                throw new ArgumentNullException(nameof(T));
            }

            var wrapper = new ComWrapper(comObject, type, type.FullName);
            return (T) wrapper.GetTransparentProxy();
        }

        /// <summary>
        ///     Wrap an object and return the transparent proxy which intercepts all calls to the object
        /// </summary>
        /// <param name="comObject">An object to intercept</param>
        /// <param name="type">Interface which defines the method and properties to intercept</param>
        /// <param name="targetName"></param>
        /// <returns>Transparent proxy to the real proxy for the object</returns>
        private static object Wrap(object comObject, Type type, string targetName)
        {
            if (null == comObject)
            {
                throw new ArgumentNullException(nameof(comObject));
            }
            if (null == type)
            {
                throw new ArgumentNullException(nameof(type));
            }

            var wrapper = new ComWrapper(comObject, type, targetName);
            return wrapper.GetTransparentProxy();
        }

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="comObject">
        ///     The COM object to wrap.
        /// </param>
        /// <param name="type">
        ///     The interface type to impersonate.
        /// </param>
        /// <param name="targetName"></param>
        private ComWrapper(object comObject, Type type, string targetName) : base(type)
        {
            _comObject = comObject;
            _comType = comObject.GetType();
            _interceptType = type;
            _targetName = targetName;
        }

        /// <summary>
        ///     If <see cref="Dispose()" /> is not called, we need to make
        ///     sure that the COM object is still cleaned up.
        /// </summary>
        ~ComWrapper()
        {
            Log.Debug().WriteLine("Finalize {0}", _interceptType);
            Dispose(false);
        }

        /// <summary>
        ///     Cleans up the COM object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Release the COM reference
        /// </summary>
        /// <param name="disposing">
        ///     <see langword="true" /> if this was called from the
        ///     <see cref="IDisposable" /> interface.
        /// </param>
        private void Dispose(bool disposing)
        {
            if (null == _comObject)
            {
                return;
            }

            Log.Debug().WriteLine("Disposing {0}", _interceptType);
            if (Marshal.IsComObject(_comObject))
            {
                try
                {
                    int count;
                    do
                    {
                        count = Marshal.ReleaseComObject(_comObject);
                        Log.Debug().WriteLine("RCW count for {0} now is {1}", _interceptType, count);
                    } while (count > 0);
                }
                catch (Exception ex)
                {
                    Log.Warn().WriteLine("Problem releasing COM object {0}", _comType);
                    Log.Warn().WriteLine(ex, "Error: ");
                }
            }
            else
            {
                Log.Warn().WriteLine("{0} is not a COM object", _comType);
            }
        }

        /// <summary>
        ///     Returns a string representing the wrapped object.
        /// </summary>
        /// <returns>
        ///     The full name of the intercepted type.
        /// </returns>
        public override string ToString()
        {
            return _interceptType.FullName;
        }

        /// <summary>
        ///     Returns the hash code of the wrapped object.
        /// </summary>
        /// <returns>
        ///     The hash code of the wrapped object.
        /// </returns>
        public override int GetHashCode()
        {
            return _comObject.GetHashCode();
        }

        /// <summary>
        ///     Compares this object to another.
        /// </summary>
        /// <param name="obj">The value to compare to.</param>
        /// <returns><see langword="true" /> if the objects are equal.</returns>
        public override bool Equals(object obj)
        {
            if (null != obj && RemotingServices.IsTransparentProxy(obj) && RemotingServices.GetRealProxy(obj) is ComWrapper wrapper)
            {
                return _comObject == wrapper._comObject;
            }

            return base.Equals(obj);
        }

        /// <summary>
        ///     Returns the base type for a reference type.
        /// </summary>
        /// <param name="byRefType">
        ///     The reference type.
        /// </param>
        /// <returns>
        ///     The base value type.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///     <paramref name="byRefType" /> is <see langword="null" />.
        /// </exception>
        private static Type GetByValType(Type byRefType)
        {
            if (null == byRefType)
            {
                throw new ArgumentNullException(nameof(byRefType));
            }

            if (byRefType.IsByRef)
            {
                var name = byRefType.FullName;
                name = name.Substring(0, name.Length - 1);
                byRefType = byRefType.Assembly.GetType(name, true);
            }

            return byRefType;
        }
    }
}
#endif