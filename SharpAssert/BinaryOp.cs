namespace SharpAssert;

/// <summary>Comparison operators supported in assertions.</summary>
public enum BinaryOp
{
    /// <summary>Equality (==)</summary>
    Eq,
    
    /// <summary>Inequality (!=)</summary>
    Ne,
    
    /// <summary>Less than (&lt;)</summary>
    Lt,
    
    /// <summary>Less than or equal (&lt;=)</summary>
    Le,
    
    /// <summary>Greater than (&gt;)</summary>
    Gt,
    
    /// <summary>Greater than or equal (&gt;=)</summary>
    Ge
}