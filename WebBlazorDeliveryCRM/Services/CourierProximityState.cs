using WebBlazorDeliveryCRM.Models;

namespace WebBlazorDeliveryCRM.Services;

public sealed class CourierProximityState
{
    public event Action? Changed;

    public NearbyDeliveryStopDto? PrimaryNearby { get; private set; }

    public void SetNearby(IReadOnlyList<NearbyDeliveryStopDto>? stops)
    {
        PrimaryNearby = stops is { Count: > 0 }
            ? stops.OrderBy(s => s.DistanceMeters).ThenBy(s => s.OrderNumber).First()
            : null;
        Changed?.Invoke();
    }

    public void Clear()
    {
        if (PrimaryNearby == null)
            return;
        PrimaryNearby = null;
        Changed?.Invoke();
    }
}
