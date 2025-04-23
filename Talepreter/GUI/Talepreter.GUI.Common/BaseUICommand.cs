using System.Windows.Input;

namespace Talepreter.GUI.Common
{
    /// <summary>
    ///     This class allows delegating the commanding logic to methods passed as parameters,
    ///     and enables a View to bind commands to objects that are not part of the element tree.
    /// </summary>
    public class BaseUICommand : ICommand
    {
        #region Fields

        private readonly Action _executeMethod;
        private readonly Func<bool>? _canExecuteMethod;
        private bool _isAutomaticRequeryDisabled;
        private List<WeakReference> _canExecuteChangedHandlers = [];

        #endregion

        #region Constructors

        /// <summary>
        ///     Constructor
        /// </summary>
        /// <param name="executeMethod">Execute method</param>
        /// <param name="canExecuteMethod">Function for can execute method flag (default null)</param>
        /// <param name="isAutomaticRequeryDisabled">Flag, is automatic requery disabled (default false)</param>
        public BaseUICommand(Action executeMethod, Func<bool>? canExecuteMethod = null, bool isAutomaticRequeryDisabled = false)
        {
            _executeMethod = executeMethod ?? throw new ArgumentNullException(nameof(executeMethod));
            _canExecuteMethod = canExecuteMethod;
            _isAutomaticRequeryDisabled = isAutomaticRequeryDisabled;
        }

        #endregion

        #region Public Methods

        /// <summary>
        ///     Method to determine if the command can be executed
        /// </summary>
        public bool CanExecute()
        {
            if (_canExecuteMethod != null) return _canExecuteMethod();
            return true;
        }

        /// <summary>
        ///     Execution of the command
        /// </summary>
        public void Execute()
        {
            if (_executeMethod != null)
            {
                _executeMethod();
                OnExecuted();
            }
        }

        /// <summary>
        ///     Property to enable or disable CommandManager's automatic requery on this command
        /// </summary>
        public bool IsAutomaticRequeryDisabled
        {
            get => _isAutomaticRequeryDisabled;
            set
            {
                if (_isAutomaticRequeryDisabled != value)
                {
                    if (value) CommandManagerHelper.RemoveHandlersFromRequerySuggested(_canExecuteChangedHandlers);
                    else CommandManagerHelper.AddHandlersToRequerySuggested(_canExecuteChangedHandlers);

                    _isAutomaticRequeryDisabled = value;
                }
            }
        }

        /// <summary>
        ///     Raises the CanExecuteChaged event
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            OnCanExecuteChanged();
        }

        /// <summary>
        ///     Raises the Executed event
        /// </summary>
        public void RaiseExecuted()
        {
            OnExecuted();
        }

        /// <summary>
        ///     Protected virtual method to raise Executed event
        /// </summary>
        protected virtual void OnExecuted()
        {
            Executed?.Invoke(this, new EventArgs());
        }

        /// <summary>
        ///     Protected virtual method to raise CanExecuteChanged event
        /// </summary>
        protected virtual void OnCanExecuteChanged()
        {
            _canExecuteChangedHandlers?.CallWeakReferenceHandlers();
        }

        /// <summary>
        /// Executed event handler, will be triggered after execution completes
        /// </summary>
        public event EventHandler? Executed;

        #endregion

        #region ICommand Members

        /// <summary>
        ///     ICommand.CanExecuteChanged implementation
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add
            {
                if (!_isAutomaticRequeryDisabled) CommandManager.RequerySuggested += value;
                CommandManagerHelper.AddWeakReferenceHandler(ref _canExecuteChangedHandlers, value, 2);
            }
            remove
            {
                if (!_isAutomaticRequeryDisabled) CommandManager.RequerySuggested -= value;
                _canExecuteChangedHandlers.RemoveWeakReferenceHandler(value);
            }
        }

        /// <summary>
        /// Can execute method
        /// </summary>
        /// <param name="parameter">Parameter only for signature, ignored</param>
        /// <returns>True if can execute command</returns>
        bool ICommand.CanExecute(object? parameter)
        {
            return CanExecute();
        }

        /// <summary>
        /// Execute method
        /// </summary>
        /// <param name="parameter">Parameter only for signature, ignored</param>
        void ICommand.Execute(object? parameter)
        {
            Execute();
        }

        #endregion
    }
}
