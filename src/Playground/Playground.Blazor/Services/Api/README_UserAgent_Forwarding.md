# User-Agent Forwarding in Blazor BFF

## Overview

This handler forwards browser information from the Blazor client to the backend API, enabling accurate session management and device detection.

## Headers Forwarded

### Client Hints (Modern Standard)

- `Sec-CH-UA` ? Browser and version
- `Sec-CH-UA-Mobile` ? Mobile device indicator (?1 or ?0)
- `Sec-CH-UA-Platform` ? Operating system
- `Sec-CH-UA-Platform-Version` ? OS version
- Additional optional hints for detailed device info

### Legacy Support

- `User-Agent` ? Full user-agent string (fallback)
- `X-Forwarded-For` ? Client IP address

## Why Client Hints?

**Client Hints** are the modern, W3C-standardized way to get device information:

? **More Accurate** - Structured data instead of parsing complex strings  
? **Privacy-Focused** - Reduces browser fingerprinting  
? **Future-Proof** - User-Agent strings are being frozen by browsers  
? **Standards-Based** - Clear semantics and browser support

## How It Works

```
Browser ? Blazor BFF ? Backend API
   ?           ?            ?
   ?   Client Hints headers ?
   ??????????????????????????
```

1. **Browser** sends Client Hints headers with each request
2. **BFF** (`ForwardedHeadersHandler`) forwards them to the API with `X-Forwarded-` prefix
3. **API** (`ClientHintsParser`) parses them for session creation

## Implementation

### Forwarding Headers (BFF)

```csharp
// ForwardedHeadersHandler.cs
protected override Task<HttpResponseMessage> SendAsync(...)
{
    // Forward Client Hints
    request.Headers.TryAddWithoutValidation("X-Forwarded-Sec-CH-UA", ...);
    request.Headers.TryAddWithoutValidation("X-Forwarded-Sec-CH-UA-Mobile", ...);
    request.Headers.TryAddWithoutValidation("X-Forwarded-Sec-CH-UA-Platform", ...);
    
    // Fallback to User-Agent
    request.Headers.TryAddWithoutValidation("X-Forwarded-User-Agent", ...);
    
    return base.SendAsync(request, cancellationToken);
}
```

### Parsing Headers (API)

```csharp
// ClientHintsParser.cs - Priority order:
1. X-Forwarded-Sec-CH-UA-* (Client Hints from BFF)
2. Sec-CH-UA-* (Direct Client Hints)
3. X-Forwarded-User-Agent (Legacy from BFF)
4. User-Agent (Direct legacy)
```

## Browser Compatibility

| Browser | Client Hints | Fallback |
|---------|-------------|----------|
| Chrome 89+ | ? Full | ? |
| Edge 89+ | ? Full | ? |
| Opera 75+ | ? Full | ? |
| Firefox | ?? Limited | ? User-Agent |
| Safari | ?? Limited | ? User-Agent |

## Testing

### View Headers in Network Tab

Open DevTools ? Network ? Select request ? Headers:

```http
Sec-CH-UA: "Chromium";v="120", "Google Chrome";v="120"
Sec-CH-UA-Mobile: ?0
Sec-CH-UA-Platform: "Windows"
```

### Request More Detailed Hints

Add to your page (optional):

```html
<meta http-equiv="Accept-CH" content="
    Sec-CH-UA,
    Sec-CH-UA-Mobile,
    Sec-CH-UA-Platform,
    Sec-CH-UA-Platform-Version,
    Sec-CH-UA-Arch,
    Sec-CH-UA-Model
">
```

## Session Management

Sessions now show accurate device information:

- **Browser**: Chrome, Edge, Firefox, Safari
- **OS**: Windows, macOS, Linux, Android, iOS
- **Device Type**: Desktop, Mobile, Tablet

## Migration from UAParser

This implementation **replaces** the old `UAParser` library:

| Old (UAParser) | New (Client Hints) |
|---------------|-------------------|
| Parse User-Agent string | Read structured Client Hints |
| Complex regex patterns | Simple header parsing |
| External dependency | Built-in implementation |
| Less accurate | More accurate |

## References

- [MDN: User-Agent Client Hints](https://developer.mozilla.org/en-US/docs/Web/HTTP/Client_hints)
- [Chrome: User-Agent Reduction](https://chromestatus.com/feature/5704553745874944)
- [W3C Client Hints Infrastructure](https://wicg.github.io/client-hints-infrastructure/)
