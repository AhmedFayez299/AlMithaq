using AlMithaq.Core.Abstractions;
using AlMithaq.Features.Quran.Services;
using AlMithaq.Features.PrayerTimes.Services;
namespace AlMithaq;
public static class DependencyInjection
{
 public static IServiceCollection AddDependencies(this IServiceCollection services)
 {
  services.AddSingleton(new HttpClient { Timeout = TimeSpan.FromSeconds(35) });
  services.AddScoped<AppNavigator>();
  services.AddScoped<IAppNavigator>(provider => provider.GetRequiredService<AppNavigator>());
  services.AddSingleton<IQuranRepository, QuranRepository>();
  services.AddSingleton<IReadingStore, ReadingStore>();
  services.AddSingleton<QuranMediaService>();
  services.AddSingleton<AlMithaq.Features.Azkar.Services.AzkarRepository>();
  services.AddSingleton<LocationService>();
  services.AddSingleton<IPrayerService, PrayerService>();
  services.AddTransient<HomePage>(); services.AddTransient<QuranPage>();
  services.AddTransient<AzkarPage>(); services.AddTransient<PrayerTimesPage>();
  services.AddTransient<QiblaPage>(); services.AddTransient<TasbeehPage>();
  services.AddTransient<SettingsPage>(); services.AddTransient<SourcesPage>(); services.AddTransient<ZakatPage>(); services.AddTransient<QiyamPage>(); services.AddTransient<KhatmahPage>(); services.AddTransient<MemorizationPage>();
  return services;
 }
}
