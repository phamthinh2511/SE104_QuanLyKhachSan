using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

public class NavigationItem : INotifyPropertyChanged
{
    private bool _isSelected;
    private int _badgeCount;
    private UserControl _pageContent;
    private Func<UserControl> _pageFactory;

    public string Title { get; set; }
    public string Icon { get; set; }

    /// <summary>
    /// Set this to create the page lazily (only when first visited).
    /// This prevents all pages from loading at startup.
    /// </summary>
    public Func<UserControl> PageFactory
    {
        get => _pageFactory;
        set { _pageFactory = value; _pageContent = null; } // reset cached instance
    }

    /// <summary>
    /// Returns the cached page, or creates it from PageFactory on first access.
    /// </summary>
    public UserControl PageContent
    {
        get
        {
            if (_pageContent == null && _pageFactory != null)
            {
                try
                {
                    _pageContent = _pageFactory();
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show(
                        $"Lỗi khởi tạo trang '{Title}': {ex.Message}",
                        "Lỗi",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    // Return an empty placeholder instead of crashing
                    _pageContent = new UserControl();
                }
            }
            return _pageContent;
        }
        set { _pageContent = value; }
    }

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