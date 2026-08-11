using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace Barakah.Persistence;

/// <summary>
/// Sets the Postgres search_path for the current connection so tenant-schema-scoped queries
/// (products/inventory/orders/etc., created by scripts/init-db.sql's create_tenant_schema())
/// resolve against tenant_{tenantId} without every DbContext needing per-table schema mapping.
/// Call once after the DbContext's connection is opened, before any tenant-scoped query runs.
/// </summary>
public static class TenantSchemaExtensions
{
    public static async Task UseTenantSchemaAsync(this DbContext dbContext, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var schemaName = $"tenant_{tenantId:N}";
        var connection = (NpgsqlConnection)dbContext.Database.GetDbConnection();

        if (connection.State != System.Data.ConnectionState.Open)
        {
            await connection.OpenAsync(cancellationToken);
        }

        await using var command = connection.CreateCommand();
        command.CommandText = $"SET search_path TO {schemaName}, public";
        await command.ExecuteNonQueryAsync(cancellationToken);
    }
}

public interface IRepository<TEntity, in TId>
    where TEntity : class
{
    Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default);
    Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default);
    Task AddAsync(TEntity entity, CancellationToken cancellationToken = default);
    void Remove(TEntity entity);
}

public class Repository<TEntity, TId>(DbContext dbContext) : IRepository<TEntity, TId>
    where TEntity : class
{
    protected readonly DbContext DbContext = dbContext;
    protected readonly DbSet<TEntity> Set = dbContext.Set<TEntity>();

    public async Task<TEntity?> GetByIdAsync(TId id, CancellationToken cancellationToken = default) =>
        await Set.FindAsync([id], cancellationToken);

    public async Task<List<TEntity>> ListAsync(CancellationToken cancellationToken = default) =>
        await Set.ToListAsync(cancellationToken);

    public async Task AddAsync(TEntity entity, CancellationToken cancellationToken = default) =>
        await Set.AddAsync(entity, cancellationToken);

    public void Remove(TEntity entity) => Set.Remove(entity);
}
