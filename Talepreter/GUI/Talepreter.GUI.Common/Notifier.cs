using System.ComponentModel;

namespace Talepreter.GUI.Common
{
    public abstract class Notifier : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        protected void TriggerPropertyChange(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
        protected void TriggerAllPropertyChange()
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        }

        protected void TriggerPropertyChanges(params string[] names)
        {
            foreach( var name in names) PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
