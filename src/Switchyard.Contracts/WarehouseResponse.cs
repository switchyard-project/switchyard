namespace Switchyard.Contracts;

public sealed record WarehouseResponse(long Id, string Name, int Rows, int Columns, int RobotCount);
