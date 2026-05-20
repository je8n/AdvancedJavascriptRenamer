# Advanced Javascript Renamer

Advanced Javascript Renamer, Windows 10 ve üzeri için hazırlanmış hafif bir WinForms dosya yeniden adlandırma aracıdır. Dosya adları Jint ile çalışan JavaScript kodlarıyla üretilir; resim, ses, video ve uygulama dosyalarından metadata okunabilir.

Proje ve çıktı adı bilerek korunmuştur:

- Proje klasörü: `advancedRenamer`
- Proje dosyası: `advancedRenamer.csproj`
- Çıktı exe: `advancedRenamer.exe`
- Uygulama görünen adı: `Advanced Javascript Renamer`

## Özellikler

- JavaScript ile yeniden adlandırma: `substr`, `replace`, `indexOf`, regex ve modern JS string metodları kullanılabilir.
- Static/Dynamic script yapısı:
  - `Static` script işlem başında bir kez çalışır.
  - `Dynamic` script listedeki her dosya için çalışır.
- Simülasyon/önizleme: dosyalar değiştirilmeden yeni adlar listede görülebilir.
- Uygula: geçerli önizleme sonuçları dosya sistemine uygulanır.
- Son işlemi geri al: son başarılı Apply işlemi ters sırayla geri alınabilir.
- Drag & drop: dosyalar ve klasörler listeye sürüklenebilir.
- Metadata desteği:
  - Genel dosya bilgileri `System.IO`
  - Resim/EXIF bilgileri `MetadataExtractor`
  - Ses/video bilgileri `TagLibSharp`
  - `.exe`/`.dll` sürüm ve imza bilgileri Windows API üzerinden
- Windows Explorer context menu entegrasyonu: kullanıcı bazlı `HKCU\Software\Classes` altına yazar, admin gerektirmez.
- Script template desteği: Static/Dynamic script çiftleri isim verilerek JSON dosyasına kaydedilebilir.
- İlk açılışta dil seçimi ve kalıcı UI dili. Desteklenen diller: İngilizce, Türkçe, Kazakistan Türkçesi, Azerbaycan Türkçesi ve Rusça.
- Güvenlik: JS çıktısındaki Windows için geçersiz dosya adı karakterleri otomatik temizlenir.

## Gereksinimler

- Windows 10 veya üzeri
- .NET Framework 4.6.2 runtime
- Build için Visual Studio 2022 veya .NET SDK/MSBuild
- Node.js gerekmez

## NuGet Paketleri

Proje `PackageReference` kullanır. Restore sırasında paketler otomatik iner:

```powershell
dotnet restore .\advancedRenamer.csproj
```

Kullanılan paketler:

- `Jint` 4.8.0
- `MetadataExtractor` 2.9.3
- `TagLibSharp` 2.3.0

## Derleme

Debug derleme:

```powershell
dotnet build .\advancedRenamer.csproj
```

Release derleme:

```powershell
dotnet build .\advancedRenamer.csproj -c Release
```

Visual Studio ile:

1. `advancedRenamer.sln` veya `advancedRenamer.csproj` dosyasını aç.
2. NuGet restore işleminin tamamlanmasını bekle.
3. Configuration olarak `Release` seç.
4. Build al.

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

Teknik not: Projede, yanlışlıkla `obj` içindeki ara exe çalıştırıldığında bağımlılık hatası azaltılsın diye runtime dependency DLL dosyalarını `obj` ara çıktı klasörüne de kopyalayan küçük bir MSBuild hedefi vardır. Bu bir dağıtım yöntemi değildir; gerçek çalıştırma ve dağıtım klasörü yine `bin\Release\net462` olmalıdır.

## Proje Yapısı

```text
advancedRenamer.csproj  Proje ve NuGet referansları
advancedRenamer.sln     Visual Studio solution
App.config              .NET Framework runtime config
Form1.cs                Ana UI, script engine, metadata, rename/undo logic
Localization.cs         İlk açılış dil seçimi ve UI metinleri
Program.cs              Uygulama başlangıcı ve startup error logging
RegistryHelper.cs       Explorer context menu ekle/kaldır logic
.gitignore              Build/cache/runtime dosyalarını repodan dışlar
README.md               Kullanım ve geliştirme dokümanı
prompt.md               Projeyi yeniden oluşturmaya yarayan üretim prompt'u
```

Commit'e girmemesi gereken klasörler:

```text
bin/
obj/
.vs/
```

Bu klasörler build sırasında otomatik oluşur.

## Kullanım Akışı

1. `Add Files/Folders` ile dosya veya klasör ekle.
2. Gerekirse dosyaları listeye sürükle bırak.
3. Static/Dynamic scriptleri düzenle veya template seç.
4. `Simulate (Preview)` ile yeni adları kontrol et.
5. Sonuç uygunsa `Apply Changes` ile dosyaları yeniden adlandır.
6. Gerekirse `Undo Last` ile son başarılı Apply işlemini geri al.

Ana grid kolonları:

- `Current Name`
- `New Name`
- `Path`
- `Size`
- `Type`
- `Status`

## Static ve Dynamic Script

`Static` script işlem başlamadan önce bir kez çalışır. Sabitler, sayaçlar ve yardımcı fonksiyonlar için uygundur:

```javascript
let counter = 0;
const prefix = "file_";

function nextName(ext) {
    return prefix + counter++.toString().padStart(3, "0") + ext;
}
```

`Dynamic` script her dosya için çalışır ve yeni dosya adını string olarak döndürmelidir:

```javascript
return nextName(ext);
```

Basit index örneği:

```javascript
return index.toString().padStart(3, "0") + "_" + name + ext;
```

Boş veya geçersiz sonuçlar uygulanmaz. Aynı hedef ad veya mevcut hedef dosya varsa satır `Invalid`/`Skipped` durumuna düşer.

## Kullanılabilir JS Değişkenleri

Dosya başına Dynamic script içinde kullanılabilir:

```text
name        Uzantısız dosya adı
ext         Uzantı, örn. .jpg
path        Klasör yolu
index       Listedeki sıfır bazlı sıra
isImage     Resim dosyası mı
isMusic     Ses dosyası mı
isVideo     Video dosyası mı
isApp       .exe veya .dll mi
size        Byte cinsinden dosya boyutu
fullName    Tam dosya yolu
created     JS Date
modified    JS Date
accessed    JS Date
attributes  FileAttributes metni
meta        Metadata nesnesi
```

## Metadata Alanları

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

## Script Template'leri

Toolbar üzerinden Static/Dynamic script çiftleri isim verilerek kaydedilebilir.

Template dosyası exe'nin yanında tutulur:

```text
script-templates.json
```

Bu dosya runtime kullanıcı verisi olduğu için `.gitignore` içindedir ve repoya commit edilmez.

## Dil Ayarı

Uygulama ilk açılışta UI dilini sorar ve seçimi exe'nin yanındaki dosyada saklar:

```text
language-settings.json
```

Desteklenen diller İngilizce, Türkçe, Kazakistan Türkçesi, Azerbaycan Türkçesi ve Rusça'dır. Toolbar'daki dil seçimi UI dilini yeniden başlatmadan değiştirir; mevcut dosya/klasör listesi korunur. Uygulamanın tekrar dil sorması için `language-settings.json` dosyasını silmek yeterlidir.

## Explorer Context Menu

`Add to Context Menu` checkbox'ı şu registry konumlarını kullanıcı bazlı yönetir:

```text
HKCU\Software\Classes\Directory\shell\advancedRenamer
HKCU\Software\Classes\Directory\Background\shell\advancedRenamer
```

Komut, checkbox işaretlendiği anda çalışan exe'nin tam yolunu kaydeder. Bu yüzden context menu için doğru yol isteniyorsa uygulamayı şu dosyadan açıp checkbox'ı kapat/aç yap:

```text
bin\Release\net462\advancedRenamer.exe
```

## Hata Log'u

Startup sırasında yakalanamayan hatalar exe klasörüne yazılır:

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

### Release build dosyayı yazamıyor

`advancedRenamer.exe` açık olabilir. Uygulamayı kapatıp tekrar derle:

```powershell
dotnet build .\advancedRenamer.csproj -c Release
```

### Context menu yanlış exe'yi açıyor

Uygulamayı doğru Release klasöründen çalıştır, checkbox'ı kapatıp tekrar aç. Registry komutu yeni exe yoluyla güncellenir.

## Git Notları

Repoya kaynak dosyalar commit edilir. Şunlar commit edilmez:

```text
bin/
obj/
.vs/
script-templates.json
language-settings.json
advancedRenamer-error.log
```
