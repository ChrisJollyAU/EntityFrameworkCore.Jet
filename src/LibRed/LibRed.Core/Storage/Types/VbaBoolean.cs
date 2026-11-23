namespace LibRed.Storage.Types;

/// <summary>
/// VBA's Boolean (VT_BOOL), as IsNull, IsEmpty, IsError, IsMissing, IsObject, IsNumeric, IsDate, CBool and Like give it
/// (verified vs ACE, in queries and calculated columns alike). Where a Variant is written as text — <c>&amp;</c>,
/// <c>Str</c>, <c>CStr</c>, <c>Format</c>, <c>Len</c>, <c>UCase</c>, <c>Left</c>, <c>Mid</c>, <c>InStr</c>, <c>StrComp</c>,
/// the byte functions, a Like operand — it is <c>"True"</c> or <c>"False"</c>. A function that takes a String
/// (<c>Asc</c>, <c>Val</c>, <c>Replace</c>, <c>StrReverse</c>, <c>InStrRev</c>), a comparison with text and a Text column
/// it is stored in take it as <c>"-1"</c> or <c>"0"</c>, and everything else as the -1 or 0 a comparison gives. A
/// comparison, <c>Not</c>, <c>And</c> and <c>Or</c> give an Integer, not one of these, and so does a Yes/No column;
/// a value is one only while an expression uses it, so a query hands out a plain Boolean.
/// </summary>
internal sealed class VbaBoolean
{
    public static readonly VbaBoolean True = new(true), False = new(false);

    private VbaBoolean(bool value) => Value = value;

    public bool Value { get; }

    public static VbaBoolean Of(bool value) => value ? True : False;

    /// <summary>The value with a VBA Boolean taken as the plain Boolean it is.</summary>
    public static object? Plain(object? value) => value is VbaBoolean b ? b.Value : value;
}
