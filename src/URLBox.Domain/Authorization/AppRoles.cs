using System;
using System.Collections.Generic;

namespace URLBox.Domain.Authorization;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Viewer = "Viewer";

    public static readonly IReadOnlyCollection<string> SystemRoles = new[]
    {
        Administrator,
        Manager,
        Viewer
    };

    public static bool IsSystemRole(string roleName)
    {
        return !string.IsNullOrWhiteSpace(roleName)
            && SystemRoleSet.Contains(roleName);
    }

    private static readonly HashSet<string> SystemRoleSet = new(
        SystemRoles,
        StringComparer.OrdinalIgnoreCase);
}
