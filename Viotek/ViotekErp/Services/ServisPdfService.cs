using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace ViotekErp.Services;

public record ServisPdfDto(
    string FormNo,
    DateTime TeslimTarihi,
    string MusteriUnvan,
    string CihazAdi,
    string SeriNumara,
    DateTime? SatinalmaTarihi,
    string GarantiText,
    string ArizaAciklama,
    string TeslimEden,
    string TeslimAlan,
    string FooterAdres,
    string FooterTelefon,
    string FooterEmail
);

public class ServisPdfService
{
    private readonly IWebHostEnvironment _env;

    public ServisPdfService(IWebHostEnvironment env)
    {
        _env = env;
    }

    public byte[] BuildTeslimFormPdf(ServisPdfDto dto)
    {
        var logoPath = Path.Combine(_env.WebRootPath, "img", "viotek.png"); // ✅ burayı kendi yoluna göre düzelt

        return Document.Create(container =>
        {
            container.Page(page =>
            {
                page.Size(PageSizes.A4);
                page.Margin(40);
                page.DefaultTextStyle(x => x.FontSize(10).FontFamily("DejaVu Sans")); // Program.cs’de register ettiğin font

                page.Content().Column(col =>
                {
                    col.Spacing(18);

                    // =========================
                    // HEADER (Logo + Şirket Adı)
                    // =========================
                    col.Item().Row(r =>
                    {
                        r.RelativeItem(2).AlignLeft().Height(42).Element(e =>
                        {
                            if (File.Exists(logoPath))
                                e.Image(logoPath).FitHeight();
                            else
                                e.Text("viotek").FontSize(26).Bold().FontColor("#E53935"); // fallback
                        });

                        r.RelativeItem(6).AlignCenter().AlignMiddle()
                            .Text("Viotek Zayıf Akım Teknolojileri San. Tic. Ltd. Şti.")
                            .FontSize(11);
                    });

                    // =========================
                    // TITLE
                    // =========================
                    col.Item().AlignCenter()
                        .Text("Teknik Servis Cihaz Teslim Formu")
                        .FontSize(15).Bold();

                    // =========================
                    // TABLE (Label / Value)
                    // =========================
                    col.Item().PaddingTop(6).Element(e => BuildInfoTable(e, dto));

                    // =========================
                    // SIGNATURES
                    // =========================
                    col.Item().PaddingTop(45).Row(r =>
                    {
                        r.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Teslim Eden").Bold();
                            c.Item().Height(10);
                            c.Item().AlignCenter().Text(dto.TeslimEden).Bold();
                        });

                        r.RelativeItem().AlignCenter().Column(c =>
                        {
                            c.Item().AlignCenter().Text("Teslim Alan").Bold();
                            c.Item().Height(10);
                            c.Item().AlignCenter().Text(dto.TeslimAlan).Bold();
                        });
                    });

                    // boşluk (ekrandaki gibi altlara insin)
                    col.Item().Height(220);

                    // =========================
                    // PRINT DATETIME (ortada)
                    // =========================
                    col.Item().AlignCenter()
                        .Text(DateTime.Now.ToString("d.M.yyyy HH:mm:ss"));

                    // =========================
                    // FOOTER LINE
                    // =========================
                    col.Item().PaddingTop(8).LineHorizontal(1);

                    // =========================
                    // FOOTER TEXTS
                    // =========================
                    col.Item().AlignCenter().Text(dto.FooterAdres).FontSize(9);
                    col.Item().AlignCenter().Text(dto.FooterTelefon).FontSize(9);
                    col.Item().AlignCenter().Text(dto.FooterEmail).FontSize(9);
                });
            });
        }).GeneratePdf();
    }

    private static void BuildInfoTable(IContainer container, ServisPdfDto dto)
    {
        static string Dt(DateTime d) => d.ToString("dd.MM.yyyy");
        static string DtN(DateTime? d) => d.HasValue ? d.Value.ToString("dd.MM.yyyy") : "-";

        container.Table(t =>
        {
            t.ColumnsDefinition(cols =>
            {
                cols.ConstantColumn(120); // sol label genişliği
                cols.RelativeColumn();    // sağ value
            });

            // ortak cell stili
            IContainer Cell(IContainer c) =>
                c.Border(1)
                 .BorderColor(Colors.Grey.Medium)
                 .PaddingVertical(4)
                 .PaddingHorizontal(6);

            void Row(string label, string value)
            {
                t.Cell().Element(Cell).Text(label).Bold();
                t.Cell().Element(Cell).Text(value ?? "-");
            }

            Row("Form Numarası", dto.FormNo);
            Row("Teslim Tarihi", Dt(dto.TeslimTarihi));
            Row("Müşteri Ünvanı", dto.MusteriUnvan);
            Row("Cihaz Adı", dto.CihazAdi);
            Row("Seri Numara", dto.SeriNumara);
            Row("Satınalma Tarihi", DtN(dto.SatinalmaTarihi));
            Row("Garanti Durumu", dto.GarantiText);
            Row("Arıza Açıklama", dto.ArizaAciklama);
        });
    }
}