using Microsoft.AspNetCore.Http;

namespace FSH.Modules.Identity.Services;

/// <summary>
/// Parses User-Agent Client Hints (modern standard) with fallback to User-Agent string.
/// Client Hints provide more accurate device information and are privacy-focused.
/// https://developer.mozilla.org/en-US/docs/Web/HTTP/Client_hints
/// </summary>
public sealed class ClientHintsParser
{
    public static ClientInfo Parse(IHeaderDictionary headers)
    {
        ArgumentNullException.ThrowIfNull(headers);

        // Try Client Hints first (modern browsers)
        if (TryParseFromClientHints(headers, out var clientInfo))
        {
            return clientInfo;
        }

        // Fallback to User-Agent parsing
        return ParseFromUserAgent(headers);
    }

    private static bool TryParseFromClientHints(IHeaderDictionary headers, out ClientInfo clientInfo)
    {
        clientInfo = new ClientInfo();

        // Try forwarded headers first (from BFF), then direct headers
        // Sec-CH-UA-Platform: Operating system
        // Examples: "Windows", "macOS", "Linux", "Android", "iOS"
        var platform = GetHeaderValue(headers, "X-Forwarded-Sec-CH-UA-Platform") 
                      ?? GetHeaderValue(headers, "Sec-CH-UA-Platform");
        if (!string.IsNullOrEmpty(platform))
        {
            clientInfo.OperatingSystem = CleanQuotedString(platform);
            clientInfo.DeviceType = DetermineDeviceTypeFromPlatform(clientInfo.OperatingSystem);
        }

        // Sec-CH-UA-Platform-Version: OS version
        var platformVersion = GetHeaderValue(headers, "X-Forwarded-Sec-CH-UA-Platform-Version")
                            ?? GetHeaderValue(headers, "Sec-CH-UA-Platform-Version");
        if (!string.IsNullOrEmpty(platformVersion))
        {
            var cleanVersion = CleanQuotedString(platformVersion);
            
            // For Windows, Client Hints provides versions like "10.0.0", "15.0.0" (Win 11)
            // We need to map these to user-friendly names
            if (string.Equals(clientInfo.OperatingSystem, "Windows", StringComparison.OrdinalIgnoreCase))
            {
                clientInfo.OsVersion = MapWindowsClientHintsVersion(cleanVersion);
            }
            else
            {
                clientInfo.OsVersion = cleanVersion;
            }
        }

        // Sec-CH-UA: Browser information
        // Example: "Chromium";v="110", "Not A(Brand";v="24", "Google Chrome";v="110"
        var secChUa = GetHeaderValue(headers, "X-Forwarded-Sec-CH-UA")
                    ?? GetHeaderValue(headers, "Sec-CH-UA");
        if (!string.IsNullOrEmpty(secChUa))
        {
            ParseBrowserFromSecChUa(secChUa, clientInfo);
        }

        // Sec-CH-UA-Mobile: "?1" for mobile, "?0" for desktop
        var mobile = GetHeaderValue(headers, "X-Forwarded-Sec-CH-UA-Mobile")
                   ?? GetHeaderValue(headers, "Sec-CH-UA-Mobile");
        if (!string.IsNullOrEmpty(mobile))
        {
            if (mobile == "?1")
            {
                clientInfo.DeviceType = "Mobile";
            }
            else if (mobile == "?0" && string.IsNullOrEmpty(clientInfo.DeviceType))
            {
                clientInfo.DeviceType = "Desktop";
            }
        }

        // Consider we have Client Hints if we got at least platform or browser info
        var hasClientHints = !string.IsNullOrEmpty(clientInfo.OperatingSystem) || 
                            !string.IsNullOrEmpty(clientInfo.Browser);

        return hasClientHints;
    }

    private static ClientInfo ParseFromUserAgent(IHeaderDictionary headers)
    {
        // Try forwarded User-Agent first, then direct
        var userAgent = GetHeaderValue(headers, "X-Forwarded-User-Agent")
                      ?? GetHeaderValue(headers, "User-Agent");
                      
        if (string.IsNullOrEmpty(userAgent))
        {
            return new ClientInfo
            {
                Browser = "Unknown",
                OperatingSystem = "Unknown",
                DeviceType = "Desktop"
            };
        }

        var info = new ClientInfo();
        var ua = userAgent.ToLowerInvariant();

        // Detect Browser
        if (ua.Contains("edg/"))
        {
            info.Browser = "Edge";
            info.BrowserVersion = ExtractVersion(userAgent, "Edg/");
        }
        else if (ua.Contains("chrome/") && !ua.Contains("edg"))
        {
            info.Browser = "Chrome";
            info.BrowserVersion = ExtractVersion(userAgent, "Chrome/");
        }
        else if (ua.Contains("firefox/"))
        {
            info.Browser = "Firefox";
            info.BrowserVersion = ExtractVersion(userAgent, "Firefox/");
        }
        else if (ua.Contains("safari/") && !ua.Contains("chrome"))
        {
            info.Browser = "Safari";
            info.BrowserVersion = ExtractVersion(userAgent, "Version/");
        }
        else
        {
            info.Browser = "Unknown";
        }

        // Detect Operating System
        if (ua.Contains("windows nt"))
        {
            info.OperatingSystem = "Windows";
            info.OsVersion = ExtractWindowsVersionFromUserAgent(ua);
        }
        else if (ua.Contains("mac os x"))
        {
            info.OperatingSystem = "macOS";
            info.OsVersion = ExtractVersion(userAgent, "Mac OS X ");
        }
        else if (ua.Contains("android"))
        {
            info.OperatingSystem = "Android";
            info.OsVersion = ExtractVersion(userAgent, "Android ");
        }
        else if (ua.Contains("iphone") || ua.Contains("ipad"))
        {
            info.OperatingSystem = "iOS";
            info.OsVersion = ExtractVersion(userAgent, "OS ");
        }
        else if (ua.Contains("linux"))
        {
            info.OperatingSystem = "Linux";
        }
        else
        {
            info.OperatingSystem = "Unknown";
        }

        // Detect Device Type
        if (ua.Contains("mobile") || ua.Contains("android") || ua.Contains("iphone"))
        {
            info.DeviceType = "Mobile";
        }
        else if (ua.Contains("tablet") || ua.Contains("ipad"))
        {
            info.DeviceType = "Tablet";
        }
        else
        {
            info.DeviceType = "Desktop";
        }

        return info;
    }

    private static void ParseBrowserFromSecChUa(string secChUa, ClientInfo info)
    {
        // Format: "Chromium";v="110", "Not A(Brand";v="24", "Google Chrome";v="110"
        var brands = secChUa.Split(',');
        
        foreach (var brand in brands)
        {
            var parts = brand.Split(';');
            if (parts.Length < 2) continue;

            var brandName = CleanQuotedString(parts[0].Trim());
            var versionPart = parts[1].Trim();

            // Skip placeholder brands
            if (brandName.Contains("Not") || brandName.Contains("Brand"))
            {
                continue;
            }

            // Map Chromium-based browsers
            if (brandName.Contains("Google Chrome") || brandName == "Chrome")
            {
                info.Browser = "Chrome";
            }
            else if (brandName.Contains("Microsoft Edge") || brandName == "Edge")
            {
                info.Browser = "Edge";
            }
            else if (brandName == "Chromium")
            {
                info.Browser ??= "Chrome"; // Default to Chrome for Chromium
            }

            // Extract version
            if (versionPart.Contains("v="))
            {
                var version = versionPart.Split('=')[1].Trim();
                info.BrowserVersion = CleanQuotedString(version);
            }

            if (!string.IsNullOrEmpty(info.Browser))
            {
                break;
            }
        }

        info.Browser ??= "Unknown";
    }

    private static string DetermineDeviceTypeFromPlatform(string platform)
    {
        return platform.ToLowerInvariant() switch
        {
            "android" or "ios" => "Mobile",
            "windows" or "macos" or "linux" or "chrome os" => "Desktop",
            _ => "Desktop"
        };
    }

    private static string MapWindowsClientHintsVersion(string version)
    {
        // Client Hints Platform-Version for Windows:
        // - "10.0.0" -> Windows 10
        // - "13.0.0" or higher -> Windows 11 (started at 10.0.22000, reported as 13+ in Client Hints)
        // - "15.0.0" -> Windows 11 (some Chrome versions)
        
        if (string.IsNullOrWhiteSpace(version))
        {
            return string.Empty;
        }

        // Parse major version
        var parts = version.Split('.');
        if (parts.Length == 0 || !int.TryParse(parts[0], out var majorVersion))
        {
            return version; // Return as-is if we can't parse
        }

        return majorVersion switch
        {
            >= 13 => "11", // Windows 11
            10 => "10",    // Windows 10
            _ => version   // Older or unknown version
        };
    }

    private static string CleanQuotedString(string value)
    {
        return value.Trim().Trim('"').Trim();
    }

    private static string? GetHeaderValue(IHeaderDictionary headers, string key)
    {
        if (headers.TryGetValue(key, out var values))
        {
            return values.FirstOrDefault();
        }
        return null;
    }

    private static string? ExtractVersion(string userAgent, string prefix)
    {
        var startIndex = userAgent.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (startIndex == -1) return null;

        startIndex += prefix.Length;
        var endIndex = userAgent.IndexOfAny([' ', ';', ')'], startIndex);
        if (endIndex == -1) endIndex = userAgent.Length;

        var version = userAgent[startIndex..endIndex];
        var dotIndex = version.IndexOf('.');
        return dotIndex > 0 ? version[..dotIndex] : version;
    }

    private static string ExtractWindowsVersionFromUserAgent(string ua)
    {
        // Modern User-Agents freeze at "Windows NT 10.0" for both Win 10 and 11
        // We can't distinguish between them from User-Agent alone
        if (ua.Contains("windows nt 10")) return "10";
        if (ua.Contains("windows nt 6.3")) return "8.1";
        if (ua.Contains("windows nt 6.2")) return "8";
        if (ua.Contains("windows nt 6.1")) return "7";
        return string.Empty;
    }
}

public sealed class ClientInfo
{
    public string? Browser { get; set; }
    public string? BrowserVersion { get; set; }
    public string? OperatingSystem { get; set; }
    public string? OsVersion { get; set; }
    public string? DeviceType { get; set; }
}
