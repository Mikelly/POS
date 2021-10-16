using Newtonsoft.Json;
using Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace Shared
{
    public class Sync
    {
        public static System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.GetCultureInfo("en-US");
        public static HttpClient client;
        public static HttpClientHandler handler;

        public static bool sync_pos = false;

        #region Inner Classes

        public class SyncLoginResponse
        {
            public string Info { get; set; }
            public string Error { get; set; }
            public string Auth { get; set; }
            public string Sess { get; set; }
            public string Oper { get; set; }
        }

        public class SyncModel
        {
            public class SyncArtikal
            {
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

            public class SyncPartner
            {
                public int IdPartner { get; set; }
                public string Naziv { get; set; }
                public string PIB { get; set; }
                public string Adresa { get; set; }
                public string Vrsta { get; set; }
            }

            public class SyncSetting
            {
                public string Key { get; set; }
                public string Value { get; set; }
            }

            public class SyncUser
            {
                public string UserName { get; set; }
                public string PasswordHash { get; set; }
                public string PasswordSalt { get; set; }
                public string RealName { get; set; }
                public string Operator { get; set; }
                public string Roles { get; set; }
            }

            public class SyncTax
            {
                public int IdPorez { get; set; }
                public string Naziv { get; set; }
                public decimal Stopa { get; set; }                                
            }

            public byte[] cert_data { get; set; }
            public string cert_pass { get; set; }

            public List<SyncArtikal> items { get; set; }
            public List<SyncPartner> partners { get; set; }
            public List<SyncSetting> settings { get; set; }
            public List<SyncUser> users { get; set; }
            public List<SyncTax> taxes { get; set; }
        }

        public class SyncInvoice
        {
            public class SyncItem
            {
                public int ItemId { get; set; }
                public string Unit { get; set; }
                public decimal Tax { get; set; }
                public decimal Quantity { get; set; }
                public decimal Price { get; set; }
                public decimal Discount { get; set; }
                public decimal Total { get; set; }
            }

            public class SyncPayment
            {
                public int Type { get; set; }
                public decimal Amount { get; set; }
            }

            public int Broj { get; set; }
            public DateTime Datum { get; set; }
            public int? Kupac { get; set; }
            public string ENU { get; set; }
            public string BU { get; set; }
            public string OPER { get; set; }
            public string IKOF { get; set; }
            public string Request { get; set; }

            public List<SyncItem> items = new List<SyncItem>();
            public List<SyncPayment> payments = new List<SyncPayment>();
        }

        #endregion

        #region Basic Methods

        static Sync()
        {
            FiscalEntities db = new FiscalEntities();
            var url = db.Settings.Where(a => a.Key == "URL").Select(a => a.Value).DefaultIfEmpty("https://fiskal.oblak.online").FirstOrDefault();
            var auth = db.Auth.FirstOrDefault();
            var cookie = auth?.Data;
            var sess = auth?.Sess;

            var baseAddress = new Uri(url);
            var cookieContainer = new CookieContainer();

            cookieContainer.Add(baseAddress, new Cookie(".ASPXAUTH", cookie));
            cookieContainer.Add(baseAddress, new Cookie("ASP.NET_SessionId", sess));

            handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            client = new HttpClient(handler) { BaseAddress = baseAddress };
        }

        public static void SetHttpClient(string auth, string sess, string url = null)
        {
            client.Dispose();
            handler.Dispose();

            FiscalEntities db = new FiscalEntities();
            url = db.Settings.Where(a => a.Key == "URL").Select(a => a.Value).DefaultIfEmpty("https://fiskal.oblak.online").FirstOrDefault();

            var baseAddress = new Uri(url);
            var cookieContainer = new CookieContainer();

            cookieContainer.Add(baseAddress, new Cookie(".ASPXAUTH", auth));
            cookieContainer.Add(baseAddress, new Cookie("ASP.NET_SessionId", sess));

            handler = new HttpClientHandler() { CookieContainer = cookieContainer };
            client = new HttpClient(handler) { BaseAddress = baseAddress };
        }

        public async static Task<string> HttpCallString(string url, FormUrlEncodedContent form = null)
        {
            HttpResponseMessage response = null;
            string responsetext = "";

            if (form != null)
                response = await client.PostAsync(url, form);
            else
                response = await client.GetAsync(url);

            responsetext = await response.Content.ReadAsStringAsync();

            response.EnsureSuccessStatusCode();

            return responsetext;
        }

        public async static Task<Stream> HttpCallStream(string url, FormUrlEncodedContent form = null)
        {
            HttpResponseMessage response = null;
            Stream responsestream = null;

            if (form != null)
                response = await client.PostAsync(url, form);
            else
                response = await client.GetAsync(url);

            responsestream = await response.Content.ReadAsStreamAsync();

            response.EnsureSuccessStatusCode();

            return responsestream;
        }

        #endregion

        #region Remote Calls

        public static bool CheckNet()
        {
            string host = "http://www.google.com";            
            Ping p = new Ping();
            try
            {
                PingReply reply = p.Send(host, 3000);
                if (reply.Status == IPStatus.Success) return true;
                else return false;
            }
            catch(Exception e) {
                return false;
            }            
        }

        public async static Task<bool> CheckAuth(bool tooltip = true)
        {
            try
            {
                var response = await HttpCallString($"/pos/checkauth");

                if (response == "0") return false;
                else return true;
            }
            catch (Exception e)
            {                
                return false;
            }
        }

        public async static Task<bool> Login(string username, string password)
        {
            var formContent = new FormUrlEncodedContent(new[] {
                new KeyValuePair<string, string>("username", username),
                new KeyValuePair<string, string>("password", password)
            });

            var response = await HttpCallString($"/pos/login", formContent);

            var login = JsonConvert.DeserializeObject<SyncLoginResponse>(response);

            var db = new FiscalEntities();

            if (login.Error != "")
            {
                var auth = db.Auth.FirstOrDefault();
                auth.Data = "";
                auth.Sess = "";
                db.SaveChanges();
                return false;
            }
            else
            {
                SetHttpClient(login.Auth, login.Sess);
                if (login.Oper != "")
                {
                    db.SetKey("OPER", login.Oper);
                }
                return true;
            }
        }

        public async static Task<string> SyncPos()
        {
            try
            {
                if (sync_pos == true) return "Sinhronizacija je već u toku.";

                sync_pos = true;                

                bool authed = await CheckAuth();
                if (authed == false) return "Neispravna autentifikacija na server.";

                FiscalEntities db = new FiscalEntities();
                var enu = db.Settings.Where(a => a.Key == "ENU").Select(a => a.Value).DefaultIfEmpty("").FirstOrDefault();

                var response = await HttpCallString($"/pos/sync?enu={enu}");

                var sync = JsonConvert.DeserializeObject<SyncModel>(response);

                await Task.Run(() =>
                {   
                    db.Database.ExecuteSqlRaw(@"DELETE FROM Items");
                    db.Database.ExecuteSqlRaw(@"DELETE FROM Partners");
                    db.Database.ExecuteSqlRaw(@"DELETE FROM Users");
                    db.Database.ExecuteSqlRaw(@"DELETE FROM Taxes");
                    db.Database.ExecuteSqlRaw(@"DELETE FROM Settings WHERE [KEY] NOT IN ('URL', 'ENU', 'PRINTER', 'PRINTER_NAME')");
                    db.Database.ExecuteSqlRaw(@"DELETE FROM Cert");
                    
                    foreach (var a in sync.items)
                    {
                        var sql = $"INSERT INTO Items(IdArtikal, Sifra, Barcode, Naziv, JM, FkPorez, Porez, Cijena, Bundle) VALUES({a.IdArtikal},'{a.Sifra}','{a.Barcode}',N'{a.Naziv}','{a.JM}',{a.FkPorez},{a.Porez.ToString("n2", culture).Replace(",", "")},{a.Cijena.ToString("n2", culture).Replace(",", "")},'{a.Bundle}')";
                        db.Database.ExecuteSqlRaw(sql);                    
                    }                    

                    foreach (var p in sync.partners)
                    {
                        var sql = $"INSERT INTO Partners(IdPartner, Naziv, PIB, Vrsta, Adresa) VALUES({p.IdPartner},N'{p.Naziv}','{p.PIB}','{p.Vrsta}','{p.Adresa}')";
                        db.Database.ExecuteSqlRaw(sql);                        
                    }                   

                    foreach (var s in sync.settings)
                    {
                        db.Database.ExecuteSqlRaw($"INSERT INTO Settings([Key],[Value]) VALUES('{s.Key}','{s.Value}')");                        
                    }

                    foreach (var u in sync.users)
                    {
                        db.Database.ExecuteSqlRaw($"INSERT INTO Users([UserName],[PasswordHash],[PasswordSalt],[RealName],[Operator],[Roles]) VALUES(N'{u.UserName}','{u.PasswordHash}','{u.PasswordSalt}',N'{u.RealName}','{u.Operator}','{u.Roles}')");
                    }

                    foreach (var t in sync.taxes)
                    {
                        db.Database.ExecuteSqlRaw($"INSERT INTO Taxes(IdPorez,Naziv,Stopa) VALUES({t.IdPorez},N'{t.Naziv}',{t.Stopa})");
                    }

                    var cert = new Cert();
                    cert.CertData = sync.cert_data;
                    cert.CertPass = sync.cert_pass;
                    db.Cert.Add(cert);
                    db.SaveChanges();                    
                });

                return "";
            }
            catch (Exception e)
            {   
                return e.Message;
            }
            finally
            {
                sync_pos = false;
            }
        }

        #endregion
    }
}
