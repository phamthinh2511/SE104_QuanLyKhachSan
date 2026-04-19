using QuanLyKhachSan_SE104.Model;
using System.ComponentModel;
using System.Runtime.CompilerServices;

public class ChiTietDatPhongItem : INotifyPropertyChanged
{
    public Phong Phong { get; set; }

    public string TenPhong => Phong?.TenPhong;

    private int _soNguoi = 1;
    public int SoNguoi
    {
        get => _soNguoi;
        set
        {
            _soNguoi = value;
            OnPropertyChanged();
        }
    }

    public DateTime NgayCheckIn { get; set; }
    public DateTime NgayCheckOut { get; set; }

    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged([CallerMemberName] string name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}