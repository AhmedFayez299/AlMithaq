namespace AlMithaq.Core.Logic;

public static class QiblaMath
{
    public static double Bearing(double latitude, double longitude)
    {
        if (!double.IsFinite(latitude) || !double.IsFinite(longitude) || Math.Abs(latitude) > 90 || Math.Abs(longitude) > 180)
            throw new ArgumentOutOfRangeException(nameof(latitude));
        const double kaabaLat = 21.422487, kaabaLon = 39.826206;
        double Rad(double n) => n * Math.PI / 180;
        var phi = Rad(latitude); var target = Rad(kaabaLat); var delta = Rad(kaabaLon - longitude);
        var bearing = Math.Atan2(Math.Sin(delta), Math.Cos(phi) * Math.Tan(target) - Math.Sin(phi) * Math.Cos(delta));
        return Normalize(bearing * 180 / Math.PI);
    }
    public static bool IsAligned(double bearing, double heading, bool hasLocation, bool hasHeading)
        => hasLocation && hasHeading && double.IsFinite(bearing) && double.IsFinite(heading) && Math.Abs(Delta(bearing, heading)) < 5;
    public static double Normalize(double degrees) => (degrees % 360 + 360) % 360;
    public static double Delta(double target, double heading) => (Normalize(target - heading) + 180) % 360 - 180;
}
