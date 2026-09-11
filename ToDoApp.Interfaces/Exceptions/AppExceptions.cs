namespace ToDoApp.Interfaces.Exceptions;

public abstract class AppException : Exception
{
    protected AppException(string message) : base(message)
    {
    }
}

public class NotFoundException : AppException
{
    public NotFoundException(string message) : base(message)
    {
    }

    public static NotFoundException For(string entity, Guid id) =>
        new($"{entity} '{id}' was not found.");
}

public class BusinessRuleException : AppException
{
    public BusinessRuleException(string message) : base(message)
    {
    }
}

public class ConflictException : AppException
{
    public ConflictException(string message) : base(message)
    {
    }
}

public class AuthenticationFailedException : AppException
{
    public AuthenticationFailedException(string message) : base(message)
    {
    }
}
