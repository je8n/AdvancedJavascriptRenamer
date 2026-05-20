using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Windows.Forms;

namespace advancedRenamer
{
    internal static class LanguageManager
    {
        private const string SettingsFileName = "language-settings.json";
        private static readonly List<LanguageInfo> Languages = new List<LanguageInfo>
        {
            new LanguageInfo("en", "English"),
            new LanguageInfo("tr", "Türkçe"),
            new LanguageInfo("kk", "Kazakistan Türkçesi"),
            new LanguageInfo("az", "Azerbaycan Türkçesi"),
            new LanguageInfo("ru", "Русский")
        };

        private static readonly Dictionary<string, Dictionary<string, string>> Texts = CreateTexts();
        private static bool _languageSetFromCommandLine;

        public static string CurrentCode { get; private set; } = "en";

        public static void ApplyCommandLine(string[] args)
        {
            string code = FindLanguageArgument(args);
            if (!IsSupported(code))
            {
                return;
            }

            CurrentCode = code;
            _languageSetFromCommandLine = true;
            SaveLanguageCode(CurrentCode);
        }

        public static void EnsureLanguageSelected()
        {
            if (_languageSetFromCommandLine)
            {
                return;
            }

            string savedCode = LoadSavedLanguageCode();
            if (IsSupported(savedCode))
            {
                CurrentCode = savedCode;
                return;
            }

            using (var form = new LanguageSelectionForm(Languages))
            {
                if (form.ShowDialog() == DialogResult.OK && IsSupported(form.SelectedCode))
                {
                    CurrentCode = form.SelectedCode;
                    SaveLanguageCode(CurrentCode);
                    return;
                }
            }

            CurrentCode = "en";
        }

        public static List<LanguageInfo> GetLanguages()
        {
            return new List<LanguageInfo>(Languages);
        }

        public static string[] RemoveLanguageArguments(string[] args)
        {
            if (args == null || args.Length == 0)
            {
                return new string[0];
            }

            var filtered = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i] ?? string.Empty;
                if (string.Equals(arg, "--lang", StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(arg, "/lang", StringComparison.OrdinalIgnoreCase))
                {
                    i++;
                    continue;
                }

                if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase) ||
                    arg.StartsWith("/lang:", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                filtered.Add(args[i]);
            }

            return filtered.ToArray();
        }

        public static bool SetLanguage(string code)
        {
            if (!IsSupported(code))
            {
                return false;
            }

            CurrentCode = code;
            SaveLanguageCode(CurrentCode);
            return true;
        }

        public static string T(string key)
        {
            Dictionary<string, string> languageTexts;
            string value;
            if (Texts.TryGetValue(CurrentCode, out languageTexts) && languageTexts.TryGetValue(key, out value))
            {
                return value;
            }

            if (Texts.TryGetValue("en", out languageTexts) && languageTexts.TryGetValue(key, out value))
            {
                return value;
            }

            return key;
        }

        private static bool IsSupported(string code)
        {
            return Languages.Any(x => string.Equals(x.Code, code, StringComparison.OrdinalIgnoreCase));
        }

        private static string FindLanguageArgument(string[] args)
        {
            if (args == null)
            {
                return null;
            }

            for (int i = 0; i < args.Length; i++)
            {
                string arg = args[i] ?? string.Empty;
                if ((string.Equals(arg, "--lang", StringComparison.OrdinalIgnoreCase) ||
                     string.Equals(arg, "/lang", StringComparison.OrdinalIgnoreCase)) &&
                    i + 1 < args.Length)
                {
                    return args[i + 1];
                }

                if (arg.StartsWith("--lang=", StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring("--lang=".Length);
                }

                if (arg.StartsWith("/lang:", StringComparison.OrdinalIgnoreCase))
                {
                    return arg.Substring("/lang:".Length);
                }
            }

            return null;
        }

        private static string LoadSavedLanguageCode()
        {
            try
            {
                string path = GetSettingsPath();
                if (!File.Exists(path))
                {
                    return null;
                }

                using (FileStream stream = File.OpenRead(path))
                {
                    var serializer = new DataContractJsonSerializer(typeof(LanguageSettings));
                    var settings = serializer.ReadObject(stream) as LanguageSettings;
                    return settings == null ? null : settings.Language;
                }
            }
            catch
            {
                return null;
            }
        }

        private static void SaveLanguageCode(string code)
        {
            try
            {
                var settings = new LanguageSettings { Language = code };
                using (FileStream stream = File.Create(GetSettingsPath()))
                {
                    var serializer = new DataContractJsonSerializer(typeof(LanguageSettings));
                    serializer.WriteObject(stream, settings);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(T("LanguageSaveError") + "\r\n\r\n" + ex.Message, T("LanguageTitle"), MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private static string GetSettingsPath()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, SettingsFileName);
        }

        private static Dictionary<string, Dictionary<string, string>> CreateTexts()
        {
            var texts = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            texts["en"] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"LanguageTitle", "Select Language"},
                {"LanguagePrompt", "Select application language"},
                {"LanguageOk", "OK"},
                {"LanguageSaveError", "The language choice could not be saved. The app will continue with the selected language for this session."},
                {"LanguageShortLabel", "Language"},
                {"DialogOk", "OK"},
                {"DialogCancel", "Cancel"},
                {"ItemCountGroupTitle", "Items"},
                {"FileOperationsGroupTitle", "File Operations"},
                {"SortOperationsGroupTitle", "Sort Operations"},
                {"TemplatesGroupTitle", "Templates"},
                {"SettingsGroupTitle", "Settings"},
                {"AddFilesFolders", "Add Files/Folders"},
                {"SimulatePreview", "Simulate"},
                {"ApplyChanges", "Apply Changes"},
                {"UndoLast", "Undo Last"},
                {"PreviewSort", "Preview"},
                {"ApplySort", "Apply"},
                {"CancelSort", "Cancel"},
                {"LoadSortTemplate", "Load"},
                {"SaveSortTemplate", "Save"},
                {"LoadTemplate", "Load"},
                {"SaveTemplate", "Save"},
                {"AddToContextMenu", "Add to Context Menu"},
                {"ItemSingular", "item"},
                {"ItemPlural", "items"},
                {"CurrentNameColumn", "Current Name"},
                {"NewNameColumn", "New Name"},
                {"PathColumn", "Path"},
                {"SizeColumn", "Size"},
                {"TypeColumn", "Type"},
                {"StatusColumn", "Status"},
                {"StaticTab", "Static"},
                {"SortTab", "Sort"},
                {"DynamicTab", "Dynamic"},
                {"AddFilesTitle", "Add files"},
                {"AddFolderDescription", "Optionally add a folder. Cancel to skip."},
                {"TemplateNotSelected", "No template selected to load."},
                {"TemplateNameRequired", "Enter a template name."},
                {"OverwriteTemplate", "Overwrite the template \"{0}\"?"},
                {"TemplateSaved", "Template saved."},
                {"TemplateSaveErrorTitle", "Template Save Error"},
                {"NoValidRenames", "There is no valid rename operation to apply."},
                {"ApplyConfirm", "{0} files will be renamed. Continue?"},
                {"UndoNoOperation", "There is no last operation to undo."},
                {"UndoConfirm", "{0} files will be restored to their old names. Continue?"},
                {"UndoRestored", "{0} files were restored."},
                {"RegistryErrorTitle", "Registry Error"},
                {"TemplateMissing", "Template was not found."},
                {"TemplateLoadErrorTitle", "Template Load Error"},
                {"TemplateListErrorTitle", "Template List Error"},
                {"LoadTemplateDialogTitle", "Load Template"},
                {"SaveTemplateDialogTitle", "Save Template"},
                {"LoadSortTemplateDialogTitle", "Load Sort Template"},
                {"SaveSortTemplateDialogTitle", "Save Sort Template"},
                {"TemplateDialogPrompt", "Select a template or enter a new name."},
                {"SortTemplateDialogPrompt", "Select a sort template or enter a new name."},
                {"SortErrorTitle", "Sort Error"},
                {"SortPreviewActiveMessage", "Apply or cancel the current sort preview first."},
                {"AddErrorTitle", "Add Error"},
                {"FileErrorTitle", "File Error"},
                {"SimulationComplete", "Simulation completed."},
                {"ContextMenuText", "Open with Advanced Javascript Renamer"},
                {"StartupErrorTitle", "Advanced Javascript Renamer Error"},
                {"UnknownError", "Unknown error."},
                {"ExePathNotFound", "Advanced Javascript Renamer executable path could not be found."},
                {"RegistryKeyCreateFailed", "Registry key could not be created."},
                {"DefaultStaticScript", "// Runs once before processing the file list.\r\n// Keep shared constants, counters, arrays, and helper functions here.\r\n\r\n// Example:\r\n// let counter = 0;\r\n// const prefix = \"file_\";\r\n// function nextName(ext) { return prefix + counter++ + ext; }\r\n"},
                {"DefaultSortScript", "// Return a sort key for the current item.\r\n// Preview Sort shows the temporary order; Apply Sort keeps it.\r\n// Examples:\r\n// return (isDirectory ? \"2_\" : \"1_\") + name.toLowerCase();\r\n// return -size;\r\n\r\nreturn index;"},
                {"DefaultDynamicScript", "// Return the new filename. ext includes the dot, for example \".jpg\".\r\n// Examples: return name.replace(/ /g, \"_\") + ext;\r\n//           return index.toString().padStart(3, \"0\") + \"_\" + name + ext;\r\n\r\nreturn name + ext;"},
                {"VariableGuide", "Variables\r\n---------\r\nname      filename without extension; folder name for folders\r\next       extension, e.g. .jpg; empty for folders\r\npath      parent folder path\r\nindex     zero-based item index\r\nisDirectory true for folders\r\nisFile    true for files\r\nisImage   true for image files\r\nisMusic   true for audio files\r\nisVideo   true for video files\r\nisApp     true for .exe/.dll files\r\nsize      file size in bytes; 0 for folders\r\nfullName  full file/folder path\r\ncreated   JS Date\r\nmodified  JS Date\r\naccessed  JS Date\r\nattributes file attributes text\r\n\r\nStatic script runs once per Simulate/Apply.\r\nSort script runs only when Preview Sort is clicked and returns a sort key.\r\nDynamic script runs once for each item.\r\n\r\nmeta\r\n----\r\nFile/folder: meta.name, meta.extension, meta.fullName\r\n             meta.path, meta.sizeBytes, meta.sizeText\r\n             meta.creationDate, meta.modifiedDate\r\n             meta.accessedDate, meta.attributes\r\n             meta.isDirectory, meta.isFile\r\n             meta.isReadOnly, meta.isHidden\r\n             meta.isSystem, meta.isArchive\r\n\r\nImage:\r\nmeta.width\r\nmeta.height\r\nmeta.dpiX, meta.dpiY\r\nmeta.cameraMake, meta.cameraModel\r\nmeta.fStop, meta.exposureTime\r\nmeta.iso, meta.focalLength\r\nmeta.dateTaken, meta.digitizedDate\r\nmeta.gpsLatitude, meta.gpsLongitude\r\nmeta.orientation\r\n\r\nAudio/Video:\r\nmeta.duration, meta.durationText\r\nmeta.videoWidth, meta.videoHeight\r\nmeta.bitrateKbps, meta.frameRate\r\nmeta.audioChannels, meta.audioSampleRate\r\nmeta.audioBitrateKbps, meta.videoCodec\r\nmeta.audioCodec\r\n\r\nMusic tags:\r\nmeta.title, meta.artist, meta.artists\r\nmeta.album, meta.year, meta.genre\r\nmeta.trackNumber, meta.bpm\r\n\r\nApp:\r\nmeta.productName, meta.fileVersion\r\nmeta.copyright, meta.description\r\nmeta.isSigned, meta.signatureValid\r\nmeta.publisher\r\n\r\nSort script must return a sort key. Dynamic script must return a string."}
            };

            texts["tr"] = MergeLanguage(texts["en"], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"LanguageTitle", "Dil Seçimi"},
                {"LanguagePrompt", "Uygulama dilini seçin"},
                {"LanguageOk", "Tamam"},
                {"LanguageSaveError", "Dil seçimi kaydedilemedi. Uygulama bu oturumda seçilen dille devam edecek."},
                {"LanguageShortLabel", "Dil"},
                {"DialogOk", "Tamam"},
                {"DialogCancel", "İptal"},
                {"ItemCountGroupTitle", "Öğe"},
                {"FileOperationsGroupTitle", "Dosya İşlemleri"},
                {"SortOperationsGroupTitle", "Sıralama İşlemleri"},
                {"TemplatesGroupTitle", "Taslaklar"},
                {"SettingsGroupTitle", "Ayarlar"},
                {"AddFilesFolders", "Dosya/Klasör Ekle"},
                {"SimulatePreview", "Simüle Et"},
                {"ApplyChanges", "Değişiklikleri Uygula"},
                {"UndoLast", "Geri Al"},
                {"PreviewSort", "Önizle"},
                {"ApplySort", "Uygula"},
                {"CancelSort", "İptal Et"},
                {"LoadSortTemplate", "Oku"},
                {"SaveSortTemplate", "Kaydet"},
                {"LoadTemplate", "Oku"},
                {"SaveTemplate", "Kaydet"},
                {"AddToContextMenu", "Sağ Tık Menüsüne Ekle"},
                {"ItemSingular", "öğe"},
                {"ItemPlural", "öğe"},
                {"CurrentNameColumn", "Mevcut Ad"},
                {"NewNameColumn", "Yeni Ad"},
                {"PathColumn", "Yol"},
                {"SizeColumn", "Boyut"},
                {"TypeColumn", "Tür"},
                {"StatusColumn", "Durum"},
                {"StaticTab", "Static"},
                {"SortTab", "Sort"},
                {"DynamicTab", "Dynamic"},
                {"AddFilesTitle", "Dosya ekle"},
                {"AddFolderDescription", "İsteğe bağlı klasör ekleyin. Geçmek için iptal edin."},
                {"TemplateNotSelected", "Yüklenecek taslak seçilmedi."},
                {"TemplateNameRequired", "Taslak için bir isim yazın."},
                {"OverwriteTemplate", "\"{0}\" taslağının üzerine yazılsın mı?"},
                {"TemplateSaved", "Taslak kaydedildi."},
                {"TemplateSaveErrorTitle", "Taslak Kaydetme Hatası"},
                {"NoValidRenames", "Uygulanacak geçerli yeniden adlandırma yok."},
                {"ApplyConfirm", "{0} dosya yeniden adlandırılacak. Devam edilsin mi?"},
                {"UndoNoOperation", "Geri alınacak son işlem yok."},
                {"UndoConfirm", "{0} dosya eski adına döndürülecek. Devam edilsin mi?"},
                {"UndoRestored", "{0} dosya geri alındı."},
                {"RegistryErrorTitle", "Registry Hatası"},
                {"TemplateMissing", "Taslak bulunamadı."},
                {"TemplateLoadErrorTitle", "Taslak Yükleme Hatası"},
                {"TemplateListErrorTitle", "Taslak Listeleme Hatası"},
                {"LoadTemplateDialogTitle", "Taslak Oku"},
                {"SaveTemplateDialogTitle", "Taslak Kaydet"},
                {"LoadSortTemplateDialogTitle", "Sıralama Taslağı Oku"},
                {"SaveSortTemplateDialogTitle", "Sıralama Taslağı Kaydet"},
                {"TemplateDialogPrompt", "Bir taslak seçin veya yeni isim yazın."},
                {"SortTemplateDialogPrompt", "Bir sıralama taslağı seçin veya yeni isim yazın."},
                {"SortErrorTitle", "Sıralama Hatası"},
                {"SortPreviewActiveMessage", "Önce mevcut sıralama önizlemesini uygulayın veya iptal edin."},
                {"AddErrorTitle", "Ekleme Hatası"},
                {"FileErrorTitle", "Dosya Hatası"},
                {"SimulationComplete", "Simülasyon tamamlandı."},
                {"ContextMenuText", "Advanced Javascript Renamer ile aç"},
                {"StartupErrorTitle", "Advanced Javascript Renamer Hatası"},
                {"UnknownError", "Bilinmeyen hata."},
                {"ExePathNotFound", "Advanced Javascript Renamer executable yolu bulunamadı."},
                {"RegistryKeyCreateFailed", "Registry anahtarı oluşturulamadı."},
                {"DefaultStaticScript", "// Dosya listesi işlenmeden önce bir kez çalışır.\r\n// Ortak sabitleri, sayaçları, dizileri ve yardımcı fonksiyonları burada tutun.\r\n\r\n// Örnek:\r\n// let counter = 0;\r\n// const prefix = \"file_\";\r\n// function nextName(ext) { return prefix + counter++ + ext; }\r\n"},
                {"DefaultSortScript", "// Mevcut öğe için sıralama anahtarı döndürün.\r\n// Sıralama Önizle geçici sıralamayı gösterir; Sıralamayı Uygula kalıcı yapar.\r\n// Örnekler:\r\n// return (isDirectory ? \"2_\" : \"1_\") + name.toLowerCase();\r\n// return -size;\r\n\r\nreturn index;"},
                {"DefaultDynamicScript", "// Yeni dosya adını döndürün. ext noktayı içerir, örneğin \".jpg\".\r\n// Örnekler: return name.replace(/ /g, \"_\") + ext;\r\n//           return index.toString().padStart(3, \"0\") + \"_\" + name + ext;\r\n\r\nreturn name + ext;"},
                {"VariableGuide", "Değişkenler\r\n-----------\r\nname      uzantısız dosya adı; klasörlerde klasör adı\r\next       uzantı, örn. .jpg; klasörlerde boş\r\npath      üst klasör yolu\r\nindex     sıfırdan başlayan liste indeksi\r\nisDirectory klasörler için true\r\nisFile    dosyalar için true\r\nisImage   resim dosyaları için true\r\nisMusic   ses dosyaları için true\r\nisVideo   video dosyaları için true\r\nisApp     .exe/.dll dosyaları için true\r\nsize      byte cinsinden dosya boyutu; klasörlerde 0\r\nfullName  tam dosya/klasör yolu\r\ncreated   JS Date\r\nmodified  JS Date\r\naccessed  JS Date\r\nattributes dosya öznitelikleri metni\r\n\r\nStatic script Simulate/Apply başına bir kez çalışır.\r\nSort script yalnızca Sıralama Önizle tıklandığında çalışır ve sıralama anahtarı döndürür.\r\nDynamic script her öğe için bir kez çalışır.\r\n\r\nmeta\r\n----\r\nDosya/klasör: meta.name, meta.extension, meta.fullName\r\n              meta.path, meta.sizeBytes, meta.sizeText\r\n              meta.creationDate, meta.modifiedDate\r\n              meta.accessedDate, meta.attributes\r\n              meta.isDirectory, meta.isFile\r\n              meta.isReadOnly, meta.isHidden\r\n              meta.isSystem, meta.isArchive\r\n\r\nResim:\r\nmeta.width\r\nmeta.height\r\nmeta.dpiX, meta.dpiY\r\nmeta.cameraMake, meta.cameraModel\r\nmeta.fStop, meta.exposureTime\r\nmeta.iso, meta.focalLength\r\nmeta.dateTaken, meta.digitizedDate\r\nmeta.gpsLatitude, meta.gpsLongitude\r\nmeta.orientation\r\n\r\nSes/Video:\r\nmeta.duration, meta.durationText\r\nmeta.videoWidth, meta.videoHeight\r\nmeta.bitrateKbps, meta.frameRate\r\nmeta.audioChannels, meta.audioSampleRate\r\nmeta.audioBitrateKbps, meta.videoCodec\r\nmeta.audioCodec\r\n\r\nMüzik etiketleri:\r\nmeta.title, meta.artist, meta.artists\r\nmeta.album, meta.year, meta.genre\r\nmeta.trackNumber, meta.bpm\r\n\r\nUygulama:\r\nmeta.productName, meta.fileVersion\r\nmeta.copyright, meta.description\r\nmeta.isSigned, meta.signatureValid\r\nmeta.publisher\r\n\r\nSort script sıralama anahtarı, Dynamic script string döndürmelidir."}
            });

            texts["kk"] = MergeLanguage(texts["en"], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"LanguageTitle", "Тіл таңдау"},
                {"LanguagePrompt", "Қолданба тілін таңдаңыз"},
                {"LanguageOk", "Жарайды"},
                {"LanguageShortLabel", "Тіл"},
                {"AddFilesFolders", "Файл/Қалта қосу"},
                {"SimulatePreview", "Сынау (Алдын ала)"},
                {"ApplyChanges", "Өзгерістерді қолдану"},
                {"UndoLast", "Соңғыны қайтару"},
                {"PreviewSort", "Сұрыптауды алдын ала көру"},
                {"ApplySort", "Сұрыптауды қолдану"},
                {"CancelSort", "Сұрыптаудан бас тарту"},
                {"LoadTemplate", "Үлгіні оқу"},
                {"SaveTemplate", "Үлгіні сақтау"},
                {"AddToContextMenu", "Контекст мәзіріне қосу"},
                {"ItemSingular", "элемент"},
                {"ItemPlural", "элемент"},
                {"CurrentNameColumn", "Ағымдағы атау"},
                {"NewNameColumn", "Жаңа атау"},
                {"PathColumn", "Жол"},
                {"SizeColumn", "Өлшем"},
                {"TypeColumn", "Түр"},
                {"StatusColumn", "Күй"},
                {"SortTab", "Sort"},
                {"AddFilesTitle", "Файл қосу"},
                {"AddFolderDescription", "Қажет болса қалта қосыңыз. Өткізу үшін бас тартыңыз."},
                {"TemplateNotSelected", "Жүктелетін үлгі таңдалмады."},
                {"TemplateNameRequired", "Үлгі атауын енгізіңіз."},
                {"OverwriteTemplate", "\"{0}\" үлгісінің үстінен жазылсын ба?"},
                {"TemplateSaved", "Үлгі сақталды."},
                {"NoValidRenames", "Қолданылатын жарамды қайта атау жоқ."},
                {"ApplyConfirm", "{0} файл қайта аталады. Жалғастыру керек пе?"},
                {"UndoNoOperation", "Қайтарылатын соңғы әрекет жоқ."},
                {"UndoConfirm", "{0} файл ескі атауына қайтарылады. Жалғастыру керек пе?"},
                {"UndoRestored", "{0} файл қайтарылды."},
                {"SortErrorTitle", "Сұрыптау қатесі"},
                {"SortPreviewActiveMessage", "Алдымен ағымдағы сұрыптау алдын ала қарауын қолданыңыз немесе бас тартыңыз."},
                {"SimulationComplete", "Сынау аяқталды."},
                {"ContextMenuText", "Advanced Javascript Renamer арқылы ашу"},
                {"UnknownError", "Белгісіз қате."}
            });

            texts["az"] = MergeLanguage(texts["en"], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"LanguageTitle", "Dil Seçimi"},
                {"LanguagePrompt", "Tətbiq dilini seçin"},
                {"LanguageOk", "Tamam"},
                {"LanguageShortLabel", "Dil"},
                {"AddFilesFolders", "Fayl/Qovluq əlavə et"},
                {"SimulatePreview", "Sınaq (Ön baxış)"},
                {"ApplyChanges", "Dəyişiklikləri tətbiq et"},
                {"UndoLast", "Geri al"},
                {"PreviewSort", "Sıralamaya ön baxış"},
                {"ApplySort", "Sıralamanı tətbiq et"},
                {"CancelSort", "Sıralamanı ləğv et"},
                {"LoadTemplate", "Şablonu oxu"},
                {"SaveTemplate", "Şablonu saxla"},
                {"AddToContextMenu", "Sağ klik menyusuna əlavə et"},
                {"ItemSingular", "element"},
                {"ItemPlural", "element"},
                {"CurrentNameColumn", "Hazırkı ad"},
                {"NewNameColumn", "Yeni ad"},
                {"PathColumn", "Yol"},
                {"SizeColumn", "Ölçü"},
                {"TypeColumn", "Növ"},
                {"StatusColumn", "Status"},
                {"SortTab", "Sort"},
                {"AddFilesTitle", "Fayl əlavə et"},
                {"AddFolderDescription", "İstəyə görə qovluq əlavə edin. Keçmək üçün ləğv edin."},
                {"TemplateNotSelected", "Yüklənəcək şablon seçilməyib."},
                {"TemplateNameRequired", "Şablon üçün ad yazın."},
                {"OverwriteTemplate", "\"{0}\" şablonunun üzərinə yazılsın?"},
                {"TemplateSaved", "Şablon saxlandı."},
                {"NoValidRenames", "Tətbiq ediləcək keçərli ad dəyişmə əməliyyatı yoxdur."},
                {"ApplyConfirm", "{0} faylın adı dəyişdiriləcək. Davam edilsin?"},
                {"UndoNoOperation", "Geri alınacaq son əməliyyat yoxdur."},
                {"UndoConfirm", "{0} fayl köhnə adına qaytarılacaq. Davam edilsin?"},
                {"UndoRestored", "{0} fayl geri qaytarıldı."},
                {"SortErrorTitle", "Sıralama xətası"},
                {"SortPreviewActiveMessage", "Əvvəlcə cari sıralama ön baxışını tətbiq edin və ya ləğv edin."},
                {"SimulationComplete", "Sınaq tamamlandı."},
                {"ContextMenuText", "Advanced Javascript Renamer ilə aç"},
                {"UnknownError", "Naməlum xəta."}
            });

            texts["ru"] = MergeLanguage(texts["en"], new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                {"LanguageTitle", "Выбор языка"},
                {"LanguagePrompt", "Выберите язык приложения"},
                {"LanguageOk", "OK"},
                {"LanguageSaveError", "Не удалось сохранить выбор языка. Приложение продолжит работу на выбранном языке в этом сеансе."},
                {"LanguageShortLabel", "Язык"},
                {"AddFilesFolders", "Добавить файлы/папки"},
                {"SimulatePreview", "Симуляция (предпросмотр)"},
                {"ApplyChanges", "Применить изменения"},
                {"UndoLast", "Отменить последнее"},
                {"PreviewSort", "Предпросмотр сортировки"},
                {"ApplySort", "Применить сортировку"},
                {"CancelSort", "Отменить сортировку"},
                {"LoadTemplate", "Загрузить шаблон"},
                {"SaveTemplate", "Сохранить шаблон"},
                {"AddToContextMenu", "Добавить в контекстное меню"},
                {"ItemSingular", "элемент"},
                {"ItemPlural", "элементов"},
                {"CurrentNameColumn", "Текущее имя"},
                {"NewNameColumn", "Новое имя"},
                {"PathColumn", "Путь"},
                {"SizeColumn", "Размер"},
                {"TypeColumn", "Тип"},
                {"StatusColumn", "Статус"},
                {"StaticTab", "Static"},
                {"SortTab", "Sort"},
                {"DynamicTab", "Dynamic"},
                {"AddFilesTitle", "Добавить файлы"},
                {"AddFolderDescription", "При необходимости добавьте папку. Нажмите Отмена, чтобы пропустить."},
                {"TemplateNotSelected", "Не выбран шаблон для загрузки."},
                {"TemplateNameRequired", "Введите имя шаблона."},
                {"OverwriteTemplate", "Перезаписать шаблон \"{0}\"?"},
                {"TemplateSaved", "Шаблон сохранен."},
                {"TemplateSaveErrorTitle", "Ошибка сохранения шаблона"},
                {"NoValidRenames", "Нет допустимых операций переименования."},
                {"ApplyConfirm", "Будет переименовано файлов: {0}. Продолжить?"},
                {"UndoNoOperation", "Нет последней операции для отмены."},
                {"UndoConfirm", "Будет восстановлено старое имя файлов: {0}. Продолжить?"},
                {"UndoRestored", "Восстановлено файлов: {0}."},
                {"RegistryErrorTitle", "Ошибка реестра"},
                {"TemplateMissing", "Шаблон не найден."},
                {"TemplateLoadErrorTitle", "Ошибка загрузки шаблона"},
                {"TemplateListErrorTitle", "Ошибка списка шаблонов"},
                {"SortErrorTitle", "Ошибка сортировки"},
                {"SortPreviewActiveMessage", "Сначала примените или отмените текущий предпросмотр сортировки."},
                {"AddErrorTitle", "Ошибка добавления"},
                {"FileErrorTitle", "Ошибка файла"},
                {"SimulationComplete", "Симуляция завершена."},
                {"ContextMenuText", "Открыть в Advanced Javascript Renamer"},
                {"StartupErrorTitle", "Ошибка Advanced Javascript Renamer"},
                {"UnknownError", "Неизвестная ошибка."},
                {"ExePathNotFound", "Путь к исполняемому файлу Advanced Javascript Renamer не найден."},
                {"RegistryKeyCreateFailed", "Не удалось создать ключ реестра."}
            });

            return texts;
        }

        private static Dictionary<string, string> MergeLanguage(Dictionary<string, string> fallback, Dictionary<string, string> overrides)
        {
            var merged = new Dictionary<string, string>(fallback, StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<string, string> item in overrides)
            {
                merged[item.Key] = item.Value;
            }

            return merged;
        }

        [DataContract]
        private sealed class LanguageSettings
        {
            [DataMember(Name = "language")]
            public string Language { get; set; }
        }

        internal sealed class LanguageInfo
        {
            public LanguageInfo(string code, string name)
            {
                Code = code;
                Name = name;
            }

            public string Code { get; private set; }
            public string Name { get; private set; }

            public override string ToString()
            {
                return Name;
            }
        }

        private sealed class LanguageSelectionForm : Form
        {
            private readonly ComboBox _languageComboBox;

            public LanguageSelectionForm(IEnumerable<LanguageInfo> languages)
            {
                Text = "Select Language / Dil Seçimi";
                StartPosition = FormStartPosition.CenterScreen;
                FormBorderStyle = FormBorderStyle.FixedDialog;
                MinimizeBox = false;
                MaximizeBox = false;
                ClientSize = new Size(360, 130);

                var label = new Label
                {
                    Text = "Select application language / Uygulama dilini seçin",
                    AutoSize = false,
                    Left = 16,
                    Top = 16,
                    Width = 328,
                    Height = 24
                };

                _languageComboBox = new ComboBox
                {
                    DropDownStyle = ComboBoxStyle.DropDownList,
                    Left = 16,
                    Top = 48,
                    Width = 328
                };
                foreach (LanguageInfo language in languages)
                {
                    _languageComboBox.Items.Add(language);
                }
                _languageComboBox.SelectedIndex = 0;

                var okButton = new Button
                {
                    Text = "OK",
                    DialogResult = DialogResult.OK,
                    Left = 244,
                    Top = 88,
                    Width = 100,
                    Height = 28
                };

                AcceptButton = okButton;
                Controls.Add(label);
                Controls.Add(_languageComboBox);
                Controls.Add(okButton);
            }

            public string SelectedCode
            {
                get
                {
                    var language = _languageComboBox.SelectedItem as LanguageInfo;
                    return language == null ? null : language.Code;
                }
            }
        }
    }
}
