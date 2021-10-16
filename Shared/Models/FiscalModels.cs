using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text;


namespace Shared.Models
{
    public class Invoice
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInvoice { get; set; }
        public DateTime Datum { get; set; }
        public int Broj { get; set; }
        public int? Partner { get; set; }
        public string ENU { get; set; }
        public string IKOF { get; set; }
        public string JIKR { get; set; }
        public string OPER { get; set; }
        public string Synced { get; set; }
        public string Status { get; set; }

        public List<InvoiceItem> InvoiceItems { get; set; }
        public List<InvoicePayment> InvoicePayments { get; set; }
    }

    public partial class InvoiceItem
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInvoiceItem { get; set; }
        public int Invoice { get; set; }
        public int ItemId { get; set; }
        public string ItemDsc { get; set; }
        public string ItemCode { get; set; }
        public string ItemUnit { get; set; }
        public decimal Quantity { get; set; }
        public decimal Tax { get; set; }
        public decimal Price { get; set; }
        public decimal Discount { get; set; }
        public decimal FinalPrice { get; set; }
        public decimal Total { get; set; }
    }

    public partial class InvoicePayment
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdInvoicePayment { get; set; }
        public int Invoice { get; set; }
        public int Tip { get; set; }
        public decimal Iznos { get; set; }
    }

    public partial class FiscalRequest
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdFiscalRequest { get; set; }
        public string IKOF { get; set; }
        public string Vrsta { get; set; }
        public string Status { get; set; }
        public System.DateTime Datum { get; set; }
        public string Request { get; set; }
        public string Synced { get; set; }
        public Nullable<decimal> Iznos { get; set; }
        public string TypeOfInv { get; set; }
        public int Broj { get; set; }
        public string FCDC { get; set; }
        public string JIKR { get; set; }
    }

    public class Setting
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdSetting { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
    }

    public partial class Auth
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdAuth { get; set; }
        public string Data { get; set; }
        public string Sess { get; set; }
    }

    public partial class Cert
    {
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int IdCert { get; set; }
        public byte[] CertData { get; set; }
        public string CertPass { get; set; }
    }

    public partial class Item
    {
        [Key]
        public int IdArtikal { get; set; }
        public string Sifra { get; set; }
        public string Barcode { get; set; }
        public string Naziv { get; set; }
        public string JM { get; set; }
        public int FkPorez { get; set; }
        public decimal Porez { get; set; }
        public decimal Cijena { get; set; }
        public string Bundle { get; set; }
    }

    public partial class Partner
    {
        [Key]
        public int IdPartner { get; set; }
        public string Naziv { get; set; }
        public string PIB { get; set; }
        public string Adresa { get; set; }
        public string Vrsta { get; set; }
    }

    public partial class User
    {
        [Key]
        public int UserName { get; set; }
        public string PasswordHash { get; set; }
        public string PasswordSalt { get; set; }
        public string RealName { get; set; }
        public string Operator { get; set; }
        public string Roles { get; set; }
    }

    public partial class Tax
    {
        [Key]
        public int IdPorez { get; set; }
        public string Naziv { get; set; }
        public string Stopa { get; set; }
    }

    public class FiscalEntities : DbContext
    {
        public DbSet<Item> Items { get; set; }
        public DbSet<Partner> Partners { get; set; }
        public DbSet<Invoice> Invoices { get; set; }
        public DbSet<Setting> Settings { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Tax> Taxes { get; set; }
        public DbSet<Auth> Auth { get; set; }
        public DbSet<Cert> Cert { get; set; }
        public DbSet<FiscalRequest> FiscalRequests { get; set; }

        private const string DatabaseName = "fiscal.db";

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            String databasePath = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), DatabaseName);
            //switch (Device.RuntimePlatform)
            //{
            //    case Device.iOS:
            //        databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", DatabaseName);
            //        break;
            //    case Device.Android:
            //        databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Personal), DatabaseName);
            //        break;
            //    default:
            //        throw new NotImplementedException("Platform not supported");
            //}
            optionsBuilder.UseSqlite($"Filename={databasePath}");
        }
    }
}
