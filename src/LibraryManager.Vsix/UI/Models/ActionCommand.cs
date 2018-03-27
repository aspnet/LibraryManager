using System;
using System.Windows.Input;

namespace Microsoft.Web.LibraryManager.Vsix.UI.Models
{
    internal class ActionCommand : ICommand
    {
        private readonly Func<bool> _canExecute;
        private readonly Action _execute;
        private bool _canExecuteValue;

        private ActionCommand(Action execute, Func<bool> canExecute, bool initialCanExecute)
        {
            _canExecuteValue = initialCanExecute;
            _canExecute = canExecute;
            _execute = execute;
        }

        public event EventHandler CanExecuteChanged;

        public static ICommand Create(Action execute, Func<bool> canExecute = null, bool initialCanExecute = true)
        {
            return new ActionCommand(execute, canExecute, initialCanExecute);
        }

        public static ICommand Create<T>(Action<T> execute, Func<T, bool> canExecute = null, bool initialCanExecute = true)
        {
            return new ActionCommand<T>(execute, canExecute, initialCanExecute);
        }

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            bool oldCanExecute = _canExecuteValue;
            _canExecuteValue = _canExecute();

            if (oldCanExecute ^ _canExecuteValue)
            {
                OnCanExecuteChanged();
            }

            return _canExecuteValue;
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        private void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    internal class ActionCommand<T> : ICommand
    {
        private readonly Func<T, bool> _canExecute;
        private readonly Action<T> _execute;
        private bool _canExecuteValue;

        public ActionCommand(Action<T> execute, Func<T, bool> canExecute, bool initialCanExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
            _canExecuteValue = initialCanExecute;
        }

        public event EventHandler CanExecuteChanged;

        public bool CanExecute(object parameter)
        {
            if (_canExecute == null)
            {
                return true;
            }

            bool oldCanExecute = _canExecuteValue;
            _canExecuteValue = _canExecute((T) parameter);

            if (oldCanExecute ^ _canExecuteValue)
            {
                OnCanExecuteChanged();
            }

            return _canExecuteValue;
        }

        public void Execute(object parameter)
        {
            _execute((T) parameter);
        }

        private void OnCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
