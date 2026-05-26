using MediatR;
using TgTodo.BuildingBlocks.Exceptions;
using TgTodo.Tasks.Application.Abstractions;
using TgTodo.Tasks.Application.Tasks;
using TgTodo.Tasks.Domain.Entities;

namespace TgTodo.Tasks.Application.Categories;

public record CreateCategoryCommand(Guid UserId, string Name, string? Emoji, Guid? GroupId) : IRequest<CategoryDto>;
public record GetCategoriesQuery(Guid UserId, Guid? GroupId) : IRequest<IReadOnlyList<CategoryDto>>;

public class CreateCategoryCommandHandler : IRequestHandler<CreateCategoryCommand, CategoryDto>
{
    private readonly ITaskRepository _tasks;
    private readonly IGroupsClient _groups;

    public CreateCategoryCommandHandler(ITaskRepository tasks, IGroupsClient groups)
    {
        _tasks = tasks;
        _groups = groups;
    }

    public async Task<CategoryDto> Handle(CreateCategoryCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new ValidationException("Название категории не может быть пустым.");

        Category category;
        if (request.GroupId.HasValue)
        {
            if (!await _groups.IsMemberAsync(request.GroupId.Value, request.UserId, ct))
                throw new ForbiddenException("Not a group member.");
            category = Category.CreateGroup(request.GroupId.Value, request.Name.Trim(), request.Emoji);
        }
        else
        {
            category = Category.CreatePersonal(request.UserId, request.Name.Trim(), request.Emoji);
        }

        await _tasks.AddCategoryAsync(category, ct);
        await _tasks.SaveChangesAsync(ct);
        return new CategoryDto(category.Id, category.Name, category.Emoji, category.GroupId);
    }
}

public class GetCategoriesQueryHandler : IRequestHandler<GetCategoriesQuery, IReadOnlyList<CategoryDto>>
{
    private readonly ITaskRepository _tasks;

    public GetCategoriesQueryHandler(ITaskRepository tasks) => _tasks = tasks;

    public async Task<IReadOnlyList<CategoryDto>> Handle(GetCategoriesQuery request, CancellationToken ct)
    {
        var categories = await _tasks.GetCategoriesAsync(request.UserId, request.GroupId, ct);
        return categories.Select(c => new CategoryDto(c.Id, c.Name, c.Emoji, c.GroupId)).ToList();
    }
}
