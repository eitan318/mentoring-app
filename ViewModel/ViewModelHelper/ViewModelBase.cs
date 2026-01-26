using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MentoringApp.ViewModel.ViewModelHelper
{
    public class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string propertyChanged)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyChanged));
        }
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;
        
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public virtual Task OnNavigatedTo(object parameter = null)
        {
            return Task.CompletedTask;
        }

        public virtual Task OnNavigatedFrom()
        {
            return Task.CompletedTask;
        }
    }
}
