namespace Cosmonauta.Models;

public class OperationResult<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public List<string> Logs { get; set; } = new();
    public string? ErrorMessage { get; set; }
    
    public static OperationResult<T> Ok(T data)
    {
        return new OperationResult<T> { Success = true, Data = data };
    }
    
    public static OperationResult<T> Fail(string error)
    {
        return new OperationResult<T> { Success = false, ErrorMessage = error };
    }
    
    public void AddLog(string message)
    {
        Logs.Add(message);
    }
}