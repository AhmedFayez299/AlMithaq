using AlMithaq.Core.Logic;
using AlMithaq.Shared.Controls;
using System.Globalization;

namespace AlMithaq.Pages;

public sealed class ZakatPage : ContentPage
{
    private readonly ChoicePicker _type = new() { Title = "نوع الزكاة", ItemsSource = new[] { "الأموال والمدخرات", "الذهب والفضة", "التجارة والأسهم والعقارات", "الزروع والثمار", "الأنعام", "زكاة الفطر", "الركاز" }, SelectedIndex = 0 };
    private readonly VerticalStackLayout _form = new() { Spacing = 10 };
    private readonly Label _result = Ui.Label("أدخل البيانات ثم اضغط احسب", 20, true);
    private readonly Label _details = Ui.Label("", 12);
    private readonly Label _note = Ui.Label("", 11);
    private readonly CheckBox _conditions = new();
    private readonly Dictionary<string, Entry> _fields = [];
    private readonly Dictionary<string, ChoicePicker> _choices = [];
    private readonly VerticalStackLayout _conditionRow;
    private readonly ScrollView _scroll = new();
    private readonly Label _confirmation;
    private string _source = "https://www.dar-alifta.org/ar/Fatwa/Details/21692";
    private const string Guide = "https://zatca.gov.sa/ar/HelpCenter/guidelines/Documents/Zakat_Individual.pdf";
    public ZakatPage()
    {
        Ui.Page(this, "حاسبة الزكاة");
        var title = Ui.Label("زكاة مالك بخطوات واضحة", 17, true);
        var intro = Ui.Label("اختر النوع، وأدخل أسعار يوم الزكاة بعملة واحدة. الحساب محلي ولا تُرسل أموالك إلى أي خدمة.", 11); Ui.ThemeProperty(intro, Label.TextColorProperty, "AppMuted");
        var confirmation = _confirmation = Ui.Label("أؤكد تحقق الحول الهجري وباقي الشروط الموضحة", 12); confirmation.VerticalOptions = LayoutOptions.Center;
        var conditionGrid = new Grid { ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star) }, ColumnSpacing = 6 }; conditionGrid.Add(_conditions); conditionGrid.Add(confirmation, 1);
        _conditionRow = Ui.Stack(conditionGrid); _conditionRow.Spacing = 0;
        _result.LineBreakMode = LineBreakMode.WordWrap;
        Ui.ThemeProperty(_details, Label.TextColorProperty, "AppMuted"); Ui.ThemeProperty(_note, Label.TextColorProperty, "AppMuted");
        var button = Ui.Button("احسب الزكاة", Calculate); button.FontSize = 13;
        var source = Ui.AsyncButton("المصدر وطريقة الحساب", () => Browser.Default.OpenAsync(_source), true); source.FontSize = 12;
        var body = Ui.Stack(title, intro, _type, Ui.Card(_form), _conditionRow, button, Ui.Card(Ui.Stack(_result, _details)), Ui.Card(Ui.Stack(Ui.Label("شروط هذا النوع", 14, true), _note, source)));
        body.Padding = new Thickness(16, 12, 16, 24); body.Spacing = 8;
        var host = new Grid { ClassId = "choice-host" }; _scroll.Content = body; host.Add(_scroll); Content = host;
        _type.SelectedIndexChanged += (_, _) => BuildForm(); _conditions.CheckedChanged += (_, _) => InvalidateResult(); BuildForm();
    }
    protected override bool OnBackButtonPressed() => PopupSheet.Dismiss(this) || base.OnBackButtonPressed();
    private void InvalidateResult() { _result.Text = "أدخل البيانات ثم اضغط احسب"; _details.Text = ""; }
    private void Field(string id, string label, string placeholder = "0")
    {
        var entry = new Entry { Placeholder = placeholder, FontFamily = "Almarai-Regular", FontSize = 14, Keyboard = Keyboard.Numeric, HorizontalTextAlignment = TextAlignment.Start };
        Ui.ThemeProperty(entry, Entry.TextColorProperty, "AppInk"); Ui.ThemeProperty(entry, Entry.PlaceholderColorProperty, "AppMuted");
        entry.HeightRequest = 50; Ui.ThemeProperty(entry, Entry.BackgroundColorProperty, "AppSoft");
        entry.TextChanged += (_, _) => InvalidateResult(); _fields[id] = entry;
        var field = Ui.Stack(Ui.Label(label, 11), entry); field.Spacing = 1; _form.Add(field);
    }
    private ChoicePicker Choice(string id, string label, string[] options)
    {
        var picker = new ChoicePicker { Title = label, ItemsSource = options, SelectedIndex = 0 };
        picker.SelectedIndexChanged += (_, _) => InvalidateResult(); _choices[id] = picker;
        var group = Ui.Stack(Ui.Label(label, 11), picker); group.Spacing = 2; _form.Add(group); return picker;
    }
    private void NisabFields()
    {
        Choice("nisab", "مرجع النصاب", ["85 غ ذهب عيار 21 · دار الإفتاء المصرية", "85 غ ذهب خالص · الدليل الإرشادي", "595 غ فضة خالصة"]);
        Field("price", "سعر جرام المعدن المختار (21 أو 24 أو فضة خالصة)", "سعر اليوم في عملتك");
    }
    private void BuildForm()
    {
        _form.Clear(); _fields.Clear(); _choices.Clear(); _conditions.IsChecked = false; InvalidateResult();
        _conditionRow.IsVisible = _type.SelectedIndex is 0 or 1 or 2 or 4;
        _confirmation.Text = _type.SelectedIndex == 4 ? "أؤكد أن الأنعام سائمة غير عاملة، وأن الحول والشروط متحققة" : "أؤكد تحقق الحول الهجري وباقي الشروط الموضحة";
        _source = Guide;
        switch (_type.SelectedIndex)
        {
            case 0:
                Field("assets", "مجموع النقود والحسابات والمدخرات المستحقة"); Field("debts", "التزامات يجوز خصمها وفق مرجعك (اختياري)"); NisabFields();
                _source = "https://www.dar-alifta.org/ar/Fatwa/Details/21692";
                _note.Text = "يشترط الملك التام وبلوغ النصاب والحول الهجري. أضف المدخر من الرواتب والإيجار؛ لا تضف قيمة مسكنك أو أدوات الاستعمال. الديون والحاجات الأصلية تختلف تفاصيلها؛ أدخل فقط الخصم الذي أجازه مرجعك. اجمع الأموال التي تُضم شرعًا في حساب واحد، ولا تكررها في قسم آخر.";
                break;
            case 1:
                var metalChoice = Choice("metal", "المعدن", ["ذهب مدخر", "فضة مدخرة"]); Field("grams", "الوزن بالجرام"); Choice("karat", "عيار الذهب", ["21", "24", "22", "18", "14", "12", "9"]); Field("purity", "نقاوة الفضة من 1000", "مثل 925"); _fields["purity"].Text = "925"; ((View)_fields["purity"].Parent!).IsVisible = false;
                Field("purePrice", "سعر جرام الذهب عيار 24 أو الفضة الخالصة");
                Choice("goldNisab", "نصاب الذهب", ["ما يعادل 85 غ عيار 21 · مصر", "85 غ ذهب خالص · الدليل"]);
                metalChoice.SelectedIndexChanged += (_, _) => { var gold = metalChoice.SelectedIndex == 0; ((View)_choices["goldNisab"].Parent!).IsVisible = gold; ((View)_choices["karat"].Parent!).IsVisible = gold; ((View)_fields["purity"].Parent!).IsVisible = !gold; };
                _note.Text = "يحوّل الوزن إلى معدن خالص، ويحسب 2.5٪ بعد تحقق الشروط. الفضة: 595 غ خالصة. اختيار نصاب الذهب يوضح اختلاف مرجع 21 و24. حلي الاستعمال محل خلاف؛ هذه الحاسبة للمدخر وما ثبتت زكاته لديك. إذا لم يبلغ المعدن وحده النصاب فقد تجب الزكاة عند ضمه لأموال أخرى وفق مرجعك.";
                break;
            case 2:
                var mode = Choice("trade", "طريقة الحساب", ["عروض تجارة أو أسهم معدّة للتداول", "أسهم استثمار · زكاة السهم المعلنة"]);
                Field("goods", "قيمة البضائع والأسهم والعقارات المعدّة للبيع"); Field("cash", "النقد والديون المرجوّة التحصيل"); Field("debts", "التزامات حالّة يجوز خصمها (اختياري)"); NisabFields();
                Field("shares", "عدد أسهم الاستثمار (للخيار الثاني فقط)"); Field("perShare", "الزكاة المستحقة لكل سهم وفق إعلان الشركة");
                mode.SelectedIndexChanged += (_, _) => { foreach (var id in new[] { "goods", "cash", "debts", "price" }) ((View)_fields[id].Parent!).IsVisible = mode.SelectedIndex == 0; foreach (var id in new[] { "shares", "perShare" }) ((View)_fields[id].Parent!).IsVisible = mode.SelectedIndex == 1; ((View)_choices["nisab"].Parent!).IsVisible = mode.SelectedIndex == 0; };
                ((View)_fields["shares"].Parent!).IsVisible = ((View)_fields["perShare"].Parent!).IsVisible = false;
                _note.Text = "التجارة تُقوّم بسعر يوم الزكاة، دون أصول التشغيل. العقار المعد للبيع يدخل هنا؛ المؤجّر لا تدخل قيمته، وإنما المدخر من ريعه ضمن الأموال. أسهم الاستثمار تُحسب من مقدار زكاة السهم الموثّق؛ لا تستخدم السعر السوقي تلقائيًا، ولا تكرر زكاة أدّتها الشركة عنك. إن لم يُعلن المقدار فاسأل مختصًا.";
                break;
            case 3:
                Field("kg", "كمية المحصول الزكوي بعد التجفيف والتنقية (كجم)");
                var harvestChoice = Choice("harvestNisab", "تقدير النصاب بالكيلوجرام", ["612 كجم · دار الإفتاء المصرية", "وزن آخر معتمد للمحصول لدى مرجعك"]);
                Field("customKg", "وزن النصاب المعتمد للمحصول (للخيار الثاني)"); ((View)_fields["customKg"].Parent!).IsVisible = false;
                harvestChoice.SelectedIndexChanged += (_, _) => ((View)_fields["customKg"].Parent!).IsVisible = harvestChoice.SelectedIndex == 1;
                Choice("irrigation", "طريقة الري", ["بلا كلفة · 10٪", "بكلفة وآلة · 5٪", "بالتساوي بين الطريقتين · 7.5٪"]);
                Field("kgPrice", "سعر الكيلوجرام (اختياري لإظهار القيمة)");
                _source = "https://www.dar-alifta.org/ar/Fatwa/Details/13476";
                _note.Text = "الزكاة عند الحصاد دون اشتراط حول. يلزم أن يكون المحصول مما تجب فيه الزكاة وفق مرجعك؛ تختلف المذاهب في الأصناف وتقدير الوزن. لا تخصم تكاليف الزراعة تلقائيًا. عند غلبة إحدى طريقتي الري اسأل مرجعك، ولا تستخدم خيار التساوي.";
                break;
            case 4:
                Choice("animal", "نوع الأنعام", ["الغنم والماعز", "البقر والجاموس", "الإبل"]); Field("animals", "عدد الرؤوس (حتى 100000)");
                _source = "https://zatca.gov.sa/ar/HelpCenter/FAQs/Documents/FAQs-Zakat-Livestock-Grain-Fruits.pdf";
                _note.Text = "الحساب للسائمة غير العاملة المعدّة للدر والنسل، بعد الحول وباقي الشروط، على أنصبة الجمهور. المعدّة للبيع تُحسب عروض تجارة، ولا تُزكّى مرتين. أنصبة الإبل والغنم: البخاري 1454، والبقر: التبيع لكل 30 والمسنّة لكل 40. الخلطة والأعمار والأوقاص لها تفاصيل؛ تأكد مع مختص قبل الإخراج. قد تصح أكثر من تركيبة في الأعداد الكبيرة.";
                break;
            case 5:
                Field("people", "عدد الأشخاص"); Field("fitr", "المقدار للفرد المعتمد في بلدك هذا العام");
                Choice("fitrUnit", "وحدة المقدار", ["قيمة نقدية وفق مرجعك", "كيلوجرام من القوت وفق مرجعك"]);
                _source = "https://www.dar-alifta.org/ar/Fatwa/Details/21778";
                _note.Text = "أدخل قيمة أو وزنًا معتمدًا للبلد والعام ونوع القوت؛ لا توجد قيمة ثابتة لجميع البلدان أو السنوات. الحساب هو عدد الأشخاص × المقدار المعتمد للفرد، بعد تحقق شروط الوجوب. إخراج القيمة وتقدير الصاع يختلفان باختلاف المرجع.";
                break;
            case 6:
                Field("rikaz", "قيمة ما ثبت أنه ركاز شرعًا ومملوك لك");
                _source = "https://www.dar-alifta.org/ar/Fatwa/Details/13458";
                _note.Text = "الحساب العددي للخمس (20٪) فقط، بعد تحقق وصف الركاز والملكية لدى جهة مختصة. لا يعني أن كل كنز أو أثر أو معدن مستخرج ركاز، ولا يمنح حق التنقيب أو التصرف في الآثار. المعادن واللقطة وحقوق الدولة لها أحكام مستقلة.";
                break;
        }
    }
    private decimal Number(string id, bool optional = false)
    {
        var value = NumberInput.Normalize(_fields[id].Text ?? "").Trim();
        if (!optional && value.Length == 0) { _fields[id].Focus(); throw new ArgumentException("أكمل الحقل الفارغ: " + ((VerticalStackLayout)_fields[id].Parent!).Children.OfType<Label>().First().Text); }
        if (optional && value.Length == 0) return 0;
        if (!decimal.TryParse(value, NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out var n) || n < 0 || n > 1_000_000_000_000m) throw new ArgumentException("أدخل أرقامًا موجبة صحيحة دون فواصل آلاف، وبفاصل عشري نقطة أو ٫");
        return n;
    }
    private int Integer(string id) { var n = Number(id); if (n != decimal.Truncate(n) || n > 100000) throw new ArgumentException("العدد يجب أن يكون صحيحًا من 1 إلى 100000"); return (int)n; }
    private decimal Nisab() { var price = Number("price"); if (price <= 0) throw new ArgumentException("أدخل سعر الجرام الحالي"); return price * (_choices["nisab"].SelectedIndex == 2 ? 595 : 85); }
    private static string Format(decimal n) => n.ToString("N2", CultureInfo.GetCultureInfo("ar-EG"));
    private void Show(ZakatAmount amount, string unit, string extra = "")
    {
        _result.Text = amount.Reached ? $"{Format(amount.Due)} {unit}" : "لم يبلغ النصاب بهذه المدخلات";
        _details.Text = $"الوعاء: {Format(amount.Basis)} {unit}\nالنصاب: {Format(amount.Nisab)} {unit}\nالنسبة: {amount.Rate:P1}\n{extra}\nنتيجة حسابية مشروطة بصحة المدخلات والشروط المذكورة.";
    }
    private async void Calculate()
    {
        foreach (var entry in _fields.Values) entry.Unfocus();
        try
        {
            if (_conditionRow.IsVisible && !_conditions.IsChecked) throw new ArgumentException("أكّد تحقق الحول الهجري وبقية الشروط أولًا؛ عدم التأكيد لا يعني أن الزكاة صفر.");
            switch (_type.SelectedIndex)
            {
                case 0: Show(ZakatCalculator.Wealth(Number("assets"), Number("debts", true), Nisab(), true), "من عملتك"); break;
                case 1:
                    var silver = _choices["metal"].SelectedIndex == 1; var grams = Number("grams"); var price = Number("purePrice"); if (price <= 0) throw new ArgumentException("أدخل سعر المعدن الخالص");
                    var pure = silver ? ZakatCalculator.PureSilver(grams, Number("purity")) : ZakatCalculator.PureGold(grams, new decimal[] { 21, 24, 22, 18, 14, 12, 9 }[_choices["karat"].SelectedIndex]);
                    var threshold = silver ? 595m : _choices["goldNisab"].SelectedIndex == 0 ? 85m * 21m / 24m : 85m;
                    Show(ZakatCalculator.Wealth(pure * price, 0, threshold * price, true), "من عملتك", $"الوزن الخالص: {pure:0.###} غ · الزكاة العينية عند بلوغ النصاب: {grams * .025m:0.###} غ من العيار نفسه"); break;
                case 2:
                    if (_choices["trade"].SelectedIndex == 1) { var shares = Number("shares"); var perShare = Number("perShare"); if (shares <= 0 || perShare <= 0) throw new ArgumentException("أدخل عدد الأسهم ومقدار الزكاة المعلن لكل سهم"); _result.Text = $"{Format(shares * perShare)} من عملتك"; _details.Text = "عدد الأسهم × زكاة السهم المعلنة، بافتراض تحقق شروط الوجوب وعدم إخراج الشركة لها نيابة عنك."; }
                    else Show(ZakatCalculator.Wealth(Number("goods") + Number("cash", true), Number("debts", true), Nisab(), true), "من عملتك");
                    break;
                case 3:
                    var harvest = ZakatCalculator.Harvest(Number("kg"), _choices["harvestNisab"].SelectedIndex == 0 ? 612 : Number("customKg"), new[] { .1m, .05m, .075m }[_choices["irrigation"].SelectedIndex]);
                    Show(harvest, "كجم", Number("kgPrice", true) > 0 ? $"القيمة الحسابية: {Format(harvest.Due * Number("kgPrice"))} من عملتك؛ جواز إخراج القيمة بحسب مرجعك." : "النتيجة من جنس المحصول."); break;
                case 4:
                    var animals = ZakatCalculator.Livestock(new[] { "sheep", "cattle", "camels" }[_choices["animal"].SelectedIndex], Integer("animals"));
                    _result.Text = animals.Count == 0 ? "لم يبلغ العدد أول نصاب" : string.Join("\n", animals.Select(a => $"{a.Count} × {a.Kind}")); _details.Text = "نتيجة عينية وفق شروط السوم والحول المذكورة. العمر المطلوب مبيّن بجوار كل نوع. يشترط السوم والحول وألا تكون الأنعام عاملة أو معدّة للتجارة."; break;
                case 5: _result.Text = $"{Format(ZakatCalculator.Fitr(Integer("people"), Number("fitr")))} " + (_choices["fitrUnit"].SelectedIndex == 0 ? "من عملتك" : "كجم"); _details.Text = "عدد الأشخاص × مقدار الفرد الذي أدخلته. لا تمثل هذه النتيجة إعلانًا لقيمة رسمية جديدة."; break;
                case 6: _result.Text = $"{Format(ZakatCalculator.Rikaz(Number("rikaz")))} من عملتك"; _details.Text = "القيمة × 20٪، بعد تحقق وصف الركاز والملكية والحقوق لدى جهة مختصة؛ لا يُطبّق تلقائيًا على المعادن والآثار."; break;
            }
        }
        catch (Exception ex) when (ex is ArgumentException or OverflowException) { _result.Text = "راجع المدخلات والشروط"; _details.Text = ex is ArgumentOutOfRangeException or OverflowException ? "تحقق من الوزن والعيار والأسعار والأعداد؛ يجب أن تكون ضمن الحدود الموضحة وأكبر من الصفر عند طلبها." : ex.Message; }
        finally { await Task.Delay(100); await _scroll.ScrollToAsync(_result, ScrollToPosition.Start, true); }
    }
}
