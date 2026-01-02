# Client Hints for Device Detection

## Overview

This module uses **User-Agent Client Hints** (CH-UA) as the primary method for detecting browser, operating system, and device type information. Client Hints are a modern, privacy-focused alternative to User-Agent string parsing.

## Why Client Hints?

### Advantages over User-Agent Parsing:
- **More Accurate**: Browsers provide structured, reliable data instead of a complex string
- **Privacy-Focused**: Reduces fingerprinting surface by requiring explicit opt-in
- **Future-Proof**: User-Agent strings are being phased out/frozen by major browsers
- **Standardized**: W3C standard with clear semantics

### Browser Support:
- ? Chrome/Edge 89+ (full support)
- ? Opera 75+
- ?? Firefox (limited support, falls back to User-Agent)
- ?? Safari (limited support, falls back to User-Agent)

## How It Works

### 1. Client Hints Headers

Modern browsers automatically send these headers:

```http
Sec-CH-UA: "Chromium";v="110", "Google Chrome";v="110"
Sec-CH-UA-Mobile: ?0
Sec-CH-UA-Platform: "Windows"
Sec-CH-UA-Platform-Version: "15.0.0"
```

### 2. BFF Header Forwarding

The Blazor BFF (`ForwardedHeadersHandler`) forwards Client Hints to the API:

```csharp
// Headers forwarded from browser ? BFF ? API
X-Forwarded-Sec-CH-UA
X-Forwarded-Sec-CH-UA-Mobile
X-Forwarded-Sec-CH-UA-Platform
X-Forwarded-Sec-CH-UA-Platform-Version
X-Forwarded-User-Agent // Fallback
```

### 3. Parsing Logic

`ClientHintsParser.cs` uses this priority:

1. **Client Hints** (modern browsers)
2. **User-Agent** (fallback for older browsers)

```csharp
var clientInfo = ClientHintsParser.Parse(headers);
// Returns: Browser, OS, Device Type, Versions
```

## Session Management Integration

Sessions are created with accurate device information:

```csharp
await _sessionService.CreateSessionAsync(
    userId: "user-123",
    refreshTokenHash: "...",
    ipAddress: "192.168.1.1",
    userAgent: "Mozilla/5.0...", // Full UA stored for reference
    expiresAt: DateTime.UtcNow.AddDays(30)
);
```

The parser automatically:
- Detects **Browser**: Chrome, Edge, Firefox, Safari
- Detects **OS**: Windows, macOS, Linux, Android, iOS
- Detects **Device Type**: Desktop, Mobile, Tablet

## Version Detection

### Windows Version Detection

**Client Hints** (Accurate):
- `Platform-Version: "10.0.0"` ? Windows 10
- `Platform-Version: "13.0.0"` ? Windows 11
- `Platform-Version: "15.0.0"` ? Windows 11

**User-Agent** (Limited):
- `Windows NT 10.0` ? Windows 10 (frozen for privacy)
- Cannot distinguish between Windows 10 and 11

> ?? **Note**: Modern browsers freeze User-Agent strings for privacy. Chrome/Edge report "Windows NT 10.0" for both Windows 10 and 11. Only Client Hints provide accurate version detection.

### macOS/iOS Version Detection

- Client Hints: Full version from `Platform-Version`
- User-Agent: Extracted from `Mac OS X 10_15` or `OS 15_0`

### Android Version Detection

- Client Hints: Full version from `Platform-Version`
- User-Agent: Extracted from `Android 13`

## Example Detection Results

### Modern Browser (Chrome with Client Hints)
```http
Sec-CH-UA: "Google Chrome";v="120"
Sec-CH-UA-Platform: "Windows"
Sec-CH-UA-Platform-Version: "15.0.0"
Sec-CH-UA-Mobile: ?0
```
**Result:**
- Browser: Chrome 120
- OS: Windows 11
- Device: Desktop

### Legacy Browser (Firefox with User-Agent)
```http
User-Agent: Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:120.0) Firefox/120.0
```
**Result:**
- Browser: Firefox 120
- OS: Windows 10 (cannot detect 11)
- Device: Desktop

### Mobile Device (Chrome Android)
```http
Sec-CH-UA: "Google Chrome";v="120"
Sec-CH-UA-Platform: "Android"
Sec-CH-UA-Platform-Version: "14.0.0"
Sec-CH-UA-Mobile: ?1
```
**Result:**
- Browser: Chrome 120
- OS: Android 14
- Device: Mobile

## Testing

### Request Client Hints in Your App

To receive more detailed Client Hints, add this to your HTML `<head>`:

```html
<meta http-equiv="Accept-CH" content="Sec-CH-UA, Sec-CH-UA-Mobile, Sec-CH-UA-Platform, Sec-CH-UA-Platform-Version">
```

Or via HTTP header:

```http
Accept-CH: Sec-CH-UA, Sec-CH-UA-Mobile, Sec-CH-UA-Platform, Sec-CH-UA-Platform-Version
```

### Verify Detection

Check the logs when creating a session:

```
Created session {SessionId} for user {UserId} using Client Hints/User-Agent
```

### View in Sessions Page

Navigate to `/sessions` in your Blazor app to see detected information:
- Browser name and version
- Operating system and version
- Device type (Desktop/Mobile/Tablet)

## Backwards Compatibility

The implementation gracefully falls back to User-Agent parsing:
- ? No breaking changes
- ? Works with all browsers
- ? Existing sessions continue working
- ? Automatic detection method selection

## Known Limitations

### User-Agent Fallback Limitations:
1. **Windows 10 vs 11**: Cannot distinguish (both show as "Windows 10")
2. **Version Freezing**: Browsers freeze User-Agent for privacy
3. **Less Accurate**: Complex regex patterns vs structured data

### Solution:
Upgrade to modern browsers that support Client Hints for accurate detection.

## References

- [MDN: User-Agent Client Hints](https://developer.mozilla.org/en-US/docs/Web/HTTP/Client_hints)
- [W3C Spec: Client Hints Infrastructure](https://wicg.github.io/client-hints-infrastructure/)
- [Chrome Platform Status: User-Agent Reduction](https://chromestatus.com/feature/5704553745874944)
- [Detecting Windows 11 with Client Hints](https://learn.microsoft.com/en-us/microsoft-edge/web-platform/how-to-detect-win11)
