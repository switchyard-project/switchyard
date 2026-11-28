namespace Switchyard.Contracts;

public sealed record ZoneResponse(long Id, string Name, string Kind, int StartRow, int StartColumn, int Rows, int Columns);
