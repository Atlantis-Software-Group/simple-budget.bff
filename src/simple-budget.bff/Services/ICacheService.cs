namespace simple_budget.bff;

public interface ICacheService
{
    public T SetEntry<T>(string key, T item); 
    public T? GetEntry<T>(string key);
}
