using Microsoft.EntityFrameworkCore;
using ViotekErp.Models;

namespace ViotekErp.Data
{
    public class MikroDbContext : DbContext
    {
        public MikroDbContext(DbContextOptions<MikroDbContext> options)
            : base(options)
        {
        }

        public DbSet<Siparis> Siparisler { get; set; }
        public DbSet<CariHesap> CariHesaplar { get; set; }

        // 🔹 KULLANICILAR tablosundan gelen view
        public DbSet<Satici> Saticilar { get; set; }

        // 🔹 Stok tabloları & view
        public DbSet<Stok> Stoklar { get; set; }
        public DbSet<StokMevcut> StokMevcut { get; set; }   // VW_STOK_MEVCUT view
        public DbSet<StokHareket> StokHareketleri { get; set; }

        // 🔹 Finans için satış özeti view’i
        public DbSet<SalesSummaryView> SalesSummaries { get; set; }  // VW_SATIS_OZET
        public DbSet<SorumluAd> SorumluAdlari { get; set; }
        public DbSet<erp_kullanici> erp_kullanici => Set<erp_kullanici>();

        public DbSet<StokAdi> StokAdlari { get; set; }
        public DbSet<TblServis> Servisler { get; set; } = default!;

        public DbSet<GiderHareket> GiderHareketler { get; set; } = null!;
                public DbSet<KasaYonetimRow> KasalarYonetim => Set<KasaYonetimRow>();


        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            
            modelBuilder.Entity<KasaYonetimRow>(entity =>
            {
                entity.ToView("KASALAR_YONETIM");
            });
            modelBuilder.Entity<GiderHareket>(e =>
    {
        e.ToTable("Gider_Hareket", "dbo");

        // KEY: #msg_S_0088
        e.HasKey(x => x.MsgS0088);

        e.Property(x => x.MsgS0998).HasColumnName("msg_S_0998");
        e.Property(x => x.MsgS0088).HasColumnName("#msg_S_0088");
        e.Property(x => x.MsgS0089).HasColumnName("msg_S_0089");

        e.Property(x => x.MsgS0223).HasColumnName("msg_S_0223");
        e.Property(x => x.MsgS0137).HasColumnName("msg_S_0137");
        e.Property(x => x.MsgS1158).HasColumnName("msg_S_1158");
        e.Property(x => x.MsgS0203).HasColumnName("msg_S_0203");
        e.Property(x => x.MsgS0433).HasColumnName("msg_S_0433");

        e.Property(x => x.MsgS1167).HasColumnName("msg_S_1167");
        e.Property(x => x.MsgS1168).HasColumnName("msg_S_1168");

        e.Property(x => x.MsgS0471).HasColumnName("msg_S_0471");
        e.Property(x => x.MsgS0472).HasColumnName("msg_S_0472");
        e.Property(x => x.MsgS0473).HasColumnName("msg_S_0473");

        e.Property(x => x.MsgS1162).HasColumnName("msg_S_1162");
        e.Property(x => x.MsgS1035).HasColumnName("msg_S_1035");

        e.Property(x => x.MsgS1163).HasColumnName("msg_S_1163");
        e.Property(x => x.MsgS1164).HasColumnName("msg_S_1164");
        e.Property(x => x.MsgS1160).HasColumnName("msg_S_1160");
        e.Property(x => x.MsgS1165).HasColumnName("msg_S_1165");
        e.Property(x => x.MsgS1166).HasColumnName("msg_S_1166");
    });
    modelBuilder.Entity<erp_kullanici>(e =>
            {
                e.HasKey(x => x.Id);
                e.HasIndex(x => x.KullaniciAdi).IsUnique();
                e.Property(x => x.Rol).HasDefaultValue("User");
                e.Property(x => x.AktifMi).HasDefaultValue(true);
            });

            modelBuilder.Entity<SorumluAd>()
                        .HasNoKey()
                        .ToView("Vw_SiparisSorumluAdlari");

            modelBuilder.Entity<StokAdi>()
                        .HasNoKey()
                        .ToView("Vw_StokAdlari");

            // STOKLAR
            modelBuilder.Entity<Stok>(e =>
            {
                e.ToTable("STOKLAR");
                e.HasKey(x => x.StoKod);

                e.Property(x => x.StoKod).HasColumnName("sto_kod");
                e.Property(x => x.StoIsim).HasColumnName("sto_isim");
                e.Property(x => x.MarkaKodu).HasColumnName("sto_marka_kodu");
                e.Property(x => x.KategoriKodu).HasColumnName("sto_kategori_kodu");
            });

            // VW_STOK_MEVCUT (VIEW)
            modelBuilder.Entity<StokMevcut>(e =>
            {
                e.ToView("VW_STOK_MEVCUT");   // view adı
                e.HasNoKey();

                e.Property(x => x.StoKod).HasColumnName("sto_kod");
                e.Property(x => x.StoIsim).HasColumnName("sto_isim");
                e.Property(x => x.StoBirim1Ad).HasColumnName("sto_birim1_ad");
                e.Property(x => x.MevcutMiktar).HasColumnName("MevcutMiktar");
            });

            // STOK_HAREKETLERI
            modelBuilder.Entity<StokHareket>(e =>
            {
                e.ToTable("STOK_HAREKETLERI");
                e.HasKey(x => x.SthGuid);

                e.Property(x => x.SthGuid).HasColumnName("sth_Guid");
                e.Property(x => x.SthTarih).HasColumnName("sth_tarih");
                e.Property(x => x.StokKod).HasColumnName("sth_stok_kod");
                e.Property(x => x.Miktar).HasColumnName("sth_miktar");
                e.Property(x => x.Tutar).HasColumnName("sth_tutar");
                e.Property(x => x.EvrakSeri).HasColumnName("sth_evrakno_seri");
                e.Property(x => x.EvrakSira).HasColumnName("sth_evrakno_sira");
                e.Property(x => x.CariKodu).HasColumnName("sth_cari_kodu");
                e.Property(x => x.PlasiyerKodu).HasColumnName("sth_plasiyer_kodu");
                e.Property(x => x.GirisDepoNo).HasColumnName("sth_giris_depo_no");
                e.Property(x => x.CikisDepoNo).HasColumnName("sth_cikis_depo_no");
                e.Property(x => x.Tip).HasColumnName("sth_tip");
            });

            // VW_SATIS_OZET (Finans ekranı için satış özeti view’i)
            modelBuilder.Entity<SalesSummaryView>(e =>
            {
                e.HasNoKey();
                e.ToView("VW_SATIS_OZET");

                e.Property(x => x.Yil).HasColumnName("Yil");
                e.Property(x => x.Ay).HasColumnName("Ay");
                e.Property(x => x.ToplamTutar).HasColumnName("ToplamTutar");
            });
        }
    }
}
