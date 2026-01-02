using Microsoft.EntityFrameworkCore;
using ViotekErp.Models;

namespace ViotekErp.Data
{
    public class ServisDbContext : DbContext
    {
        public ServisDbContext(DbContextOptions<ServisDbContext> options) : base(options) { }

        public DbSet<TblServis> Servisler => Set<TblServis>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TblServis>(e =>
            {
                e.ToTable("TBL_SERVIS", tb => tb.HasTrigger("TG_SERVIS_INSERT"));

                e.HasKey(x => x.ServisId);

                // Kolon eşleşmeleri (TblServis property adların buna uygunsa gerek yok,
                // değilse kesin yaz)
                e.Property(x => x.ServisId).HasColumnName("SERVIS_ID");
                e.Property(x => x.ServisTarih).HasColumnName("SERVIS_TARIH");
                e.Property(x => x.ServisCariKod).HasColumnName("SERVIS_CARI_KOD");
                e.Property(x => x.ServisStokKod).HasColumnName("SERVIS_STOK_KOD");
                e.Property(x => x.ServisSeriNumara).HasColumnName("SERVIS_SERI_NUMARA");
                e.Property(x => x.ServisSatinalmaTarih).HasColumnName("SERVIS_SATINALMA_TARIH");
                e.Property(x => x.ServisGaranti).HasColumnName("SERVIS_GARANTI");
                e.Property(x => x.ServisTeslimEden).HasColumnName("SERVIS_TESLIM_EDEN");
                e.Property(x => x.ServisTeslimAlan).HasColumnName("SERVIS_TESLIM_ALAN");
                e.Property(x => x.ServisArizaAciklama).HasColumnName("SERVIS_ARIZA_ACIKLAMA");

                e.Property(x => x.TedGonderimTarih).HasColumnName("TED_GONDERIM_TARIH");
                e.Property(x => x.TedGonderimCariKod).HasColumnName("TED_GONDERIM_CARI_KOD");
                e.Property(x => x.TedGonderimAciklama).HasColumnName("TED_GONDERIM_ACIKLAMA");

                e.Property(x => x.TedAlimTarih).HasColumnName("TED_ALIM_TARIH");
                e.Property(x => x.TedAlimYapilanIslem).HasColumnName("TED_ALIM_YAPILAN_ISLEM");
                e.Property(x => x.TedAlimSeriNumara).HasColumnName("TED_ALIM_SERI_NUMARA");
                e.Property(x => x.TedAlimTeslimAlan).HasColumnName("TED_ALIM_TESLIM_ALAN");

                e.Property(x => x.MusTeslimTarih).HasColumnName("MUS_TESLIM_TARIH");
                e.Property(x => x.MusYapilanIslem).HasColumnName("MUS_YAPILAN_ISLEM");
                e.Property(x => x.MusSeriNumara).HasColumnName("MUS_SERI_NUMARA");
                e.Property(x => x.MusTeslimEden).HasColumnName("MUS_TESLIM_EDEN");
                e.Property(x => x.MusTeslimAlan).HasColumnName("MUS_TESLIM_ALAN");

                e.Property(x => x.ServisDurum).HasColumnName("SERVIS_DURUM");
                e.Property(x => x.ServisTamamlandi).HasColumnName("SERVIS_TAMAMLANDI");
                e.Property(x => x.ServisAktif).HasColumnName("SERVIS_AKTIF");

                e.Property(x => x.InsertId).HasColumnName("INSERT_ID");
                e.Property(x => x.InsertDate).HasColumnName("INSERT_DATE");
                e.Property(x => x.UpdateId).HasColumnName("UPDATE_ID");
                e.Property(x => x.UpdateDate).HasColumnName("UPDATE_DATE");

                e.Property(x => x.ServisGirenSaticiKod).HasColumnName("SERVIS_GIREN_SATICI_KOD");
            });
        }
    }
}