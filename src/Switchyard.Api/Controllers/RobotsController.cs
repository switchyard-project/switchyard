using Microsoft.AspNetCore.Mvc;
using Switchyard.Api.Data;
using Switchyard.Contracts;

namespace Switchyard.Api.Controllers;

[ApiController]
[Route("api/robots")]
public sealed class RobotsController : ControllerBase
{
    private readonly RobotRepository _robots;

    public RobotsController(RobotRepository robots)
    {
        _robots = robots;
    }

    [HttpPost]
    public ActionResult<RobotResponse> Register(RegisterRobotRequest request)
    {
        var robot = new RobotResponse(request.Id, DateTimeOffset.UtcNow);
        if (!_robots.TryAdd(robot))
        {
            return Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "El robot ya está registrado.",
                detail: $"Ya existe un robot con el identificador '{request.Id}'.");
        }

        return CreatedAtAction(nameof(GetById), new { id = robot.Id }, robot);
    }

    [HttpGet("{id}")]
    public ActionResult<RobotResponse> GetById(string id)
    {
        var robot = _robots.GetById(id);
        if (robot is null)
        {
            return Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "No se ha encontrado el robot.");
        }

        return Ok(robot);
    }

    [HttpGet]
    public ActionResult<IReadOnlyList<RobotResponse>> GetAll()
    {
        return Ok(_robots.GetAll());
    }
}
