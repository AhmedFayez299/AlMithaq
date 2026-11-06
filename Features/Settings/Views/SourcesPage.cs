using AlMithaq.Core.Abstractions;
using AlMithaq.Shared.Controls;
namespace AlMithaq.Pages;
public sealed class SourcesPage : ContentPage
{
    private readonly IQuranRepository _quran;
    public SourcesPage(IQuranRepository quran)
    {
        _quran = quran; Ui.Page(this, "المصادر والتراخيص");
        View Section(string title, string text, params View[] actions)
        {
            var body = Ui.Stack(Ui.Label(title, 15, true), Ui.Label(text, 12)); body.Spacing = 9;
            foreach (var action in actions) body.Add(action); var card = Ui.Card(body); card.Padding = 15; return card;
        }
        Button Link(string title, string url) { var button = Ui.AsyncButton(title, () => Browser.Default.OpenAsync(url), true); button.FontSize = 12; return button; }
        var intro = Ui.Label("مراجع واضحة... ومحتوى تعرف مصدره", 16, true);
        var detail = Ui.Label("معلومات المحتوى وحقوقه، مستقلة عن تفضيلات التطبيق.", 11); Ui.ThemeProperty(detail, Label.TextColorProperty, "AppMuted");
        var stack = Ui.Stack(intro, detail,
            Section("القرآن الكريم", "نص Tanzil Uthmani 1.1 محفوظ دون تعديل، © 2007–2026 Tanzil Project؛ CC BY 3.0 مع شرط عدم تغيير النص. بيانات الأجزاء والصفحات من AlQuran.cloud، وبدايات أرباع الأحزاب من بيانات Tanzil الأصلية بترخيص CC BY. العرض النصي مستقل عن صفحات المصحف المطبوعة.", Link("مصدر النص · Tanzil", "https://tanzil.net"), Link("بيانات تقسيم القرآن · Tanzil", "https://tanzil.net/docs/Quran_Metadata"), Ui.AsyncButton("إشعار ترخيص النص", ShowQuranLicenseAsync, true)),
            Section("صفحات المصحف", "صفحات رقمية من مجمع الملك فهد عبر Quran.ws، وإحداثيات الآيات وبيانات المصدر CC BY 4.0. التراخيص وإشعارات المصدر مرفقة بالحزمة.", Link("حقوق المصحف الرقمي", "https://dm.qurancomplex.gov.sa/rights/")),
            Section("التلاوات والتفسير", "التفسير الميسر لمجمع الملك فهد عبر AlQuran.cloud. تلاوات Islamic Network مجانية وفق شروط المصدر، ويحتفظ القراء وأصحاب الحقوق بحقوق التسجيلات.", Link("شروط المصدر", "https://alquran.cloud/terms-and-conditions"), Link("تلاوات آية بآية · EveryAyah", "https://everyayah.com/")),
            Section("صوت الصلاة على النبي", "تسجيل قدّمه مالك التطبيق لتذكير الصلاة على النبي. ضُبطت جهارته وضُغط إلى AAC مع الاحتفاظ بالتسجيل كاملًا.", Ui.Label("الصوت يعمل دون إنترنت.", 12)),
            Section("تسجيلات الأذان", "علي بن أحمد ملا وأحمد الحداد: ملفان قدّمهما صاحب المشروع. الأول محفوظ دون إعادة ترميز، والثاني مضغوط إلى AAC أحادي القناة دون اقتطاع. الحقوق لأصحاب التسجيلات؛ لا تُنسب إليهما تراخيص التسجيلات السابقة.", Ui.AsyncButton("تفاصيل ملفات الأذان", ShowAdhanLicensesAsync, true)),
            Section("حصن المسلم", "مكتبة نصية كاملة من المصدر الرقمي asellam/HisnElMuslim: 133 بابًا و302 نص، مع حفظ النصوص والمراجع وأعداد التكرار كما وردت. إعداد سعيد بن علي بن وهف القحطاني، والنسخة الرقمية لعبد الله سَلّام بترخيص MIT. ترخيص النسخة الرقمية مضمّن مع التطبيق.", Link("مصدر حصن المسلم", "https://github.com/asellam/HisnElMuslim")),
            Section("الأذكار والأدعية", "الصباح والمساء: مشروع AbdelrahmanBayoumi/Azkar-App، MIT. مراجع الأحاديث ظاهرة كما وردت في البيانات. أذكار النوم وبعد الصلاة تحمل مراجع الصحيحين داخل البطاقات."),
            Section("حاسبة الزكاة", "الحسابات عددية مشروطة بالمدخلات وتحقق الشروط. يوضّح كل نوع مرجعه، ومنها دار الإفتاء المصرية ودليل زكاة الأفراد وإرشادات الأنعام. أسعار المعادن وقيمة الفطر تُدخل يدويًا وفق اليوم والبلد؛ لا يُدّعى أن الحاسبة تغطي كل خلاف أو حالة خاصة.",
                Link("دليل زكاة الأفراد", "https://zatca.gov.sa/ar/HelpCenter/guidelines/Documents/Zakat_Individual.pdf"), Link("أنصبة الأنعام والمحاصيل", "https://zatca.gov.sa/ar/HelpCenter/FAQs/Documents/FAQs-Zakat-Livestock-Grain-Fruits.pdf"), Link("نصاب المال · دار الإفتاء", "https://www.dar-alifta.org/ar/Fatwa/Details/21692")),
            Section("الحساب والخطوط والرسومات", "حساب الصلاة: Adhan، MIT. عرض الوسائط: .NET MAUI Community Toolkit، MIT. خطا El Messiri وAmiri Quran: SIL Open Font License 1.1. الهوية والرسومات الخفيفة صُممت للتطبيق."));
        stack.Padding = new Thickness(16, 12, 16, 24); stack.Spacing = 6; Content = new ScrollView { Content = stack };
    }
    private async Task ShowQuranLicenseAsync() { var document = await _quran.LoadAsync(); await DisplayAlertAsync("Tanzil Quran Text License", document.CopyrightNotice, "إغلاق"); }
    private async Task ShowAdhanLicensesAsync()
    {
        using var stream = await FileSystem.OpenAppPackageFileAsync("adhan-notice.txt"); using var reader = new StreamReader(stream);
        await DisplayAlertAsync("مصادر الأذان", await reader.ReadToEndAsync(), "إغلاق");
    }
}
