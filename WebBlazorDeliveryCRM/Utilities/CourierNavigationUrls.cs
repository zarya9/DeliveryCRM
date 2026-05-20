using System.Globalization;

namespace WebBlazorDeliveryCRM.Utilities;

public static class CourierNavigationUrls
{
    public static string? YandexRoute(double fromLat, double fromLon, double toLat, double toLon)
    {
        if (!IsValidCoord(fromLat, fromLon) || !IsValidCoord(toLat, toLon))
            return null;
        return string.Create(CultureInfo.InvariantCulture,
            $"https://yandex.ru/maps/?rtext={fromLat},{fromLon}~{toLat},{toLon}&rtt=auto");
    }

    public static string? GoogleRoute(double fromLat, double fromLon, double toLat, double toLon)
    {
        if (!IsValidCoord(fromLat, fromLon) || !IsValidCoord(toLat, toLon))
            return null;
        return string.Create(CultureInfo.InvariantCulture,
            $"https://www.google.com/maps/dir/?api=1&origin={fromLat},{fromLon}&destination={toLat},{toLon}&travelmode=driving");
    }

    public static string? TwoGisRoute(double fromLat, double fromLon, double toLat, double toLon)
    {
        if (!IsValidCoord(fromLat, fromLon) || !IsValidCoord(toLat, toLon))
            return null;
        return string.Create(CultureInfo.InvariantCulture,
            $"https://2gis.ru/routeSearch/rsType/car/to/{toLon},{toLat}/from/{fromLon},{fromLat}");
    }

    public static string? PointOnMap(double lat, double lon)
    {
        if (!IsValidCoord(lat, lon))
            return null;
        return string.Create(CultureInfo.InvariantCulture,
            $"https://yandex.ru/maps/?pt={lon},{lat}&z=17&l=map");
    }

    private static bool IsValidCoord(double lat, double lon) =>
        Math.Abs(lat) >= 0.001 || Math.Abs(lon) >= 0.001;
}
