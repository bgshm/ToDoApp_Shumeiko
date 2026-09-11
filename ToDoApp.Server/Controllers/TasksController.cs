using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ToDoApp.Interfaces.Dtos;
using ToDoApp.Interfaces.Services;

namespace ToDoApp.Server.Controllers;

[ApiController]
[Authorize]
[Route("api/tasks")]
[Produces("application/json")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly ICurrentUser _currentUser;

    public TasksController(ITaskService taskService, ICurrentUser currentUser)
    {
        _taskService = taskService;
        _currentUser = currentUser;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<TaskDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<TaskDto>>> GetPaged(
        [FromQuery] TaskQuery query,
        CancellationToken cancellationToken)
    {
        var result = await _taskService.GetPagedAsync(_currentUser.UserId, query, cancellationToken);
        return Ok(result);
    }

    [HttpGet("board")]
    [ProducesResponseType(typeof(BoardDto), StatusCodes.Status200OK)]
    public async Task<ActionResult<BoardDto>> GetBoard(
        [FromQuery] BoardQuery query,
        CancellationToken cancellationToken)
    {
        var board = await _taskService.GetBoardAsync(_currentUser.UserId, query, cancellationToken);
        return Ok(board);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        var task = await _taskService.GetByIdAsync(_currentUser.UserId, id, cancellationToken);
        return Ok(task);
    }

    [HttpPost]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<TaskDto>> Create(
        [FromBody] CreateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.CreateAsync(_currentUser.UserId, request, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = task.Id }, task);
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Update(
        Guid id,
        [FromBody] UpdateTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.UpdateAsync(_currentUser.UserId, id, request, cancellationToken);
        return Ok(task);
    }

    [HttpPost("{id:guid}/move")]
    [ProducesResponseType(typeof(TaskDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TaskDto>> Move(
        Guid id,
        [FromBody] MoveTaskRequest request,
        CancellationToken cancellationToken)
    {
        var task = await _taskService.MoveAsync(_currentUser.UserId, id, request, cancellationToken);
        return Ok(task);
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid id, CancellationToken cancellationToken)
    {
        await _taskService.DeleteAsync(_currentUser.UserId, id, cancellationToken);
        return NoContent();
    }
}
