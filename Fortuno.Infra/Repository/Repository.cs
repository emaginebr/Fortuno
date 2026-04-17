using Fortuno.Infra.Context;
using Fortuno.Infra.Interfaces.Repository;
using Microsoft.EntityFrameworkCore;

namespace Fortuno.Infra.Repository;

public abstract class Repository<TModel> : IRepository<TModel> where TModel : class
{
    protected readonly FortunoContext _context;

    protected Repository(FortunoContext context)
    {
        _context = context;
    }

    public virtual async Task<TModel?> GetByIdAsync(long id)
        => await _context.Set<TModel>().FindAsync(id);

    public virtual async Task<List<TModel>> ListAllAsync()
        => await _context.Set<TModel>().AsNoTracking().ToListAsync();

    public virtual async Task<TModel> InsertAsync(TModel entity)
    {
        _context.Set<TModel>().Add(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task<TModel> UpdateAsync(TModel entity)
    {
        _context.Set<TModel>().Update(entity);
        await _context.SaveChangesAsync();
        return entity;
    }

    public virtual async Task DeleteAsync(long id)
    {
        var entity = await _context.Set<TModel>().FindAsync(id);
        if (entity is null) return;
        _context.Set<TModel>().Remove(entity);
        await _context.SaveChangesAsync();
    }

    public virtual IQueryable<TModel> Query() => _context.Set<TModel>().AsQueryable();
}
