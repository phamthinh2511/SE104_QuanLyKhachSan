
using QuanLyKhachSan_SE104.Model;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.Utilities
{
    /// <summary>
    /// Attached Property giúp ContextMenu trong DataTemplate 
    /// tìm được ViewModel nằm ngoài Visual Tree.
    /// Giải quyết bug: ContextMenu không bind được Command từ ViewModel
    /// vì nó nằm ngoài Visual Tree của ItemsControl.
    /// </summary>
    public static class PhongContextMenuHelper
    {
        public static readonly DependencyProperty DataContextProxyProperty =
            DependencyProperty.RegisterAttached(
                "DataContextProxy",
                typeof(object),
                typeof(PhongContextMenuHelper),
                new PropertyMetadata(null, OnDataContextProxyChanged));

        public static object GetDataContextProxy(DependencyObject obj)
            => obj.GetValue(DataContextProxyProperty);

        public static void SetDataContextProxy(DependencyObject obj, object value)
            => obj.SetValue(DataContextProxyProperty, value);

        private static void OnDataContextProxyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is ContextMenu menu)
                menu.DataContext = e.NewValue;
        }
    }
}