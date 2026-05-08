using System.Linq.Expressions;
using BudgetSprite.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace BudgetSprite.Api.Repositories;

public class Repository<T>(AppDbContext db) where T : class
{
    protected readonly AppDbContext Db = db;
    protected readonly DbSet<T> Set = db.Set<T>();

    public async Task<T?> GetByIdAsync(object id) =>
        await Set.FindAsync(id);

    public async Task<List<T>> GetAllAsync() =>
        await Set.ToListAsync();

    public async Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate) =>
        await Set.Where(predicate).ToListAsync();

    public async Task AddAsync(T entity)
    {
        await Set.AddAsync(entity);
        await Db.SaveChangesAsync();
    }

    public async Task UpdateAsync(T entity)
    {
        Set.Update(entity);
        await Db.SaveChangesAsync();
    }

    public async Task DeleteAsync(T entity)
    {
        Set.Remove(entity);
        await Db.SaveChangesAsync();
    }

    public IQueryable<T> Query() => Set.AsQueryable();
}
