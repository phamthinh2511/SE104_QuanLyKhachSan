using System.ComponentModel;

public class ThongTinNhanPhongDTO : INotifyPropertyChanged
{
    public int MaChiTietDatPhong { get; set; }
    public int MaDatPhong { get; set; }
    public int MaKhachHang { get; set; }
    public int MaPhong { get; set; }

    public string TenKhachHang { get; set; }
    public string TenLoaiPhong { get; set; }
    public string TenPhong { get; set; }
    public DateTime NgayCheckInDuKien { get; set; }
    public decimal TienCoc { get; set; }
    public string CCCD_Passport { get; set; }
    public int SoNguoi { get; set; }

    // Implement INotifyPropertyChanged cơ bản...
    public event PropertyChangedEventHandler PropertyChanged;
    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}