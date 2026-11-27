using Microsoft.AspNetCore.Mvc;
using Switchyard.Api.Data;
using Switchyard.Contracts;

namespace Switchyard.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public sealed class TransportTasksController : ControllerBase
{
    private readonly TransportTaskRepository _tasks;
    private readonly RobotRepository _robots;

    public TransportTasksController(TransportTaskRepository tasks, RobotRepository robots)
    {
        _tasks = tasks;
        _robots = robots;
    }

    [HttpPost]
    public ActionResult<TransportTaskResponse> Create(CreateTransportTaskRequest request)
    {
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
