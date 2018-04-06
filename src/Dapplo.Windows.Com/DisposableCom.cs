using System;
using System.Runtime.InteropServices;

namespace Dapplo.Windows.Com
{
    /// <summary>
    /// A simple com wrapper which helps with "using"
    /// </summary>
    /// <typeparam name="T">Type which is wrapped</typeparam>
    public interface IDisposableCom<T> : IDisposable
    {
        /// <summary>
        /// Access to the actual com object
        /// </summary>
        T ComObject
        {
            get;
            set;
        }
    }

    /// <summary>
    /// A factory for IDisposableCom
    /// </summary>
    public static class DisposableCom
    {
        /// <summary>
        /// Create a ComDisposable for the supplied type object
        /// </summary>
        /// <typeparam name="T">Type of the COM Object to create</typeparam>
        /// <param name="obj">Instance of T</param>
        /// <returns>IDisposableCom of type T</returns>
        public static IDisposableCom<T> Create<T>(T obj)
        {
            return obj != null ? new DisposableComImplementation<T>(obj) : null;
        }
    }

    /// <summary>
    /// Implementation of the IDisposableCom, this is internal to prevent other code to use it directly
    /// </summary>
    /// <typeparam name="T"></typeparam>
    internal sealed class DisposableComImplementation<T> : IDisposableCom<T>
    {
        private bool _disposed;

        public DisposableComImplementation(T obj)
        {
            ComObject = obj;
        }

        public T ComObject
        {
            get;
            set;
        }

        /// <summary>
        /// Cleans up the COM object.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        ~DisposableComImplementation()
        {
            Dispose(false);
        }

        /// <summary>
        /// Release the COM reference
        /// </summary>
        /// <param name="disposing"><see langword="true"/> if this was called from the<see cref="IDisposable"/> interface.</param>
        private void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            // Do not catch an exception from this.

            if (disposing)
            {
                // DisposeManagedResources
            }
            try
            {
                if (ComObject != null && Marshal.IsComObject(ComObject))
                {
                    Marshal.ReleaseComObject(ComObject);
                }
                ComObject = default;
            }
            catch
            {
                // Ignore all
            }
        }
    }
}
