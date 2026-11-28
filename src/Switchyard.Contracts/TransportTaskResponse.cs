namespace Switchyard.Contracts;

public sealed record TransportTaskResponse(
    long Id, string Pickup, string Destination, string Status, string? RobotId, long WarehouseId = 1)
{
    public const string Pending = "pending";
    public const string Assigned = "assigned";
    public const string Completed = "completed";
}
