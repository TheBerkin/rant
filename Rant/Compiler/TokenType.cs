namespace Rant.Compiler
{
    internal enum TokenType
    {
        /// <summary>
        /// Regular text with no special function.
        /// </summary>
        Text,
        /// <summary>
        /// A format string used to output a reserved or random character.
        /// Used by: Plaintext, arguments
        /// </summary>
        EscapeSequence,
        /// <summary>
        /// [
        /// <para>
        /// Used by: Tags (opening)
        /// </para>
        /// </summary>
        LeftSquare,
        /// <summary>
        /// ]
        /// <para>
        /// Used by: Tags (closure)
        /// </para>
        /// </summary>
        RightSquare,
        /// <summary>
        /// {
        /// <para>
        /// Used by: Blocks (opening)
        /// </para>
        /// </summary>
        LeftCurly,
        /// <summary>
        /// }
        /// <para>
        /// Used by: Blocks (closure)
        /// </para>
        /// </summary>
        RightCurly,
        /// <summary>
        /// &lt;
        /// <para>
        /// Used by: Queries (opening)
        /// </para>
        /// </summary>
        LeftAngle,
        /// <summary>
        /// &gt;
        /// <para>
        /// Used by: Queries (closure)
        /// </para>
        /// </summary>
        RightAngle,
        /// <summary>
        /// (
        /// <para>
        /// Used by: Arithmetic (opening)
        /// </para>
        /// </summary>
        LeftParen,
        /// <summary>
        /// )
        /// <para>
        /// Used by: Arithmetic (closure)
        /// </para>
        /// </summary>
        RightParen,
        /// <summary>
        /// |
        /// <para>
        /// Used by: Blocks (item separator)
        /// </para>
        /// </summary>
        Pipe,
        /// <summary>
        /// :
        /// <para>
        /// Used by: Tags (follows name)
        /// </para>
        /// </summary>
        Colon,
        /// <summary>
        /// ;
        /// <para>
        /// Used by: Tags (argument separator)
        /// </para>
        /// </summary>
        Semicolon,
        /// <summary>
        /// ::
        /// <para>
        /// Used by: Queries (carrier operator)
        /// </para>
        /// </summary>
        DoubleColon,
        /// <summary>
        /// @
        /// <para>
        /// Used by: Tags (constant arg notation), Arithmetic (statement modifier)
        /// </para>
        /// </summary>
        At,
        /// <summary>
        /// ?
        /// <para>
        /// Used by: Tags (metapatterns), Queries (whitelist regex)
        /// </para>
        /// </summary>
        Question,
        /// <summary>
        /// /
        /// <para>
        /// Used by: Queries (regex filters)
        /// </para>
        /// </summary>
        ForwardSlash,
        /// <summary>
        /// !
        /// <para>
        /// Used by: Queries ('not' class constraint modifier)
        /// </para>
        /// </summary>
        Exclamation,
        /// <summary>
        /// $
        /// <para>
        /// Used by: Queries ('only' modifier)
        /// </para>
        /// </summary>
        Dollar,
        /// <summary>
        /// -
        /// <para>
        /// Used by: Queries (class constraint)
        /// </para>
        /// </summary>
        Hyphen,
        /// <summary>
        /// ,
        /// <para>
        /// Used by: Queries (subtype prefix)
        /// </para>
        /// </summary>
        Comma,
        /// <summary>
        /// ?!
        /// <para>
        /// Used by: Queries (blacklist regex)
        /// </para>
        /// </summary>
        Without,
        /// <summary>
        /// Javascript-style regular expression.
        /// <para>
        /// Used by: Queries (blacklist/whitelist)
        /// </para>
        /// </summary>
        Regex,
        /// <summary>
        /// Comments, whitespace, etc.
        /// </summary>
        Ignore,
        /// <summary>
        /// " ... "
        /// Used by: Tags
        /// </summary>
        ConstantLiteral,
        /// <summary>
        /// Used by: Tags
        /// </summary>
        Whitespace,
        /// <summary>
        /// =
        /// <para>
        /// Used by: Carriers
        /// </para> 
        /// </summary>
        Equal,
        /// <summary>
        /// &
        /// <para>
        /// Used by: Carriers
        /// </para>
        /// </summary>
        Ampersand,
        /// <summary>
        /// End of file.
        /// </summary>
        EOF
    }
}