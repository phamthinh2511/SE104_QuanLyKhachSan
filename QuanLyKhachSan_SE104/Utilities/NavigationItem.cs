using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

public class NavigationItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _badgeCount;
    public string Title { get; set; }
    public string Icon { get; set; }
    public UserControl PageContent { get; set; }
    public int BadgeCount
    {
        get => _badgeCount;
        set
        {
            _badgeCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasBadge));
        }
    }
    public bool HasBadge => _badgeCount > 0;
    public bool IsSelected
    {
        get => _isSelected;
        set { _isSelected = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}