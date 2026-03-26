## Release 0.1.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
ZA0001  | ZeroAlloc | Error    | ZeroAllocBase derived class must be partial
ZA0002  | ZeroAlloc | Warning  | ZeroAllocBase derived class should be internal
ZA1001  | ZeroAlloc | Warning  | Unsupported argument type
ZA1002  | ZeroAlloc | Warning  | Potential recursive ZeroAlloc usage in ISpanFormattable
ZA1003  | ZeroAlloc | Info     | Nested ZeroAlloc call detected (uses heap fallback)
ZA1004  | ZeroAlloc | Warning  | Type falls back to ToString() causing allocation
ZA2001  | ZeroAlloc.Parsing | Error | Member type is not parsable
ZA2002  | ZeroAlloc.Parsing | Error | Inconsistent member ordering
ZA2003  | ZeroAlloc.Parsing | Error | Duplicate order value
ZA2004  | ZeroAlloc.Parsing | Error | byte[] requires [BinaryFixedLength]
ZA2005  | ZeroAlloc.Parsing | Error | [BinaryParsable] only allowed on structs
ZA2006  | ZeroAlloc.Parsing | Error | Type must be partial
ZA2007  | ZeroAlloc.Parsing | Error | [BinaryOrder] and [BinaryIgnore] conflict
ZA2008  | ZeroAlloc.Parsing | Error | Type requires byte alignment
ZA2009  | ZeroAlloc.Parsing | Error | String requires length encoding attribute
ZA2010  | ZeroAlloc.Parsing | Error | Invalid PaddingBits value
ZA2011  | ZeroAlloc.Parsing | Error | Length field must come before dependent field
ZA2012  | ZeroAlloc.Parsing | Error | Length field not found
ZA2013  | ZeroAlloc.Parsing | Error | Bytes/Memory requires length encoding

## Release 0.2.0

### New Rules

Rule ID | Category | Severity | Notes
--------|----------|----------|------
ZA3001  | ZeroAlloc.Writing | Error | Member type is not writable
ZA3002  | ZeroAlloc.Writing | Error | Inconsistent member ordering
ZA3003  | ZeroAlloc.Writing | Error | Duplicate order value
ZA3004  | ZeroAlloc.Writing | Error | byte[] requires length specification
ZA3005  | ZeroAlloc.Writing | Error | [BinaryWritable] only allowed on structs
ZA3006  | ZeroAlloc.Writing | Error | Type must be partial
ZA3007  | ZeroAlloc.Writing | Error | [BinaryOrder] and [BinaryIgnore] conflict
ZA3008  | ZeroAlloc.Writing | Error | Type requires byte alignment
ZA3009  | ZeroAlloc.Writing | Error | String requires length encoding
ZA3010  | ZeroAlloc.Writing | Error | Invalid padding bits value
ZA3011  | ZeroAlloc.Writing | Error | Bytes/Memory requires length encoding
ZA3012  | ZeroAlloc.Writing | Error | Fixed length must be greater than zero
ZA3013  | ZeroAlloc.Writing | Error | Conflicting length/encoding attributes
ZA2014  | ZeroAlloc.Parsing | Error | Fixed length must be greater than zero
ZA2015  | ZeroAlloc.Parsing | Error | Conflicting length/encoding attributes