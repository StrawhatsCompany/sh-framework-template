using SH.Framework.Library.Cqrs.Implementation;

namespace Business.Caching;

public class CacheProviderResultCode
{
    public const string Category = "CACHEPROVIDER";

    public static ResultCode KeyNotFound => ResultCode.Instance(2000, Category, "Cache key not found");
    public static ResultCode SerializationFailed => ResultCode.Instance(2001, Category, "Failed to serialize / deserialize the cached value");
}
