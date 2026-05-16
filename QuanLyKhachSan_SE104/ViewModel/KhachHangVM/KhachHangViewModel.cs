using QuanLyKhachSan_SE104.DAL;
using QuanLyKhachSan_SE104.Model;
using QuanLyKhachSan_SE104.Utilities;
using QuanLyKhachSan_SE104.ViewModel.DanhMuc;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace QuanLyKhachSan_SE104.ViewModel.KhachHangVM
{
    public class KhachHangViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private QuanLyKhachHangDAL _dal;

        private ObservableCollection<KhachHang> _danhSachKhachHang;
        public ObservableCollection<KhachHang> DanhSachKhachHang
        {
            get => _danhSachKhachHang;
            set { _danhSachKhachHang = value;
                OnPropertyChanged();
            }
        }

        // Biến lưu trữ từ khóa tìm kiếm
        private string _searchText;
        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged();
                TimKiem(); 
            }
        }

        public ICommand EditCommand { get; }

        public KhachHangViewModel()
        {
            _dal = new QuanLyKhachHangDAL();
            LoadData();

            EditCommand = new RelayCommand<KhachHang>(ExecuteEdit);
        }

        private void LoadData()
        {
            DanhSachKhachHang = new ObservableCollection<KhachHang>(_dal.LayDanhSach());
        }

        private void TimKiem()
        {
            DanhSachKhachHang = new ObservableCollection<KhachHang>(_dal.LayDanhSach(SearchText));
        }

        private void ExecuteEdit(KhachHang kh)
        {
            if (kh == null) return;

            var vm = new KhachHangCRUDViewModel(kh);
            vm.OnSaved = (_) => LoadData();

            var win = new View.KhachHang.KhachHangCRUD(vm);
            win.Owner = Application.Current.MainWindow;
            win.ShowDialog();
        }
    }
}
