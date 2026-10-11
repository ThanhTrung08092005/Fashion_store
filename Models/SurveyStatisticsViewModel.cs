namespace Fashion_store.Models;

public sealed class SurveyStatisticsViewModel
{
    public int MaKS { get; init; }
    public string TenKhaoSat { get; init; } = string.Empty;
    public string? MoTa { get; init; }
    public DateTime NgayBatDau { get; init; }
    public DateTime NgayKetThuc { get; init; }
    public int TongSoPhieu { get; init; }
    public int SoPhieuHoanThanh { get; init; }
    public double TyLeHoanThanh => TongSoPhieu == 0 ? 0 : (double)SoPhieuHoanThanh * 100 / TongSoPhieu;
    public IReadOnlyList<SurveyQuestionStatisticsViewModel> CauHoi { get; init; } = Array.Empty<SurveyQuestionStatisticsViewModel>();
}

public sealed class SurveyQuestionStatisticsViewModel
{
    public int MaCauHoi { get; init; }
    public int ThuTu { get; init; }
    public string NoiDung { get; init; } = string.Empty;
    public string LoaiCauHoi { get; init; } = string.Empty;
    public int SoNguoiTraLoi { get; init; }
    public double? DiemTrungBinh { get; init; }
    public IReadOnlyList<SurveyOptionStatisticsViewModel> LuaChons { get; init; } = Array.Empty<SurveyOptionStatisticsViewModel>();
    public IReadOnlyList<SurveyTextAnswerViewModel> TraLoiTuLuan { get; init; } = Array.Empty<SurveyTextAnswerViewModel>();
}

public sealed class SurveyOptionStatisticsViewModel
{
    public int? MaLuaChon { get; init; }
    public string NoiDung { get; init; } = string.Empty;
    public int SoLuotChon { get; init; }
    public double TyLe { get; init; }
}

public sealed class SurveyTextAnswerViewModel
{
    public int MaPhieuKS { get; init; }
    public DateTime? NgayNop { get; init; }
    public string NoiDung { get; init; } = string.Empty;
}
