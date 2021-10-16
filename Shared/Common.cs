using FiscalReference;
using Newtonsoft.Json;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace Shared
{
    public static class Common
    {
        public static void SetKey(this FiscalEntities db, string key, string value)
        {            
            var set = db.Settings.Where(a => a.Key == key).FirstOrDefault();
            if (set == null)
            {                
                set = new Setting() { Key = key, Value = value };
                db.Settings.Add(set);
            }
            else
            {
                set.Value = value;
            }
            db.SaveChanges();
        }

        public static RegisterInvoiceRequest RequestFromXML(string xmlstring)
        {
            var xml = new XmlSerializer(typeof(RegisterInvoiceRequest));
            RegisterInvoiceRequest request;
            using (TextReader reader = new StringReader(xmlstring))
            {
                request = (RegisterInvoiceRequest)xml.Deserialize(reader);
            }
            return request;
        }


        public static string SubstrIncl(this string text, int from, int to)
        {
            return text.Substring(from, to - from + 1);
        }

        public static string SubstrExcl(this string text, int from, int to)
        {
            return text.Substring(from + 1, to - from - 1);
        }

        public static decimal Round4(this decimal num)
        {
            return decimal.Parse(num.ToString("0.0000"));
        }

        public static decimal Round4(this decimal? num)
        {
            return decimal.Parse((num ?? 0m).ToString("0.0000"));
        }

        public static decimal Round6(this decimal num)
        {
            return decimal.Parse(num.ToString("0.000000"));
        }

        public static decimal Round6(this decimal? num)
        {
            return decimal.Parse((num ?? 0m).ToString("0.000000"));
        }

        public static decimal Round2(this decimal num)
        {
            return decimal.Parse(num.ToString("0.00"));
        }

        public static decimal Round2(this decimal? num)
        {
            return decimal.Parse((num ?? 0m).ToString("0.00"));
        }

        public static DateTime forXML(this DateTime dateTime)
        {
            var dt = TimeZoneInfo.ConvertTime(dateTime, TimeZoneInfo.Local);
            return dt.AddTicks(-(dt.Ticks % TimeSpan.TicksPerSecond));
        }

        public static string GetFiscalParameter(string parameter)
        {
            var db = new FiscalEntities();

            var test = "N";

            if (parameter == "URL" && test == "N") return "https://efi.tax.gov.me:443/fs-v1";
            if (parameter == "URL" && test == "Y") return "https://efitest.tax.gov.me:443/fs-v1";
            if (parameter == "QR" && test == "N") return "https://mapr.tax.gov.me/";
            if (parameter == "QR" && test == "Y") return "https://efitest.tax.gov.me";

            return null;
        }


        public static string ToXML(this object type)
        {
            using (var stringwriter = new System.IO.StringWriter())
            {
                var ns = new XmlSerializerNamespaces();
                ns.Add("", "https://efi.tax.gov.me/fs/schema");

                var serializer = new XmlSerializer(type.GetType());
                var settings = new XmlWriterSettings();
                settings.Indent = false;
                settings.CheckCharacters = true;
                settings.ConformanceLevel = ConformanceLevel.Document;
                settings.NamespaceHandling = NamespaceHandling.OmitDuplicates;
                settings.NewLineOnAttributes = false;
                settings.OmitXmlDeclaration = true;

                using (var stream = new StringWriter())
                using (var writer = XmlWriter.Create(stream, settings))
                {
                    serializer.Serialize(writer, type, ns);
                    return stream.ToString();
                }
            }
        }

        public static class Encryptor
        {
            public static string EncryptSimple(string clearText)
            {
                string EncryptionKey = "abc123";
                byte[] clearBytes = Encoding.Unicode.GetBytes(clearText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateEncryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(clearBytes, 0, clearBytes.Length);
                            cs.Close();
                        }
                        clearText = Convert.ToBase64String(ms.ToArray());
                    }
                }
                return clearText;
            }

            public static string DecryptSimple(string cipherText)
            {
                string EncryptionKey = "abc123";
                cipherText = cipherText.Replace(" ", "+");
                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (Aes encryptor = Aes.Create())
                {
                    Rfc2898DeriveBytes pdb = new Rfc2898DeriveBytes(EncryptionKey, new byte[] { 0x49, 0x76, 0x61, 0x6e, 0x20, 0x4d, 0x65, 0x64, 0x76, 0x65, 0x64, 0x65, 0x76 });
                    encryptor.Key = pdb.GetBytes(32);
                    encryptor.IV = pdb.GetBytes(16);
                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor.CreateDecryptor(), CryptoStreamMode.Write))
                        {
                            cs.Write(cipherBytes, 0, cipherBytes.Length);
                            cs.Close();
                        }
                        cipherText = Encoding.Unicode.GetString(ms.ToArray());
                    }
                }
                return cipherText;
            }

            public static string Base64Encode(string plainText)
            {
                var plainTextBytes = Encoding.UTF8.GetBytes(plainText);
                return Convert.ToBase64String(plainTextBytes);
            }

            public static string Base64Decode(string base64EncodedData)
            {
                var base64EncodedBytes = Convert.FromBase64String(base64EncodedData);
                return Encoding.UTF8.GetString(base64EncodedBytes);
            }

            public static string Hash(string value, string salt)
            {
                byte[] bytes = Encoding.Unicode.GetBytes(value);
                byte[] src = Convert.FromBase64String(salt);
                byte[] dst = new byte[src.Length + bytes.Length];
                Buffer.BlockCopy(src, 0, dst, 0, src.Length);
                Buffer.BlockCopy(bytes, 0, dst, src.Length, bytes.Length);
                HashAlgorithm algorithm = HashAlgorithm.Create("SHA1");
                byte[] inArray = algorithm.ComputeHash(dst);
                return Convert.ToBase64String(inArray);
            }
        }

        public static RegisterInvoiceRequest Invoice(this Invoice doc, FiscalEntities db)
        {
            var tcr = doc.ENU;
            var now = DateTime.Now;
            doc.Datum = now;

            var inv = new RegisterInvoiceRequest();
            inv.Header = new RegisterInvoiceRequestHeaderType();
            inv.Id = "Request";
            inv.Version = 1;
            inv.Invoice = new InvoiceType();

            // DEFINICIJA TIPA RACUNA...
            inv.Invoice.TypeOfInv = InvoiceSType.NONCASH;
            inv.Invoice.InvType = InvoiceTSType.INVOICE;
            inv.Header.UUID = Guid.NewGuid().ToString();
            inv.Header.SendDateTime = now.forXML();

            inv.Invoice.TypeOfInv = InvoiceSType.CASH;
            inv.Invoice.TCRCode = db.Settings.Where(a => a.Key == "ENU").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();
            inv.Invoice.IssueDateTime = now.forXML();
            inv.Invoice.BusinUnitCode = db.Settings.Where(a => a.Key == "BU").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();
            inv.Invoice.InvOrdNum = doc.Broj;
            inv.Invoice.IsIssuerInVAT = true;

            inv.Invoice.TaxPeriod = inv.Invoice.IssueDateTime.ToString("MM") + "/" + inv.Invoice.IssueDateTime.ToString("yyyy");
            inv.Invoice.InvNum = inv.CalcInvNum();

            var oper = db.Settings.Where(a => a.Key == "OPER").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();
            //if ((oper ?? "") == "") throw new Exception("Neispravno definisan operator!");

            inv.Invoice.OperatorCode = oper;
            inv.Invoice.SoftCode = db.Settings.Where(a => a.Key == "SOFT_CODE_PU").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();

            inv.Invoice.Currency = new CurrencyType();
            inv.Invoice.Currency.Code = CurrencyCodeSType.EUR;
            inv.Invoice.Currency.ExRate = 1;

            inv.Invoice.Seller = new SellerType();
            inv.Invoice.Seller.IDType = IDTypeSType.TIN;
            inv.Invoice.Seller.IDNum = db.Settings.Where(a => a.Key == "PIB").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();
            inv.Invoice.Seller.Name = db.Settings.Where(a => a.Key == "NAZIV").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();
            inv.Invoice.Seller.Address = db.Settings.Where(a => a.Key == "ADRESA").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();

            if (doc.Partner != null)
            {
                var partner = db.Partners.SingleOrDefault(a => a.IdPartner == doc.Partner);

                inv.Invoice.Buyer = new BuyerType();
                inv.Invoice.Buyer.IDType = IDTypeSType.TIN;
                inv.Invoice.Buyer.IDNum = (partner.PIB ?? "00000000").Trim();
                inv.Invoice.Buyer.Name = partner.Naziv.Trim();
                inv.Invoice.Buyer.Address = (partner.Adresa ?? "-").Trim();

                if (partner.PIB == null || partner.PIB == "") throw new Exception("Morate unijeti ispravan PIB kupca!");
            }
            else
            {
                inv.Invoice.Buyer = new BuyerType();
                inv.Invoice.Buyer.IDType = IDTypeSType.TIN;
                inv.Invoice.Buyer.IDNum = "00000000";
                inv.Invoice.Buyer.Name = "Fizičko lice";
            }

            List<InvoiceItem> stavke = new List<InvoiceItem>();
            List<InvoicePayment> placanja = new List<InvoicePayment>();

            inv.Invoice.TaxFreeAmt = 0;
            inv.Invoice.MarkUpAmt = 0;
            inv.Invoice.GoodsExAmt = 0;

            {
                stavke = doc.InvoiceItems.ToList();
                placanja = doc.InvoicePayments.ToList();

                #region Placanja

                var payments = new List<PayMethodType>();
                if (placanja.Any())
                {
                    foreach (var p in placanja)
                    {
                        var pay = new PayMethodType();
                        pay.Amt = p.Iznos.Round2();
                        pay.Type = p.Tip == 1 ? PaymentMethodTypeSType.BANKNOTE : p.Tip == 2 ? PaymentMethodTypeSType.ACCOUNT : p.Tip == 3 ? PaymentMethodTypeSType.CARD : p.Tip == 4 ? PaymentMethodTypeSType.ADVANCE : PaymentMethodTypeSType.OTHER;
                        payments.Add(pay);
                    }
                    if (payments.All(a => a.Type == PaymentMethodTypeSType.ACCOUNT || a.Type == PaymentMethodTypeSType.ADVANCE)) inv.Invoice.TypeOfInv = InvoiceSType.NONCASH;
                }

                inv.Invoice.PayMethods = payments.GroupBy(a => new { a.Type, AdvIIC = a.Type == PaymentMethodTypeSType.ADVANCE ? payments.Select(b => b.AdvIIC).FirstOrDefault() : null }).Select(a => new PayMethodType() { Type = a.Key.Type, AdvIIC = a.Key.AdvIIC, Amt = a.Sum(b => b.Amt) }).ToArray();

                #endregion

                #region Stavke

                var items = new List<InvoiceItemType>();

                var prc_type = "UPA";

                foreach (var s in stavke)
                {
                    var artikal = db.Items.SingleOrDefault(a => a.IdArtikal == s.ItemId);
                    var itm = new InvoiceItemType();
                    itm.C = artikal.Sifra ?? artikal.IdArtikal.ToString().Trim();
                    itm.N = artikal.Naziv.Substring(0, artikal.Naziv.Length >= 50 ? 50 : artikal.Naziv.Length).Trim();
                    itm.Q = (double)s.Quantity; // Kolicina
                    itm.U = artikal.JM; // Jedinica Mjere
                    itm.VR = (s.Tax).Round4(); // Stopa PDV-a
                    itm.UPA = (s.FinalPrice).Round4(); // Cijena sa PDV
                    itm.UPB = (s.FinalPrice / ((1 + s.Tax / 100m) * (1m - s.Discount / 100m))).Round4(); // Cijena bez PDV

                    //if (s.FkPorez >= 117 && s.FkPorez <= 140)
                    //{
                    //    itm.EX = GetExemptS(s.FkPorez.Value);
                    //    itm.EXSpecified = true;
                    //}

                    itm.R = s.Discount.Round2(); // Rabat u procentima
                    itm.RR = false; // Da li rabat smanjuje cijenu
                    itm.RSpecified = true;
                    itm.RRSpecified = true;

                    itm.PA = (s.Quantity * s.FinalPrice).Round4(); // Cijena sa PDV-a
                    itm.VA = (s.Quantity * s.FinalPrice * (1m - 1m / (1 + s.Tax / 100m))).Round4(); // Iznos PDV-a
                    itm.PB = (s.Quantity * s.FinalPrice - s.Quantity * s.FinalPrice * (1m - 1m / (1 + s.Tax / 100m))).Round4(); // Iznos bez PDV-a                    

                    itm.VRSpecified = true;
                    itm.VASpecified = true;
                    items.Add(itm);
                }
                inv.Invoice.Items = items.ToArray();

                #endregion

                #region Porezi

                var taxes = new List<SameTaxType>();

                var porezi = stavke.GroupBy(a => a.Tax);

                if (db.Settings.Where(a => a.Key == "INVAT").Select(a => a.Value).DefaultIfEmpty("D").FirstOrDefault() == "D")
                {
                    foreach (var p in items.GroupBy(a => new { a.EX, a.EXSpecified, a.VR }))
                    {
                        var tax = new SameTaxType();
                        tax.VATRate = p.Key.VR.Round2();
                        //tax.VATRateSpecified = true;
                        tax.NumOfItems = p.Count();
                        tax.PriceBefVAT = (p.Select(a => a.PB).Sum()).Round2();
                        tax.VATAmt = (p.Select(a => a.VA).Sum()).Round2();
                        //tax.VATAmtSpecified = true;
                        if (p.Key.EXSpecified)
                        {
                            tax.ExemptFromVAT = p.Key.EX;
                            tax.ExemptFromVATSpecified = true;
                            tax.VATRateSpecified = false;
                            tax.VATAmtSpecified = false;
                        }
                        else
                        {
                            tax.ExemptFromVATSpecified = false;
                            tax.VATRateSpecified = true;
                            tax.VATAmtSpecified = true;
                        }
                        taxes.Add(tax);
                    }


                    inv.Invoice.SameTaxes = taxes.ToArray();

                    inv.Invoice.TotVATAmt = taxes.Select(a => a.VATAmt).Sum().Round2();
                    inv.Invoice.TotPriceWoVAT = (inv.Invoice.TotPrice - inv.Invoice.TotVATAmt).Round2();
                }

                #endregion

                #region Total

                inv.Invoice.TotPriceWoVAT = stavke.Select(a => a.Total).DefaultIfEmpty(0m).Sum().Round2();
                inv.Invoice.TotPrice = stavke.Select(a => a.Total).DefaultIfEmpty(0m).Sum().Round2();

                if (db.Settings.Where(a => a.Key == "INVAT").Select(a => a.Value).DefaultIfEmpty("D").FirstOrDefault() == "D")
                {
                    inv.Invoice.TotVATAmt = inv.Invoice.SameTaxes.Select(a => a.VATAmt).Sum();
                    inv.Invoice.TotVATAmtSpecified = true;
                }
                else
                {
                    inv.Invoice.TaxFreeAmt = inv.Invoice.TotPriceWoVAT;
                    inv.Invoice.TaxFreeAmtSpecified = true;
                }

                #endregion
            }

            return inv;
        }


        public static RegisterInvoiceRequest Fiscalize(Invoice racun, FiscalEntities db)
        {
            racun = db.Invoices.Single(a => a.IdInvoice == racun.IdInvoice);
            var tcr = racun.ENU;

            if (racun.Status == "F") throw new Exception("Račun je već fiskalizovan!");


            var url = GetFiscalParameter("URL");

            //if (doc.Broj == 0)
            {

            }

            // ------- GENERISANJE REQUESTA --------
            var request = racun.Invoice(db);

            System.ServiceModel.Channels.Binding binding = new System.ServiceModel.BasicHttpBinding(System.ServiceModel.BasicHttpSecurityMode.Transport);
            System.ServiceModel.EndpointAddress endpoint = new System.ServiceModel.EndpointAddress(url);
            FiscalizationServicePortTypeClient client = new FiscalizationServicePortTypeClient(binding, endpoint);
            System.Net.ServicePointManager.SecurityProtocol = System.Net.SecurityProtocolType.Tls12;

            FiscalRequest fr = null;
            RegisterInvoiceResponse response = null;

            var xml = request.SignRequest();

            try
            {
                fr = new FiscalRequest();
                fr.Vrsta = "RegisterInvoice";
                db.FiscalRequests.Add(fr);
                fr.Datum = DateTime.Now;
                fr.Status = "N";
                fr.Request = xml;
                fr.IKOF = request.Invoice.IIC;

                File.WriteAllText("request.xml", xml);

                racun.Status = "N";
                racun.IKOF = request.Invoice.IIC;
                //racun.JIKR = request.URL;

                db.SaveChanges();

                //response = client.registerInvoice(request);
                //fr.Status = "F";
                //racun.Status = "F";
                //racun.JIKR = response.FIC;
                db.SaveChanges();

            }
            catch (Exception excp)
            {
                throw new Exception(excp.Message);
            }
            finally
            {
                db.SaveChanges();
            }


            return request;
        }
    }
}
