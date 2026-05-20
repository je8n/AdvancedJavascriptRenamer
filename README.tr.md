# Advanced Javascript Renamer

Advanced Javascript Renamer, Windows 10 ve üzeri için hazırlanmış hafif bir WinForms dosya yeniden adlandırma aracıdır. Dosya adları Jint ile çalışan JavaScript kodlarıyla üretilir; resim, ses, video ve uygulama dosyalarından üst veri okunabilir.

Proje ve çıktı adı bilerek korunmuştur:

- Proje klasörü: `advancedRenamer`
- Proje dosyası: `advancedRenamer.csproj`
- Çıktı exe: `advancedRenamer.exe`
- Uygulama görünen adı: `Advanced Javascript Renamer`

## Özellikler

- JavaScript ile yeniden adlandırma: `substr`, `replace`, `indexOf`, düzenli ifade ve modern JS metin metodları kullanılabilir.
- Static/Sort/Dynamic betik yapısı:
  - `Static` betiği işlem başında bir kez çalışır.
  - `Sort` betiği yalnızca Sıralama İşlemleri altındaki `Önizle` tıklandığında geçici liste sırası üretir.
  - `Dynamic` betiği listedeki her öğe için çalışır.
- Simülasyon/önizleme: dosyalar değiştirilmeden yeni adlar listede görülebilir.
- Uygula: geçerli önizleme sonuçları dosya sistemine uygulanır.
- Son işlemi geri al: son başarılı uygulama işlemi ters sırayla geri alınabilir.
- Sürükle bırak: dosyalar ve klasörler listeye sürüklenebilir.
- Üst veri desteği:
  - Genel dosya bilgileri `System.IO`
  - Resim/EXIF bilgileri `MetadataExtractor`
  - Ses/video bilgileri `TagLibSharp`
  - `.exe`/`.dll` sürüm ve imza bilgileri Windows API üzerinden
- Windows Gezgini sağ tık menüsü entegrasyonu: kullanıcı bazlı `HKCU\Software\Classes` altına yazar, yönetici yetkisi gerektirmez.
- Taslak desteği: Static/Dynamic taslakları ve Sort taslakları ayrı ayrı isim verilerek JSON dosyasına kaydedilebilir.
- İlk açılışta dil seçimi ve kalıcı UI dili. Desteklenen diller: İngilizce, Türkçe, Kazakistan Türkçesi, Azerbaycan Türkçesi ve Rusça.
- Güvenlik: JS çıktısındaki Windows için geçersiz dosya adı karakterleri otomatik temizlenir.

## Gereksinimler

- Windows 10 veya üzeri
- .NET Framework 4.6.2 çalışma zamanı
- Derleme için Visual Studio 2022 veya .NET SDK/MSBuild
- Node.js gerekmez

## NuGet Paketleri

Proje `PackageReference` kullanır. Geri yükleme sırasında paketler otomatik iner:

```powershell
dotnet restore .\advancedRenamer.csproj
```

Kullanılan paketler:

- `Jint` 4.8.0
- `MetadataExtractor` 2.9.3
- `TagLibSharp` 2.3.0

## Derleme

Hata ayıklama derlemesi:

```powershell
dotnet build .\advancedRenamer.csproj
```

Yayın derlemesi:

```powershell
dotnet build .\advancedRenamer.csproj -c Release
```

Visual Studio ile:

1. `advancedRenamer.sln` veya `advancedRenamer.csproj` dosyasını aç.
2. NuGet paket geri yükleme işleminin tamamlanmasını bekle.
3. Yapılandırma olarak `Release` seç.
4. Projeyi derle.

## Çalıştırma

Doğru çalıştırma klasörü:

```text
bin\Release\net462\
```

Çalıştırılacak dosya:

```text
bin\Release\net462\advancedRenamer.exe
```

Önemli: `obj` klasörü ara derleme klasörüdür. Uygulama normalde `obj\Release\net462` içinden çalıştırılmamalıdır. Uygulamayı tek başına başka yere kopyalarsan yanında `Jint.dll`, `MetadataExtractor.dll`, `TagLibSharp.dll` ve diğer bağımlılık DLL dosyaları da bulunmalıdır.

Teknik not: Projede, yanlışlıkla `obj` içindeki ara exe çalıştırıldığında bağımlılık hatası azaltılsın diye çalışma zamanı bağımlılığı DLL dosyalarını `obj` ara çıktı klasörüne de kopyalayan küçük bir MSBuild hedefi vardır. Bu bir dağıtım yöntemi değildir; gerçek çalıştırma ve dağıtım klasörü yine `bin\Release\net462` olmalıdır.

## Proje Yapısı

```text
advancedRenamer.csproj  Proje ve NuGet referansları
advancedRenamer.sln     Visual Studio çözüm dosyası
App.config              .NET Framework çalışma zamanı yapılandırması
Form1.cs                Ana arayüz, betik motoru, üst veri, yeniden adlandırma ve geri alma mantığı
Localization.cs         İlk açılış dil seçimi ve UI metinleri
Program.cs              Uygulama başlangıcı ve başlangıç hata kaydı
RegistryHelper.cs       Windows Gezgini sağ tık menüsü ekleme/kaldırma mantığı
.gitignore              Derleme, önbellek ve çalışma zamanı dosyalarını repodan dışlar
README.md               İngilizce kullanım ve geliştirme dokümanı
README.tr.md            Türkçe kullanım ve geliştirme dokümanı
prompt.md               Projeyi yeniden oluşturmaya yarayan üretim prompt'u
```

Commit'e girmemesi gereken klasörler:

```text
bin/
obj/
.vs/
```

Bu klasörler derleme sırasında otomatik oluşur.

## Kullanım Akışı

1. `Dosya/Klasör Ekle` ile dosya veya klasör ekle.
2. Gerekirse dosyaları listeye sürükle bırak.
3. Static/Sort/Dynamic betiklerini düzenle veya taslak seç.
4. Gerekirse Sıralama İşlemleri altındaki `Önizle` ile liste sırasını dene; doğruysa `Uygula`, değilse `İptal Et`.
5. `Simüle Et (Önizleme)` ile yeni adları kontrol et.
6. Sonuç uygunsa `Değişiklikleri Uygula` ile dosyaları yeniden adlandır.
7. Gerekirse `Geri Al` ile son başarılı uygulama işlemini geri al.

Ana liste kolonları:

- `Mevcut Ad`
- `Yeni Ad`
- `Yol`
- `Boyut`
- `Tür`
- `Durum`

Klasör eklendiğinde yalnızca o klasörün doğrudan içindeki dosyalar ve alt klasörler listeye alınır. Alt klasörlerin içi taranmaz.

## Static, Sort ve Dynamic Betik

`Static` betiği işlem başlamadan önce bir kez çalışır. Sabitler, sayaçlar ve yardımcı fonksiyonlar için uygundur:

```javascript
let counter = 0;
const prefix = "file_";

function nextName(ext) {
    return prefix + counter++.toString().padStart(3, "0") + ext;
}
```

`Sort` betiği yeniden adlandırma sırasında otomatik çalışmaz. Yalnızca Sıralama İşlemleri altındaki `Önizle` tıklandığında her öğe için çalışır ve sıralama anahtarı döndürür. Önizleme geçicidir; kalıcı liste sırası için `Uygula` tıklanmalıdır:

```javascript
return (isDirectory ? "2_" : "1_") + name.toLowerCase();
```

`Dynamic` betiği her öğe için çalışır ve yeni dosya/klasör adını metin olarak döndürmelidir:

```javascript
return nextName(ext);
```

Basit index örneği:

```javascript
return index.toString().padStart(3, "0") + "_" + name + ext;
```

Boş veya geçersiz sonuçlar uygulanmaz. Aynı hedef ad veya mevcut hedef dosya/klasör varsa satır `Invalid`/`Skipped` durumuna düşer.

`Ayarlar` altında `Aynı dosyaları yeniden adlandır` işaretliyse, mevcut hedef ad veya listedeki başka bir hedefle çakışan adlar otomatik numaralandırılır. Numara uzantıdan önce eklenir: `dosya (2).jpg`, `dosya (3).jpg`.

## Kullanılabilir JS Değişkenleri

Dosya veya klasör başına Sort ve Dynamic betikleri içinde kullanılabilir:

```text
name        Uzantısız dosya adı; klasörlerde klasör adı
ext         Uzantı, örn. .jpg; klasörlerde boş
path        Üst klasör yolu
index       Listedeki sıfır bazlı sıra
isDirectory Klasör mü
isFile      Dosya mı
isImage     Resim dosyası mı
isMusic     Ses dosyası mı
isVideo     Video dosyası mı
isApp       .exe veya .dll mi
size        Byte cinsinden dosya boyutu; klasörlerde 0
fullName    Tam dosya/klasör yolu
created     JS tarih nesnesi
modified    JS tarih nesnesi
accessed    JS tarih nesnesi
attributes  Dosya öznitelikleri metni
meta        Üst veri nesnesi
```

## Üst Veri Alanları

Genel dosya alanları:

```text
meta.name
meta.extension
meta.fullName
meta.path
meta.sizeBytes
meta.sizeText
meta.creationDate
meta.modifiedDate
meta.accessedDate
meta.attributes
meta.isDirectory
meta.isFile
meta.isReadOnly
meta.isHidden
meta.isSystem
meta.isArchive
```

Resim/EXIF alanları:

```text
meta.width
meta.height
meta.dpiX
meta.dpiY
meta.cameraMake
meta.cameraModel
meta.fStop
meta.exposureTime
meta.iso
meta.focalLength
meta.dateTaken
meta.digitizedDate
meta.gpsLatitude
meta.gpsLongitude
meta.orientation
```

Ses/müzik alanları:

```text
meta.title
meta.artist
meta.artists
meta.album
meta.year
meta.genre
meta.trackNumber
meta.bpm
meta.duration
meta.durationText
meta.audioChannels
meta.audioSampleRate
meta.audioBitrateKbps
meta.audioCodec
```

Video alanları:

```text
meta.duration
meta.durationText
meta.videoWidth
meta.videoHeight
meta.bitrateKbps
meta.frameRate
meta.audioChannels
meta.audioSampleRate
meta.audioBitrateKbps
meta.videoCodec
meta.audioCodec
```

Not: `frameRate` TagLibSharp tarafından her video formatında sağlanmadığı için çoğu dosyada `0` kalabilir.

Uygulama dosyası alanları:

```text
meta.productName
meta.fileVersion
meta.copyright
meta.description
meta.isSigned
meta.signatureValid
meta.publisher
```

## Taslaklar

Araç çubuğundaki `Taslaklar` grubu Static/Dynamic betiklerini kaydeder ve okur. Sıralama İşlemleri grubundaki `Oku`/`Kaydet` butonları yalnızca Sort betiğini yönetir.

Taslak dosyaları exe'nin yanında tutulur:

```text
script-templates.json
sort-templates.json
```

Bu dosya çalışma zamanı kullanıcı verisi olduğu için `.gitignore` içindedir ve repoya işlenmez.

## Dil Ayarı

Uygulama ilk açılışta arayüz dilini sorar ve seçimi exe'nin yanındaki dosyada saklar:

```text
language-settings.json
```

Desteklenen diller İngilizce, Türkçe, Kazakistan Türkçesi, Azerbaycan Türkçesi ve Rusça'dır. Araç çubuğundaki dil seçimi arayüz dilini yeniden başlatmadan değiştirir; mevcut dosya/klasör listesi korunur. Uygulamanın tekrar dil sorması için `language-settings.json` dosyasını silmek yeterlidir.

## Windows Gezgini Sağ Tık Menüsü

`Sağ Tık Menüsüne Ekle` onay kutusu şu kayıt defteri konumlarını kullanıcı bazlı yönetir:

```text
HKCU\Software\Classes\Directory\shell\advancedRenamer
HKCU\Software\Classes\Directory\Background\shell\advancedRenamer
```

Komut, onay kutusu işaretlendiği anda çalışan exe'nin tam yolunu kaydeder. Bu yüzden sağ tık menüsü için doğru yol isteniyorsa uygulamayı şu dosyadan açıp onay kutusunu kapat/aç yap:

```text
bin\Release\net462\advancedRenamer.exe
```

## Hata Günlüğü

Başlangıç sırasında yakalanamayan hatalar exe klasörüne yazılır:

```text
advancedRenamer-error.log
```

Bu dosya `.gitignore` içindedir.

## Sık Karşılaşılan Sorunlar

### MetadataExtractor bulunamadı

Uygulama muhtemelen `obj` klasöründen veya tek başına kopyalanmış exe olarak çalıştırılmıştır. Şu klasörden çalıştır:

```text
bin\Release\net462\advancedRenamer.exe
```

Exe başka yere taşınacaksa aynı klasörde bağımlılık DLL dosyaları da taşınmalıdır.

### Release derlemesi dosyayı yazamıyor

`advancedRenamer.exe` açık olabilir. Uygulamayı kapatıp tekrar derle:

```powershell
dotnet build .\advancedRenamer.csproj -c Release
```

### Sağ tık menüsü yanlış exe'yi açıyor

Uygulamayı doğru Release klasöründen çalıştır, onay kutusunu kapatıp tekrar aç. Kayıt defteri komutu yeni exe yoluyla güncellenir.

## Git Notları

Kaynak dosyalar repoya işlenir. Şunlar işlenmez:

```text
bin/
obj/
.vs/
script-templates.json
sort-templates.json
language-settings.json
advancedRenamer-error.log
```
