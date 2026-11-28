using Microsoft.AspNetCore.Mvc;
using Switchyard.Api.Data;
using Switchyard.Contracts;

namespace Switchyard.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TransportTasksController : ControllerBase
{
    private readonly WarehouseRepository _warehouses;
    private readonly TransportTaskRepository _tasks;
    private readonly RobotRepository _robots;

    public TransportTasksController(TransportTaskRepository tasks, RobotRepository robots, WarehouseRepository warehouses)
    {
        _tasks = tasks;
        _robots = robots;
        _warehouses = warehouses;
    }

    [HttpPost]
    public ActionResult<TransportTaskResponse> Create(CreateTransportTaskRequest request)
    {
        if (_warehouses.GetById(request.WarehouseId) is null) return NotFound();
        var task = _tasks.Create(request);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpGet("{id:long}")]
    public ActionResult<TransportTaskResponse> GetById(long id)
    {
        var task = _tasks.GetById(id);
        return task is null
            ? Problem(statusCode: StatusCodes.Status404NotFound, title: "No se ha encontrado la tarea.")
            : Ok(task);
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<TransportTaskResponse>> GetAll(long warehouseId = 1)
    {
        if (_warehouses.GetById(warehouseId) is null) return NotFound();
        return Ok(_tasks.GetAll(warehouseId));
    }

    [HttpPost("/api/robots/{robotId}/tasks/claim")]
    public ActionResult<TransportTaskResponse> Claim(string robotId)
    {
        if (_robots.GetById(robotId) is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "No se ha encontrado el robot.");
        }

        var task = _tasks.Claim(robotId);
        return task is null ? NoContent() : Ok(task);
    }

    [HttpPut("{id:long}/assignment")]
    public ActionResult<TransportTaskResponse> Assign(long id, AssignTransportTaskRequest request)
    {
        if (_tasks.GetById(id) is null || _robots.GetById(request.RobotId) is null)
            return Problem(statusCode: 404, title: "No se ha encontrado la tarea o el robot.");

        var task = _tasks.Assign(id, request.RobotId);
        return task is not null ? Ok(task) : Problem(statusCode: 409,
            title: "No se puede asignar la tarea.",
            detail: "Deben pertenecer al mismo almacén, la tarea debe estar pendiente y el robot libre. Una asignación idéntica se puede repetir mientras siga activa.");
    }

    [HttpPut("/api/robots/{robotId}/tasks/{id:long}/completion")]
    public ActionResult<TransportTaskResponse> Complete(string robotId, long id)
    {
        var task = _tasks.Complete(id, robotId);
        if (task is not null)
        {
            return Ok(task);
        }

        if (_tasks.GetById(id) is null)
        {
            return Problem(statusCode: StatusCodes.Status404NotFound, title: "No se ha encontrado la tarea.");
        }

        return Problem(
            statusCode: StatusCodes.Status409Conflict,
            title: "Solo el robot asignado puede completar la tarea.");
    }
}
