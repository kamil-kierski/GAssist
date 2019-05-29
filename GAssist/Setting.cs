using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace GAssist
{
    public class Setting : INotifyPropertyChanged
    {
        private bool _toggled;

        public string Text { get; set; }

        public bool IsToggled
        {
            get => _toggled;
            set
            {
                if (_toggled != value)
                {
                    _toggled = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}