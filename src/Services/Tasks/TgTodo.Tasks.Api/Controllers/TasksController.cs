using MediatR;
using Microsoft.AspNetCore.Mvc;
using TgTodo.Contracts.Enums;
using TgTodo.Tasks.Application.Categories;
using TgTodo.Tasks.Application.Tasks;

namespace TgTodo.Tasks.Api.Controllers;

[ApiController]
[Route("api/tasks")]
public class TasksController : ControllerBase
{
    private readonly IMediator _mediator;

    public TasksController(IMediator mediator) => _mediator = mediator;

    private Guid? CurrentUserId =>
        Guid.TryParse(Request.Headers["X-User-Id"], out var id) ? id : null;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<TaskDto>>> GetTasks(
        [FromQuery] Guid? groupId,
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var tasks = await _mediator.Send(new GetTasksQuery(userId, groupId, date), ct);
        return Ok(tasks);
    }

    [HttpGet("month")]
    public async Task<ActionResult<MonthTasksDto>> GetMonthTasks(
        [FromQuery] Guid? groupId,
        [FromQuery] int year,
        [FromQuery] int month,
        CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        if (year < 2000 || year > 2100)
            year = DateTime.UtcNow.Year;
        if (month is < 1 or > 12)
            month = DateTime.UtcNow.Month;
        var result = await _mediator.Send(new GetMonthTasksQuery(userId, groupId, year, month), ct);
        return Ok(result);
    }

    [HttpPost]
    public async Task<ActionResult<TaskDto>> Create([FromBody] CreateTaskRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var task = await _mediator.Send(new CreateTaskCommand(
            userId,
            request.Scope,
            request.Title,
            request.PointsReward,
            request.Recurrence,
            request.Weekday,
            request.DayOfMonth,
            request.IntervalDays,
            request.PersonalVisibility,
            request.CompletionMode,
            request.GroupId,
            request.AssignedToUserId,
            request.CategoryId,
            request.VisibilityGroupId,
            request.StartDate), ct);
        return Ok(task);
    }

    [HttpPost("{taskId:guid}/complete")]
    public async Task<ActionResult<TaskDto>> Complete(
        Guid taskId,
        [FromQuery] DateOnly? date,
        CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var task = await _mediator.Send(new CompleteTaskCommand(userId, taskId, date), ct);
        return Ok(task);
    }
}

[ApiController]
[Route("api/categories")]
public class CategoriesController : ControllerBase
{
    private readonly IMediator _mediator;

    public CategoriesController(IMediator mediator) => _mediator = mediator;

    private Guid? CurrentUserId =>
        Guid.TryParse(Request.Headers["X-User-Id"], out var id) ? id : null;

    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> Get(
        [FromQuery] Guid? groupId,
        CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var categories = await _mediator.Send(new GetCategoriesQuery(userId, groupId), ct);
        return Ok(categories);
    }

    [HttpPost]
    public async Task<ActionResult<CategoryDto>> Create([FromBody] CreateCategoryRequest request, CancellationToken ct)
    {
        if (CurrentUserId is not { } userId) return Unauthorized();
        var category = await _mediator.Send(
            new CreateCategoryCommand(userId, request.Name, request.Emoji, request.GroupId), ct);
        return Ok(category);
    }
}

public record CreateTaskRequest(
    TaskScope Scope,
    string Title,
    int PointsReward,
    RecurrenceType Recurrence,
    int? Weekday,
    int? DayOfMonth,
    int? IntervalDays,
    PersonalTaskVisibility PersonalVisibility,
    CompletionMode CompletionMode,
    Guid? GroupId,
    Guid? AssignedToUserId,
    Guid? CategoryId,
    Guid? VisibilityGroupId,
    DateOnly? StartDate);

public record CreateCategoryRequest(string Name, string? Emoji, Guid? GroupId);
