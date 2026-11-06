global using System.Text.Json;
public static class Preferences
{
    private static readonly Dictionary<string,object> Data=[];
    public static T Get<T>(string key,T fallback)=>Data.TryGetValue(key,out var value)?(T)value:fallback;
    public static void Set<T>(string key,T value)=>Data[key]=value!;
    public static bool ContainsKey(string key)=>Data.ContainsKey(key);
}
namespace AlMithaq.Features.Quran.Services
{
    public static class WirdReminders
    {
        private static bool _refreshing;
        public static void Refresh()
        {
            if(_refreshing) throw new Exception("Recursive reminder scheduling");
            _refreshing=true;try{_ = DailyWirdStore.ForReminders();}finally{_refreshing=false;}
        }
    }
}
