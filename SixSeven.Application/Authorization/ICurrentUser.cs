namespace SixSeven.Application.Authorization;

public interface ICurrentUser
{
    Guid? UserId { get; }
}