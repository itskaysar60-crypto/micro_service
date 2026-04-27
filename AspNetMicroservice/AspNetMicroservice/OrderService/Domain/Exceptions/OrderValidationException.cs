namespace OrderService.Domain.Exceptions;

/// <summary>
/// Thrown when order data fails validation.
/// </summary>
public class OrderValidationException : Exception
{
    public OrderValidationException(string message) : base(message) { }
}
