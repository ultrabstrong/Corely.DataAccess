namespace Corely.DataAccess.EntityFramework;

internal static class EFUoWScope
{
    private static readonly AsyncLocal<bool> _isActive = new();
    public static bool IsActive => _isActive.Value;
    public static void Begin() => _isActive.Value = true;
    public static void End() => _isActive.Value = false;
}
