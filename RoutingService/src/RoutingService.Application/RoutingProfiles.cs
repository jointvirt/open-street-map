namespace RoutingService.Application;

public static class RoutingProfiles
{
    public const string Driving = "driving";
    public const string Walking = "walking";
    public const string Cycling = "cycling";

    public static readonly string[] All = [Driving, Walking, Cycling];

    public static string ToOsrmEngineProfile(string profile) =>
        profile switch
        {
            Driving => "car",
            Walking => "foot",
            Cycling => "bike",
            _ => throw new ArgumentOutOfRangeException(nameof(profile), profile, "Unsupported routing profile.")
        };
}
