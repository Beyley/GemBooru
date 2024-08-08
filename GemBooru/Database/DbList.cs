namespace GemBooru.Database;

public class DbList<T>
{
    public IQueryable<T> Items;
    public int Index;
    public int Count;
    public int TotalEntries;
    public int NextEntryIndex;
    
    public DbList(IQueryable<T> queryable, int skip, int take)
    {
        TotalEntries = queryable.Count();
        Items = queryable.Skip(skip).Take(take);
        Index = skip;
        Count = Items.Count();
        NextEntryIndex = skip + Count;
    }
}