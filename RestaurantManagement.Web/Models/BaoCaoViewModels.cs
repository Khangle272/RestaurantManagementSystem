using RestaurantManagement.API.Models;

namespace RestaurantManagement.Web.Models;

public class BaoCaoDashboardViewModel
{
    public decimal TongTienCocDaTru { get; set; }
    public int SoHoaDonThieuNgayThanhToan { get; set; }
    public DateTimeOffset TuNgay { get; set; }
    public DateTimeOffset DenNgay { get; set; }

    // KPI Doanh thu & Đơn hàng
    public decimal TongDoanhThu { get; set; }
    public int TongSoDonHang { get; set; }
    public int SoDonThanhCong { get; set; }
    public int SoDonDaHuy { get; set; }
    public decimal GiaTriDonTrungBinh => SoDonThanhCong > 0 ? TongDoanhThu / SoDonThanhCong : 0;
    public decimal TongChiPhiKhuyenMai { get; set; }

    // Biểu đồ xu hướng 14 ngày gần nhất
    public List<DoanhThuTimelineRow> DoanhThuTheoNgay { get; set; } = [];

    // Cơ cấu thanh toán
    public decimal DoanhThuTienMat { get; set; }
    public decimal DoanhThuChuyenKhoan { get; set; }
    public decimal DoanhThuThe { get; set; }

    // Top món bán chạy
    public List<TopMonBanChayItem> TopMonBanChay { get; set; } = [];

    // Tồn kho cảnh báo
    public decimal TongGiaTriTonKho { get; set; }
    public int SoNguyenLieuCanhBao { get; set; }
    public List<NguyenLieuCanhBaoItem> NguyenLieuCanhBao { get; set; } = [];

    // Hiệu quả khuyến mãi tóm tắt
    public List<HieuQuaKhuyenMaiItem> TopKhuyenMaiHieuQua { get; set; } = [];
}

public class BaoCaoDoanhThuViewModel
{
    public int SoHoaDonThieuNgayThanhToan { get; set; }
    public string CheDo { get; set; } = "TheoNgay"; // TheoNgay, TheoThang, TheoNam
    public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime DenNgay { get; set; } = DateTime.Today;
    public int Nam { get; set; } = DateTime.Today.Year;

    public decimal TongDoanhThu { get; set; }
    public decimal TongTienHang { get; set; }
    public decimal TongTienGiam { get; set; }
    public decimal TongTienCocDaTru { get; set; }
    public int TongSoDonHang { get; set; }
    public int SoDonThanhCong { get; set; }
    public int SoDonDaHuy { get; set; }
    public decimal GiaTriDonTrungBinh => SoDonThanhCong > 0 ? TongDoanhThu / SoDonThanhCong : 0;

    // Phân bổ thanh toán
    public decimal DoanhThuTienMat { get; set; }
    public decimal DoanhThuChuyenKhoan { get; set; }
    public decimal DoanhThuThe { get; set; }

    // Phân bổ loại đơn
    public decimal DoanhThuTaiBan { get; set; }
    public decimal DoanhThuMangDi { get; set; }
    public decimal DoanhThuGiaoHang { get; set; }

    public List<DoanhThuTimelineRow> ChiTietTimeline { get; set; } = [];
}

public class DoanhThuTimelineRow
{
    public string NhanThoiGian { get; set; } = "";
    public int SoDonThanhCong { get; set; }
    public int SoDonHuy { get; set; }
    public decimal TongTienHang { get; set; }
    public decimal TienGiam { get; set; }
    public decimal DoanhThuThuc { get; set; }
    public decimal TienMat { get; set; }
    public decimal ChuyenKhoan { get; set; }
    public decimal The { get; set; }
}

public class BaoCaoMonBanChayViewModel
{
    public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime DenNgay { get; set; } = DateTime.Today;
    public int? DanhMucId { get; set; }
    public int TopN { get; set; } = 10;

    public decimal TongDoanhThuMon { get; set; }
    public int TongSoLuongMonDaBan { get; set; }

    public List<DanhMucOptionItem> DanhMucOptions { get; set; } = [];
    public List<TopMonBanChayItem> DanhSachMon { get; set; } = [];
    public List<CoCauDanhMucItem> CoCauDanhMuc { get; set; } = [];
}

public class DanhMucOptionItem
{
    public int Id { get; set; }
    public string TenDanhMuc { get; set; } = "";
}

public class TopMonBanChayItem
{
    public int MonAnId { get; set; }
    public string TenMon { get; set; } = "";
    public string TenDanhMuc { get; set; } = "";
    public string? HinhAnh { get; set; }
    public string SizePhoBien { get; set; } = "Mặc định";
    public int SoLuongDaBan { get; set; }
    public decimal DoanhThuMangLai { get; set; }
    public decimal TyTrongDoanhThu { get; set; }
}

public class CoCauDanhMucItem
{
    public string TenDanhMuc { get; set; } = "";
    public int SoLuongBan { get; set; }
    public decimal DoanhThu { get; set; }
    public decimal TyTrong { get; set; }
}

public class BaoCaoTonKhoTongHopViewModel
{
    public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime DenNgay { get; set; } = DateTime.Today;

    public decimal TongGiaTriTonHienTai { get; set; }
    public decimal TongGiaTriNhapTrongKy { get; set; }
    public decimal TongGiaTriXuatTrongKy { get; set; }
    public decimal TongGiaTriThanhLyTrongKy { get; set; }
    public int SoNguyenLieuCanhBao { get; set; }

    public List<NguyenLieuCanhBaoItem> DanhSachCanhBao { get; set; } = [];
    public List<TonKhoChiTietRow> ChiTietTonKho { get; set; } = [];
}

public class NguyenLieuCanhBaoItem
{
    public int Id { get; set; }
    public string TenNguyenLieu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public decimal SoLuongTon { get; set; }
    public decimal NguongCanhBao { get; set; }
    public decimal DonGia { get; set; }
    public decimal ThieuHut => Math.Max(0, NguongCanhBao - SoLuongTon);
}

public class TonKhoChiTietRow
{
    public int Id { get; set; }
    public string TenNguyenLieu { get; set; } = "";
    public string DonViTinh { get; set; } = "";
    public string? DanhMuc { get; set; }
    public decimal SoLuongTon { get; set; }
    public decimal NguongCanhBao { get; set; }
    public decimal DonGia { get; set; }
    public decimal GiaTriTon => SoLuongTon * DonGia;
    public bool CanhBao => SoLuongTon <= NguongCanhBao;
}

public class BaoCaoHieuQuaKhuyenMaiViewModel
{
    public int SoHoaDonThieuLichSu { get; set; }
    public decimal TongGiamThuCong { get; set; }
    public DateTime TuNgay { get; set; } = DateTime.Today.AddDays(-30);
    public DateTime DenNgay { get; set; } = DateTime.Today;

    public decimal TongChiPhiKhuyenMai { get; set; }
    public decimal TongDoanhThuDonKhuyenMai { get; set; }
    public int SoDonApDungKhuyenMai { get; set; }
    public decimal TyLeChietKhauTrungBinh => (TongDoanhThuDonKhuyenMai + TongChiPhiKhuyenMai) > 0
        ? (TongChiPhiKhuyenMai / (TongDoanhThuDonKhuyenMai + TongChiPhiKhuyenMai) * 100m) : 0;
    public decimal TySuatHieuQuaROI => TongChiPhiKhuyenMai > 0 ? TongDoanhThuDonKhuyenMai / TongChiPhiKhuyenMai : 0;

    public List<HieuQuaKhuyenMaiItem> DanhSachChuongTrinh { get; set; } = [];
}

public class HieuQuaKhuyenMaiItem
{
    public int KhuyenMaiId { get; set; }
    public string TenChuongTrinh { get; set; } = "";
    public PhamViUuDai PhamVi { get; set; }
    public KieuGiam KieuGiam { get; set; }
    public decimal GiaTri { get; set; }
    public bool CanVoucher { get; set; }
    public int SoLuotApDung { get; set; }
    public decimal TongTienGiam { get; set; }
    public decimal DoanhThuMangLai { get; set; }
    public decimal TyLeGiamTrungBinh => (DoanhThuMangLai + TongTienGiam) > 0
        ? (TongTienGiam / (DoanhThuMangLai + TongTienGiam) * 100m) : 0;
    public decimal ChiSoROI => TongTienGiam > 0 ? DoanhThuMangLai / TongTienGiam : 0;

    public string DanhGiaHieuQua
    {
        get
        {
            if (SoLuotApDung == 0) return "Chưa phát sinh lượt dùng";
            if (ChiSoROI >= 8) return "Tỷ lệ doanh thu/giảm cao";
            if (ChiSoROI >= 4) return "Tỷ lệ doanh thu/giảm khá";
            if (ChiSoROI >= 2) return "Tỷ lệ doanh thu/giảm trung bình";
            return "Tiền giảm cao so với doanh thu bill";
        }
    }
}

// Báo cáo nộp ca: dòng tiền thực thu/chi trong ngày theo ca, tách tiền mặt và chuyển khoản.
public class BaoCaoNopCaViewModel
{
    public DateOnly Ngay { get; set; }
    public string? CaId { get; set; }
    public List<NopCaRow> TheoCa { get; set; } = [];
    public List<ChiTietThanhToan> GiaoDich { get; set; } = [];
    public decimal TongTienMat => TheoCa.Sum(x => x.TienMat);
    public decimal TongChuyenKhoan => TheoCa.Sum(x => x.ChuyenKhoan);
    public decimal TongKhac => TheoCa.Sum(x => x.Khac);
    public decimal TongThucThu => TheoCa.Sum(x => x.Tong);
}

public class NopCaRow
{
    public string CaId { get; set; } = "";
    public string TenCa { get; set; } = "";
    public string KhungGio { get; set; } = "";
    public int SoGiaoDich { get; set; }
    public decimal CocTienMat { get; set; }
    public decimal CocChuyenKhoan { get; set; }
    public decimal ThanhToanTienMat { get; set; }
    public decimal ThanhToanChuyenKhoan { get; set; }
    public decimal HoanTienMat { get; set; }
    public decimal HoanChuyenKhoan { get; set; }
    public decimal Khac { get; set; }
    public decimal TienMat => CocTienMat + ThanhToanTienMat - HoanTienMat;
    public decimal ChuyenKhoan => CocChuyenKhoan + ThanhToanChuyenKhoan - HoanChuyenKhoan;
    public decimal Tong => TienMat + ChuyenKhoan + Khac;
}
