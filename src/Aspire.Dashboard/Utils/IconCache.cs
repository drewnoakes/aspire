// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using Microsoft.FluentUI.AspNetCore.Components;

namespace Aspire.Dashboard;

internal static class IconCache
{
    private static readonly ConcurrentDictionary<(string Name, IconSize Size, IconVariant Variant), Icon?> s_iconCache = new();

    public static Icon? GetIcon(string iconName, IconSize size, IconVariant variant = IconVariant.Regular)
    {
        // Icons.GetInstance isn't efficient. Cache icon lookup.
        return s_iconCache.GetOrAdd((iconName, size, variant), static key =>
        {
            try
            {
                return Icons.GetInstance(new IconInfo
                {
                    Name = key.Name,
                    Variant = key.Variant,
                    Size = key.Size
                });
            }
            catch
            {
                // Icon name couldn't be found.
                return null;
            }
        });
    }
}
