using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using System.Security.Claims;
using System.Web.Mvc;
using FuzzySharp;

namespace KDS_Projesi.Controllers
{
    public class HomeController : Controller
    {
        
        public ActionResult Index()
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }
            

            using (var context = new KdsEntities1())
            {
                // Kullanıcının randevularını getir
                int userId = Convert.ToInt32(Session["UserId"]);
                var randevular = context.Randevular.Where(r => r.HastaID == userId).ToList();

                // Tüm doktorları getir
                var doktorlar = context.Doktorlar.ToList();

                ViewBag.Randevular = randevular;
                ViewBag.Doktorlar = doktorlar; // Doktorlar listesini View'e gönder
            }

            return View();
        }



        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string username, string password)
        {
            using (var db = new KdsEntities1())
            {
                var user = db.Hastalar.SingleOrDefault(u => u.KullaniciAdi == username && u.Sifre == password);
                if (user != null)
                {
                    Session["UserId"] = user.HastaID;
                    Session["UserName"] = user.Isim;
                    Session["SurName"] = user.Soyisim;
                    return RedirectToAction("Index", "Home");
                }
            }
            ViewBag.Error = "Geçersiz kullanıcı adı veya şifre!";
            return View();
        }

        public ActionResult Register()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Register(string isim, string soyisim, string kullaniciAdi, string sifre)
        {
            using (var db = new KdsEntities1())
            {
                // Kullanıcı adı ile var olan veriyi kontrol et
                var existingUser = db.Hastalar.SingleOrDefault(u => u.KullaniciAdi == kullaniciAdi);

                // Eğer kullanıcı adı varsa, hata mesajı göster
                if (existingUser != null)
                {
                    ViewBag.Error = "Bu kullanıcı adı zaten mevcut!";
                    return View();
                }

                // Yeni kullanıcıyı ekle
                var newUser = new Hastalar
                {
                    Isim = isim,
                    Soyisim = soyisim,
                    KullaniciAdi = kullaniciAdi,
                    Sifre = sifre
                };

                db.Hastalar.Add(newUser);
                db.SaveChanges();

                TempData["SuccessMessage"] = "Kayıt başarılı! Lütfen giriş yapınız.";
                return RedirectToAction("Login");
            }
        }


        [HttpPost]
        //public JsonResult Sorgula(string poliklinik, DateTime gun, TimeSpan saat)
        //{
        //    using (var context = new KdsEntities1())
        //    {
        //        var doktorlar = context.Doktorlar
        //            .Where(d => d.Poliklinik == poliklinik && d.Gun == gun && d.Saat == saat && d.RandevuDurumu == false)
        //            .ToList();

        //        if (doktorlar.Any())
        //        {
        //            // Doktor bilgilerini session veya tempdata ile taşıyabilirsiniz
        //            TempData["DoktorListesi"] = doktorlar;
        //            return Json(new { success = true });
        //        }

        //        return Json(new { success = false });
        //    }
        //}
        public JsonResult Sorgula(string poliklinik, DateTime gun, TimeSpan? saat)
        {
            using (var context = new KdsEntities1())
            {
                var query = context.Doktorlar
                    .Where(d => d.Poliklinik == poliklinik && d.Gun == gun && d.RandevuDurumu == false);

                // Eğer saat seçilmişse, saati filtrele
                if (saat.HasValue)
                {
                    query = query.Where(d => d.Saat == saat.Value);
                }

                var doktorlar = query.ToList();

                if (doktorlar.Any())
                {
                    TempData["DoktorListesi"] = doktorlar;
                    return Json(new { success = true });
                }

                return Json(new { success = false });
            }
        }


        public ActionResult Listele()
        {
            var doktorlar = TempData["DoktorListesi"] as List<Doktorlar>;

            if (doktorlar == null || !doktorlar.Any())
            {
                return RedirectToAction("Index"); // Eğer doktor yoksa ana sayfaya dön
            }

            // Doktor listesini Session'da sakla
            Session["ListeDoktorlar"] = doktorlar;

            return View(doktorlar);
        }


        // Randevu Oluşturma
        [HttpPost]
        public ActionResult RandevuOlustur(int doktorID)
        {
            if (Session["UserId"] == null)
            {
                return RedirectToAction("Login");
            }

            int hastaID = (int)Session["UserId"];
            string connectionString = "Server=DELL;Database=Kds;Integrated Security=True;";


            using (var connection = new SqlConnection(connectionString))
            {
                connection.Open();

                using (var command = new SqlCommand("sp_RandevuOlustur", connection))
                {
                    command.CommandType = System.Data.CommandType.StoredProcedure;
                    command.Parameters.AddWithValue("@DoktorID", doktorID);
                    command.Parameters.AddWithValue("@HastaID", hastaID); // HastaID'yi direkt alıyoruz

                    // Prosedürü çalıştırıyoruz
                    command.ExecuteNonQuery();
                }
            }

            return RedirectToAction("Index"); // Başka bir sayfaya yönlendirme veya başarı mesajı ekleyebilirsiniz.
        }

        public ActionResult Ozellestir()
        {
            return View();
        }


        //public ActionResult Listele2()
        //{
        //    var doktorlar = TempData["DoktorListesi"] as List<Doktorlar>;

        //    if (doktorlar == null || !doktorlar.Any())
        //    {
        //        return RedirectToAction("Index"); // Eğer doktor yoksa Listele'ye dön
        //    }

        //    return View(doktorlar);
        //}
        public ActionResult Listele2()
        {
            var doktorlar = TempData["DoktorListesi"] as List<Doktorlar>;

            if (doktorlar == null || !doktorlar.Any())
            {
                TempData["Message"] = "Kriterlerinize uygun doktor bulunamadı. Ana sayfaya yönlendiriliyorsunuz...";
                return View(); // Boş View döndür
            }

            return View(doktorlar); // Listeyi gönder
        }



        KdsEntities1 _context = new KdsEntities1();
        
        [HttpPost]
        public ActionResult KriterDegerlendir(int deneyim, int memnuniyet, int iletisim, int tedavi)
        {
            // 1. Normalizasyon işlemleri
            // Deneyim 0-50 arasında olduğu için, bu değeri 0 ile 1 arasında normalize ediyoruz
            double normalizedDeneyim = (double)deneyim / 50.0;

            // Memnuniyet ve İletişim 0-100 arasında olduğu için, bu değeri 0 ile 1 arasında normalize ediyoruz
            double normalizedMemnuniyet = (double)memnuniyet / 100.0;
            double normalizedIletisim = (double)iletisim / 100.0;

            // Tedavi Maliyeti 0-50000 arasında olduğu için, bunu ters normalizasyona tabi tutuyoruz
            double normalizedTedavi = 1.0 - ((double)tedavi / 50000.0);

            // Kullanıcının verdiği kriterlerden hedef skor hesaplanıyor
            double hedefSkor = (normalizedDeneyim * 0.4) + (normalizedMemnuniyet * 0.3) + (normalizedIletisim * 0.2) + (normalizedTedavi * 0.1);

            ViewBag.HedefSkor = hedefSkor;
            // Listele'den gelen doktor listesini Session'dan alıyoruz
            var doktorlar = Session["ListeDoktorlar"] as List<Doktorlar>;

            if (doktorlar == null || !doktorlar.Any())
            {
                return RedirectToAction("Listele2"); // Eğer doktor listesi yoksa Listele'ye yönlendir
            }

            // Doktorların skorlarını hesaplayıp filtreleme işlemi
            var uygunDoktorlar = (from doktor in doktorlar
                                  join kriter in _context.Kriterler
                                  on doktor.DoktorID equals kriter.DoktorID
                                  select new
                                  {
                                      doktor.DoktorID,
                                      doktor.Ad,
                                      doktor.Soyad,
                                      doktor.Poliklinik,
                                      doktor.Gun,
                                      doktor.Saat,
                                      // Kriterlerin normalize edilmiş değerleri ile skor hesaplanıyor
                                      Skor = (kriter.Deneyim * 0.4) / 50.0 +
                                             (kriter.Memnuniyet * 0.3) / 100.0 +
                                             (kriter.Iletisim * 0.2) / 100.0 +
                                             (1 - (double)kriter.TedaviMaliyetleri / 50000.0) * 0.1
                                  })
                                  .Where(d => d.Skor >= hedefSkor) // Kullanıcının hedef skorundan büyük olanları filtreleme
                                  .OrderByDescending(d => d.Skor) // Skora göre sıralama
                                  .ToList();

            if (!uygunDoktorlar.Any())
            {
                // Eğer uygun doktor yoksa mesaj ayarla ve Listele2'ye yönlendir
                TempData["Message"] = "Kriterlerinize uygun doktor bulunamadı.";
                return RedirectToAction("Listele2");
            }

            // Modele uygun hale getiriyoruz
            var doktorlarModel = uygunDoktorlar.Select(d => new Doktorlar
            {
                DoktorID = d.DoktorID,
                Ad = d.Ad,
                Soyad=d.Soyad,
                Poliklinik = d.Poliklinik,
                Gun = d.Gun,
                Saat = d.Saat,
                Skor = (double)d.Skor
            }).ToList();

            // Listeyi TempData'ya kaydediyoruz
            TempData["DoktorListesi"] = doktorlarModel;

            // Listele2 view'ına uygun doktorları gönderiyoruz
            return RedirectToAction("Listele2");
        }

        
        // Tedavi maliyetini normalize etme fonksiyonu
        private double NormalizeTedavi(int tedavi)
        {
            if (tedavi <= 10000)
            {
                return (double)tedavi / 10000; // 0-10k aralığına normalize et
            }
            else if (tedavi <= 20000)
            {
                return (double)(tedavi - 10000) / 10000; // 10k-20k aralığına normalize et
            }
            else
            {
                return 1.0; // 20k üzeri değerler için maksimum değeri (1.0) döndür
            }
        }


        public ActionResult ListeleSkorlar()
        {
            return View();
        }
        [HttpGet]
        public ActionResult OzellestirMoora()
        {
            return View(); // Bu sayfada kriterleri alacağımız formu göstereceğiz
        }


        [HttpPost]
        public ActionResult KriterDegerlendirMoora(int deneyim, int memnuniyet, int iletisim, int tedavi)
        {
            // Kriterlerin normalize edilmesi (MOORA için)
            var kriterler = _context.Kriterler.ToList();

            double toplamDeneyim = Math.Sqrt(kriterler.Sum(k => Math.Pow((double)(k.Deneyim / 50.0), 2)));
            double toplamMemnuniyet = Math.Sqrt(kriterler.Sum(k => Math.Pow((double)(k.Memnuniyet / 100.0), 2)));
            double toplamIletisim = Math.Sqrt(kriterler.Sum(k => Math.Pow((double)(k.Iletisim / 100.0), 2)));
            double toplamTedavi = Math.Sqrt(kriterler.Sum(k => Math.Pow((double)(1.0 - (k.TedaviMaliyetleri / 50000.0)), 2)));

            // Kullanıcının normalize edilmiş kriter değerleri
            double normalizedDeneyim = (double)deneyim / 50.0 / toplamDeneyim;
            double normalizedMemnuniyet = (double)memnuniyet / 100.0 / toplamMemnuniyet;
            double normalizedIletisim = (double)iletisim / 100.0 / toplamIletisim;
            double normalizedTedavi = (1.0 - ((double)tedavi / 50000.0)) / toplamTedavi;

            // Kullanıcının net skor hesabı (Yarar - Maliyet)
            double hedefSkor = (normalizedDeneyim * 0.4) + (normalizedMemnuniyet * 0.3) + (normalizedIletisim * 0.2) - (normalizedTedavi * 0.1);

            ViewBag.HedefSkor = hedefSkor;

            // Listele'den gelen doktor listesini Session'dan alıyoruz
            var doktorlar = Session["ListeDoktorlar"] as List<Doktorlar>;

            if (doktorlar == null || !doktorlar.Any())
            {
                return RedirectToAction("Listele2"); // Eğer doktor listesi yoksa Listele'ye yönlendir
            }

            // Doktorların MOORA yöntemi ile skorlarını hesaplayıp filtreleme işlemi
            var uygunDoktorlar = (from doktor in doktorlar
                                  join kriter in _context.Kriterler
                                  on doktor.DoktorID equals kriter.DoktorID
                                  select new
                                  {
                                      doktor.DoktorID,
                                      doktor.Ad,
                                      doktor.Soyad,
                                      doktor.Poliklinik,
                                      doktor.Gun,
                                      doktor.Saat,
                                      // MOORA yöntemi ile skor hesaplama
                                      Skor = ((kriter.Deneyim / 50.0) / toplamDeneyim * 0.4) +
                                             ((kriter.Memnuniyet / 100.0) / toplamMemnuniyet * 0.3) +
                                             ((kriter.Iletisim / 100.0) / toplamIletisim * 0.2) -
                                             ((1.0 - (kriter.TedaviMaliyetleri / 50000.0)) / toplamTedavi * 0.1)
                                  })
                                  .Where(d => d.Skor >= hedefSkor) // Kullanıcının hedef skorundan büyük olanları filtreleme
                                  .OrderByDescending(d => d.Skor) // Skora göre sıralama
                                  .ToList();

            if (!uygunDoktorlar.Any())
            {
                // Eğer uygun doktor yoksa mesaj ayarla ve Listele2'ye yönlendir
                TempData["Message"] = "Kriterlerinize uygun doktor bulunamadı.";
                return RedirectToAction("Listele2");
            }

            // Modele uygun hale getiriyoruz
            var doktorlarModel = uygunDoktorlar.Select(d => new Doktorlar
            {
                DoktorID = d.DoktorID,
                Ad = d.Ad,
                Soyad = d.Soyad,
                Poliklinik = d.Poliklinik,
                Gun = d.Gun,
                Saat = d.Saat,
                Skor = (double)d.Skor
            }).ToList();

            // Listeyi TempData'ya kaydediyoruz
            TempData["DoktorListesi"] = doktorlarModel;

            // Listele2 view'ına uygun doktorları gönderiyoruz
            return RedirectToAction("Listele2");
        }

        [HttpPost]
        public ActionResult IptalEt(int randevuId)
        {
            using (var context = new KdsEntities1())
            {
                // Randevu ID'sine göre ilgili randevuyu bul
                var randevu = context.Randevular.FirstOrDefault(r => r.RandevuID == randevuId);

                if (randevu != null)
                {
                    // Randevuyu sil
                    context.Randevular.Remove(randevu);
                    context.SaveChanges();
                    return Json(new { success = true });
                }

                return Json(new { success = false });
            }
        }

        [HttpPost]
        public ActionResult RandevuIptal(string doktorAdi, string poliklinik)
        {
            try
            {
                var hastaId = (int?)Session["UserId"];  // Kullanıcı ID'si Session'dan alınır.

                if (hastaId == null)
                {
                    return Json(new { success = false, message = "Kullanıcı kimliği bulunamadı." });
                }

                // Kullanıcıya ait randevuyu doktor adı ve polikliniğe göre buluyoruz
                var randevu = _context.Randevular
                    .FirstOrDefault(r => r.HastaID == hastaId && r.Poliklinik == poliklinik && r.Doktorlar.Ad == doktorAdi);

                if (randevu == null)
                {
                    return Json(new { success = false, message = "Randevu bulunamadı. Lütfen bilgilerinizi kontrol edin." });
                }

                // Randevuyu siliyoruz
                _context.Randevular.Remove(randevu);
                _context.SaveChanges();

                return Json(new { success = true, message = "Randevunuz başarıyla iptal edildi." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Bir hata oluştu. Lütfen tekrar deneyin." });
            }
        }

        // Bu veri seti, her polikliniğin şikayetlere karşı ağırlıklarını içeriyor
        private readonly Dictionary<string, Dictionary<string, double>> poliklinikVerileri = new Dictionary<string, Dictionary<string, double>>
{
    { "Kardiyoloji", new Dictionary<string, double> {
        { "kalp", 90.0 },
        { "göğüs", 50.0 },
        { "çarpıntı", 50.0 },
        { "bayılma", 55.0 },
        { "mide", 0.0 },
        { "baş", 10.0 },
        { "ishal", 0.0 },
        { "kabızlık", 0.0 },
        { "eklem", 0.0 },
        { "kulak", 0.0 },
        { "kaşıntı", 0.0 },
  
    }},
    { "Dahiliye", new Dictionary<string, double> {
        { "kalp", 10.0 },
        { "göğüs", 40.0 }, 
        { "çarpıntı", 50.0 },
        { "bayılma", 45.0 },
        { "mide", 20.0 },
        { "baş", 30.0 },
        { "ishal", 50.0 },
        { "kabızlık", 50.0 }, 
        { "eklem", 20.0 },
        { "kulak", 0.0 },
        { "kaşıntı", 0.0 },
        { "kan verme" ,100.0},
        
    }},
    { "Ortopedi", new Dictionary<string, double> {
        { "kalp", 0.0 },
        { "göğüs", 0.0 },
        { "çarpıntı", 0.0 },
        { "bayılma", 0.0 },
        { "mide", 0.0 },
        { "baş", 0.0 },
        { "ishal", 0.0 },
        { "kabızlık", 0.0 },
        { "eklem", 80.0 },
        { "kulak", 0.0 },
        { "kaşıntı", 0.0 },
        
    }},
   
    { "Gastroenteroloji", new Dictionary<string, double> {
        { "kalp", 0.0 },
        { "göğüs", 0.0 },
        { "çarpıntı", 0.0 },
        { "bayılma ", 0.0 },
        { "mide", 80.0 },
        { "baş", 0.0 },
        { "ishal", 50.0 },
        { "kabızlık", 50.0 },
        { "eklem", 0.0 },
        { "kulak", 0.0 },
        { "kaşıntı", 0.0 },
        
    }},
    { "KBB", new Dictionary<string, double> {
        { "kalp", 0.0 }, 
        { "göğüs", 10.0 },
        { "çarpıntı", 0.0 },
        { "bayılma", 0.0 }, 
        { "mide", 0.0 },
        { "baş", 60.0 },
        { "ishal", 0.0 },
        { "kabızlık", 0.0 }, 
        { "eklem", 0.0 },
        { "kulak", 100.0 },
        { "kaşıntı", 0.0 },
        
    }},
    { "Cildiye", new Dictionary<string, double> {
        { "kalp", 0.0 },
        { "göğüs", 0.0 },
        { "çarpıntı", 0.0 },
        { "bayılma", 0.0 }, 
        { "mide", 0.0 },
        { "baş", 0.0 },
        { "ishal", 0.0 },
        { "kabızlık", 0.0 },
        { "eklem", 0.0 },
        { "kulak", 0.0 }, 
        { "kaşıntı", 100.0 },
       
    }},
   
};


        private readonly Dictionary<string, List<string>> sinonimListesi = new Dictionary<string, List<string>>
        {
            { "kulak", new List<string> { "duymuyorum", "ses", "işitme" ,"kulak","kulağımda","boğaz","boğazımda","burnumda","burun","solunum","çınlama","tıkanıklığı","bademcik","yutkunma"} },
            { "mide", new List<string> { "mide","yanma","kusmak","bulantı","reflü","karın","midemde" ,"gaz"} },
            { "eklem", new List<string> { "kemik", "kırık", "çıkık","kemik" ,"kas","romatizma","kramp","lif","fıtık","omurgamda","bel","sırt","hareket"} },
            { "kaşıntı", new List<string> { "kaşıntı", "döküntü", "kızarıklık","leke" ,"sivilce","yanık","alerji","egzama"} },
            { "kalp", new List<string> { "kalp","kalb","kalbimde", "tansiyon", "ritim", "kapakçık","damar"} },
            { "göğüs", new List<string> {"göğüs","nefes","solunum","astım","öksürük","hırıltı","göğsümde"} },
            { "çarpıntı", new List<string> {"çarpıntı","nabız","ritim"} },
            { "bayılma", new List<string> {"bayılma","baygınlık","bilinç"} },
            { "baş", new List<string> {"baş","migren","sinüzit","başımda"} },
            { "ishal", new List<string> {"ishal","lavobo","karın","bağırsak","gaz","karnımda"} },
            { "kabızlık", new List<string> {"kabızlık","karın","bağırsak","gaz","karnımda"} },

        };

        [HttpPost]
        public JsonResult UygunPoliklinikler(RandevuData data)
        {
            var poliklinikSonuclari = new List<Dictionary<string, object>>();

            // Şikayet kontrolü
            if (string.IsNullOrEmpty(data.Sikayet))
            {
                return Json(new { success = false, message = "Geçerli bir şikayet girin." });
            }

            // Kullanıcı şikayetini temizleyip küçük harfe çeviriyoruz
            string kullaniciSikayeti = data.Sikayet.ToLower().Trim();

            // Şikayet kelimelerini ayırıyoruz
            var kullaniciSikayetKelimeleri = kullaniciSikayeti.Split(new[] { ' ', ',', '.', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var poliklinik in poliklinikVerileri)
            {
                double toplamAgirlik = 0;

                foreach (var sikayet in poliklinik.Value)
                {
                    foreach (var kelime in kullaniciSikayetKelimeleri)
                    {
                        bool eslesmeBulundu = false; // Eşleşme kontrolü için bayrak

                        // 1. Doğrudan kelime eşleşmesi
                        if (sinonimListesi.ContainsKey(sikayet.Key) && sinonimListesi[sikayet.Key].Contains(kelime))
                        {
                            toplamAgirlik += sikayet.Value;
                            eslesmeBulundu = true; // Eşleşme bulundu
                        }

                        // 2. Sinonim eşleşmesi (sadece doğrudan eşleşme yoksa)
                        if (!eslesmeBulundu && sinonimListesi.TryGetValue(sikayet.Key, out var sinonimler))
                        {
                            foreach (var sinonim in sinonimler)
                            {
                                int benzerlikSkoru = Fuzz.Ratio(sinonim, kelime);
                                if (benzerlikSkoru >= 70)
                                {
                                    toplamAgirlik += sikayet.Value;
                                    eslesmeBulundu = true; // Eşleşme bulundu
                                    break; // Bir sinonim eşleşmesi yeterli
                                }
                            }
                        }
                    }
                }

                // Pozitif ağırlıklı poliklinikleri ekliyoruz
                if (toplamAgirlik > 0)
                {
                    poliklinikSonuclari.Add(new Dictionary<string, object>
            {
                { "poliklinik", poliklinik.Key },
                { "agirlik", toplamAgirlik }
            });
                }
            }

            // Poliklinikleri toplam ağırlıklarına göre sıralıyoruz
            var sortedPoliklinikler = poliklinikSonuclari
                .OrderByDescending(p => (double)p["agirlik"])
                .ToList();

            // Eğer uygun poliklinik bulunamadıysa bilgilendirme mesajı döndür
            if (!sortedPoliklinikler.Any())
            {
                return Json(new { success = false, message = "Şikayetinize uygun bir poliklinik bulunamadı." });
            }

            // Sonuçları döndür
            double maxAgirlik = sortedPoliklinikler.Max(p => (double)p["agirlik"]);

            // Ağırlıkları normalleştiriyoruz
            var normalizedPoliklinikler = sortedPoliklinikler.Select(p => new
            {
                poliklinik = p["poliklinik"],
                agirlik = maxAgirlik > 0 ? ((double)p["agirlik"] / maxAgirlik) : 0
            }).ToList();

            // Sonuçları döndür
            return Json(new { success = true, poliklinikler = normalizedPoliklinikler });
        }


        [HttpPost]
        public ActionResult Listele3(string poliklinikler)
        {
            var poliklinikListesi = JsonConvert.DeserializeObject<List<Poliklinik>>(poliklinikler);
            return View(poliklinikListesi);
        }
        //public ActionResult Doktorlar(string poliklinikAd)
        //{
        //    var doktorlar = _context.Doktorlar.Where(d => d.Ad == poliklinikAd).ToList();
        //    return View(doktorlar);
        //}

        public ActionResult Doktorlar(string poliklinikAd)
        {
            var doktorlar = _context.Doktorlar
                .Where(d => d.Poliklinik == poliklinikAd)
                .ToList();
            return View(doktorlar);
        }




    }
    public class Poliklinik
    {
        [JsonProperty("poliklinik")]
        public string Ad { get; set; }
        public double Agirlik { get; set; }  // Ağırlık, şikayetin poliklinik ile ilişkisinin derecesini gösterir
    }

    public class RandevuData
    {
        public string Sikayet { get; set; }
    }

    public class DoktorSkorViewModel
    {
        public int DoktorID { get; set; }
        public string Ad { get; set; }
        public string Soyad { get; set; }
        public string Poliklinik { get; set; }
        public string Gun { get; set; }
        public TimeSpan Saat { get; set; }
        public double Skor { get; set; } // Hesaplanan skor
    }

   

    // ViewModel for displaying appointment information
    public class RandevuViewModel
    {
        public string DoktorAdi { get; set; }
        public string DoktorSoyadi { get; set; }
        public string Poliklinik { get; set; }
        public DateTime Gun { get; set; }
        public string Saat { get; set; }
        public int? KalanGun { get; set; }
    }

   

   


}
