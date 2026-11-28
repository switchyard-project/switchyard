namespace Switchyard.Contracts;

public sealed record RobotResponse(string Id, DateTimeOffset RegisteredAt, long WarehouseId = 1);
