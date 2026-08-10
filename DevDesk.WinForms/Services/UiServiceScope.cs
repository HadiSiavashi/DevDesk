using Microsoft.Extensions.DependencyInjection;

namespace DevDesk.WinForms.Services;

public static class UiServiceScope
{
    public static IServiceScope CreateScope(IServiceScopeFactory factory) => factory.CreateScope();
}
