# JsonComparer Function Extension Design Document

## Overview

This document outlines the design for extending the JsonComparer utility in the TestSupport library to support function execution alongside the existing variable extraction capabilities.

## Current State

### Existing Functionality
- JsonComparer supports token extraction using `[[TOKEN_NAME]]` syntax
- Provides exact matching and subset matching capabilities
- Returns extracted tokens and detailed mismatch information
- Used primarily in BDD tests for API response validation

### Current Token Types
- **Variable Extraction**: `[[USER_ID]]`, `[[CREATED_DATE]]`
- **URL Substitution**: `{{BASE_URL}}` (used in BDD test URLs)

## Proposed Enhancement

### New Function Syntax
- **Functions**: `{{FUNCTION_NAME()}}` - executed and replaced with generated values
- **Variables**: `{{VARIABLE_NAME}}` - used for substitution (existing behavior)
- **Extraction**: `[[TOKEN_NAME]]` - extracted from actual values (existing behavior)

### Function vs Variable Detection
Functions are distinguished from variables by the presence of parentheses:
- `{{GUID()}}` → Function (execute and replace)
- `{{USER_ID}}` → Variable (substitute from context)
- `[[EXTRACTED_ID]]` → Extraction token (extract from actual JSON)

## Phase 1 Implementation (Current)

### Scope
Implement parameterless functions only:
- `{{GUID()}}` - Generate new GUID
- `{{NOW()}}` - Current local datetime
- `{{UTCNOW()}}` - Current UTC datetime

### Architecture Components

#### 1. Function Interface
```csharp
public interface IJsonFunction
{
    string Execute();
}
```

#### 2. Function Registry
```csharp
public class JsonFunctionRegistry
{
    private readonly Dictionary<string, IJsonFunction> _functions;
    
    public void RegisterFunction(string name, IJsonFunction function);
    public bool TryGetFunction(string name, out IJsonFunction function);
    public string[] GetRegisteredFunctions();
}
```

#### 3. Built-in Functions
- **GuidFunction**: Returns `Guid.NewGuid().ToString()`
- **NowFunction**: Returns `DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss.fffK")`
- **UtcNowFunction**: Returns `DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")`

#### 4. JsonComparer Integration
- Add function registry as constructor dependency
- Process functions before existing comparison logic
- Maintain backward compatibility

### Processing Flow
1. **Function Preprocessing**: Replace `{{FUNCTION()}}` tokens with executed results
2. **Variable Substitution**: Replace `{{VARIABLE}}` tokens with provided values
3. **Comparison**: Perform existing exact/subset matching
4. **Token Extraction**: Extract `[[TOKEN]]` values from actual JSON

### File Structure
```
TestSupport/
├── Functions/
│   ├── IJsonFunction.cs
│   ├── JsonFunctionRegistry.cs
│   └── BuiltInFunctions/
│       ├── GuidFunction.cs
│       ├── NowFunction.cs
│       └── UtcNowFunction.cs
├── JsonComparer.cs (modified)
└── ... (existing files)
```

## Phase 2 Implementation (Future)

### Parameterized Functions
Support functions with parameters:
- `{{RANDOM(min, max)}}` - Random number in range
- `{{DATE(format)}}` - Formatted current date
- `{{SUBSTRING(text, start, length)}}` - String manipulation
- `{{ADD(a, b)}}` - Mathematical operations

### Parameter Types
- **Positional**: `{{RANDOM(1, 100)}}`
- **Named**: `{{DATE(format="yyyy-MM-dd")}}`
- **String Literals**: `{{REPLACE("hello world", " ", "_")}}`
- **Nested Functions**: `{{ADD(RANDOM(1,10), 5)}}`

### Advanced Function Categories

#### Mathematical Functions
- `{{ADD(a, b)}}`, `{{SUBTRACT(a, b)}}`, `{{MULTIPLY(a, b)}}`, `{{DIVIDE(a, b)}}`
- `{{RANDOM(min, max)}}`, `{{ROUND(value, decimals)}}`

#### String Functions
- `{{SUBSTRING(text, start, length)}}`, `{{REPLACE(text, old, new)}}`
- `{{UPPER(text)}}`, `{{LOWER(text)}}`, `{{TRIM(text)}}`
- `{{CONCAT(text1, text2, ...)}}`, `{{LENGTH(text)}}`

#### Date/Time Functions
- `{{DATE(format)}}`, `{{TIME(format)}}`, `{{DATETIME(format)}}`
- `{{ADDDAYS(date, days)}}`, `{{ADDHOURS(date, hours)}}`
- `{{FORMATDATE(date, format)}}`

#### Utility Functions
- `{{ENCODE_BASE64(text)}}`, `{{DECODE_BASE64(text)}}`
- `{{HASH_MD5(text)}}`, `{{HASH_SHA256(text)}}`
- `{{JSON_PATH(json, path)}}` - Extract value from JSON

### Context-Aware Functions
Functions that can access test context:
- `{{PREVIOUS_RESPONSE(path)}}` - Extract from previous API response
- `{{TEST_DATA(key)}}` - Access test data repository
- `{{ENVIRONMENT(variable)}}` - Access environment variables

## Phase 3 Implementation (Future)

### Custom Function Registration
Allow tests to register custom functions:
```csharp
registry.RegisterFunction("CUSTOM_ID", () => GenerateCustomId());
registry.RegisterFunction("TEST_USER", () => CreateTestUser().Id);
```

### Function Composition
Support complex function combinations:
- `{{CONCAT(UPPER(firstName), "_", LOWER(lastName))}}`
- `{{DATE(ADDDAYS(NOW(), 7), "yyyy-MM-dd")}}`

### Conditional Functions
- `{{IF(condition, trueValue, falseValue)}}`
- `{{SWITCH(value, case1, result1, case2, result2, default)}}`

## Implementation Considerations

### Performance
- Function execution happens during preprocessing
- Results are cached within single comparison operation
- Minimal impact on existing comparison performance

### Security
- Built-in functions are safe by design
- Custom function registration requires careful validation
- No file system or network access in built-in functions

### Error Handling
- Invalid function names throw descriptive exceptions
- Function execution errors are wrapped with context
- Malformed syntax provides clear error messages

### Testing Strategy
- Unit tests for each built-in function
- Integration tests with JsonComparer
- BDD tests demonstrating real-world usage
- Performance benchmarks for regression testing

### Backward Compatibility
- Existing `[[TOKEN]]` syntax unchanged
- Existing `{{VARIABLE}}` behavior preserved
- No breaking changes to public API

## Migration Path

### Phase 1 Adoption
1. Update existing BDD tests to use new function syntax where appropriate
2. Replace hardcoded GUIDs with `{{GUID()}}` in test expectations
3. Use `{{NOW()}}` for timestamp validation in API tests

### Documentation Updates
- Update CLAUDE.md with new function syntax
- Add examples to README
- Create function reference documentation

## Success Criteria

### Phase 1
- [ ] All built-in functions implemented and tested
- [ ] JsonComparer integration complete
- [ ] Backward compatibility maintained
- [ ] BDD tests updated to demonstrate usage
- [ ] Performance impact < 5% for existing functionality

### Future Phases
- [ ] Parameter parsing implemented
- [ ] Advanced function library complete
- [ ] Custom function registration working
- [ ] Comprehensive documentation available
- [ ] Real-world usage validated in complex test scenarios

## Conclusion

This enhancement significantly expands the JsonComparer's capabilities while maintaining its core simplicity and reliability. The phased approach ensures stable incremental delivery with clear migration paths for existing code.
