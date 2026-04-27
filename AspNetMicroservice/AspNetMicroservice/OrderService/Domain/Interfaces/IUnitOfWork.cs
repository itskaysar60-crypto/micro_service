namespace OrderService.Domain.Interfaces;

/// <summary>
/// Abstracts database transaction lifecycle so the Application layer
/// never references EF Core directly.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    Task BeginTransactionAsync(CancellationToken ct = default);
    Task CommitAsync(CancellationToken ct = default);
    Task RollbackAsync(CancellationToken ct = default);
}
