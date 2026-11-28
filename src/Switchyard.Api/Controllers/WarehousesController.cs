using Microsoft.AspNetCore.Mvc;
using Switchyard.Api.Data;
using Switchyard.Contracts;

namespace Switchyard.Api.Controllers;

[ApiController]
[Route("api/warehouses")]
public sealed class WarehousesController(WarehouseRepository warehouses) : ControllerBase
{
    [HttpGet]
    public ActionResult<IReadOnlyList<WarehouseResponse>> GetAll() => Ok(warehouses.GetAll());

    [HttpGet("{id:long}")]
    public ActionResult<WarehouseResponse> GetById(long id) =>
        warehouses.GetById(id) is { } warehouse ? Ok(warehouse) : NotFound();

    [HttpPost]
    public ActionResult<WarehouseResponse> Create(CreateWarehouseRequest request)
    {
        var warehouse = warehouses.Create(request);
        return warehouse is null
            ? Problem(statusCode: 409, title: "Ya existe un almacén con ese nombre.")
            : CreatedAtAction(nameof(GetById), new { id = warehouse.Id }, warehouse);
    }

    [HttpGet("{id:long}/zones")]
    public ActionResult<IReadOnlyList<ZoneResponse>> GetZones(long id) =>
        warehouses.GetById(id) is null ? NotFound() : Ok(warehouses.GetZones(id));

    [HttpPost("{id:long}/zones")]
    public ActionResult<ZoneResponse> CreateZone(long id, CreateZoneRequest request)
    {
        var warehouse = warehouses.GetById(id);
        if (warehouse is null) return NotFound();
        if (request.StartRow + request.Rows - 1 > warehouse.Rows
            || request.StartColumn + request.Columns - 1 > warehouse.Columns)
            return Problem(statusCode: 400, title: "La zona debe quedar dentro del almacén.");

        var zone = warehouses.CreateZone(id, request);
        return zone is null
            ? Problem(statusCode: 409, title: "El nombre ya existe o la zona se solapa con otra.")
            : CreatedAtAction(nameof(GetZones), new { id }, zone);
    }
}
