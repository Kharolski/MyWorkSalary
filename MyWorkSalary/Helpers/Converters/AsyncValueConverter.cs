using System.Collections.Concurrent;
using System.Globalization;

/// <summary>
/// Bas-klass för Value Converters som behöver hämta data asynkront.
/// 
/// PROBLEM: IValueConverter.Convert() är inte async, men vi behöver ofta hämta data från databas/API.
/// 
/// LÖSNING: Denna klass löser problemet genom:
/// 1. 🔄 Returnerar default-värde omedelbart (t.ex. "..." för loading)
/// 2. 🚀 Startar async-laddning i bakgrunden
/// 3. 💾 Cachar resultatet för snabb åtkomst nästa gång
/// 4. 🔔 Triggar UI-uppdatering när data är klar
/// 
/// ANVÄNDNING:
/// - Ärv från denna klass
/// - Implementera LoadDataAsync() för att hämta data
/// - UI visar först "..." sedan uppdateras automatiskt med rätt data
/// 
/// EXEMPEL: SickLeaveHoursConverter hämtar sjukdata från databas asynkront
/// </summary>
/// <typeparam name="TInput">Input-typ (t.ex. WorkShift)</typeparam>
/// <typeparam name="TOutput">Output-typ (t.ex. string)</typeparam>
public abstract class AsyncValueConverter<TInput, TOutput> : IValueConverter
{
    private readonly ConcurrentDictionary<string, TOutput> _cache = new();
    private readonly ConcurrentDictionary<string, Task<TOutput>> _pendingTasks = new();

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is not TInput input)
            return GetDefaultValue();

        var cacheKey = GetCacheKey(input, parameter);

        // 1. Kolla cache först
        if (_cache.TryGetValue(cacheKey, out var cachedResult))
            return cachedResult;

        // 2. Kolla om vi redan laddar denna data
        if (_pendingTasks.TryGetValue(cacheKey, out var pendingTask))
        {
            // Returnera default medan vi väntar
            return GetDefaultValue();
        }

        // 3. Starta async-laddning
        var task = LoadDataAsync(input, parameter, culture);
        _pendingTasks[cacheKey] = task;

        // 4. Hantera resultatet när det kommer
        _ = Task.Run(async () =>
        {
            try
            {
                var result = await task;
                _cache[cacheKey] = result;
                _pendingTasks.TryRemove(cacheKey, out _);

                // Trigga UI-uppdatering
                await MainThread.InvokeOnMainThreadAsync(() =>
                {
                    OnDataLoaded?.Invoke(cacheKey, result);
                });
            }
            catch (Exception ex)
            {
                _pendingTasks.TryRemove(cacheKey, out _);
                System.Diagnostics.Debug.WriteLine($"❌ AsyncConverter fel: {ex.Message}");
            }
        });

        return GetDefaultValue();
    }

    // Abstract metoder som subklasser implementerar
    protected abstract Task<TOutput> LoadDataAsync(TInput input, object parameter, CultureInfo culture);
    protected abstract string GetCacheKey(TInput input, object parameter);
    protected abstract TOutput GetDefaultValue();

    // Event för UI-uppdatering
    public static event Action<string, object> OnDataLoaded;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
