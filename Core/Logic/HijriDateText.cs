using System.Globalization;
namespace AlMithaq.Core.Logic;
public static class HijriDateText
{
    public static readonly string[] Months = ["محرم", "صفر", "ربيع الأول", "ربيع الآخر", "جمادى الأولى", "جمادى الآخرة", "رجب", "شعبان", "رمضان", "شوال", "ذو القعدة", "ذو الحجة"];
    public static string Format(DateTime date)
    {
        var calendar = new HijriCalendar();
        return $"{calendar.GetDayOfMonth(date)} {Months[calendar.GetMonth(date) - 1]} {calendar.GetYear(date)} هـ";
    }
}
