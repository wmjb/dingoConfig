namespace contracts.Adapters;

public class AdapterStatusResponse
{
    public bool IsConnected { get; set; }
    public string AdapterName { get; set; } = string.Empty;
}