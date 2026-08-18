namespace WBand.Dev.Services;

public sealed class ServiceInfoManager
{
    private readonly List<ServiceInfo> services = [];
    private readonly Lock syncRoot = new();

    public event Action? OnChanged;

    public void Add(ServiceInfo service)
    {
        lock (this.syncRoot)
        {
            this.services.Add(service);
        }
    }

    public IEnumerable<ServiceInfo> Services
    {
        get
        {
            lock (this.syncRoot)
            {
                return [.. this.services];
            }
        }
    }

    public ServiceInfo GetByName(string name)
    {
        lock (this.syncRoot)
        {
            return this.services.First(x =>
                x.ServiceName.Equals(name, StringComparison.CurrentCultureIgnoreCase)
            );
        }
    }

    internal void Update(string serviceName, ServiceInfo serviceInfo)
    {
        lock (this.syncRoot)
        {
            this.services.Remove(this.services.First(x => x.ServiceName == serviceName));
            this.services.Add(serviceInfo);
        }

        this.OnChanged?.Invoke();
    }
}
