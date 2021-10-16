using Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using Shared;
using static Shared.Common;

namespace FiscalReference
{
    public partial class RegisterInvoiceRequest
    {
        public const string XML_SCHEMA_NS = "https://efi.tax.gov.me/fs/schema";
        public const string XML_REQUEST_ID = "Request";
        public const string XML_SIG_METHOD = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";
        public const string XML_DIG_METHOD = "http://www.w3.org/2001/04/xmlenc#sha256";

        [XmlIgnore]
        public string URL
        {
            get
            {
                var d = this.Invoice.IssueDateTime;
                var dtm = $"{d.ToString("yyyy")}-{d.ToString("MM")}-{d.ToString("dd")}T{d.ToString("HH")}:{d.ToString("mm")}:{d.ToString("ss")}{d.ToString("zzz")}";

                var qrurl = GetFiscalParameter("QR");

                return $@"{qrurl}/ic/#/verify?iic={this.Invoice.IIC}&tin={this.Invoice.Seller.IDNum}&crtd={dtm}&ord={this.Invoice.InvOrdNum}&bu={this.Invoice.BusinUnitCode}&cr={this.Invoice.TCRCode}&sw={this.Invoice.SoftCode}&prc={this.Invoice.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US")).Replace(",", "")}";
            }
            set
            {

            }
        }

        [XmlIgnore]
        public string FIC
        {
            get;
            set;
        }

        [XmlIgnore]
        public string OperatorName
        {
            get;
            set;
        }


        public string CalcInvNum()
        {
            if (this.Invoice.TypeOfInv == InvoiceSType.CASH)
            {
                return $"{this.Invoice.BusinUnitCode}/{this.Invoice.InvOrdNum}/{this.Invoice.IssueDateTime.Year}/{this.Invoice.TCRCode}";
            }
            else
            {
                return $"{this.Invoice.BusinUnitCode}/{this.Invoice.InvOrdNum}/{this.Invoice.IssueDateTime.Year}/{this.Invoice.TCRCode}";
            }
        }

        public void SignIKOF()
        {
            FiscalEntities db = new FiscalEntities();
            var pass = db.Settings.Where(a => a.Key == "SSL_CERT_PU_PASS").Single().Value;
            var cert = db.Cert.FirstOrDefault().CertData;

            var iicInput = "";
            // issuerTIN
            iicInput += "12345678";
            // dateTimeCreated
            iicInput += "|2019-06-12T17:05:43+02:00";
            // invoiceNumber
            iicInput += "|9952";
            // busiUnitCode
            iicInput += "|bb123bb123";
            // tcrCode
            iicInput += "|cc123cc123";
            // softCode
            iicInput += "|ss123ss123";
            // totalPrice
            iicInput += "|99.01";

            var d = this.Invoice.IssueDateTime;
            var dtm = $"{d.ToString("yyyy")}-{d.ToString("MM")}-{d.ToString("dd")}T{d.ToString("HH")}:{d.ToString("mm")}:{d.ToString("ss")}{d.ToString("zzz")}";

            //iicInput = $"{this.Invoice.Seller.IDNum}|{this.Invoice.IssueDateTime.ToString("yyyy-MM-ddTHH:mm:sszzz")}|{this.Invoice.InvOrdNum}|{this.Invoice.BusinUnitCode}|{this.Invoice.TCRCode}|{this.Invoice.SoftCode}|{this.Invoice.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}";
            iicInput = $"{this.Invoice.Seller.IDNum}|{dtm}|{this.Invoice.InvOrdNum}|{this.Invoice.BusinUnitCode}|{this.Invoice.TCRCode}|{this.Invoice.SoftCode}|{this.Invoice.TotPrice.ToString("n2", System.Globalization.CultureInfo.GetCultureInfo("en-US")).Replace(",", "")}";

            X509Certificate2 keyStore = new X509Certificate2(cert, Encryptor.DecryptSimple(pass));

            // Load a private from a key store
            RSA privateKey = keyStore.GetRSAPrivateKey();

            // Create IIC signature according to RSASSA-PKCS-v1_5            
            byte[] iicSignature = privateKey.SignData(Encoding.ASCII.GetBytes(iicInput), HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);
            string iicSignatureString = BitConverter.ToString(iicSignature).Replace("-", string.Empty);
            //Console.WriteLine("The IIC signature is: " + iicSignatureString);

            this.Invoice.IICSignature = iicSignatureString;

            // Hash IIC signature with MD5 to create IIC
            byte[] iic = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(iicSignature);
            string iicString = BitConverter.ToString(iic).Replace("-", string.Empty);

            this.Invoice.IIC = iicString;
        }

        public string SignRequest()
        {
            FiscalEntities db = new FiscalEntities();
            var pass = db.Settings.Where(a => a.Key == "SSL_CERT_PU_PASS").Single().Value;
            var cert = db.Cert.FirstOrDefault().CertData;

            using (X509Certificate2 keyStore = new X509Certificate2(cert, Encryptor.DecryptSimple(pass)))
            {
                try
                {
                    this.SignIKOF();

                    var REQUEST_TO_SIGN = this.ToXML();

                    // Load a private from a key store
                    RSA privateKey = keyStore.GetRSAPrivateKey();

                    // Convert string XML to object
                    XmlDocument request = new XmlDocument();
                    request.LoadXml(REQUEST_TO_SIGN);

                    // Create key info element
                    KeyInfo keyInfo = new KeyInfo();
                    KeyInfoX509Data keyInfoData = new KeyInfoX509Data();
                    keyInfoData.AddCertificate(keyStore);
                    keyInfo.AddClause(keyInfoData);

                    // Create signature reference
                    Reference reference = new Reference("");
                    reference.AddTransform(new XmlDsigEnvelopedSignatureTransform(false));
                    reference.AddTransform(new XmlDsigExcC14NTransform(false));
                    reference.DigestMethod = XML_DIG_METHOD;
                    reference.Uri = "#" + XML_REQUEST_ID;

                    // Create signature
                    SignedXml xml = new SignedXml(request);
                    xml.SigningKey = privateKey;
                    xml.SignedInfo.CanonicalizationMethod = SignedXml.XmlDsigExcC14NTransformUrl;
                    xml.SignedInfo.SignatureMethod = XML_SIG_METHOD;
                    xml.KeyInfo = keyInfo;
                    xml.AddReference(reference);
                    xml.ComputeSignature();
                    // Add signature element to the request
                    XmlElement signature = xml.GetXml();
                    request.DocumentElement.AppendChild(signature);

                    this.Signature = new SignatureType();

                    this.Signature.SignedInfo = new SignedInfoType();
                    this.Signature.SignedInfo.CanonicalizationMethod = new CanonicalizationMethodType();
                    this.Signature.SignedInfo.CanonicalizationMethod.Algorithm = "http://www.w3.org/2001/10/xml-exc-c14n#";
                    this.Signature.SignedInfo.SignatureMethod = new SignatureMethodType();
                    this.Signature.SignedInfo.SignatureMethod.Algorithm = "http://www.w3.org/2001/04/xmldsig-more#rsa-sha256";

                    var Reference = new ReferenceType();
                    Reference.URI = "#Request";

                    var transform1 = new TransformType();
                    transform1.Algorithm = "http://www.w3.org/2000/09/xmldsig#enveloped-signature";
                    var transform2 = new TransformType();
                    transform2.Algorithm = "http://www.w3.org/2001/10/xml-exc-c14n#";

                    Reference.Transforms = new TransformType[] { transform1, transform2 };

                    var sig = signature.OuterXml.ToString();
                    var digest = sig.SubstrExcl(sig.IndexOf("<DigestValue>") + 12, sig.IndexOf("</DigestValue>"));
                    var prvt = sig.SubstrExcl(sig.IndexOf("<X509Certificate>") + 16, sig.IndexOf("</X509Certificate>"));
                    var sigval = sig.SubstrExcl(sig.IndexOf("<SignatureValue>") + 15, sig.IndexOf("</SignatureValue>"));

                    Reference.DigestMethod = new DigestMethodType();
                    Reference.DigestMethod.Algorithm = "http://www.w3.org/2001/04/xmlenc#sha256";
                    Reference.DigestValue = Convert.FromBase64String(digest);

                    this.Signature.SignedInfo.Reference = new ReferenceType[] { Reference };

                    this.Signature.SignatureValue = new SignatureValueType();
                    this.Signature.SignatureValue.Value = Convert.FromBase64String(sigval);

                    this.Signature.KeyInfo = new KeyInfoType();
                    var x509 = new X509DataType();
                    x509.ItemsElementName = new ItemsChoiceType[] { ItemsChoiceType.X509Certificate };
                    x509.Items = new object[] { Convert.FromBase64String(prvt) };
                    this.Signature.KeyInfo.Items = new object[] { x509 };
                    this.Signature.KeyInfo.ItemsElementName = new ItemsChoiceType2[] { ItemsChoiceType2.X509Data };

                    // Convert signed request to string and print
                    StringWriter sw = new StringWriter();
                    XmlTextWriter xw = new XmlTextWriter(sw);

                    request.WriteTo(xw);
                    return sw.ToString();
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }
            }
            return "";
        }
    }
}
