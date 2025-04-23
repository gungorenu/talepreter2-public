using System.Windows.Input;
using System.Windows.Threading;

namespace Talepreter.GUI.Common
{
    /// <summary>
    ///     This class contains methods for the CommandManager that help avoid memory leaks by
    ///     using weak references.
    /// </summary>
    public static class CommandManagerHelper
    {
        /// <summary>
        /// Calls weak reference handlers
        /// </summary>
        /// <param name="handlers">Handler list</param>
        public static void CallWeakReferenceHandlers(this List<WeakReference> handlers)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                if (handlers != null)
                {
                    // Take a snapshot of the handlers before we call out to them since the handlers
                    // could cause the array to me modified while we are reading it.

                    var callees = new EventHandler[handlers.Count];
                    int count = 0;

                    for (int i = handlers.Count - 1; i >= 0; i--)
                    {
                        var reference = handlers[i];
                        if (reference.Target is not EventHandler handler)
                        {
                            // Clean up old handlers that have been collected
                            handlers.RemoveAt(i);
                        }
                        else
                        {
                            callees[count] = handler;
                            count++;
                        }
                    }

                    // Call the handlers that we snapshotted
                    for (int i = 0; i < count; i++)
                    {
                        EventHandler handler = callees[i];
                        handler(null, EventArgs.Empty);
                    }
                }
            });
        }

        /// <summary>
        /// Adds handlers to requery suggested list
        /// </summary>
        /// <param name="handlers">Handler list</param>
        public static void AddHandlersToRequerySuggested(this List<WeakReference> handlers)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                if (handlers != null) foreach (WeakReference handlerRef in handlers) if (handlerRef.Target is EventHandler handler) CommandManager.RequerySuggested += handler;
            });
        }

        /// <summary>
        /// Removes handlers from requery suggested list
        /// </summary>
        /// <param name="handlers">Handler list</param>
        public static void RemoveHandlersFromRequerySuggested(this List<WeakReference> handlers)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                if (handlers != null) foreach (WeakReference handlerRef in handlers) if (handlerRef.Target is EventHandler handler) CommandManager.RequerySuggested -= handler;
            });
        }

        /// <summary>
        /// Adds new weak reference handler
        /// </summary>
        /// <param name="handlers">Handler list</param>
        /// <param name="handler">Handler to be added</param>
        /// <param name="defaultListSize">Default list size for handler list</param>
        public static void AddWeakReferenceHandler(ref List<WeakReference> handlers, EventHandler? handler, int defaultListSize = -1)
        {
            handlers ??= (defaultListSize > 0 ? new List<WeakReference>(defaultListSize) : []);
            var list = handlers;
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                if (handler != null) list.Add(new WeakReference(handler));
            });
        }

        /// <summary>
        /// Removes weak reference handler from handler list
        /// </summary>
        /// <param name="handlers">Handler list</param>
        /// <param name="handler">Handler to be removed</param>
        public static void RemoveWeakReferenceHandler(this List<WeakReference> handlers, EventHandler? handler)
        {
            Dispatcher.CurrentDispatcher.BeginInvoke(() =>
            {
                if (handlers != null && handler != null)
                {
                    for (int i = handlers.Count - 1; i >= 0; i--)
                    {
                        WeakReference reference = handlers[i];
                        if ((reference.Target is not EventHandler existingHandler) || (existingHandler == handler))
                        {
                            // Clean up old handlers that have been collected
                            // in addition to the handler that is to be removed.
                            handlers.RemoveAt(i);
                        }
                    }
                }
            });
        }
    }
}
