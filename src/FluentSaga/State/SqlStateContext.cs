using Microsoft.EntityFrameworkCore;

namespace FluentSaga.State;

public class SqlStateContext : DbContext
{
    public SqlStateContext(DbContextOptions options)
        : base(options)
    {
    }
    
    public DbSet<SqlSagaState> States { get; set; }
}

public class SqlSagaState
{
    public string SagaId { get; set; }

    public string State { get; set; }
}