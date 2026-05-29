namespace LiteBase;

public interface ILiteBaseAccess
{
    Task<bool> IsAuthorizedAsync();
}
