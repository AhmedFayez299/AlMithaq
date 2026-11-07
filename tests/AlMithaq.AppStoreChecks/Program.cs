using System.Text.Json;
using AlMithaq.Platforms.iOS;

const string bundle = "com.companyname.almithaq";
var passed = 0;
string Lookup(string version, string id = bundle, string minimumOS = "15.0", string url = "https://apps.apple.com/eg/app/id123456") =>
    JsonSerializer.Serialize(new { results = new[] { new { kind = "software", bundleId = id, version, minimumOsVersion = minimumOS, trackViewUrl = url } } });
void Check(string name, bool result)
{
    if (!result) throw new InvalidOperationException(name);
    passed++;
}
AppStoreUpdateInfo? Read(string json, string installed = "1.9", string os = "18.7.1") =>
    AppStoreUpdateInfo.FromLookup(json, bundle, installed, os);

Check("Versions compare numerically", Read(Lookup("1.10"))?.Version == "1.10");
Check("Equal versions do not prompt", Read(Lookup("1.9")) is null);
Check("Trailing zeroes do not create an update", Read(Lookup("1.9.0.0")) is null);
Check("Older store release does not downgrade", Read(Lookup("1.8.9")) is null);
Check("Single component is normalized", Read(Lookup("2.0"), "2") is null);
Check("Unpublished app has no prompt", Read("{\"resultCount\":0,\"results\":[]}") is null);
Check("Wrong bundle is ignored", Read(Lookup("2.0", id: "another.app")) is null);
Check("Unsupported OS is ignored", Read(Lookup("2.0", minimumOS: "19.0")) is null);
Check("Malformed version is ignored", Read(Lookup("next")) is null);
Check("Invalid installed version is ignored", Read(Lookup("2.0"), "unknown") is null);
Check("External update URLs are rejected", Read(Lookup("2.0", url: "https://example.com/update")) is null);
Check("Non-HTTPS links are rejected", Read(Lookup("2.0", url: "http://apps.apple.com/eg/app/id123456")) is null);
Check("Non-array results are ignored", Read("{\"results\":null}") is null);
Console.WriteLine($"Passed {passed} App Store update checks.");
