using System.Reflection;

namespace Antelcat.Shared.I18N;

public static class I18NAssemblyProvider
{
    public static Assembly? Assembly { get; private set; } = null;

    public static void SetAssembly(Assembly assembly)
    {
        Assembly = assembly;
    }

    public static void SetAssembly(Type type)
    {
        _ = type ?? throw new ArgumentNullException(nameof(type));
        Assembly = type.Assembly;
    }

    public static void SetAssembly<TLangKeys>() where TLangKeys : class
    {
        SetAssembly(typeof(TLangKeys));
    }
}
