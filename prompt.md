- Do not add xml doc comments for internal apis
  - Do not add brackets for simple one-line if statements.
    ```csharp
      // BAD
      if (result)
      {
          return string.Empty;
      }
    ```
  
    ```csharp
      // GOOD
      if (result)
          return string.Empty;
    ```
    - Do not add default `private` modifier
    - NEVER use underscores for field names (eg, `_evaluatedValues` is bad, should be just `evaluatedValues`)
    - Always write xml doc comments from user's POV. Example of poorly written comment below:
      ```csharp
      /// <summary>Entry point users call in tests. Rewriter replaces this call.</summary>
      public static void Assert(
      ```
    - NEVER use Arrange/Act/Assert comments in test, instead separate parts with empty lines
    - AVOID testing multiple independent behaviors in one test (e.g., bundling). 
      The below test should be split into multiple independent tests (one for ==, one for !=, etc)
      ```csharp
        [Test]
        public void Should_handle_all_comparison_operators()
        {
            // Test ==
            int x = 5;
            int y = 10;
            Expression<Func<bool>> exprEq = () => x == y;
            var actionEq = () => SharpInternal.Assert(exprEq, "x == y", "TestFile.cs", 1);
            actionEq.Should().Throw<SharpAssertionException>()
            .WithMessage("*5*10*");
    
          // Test !=  
          int a = 5;
          int b = 5;
          Expression<Func<bool>> exprNe = () => a != b;
          var actionNe = () => SharpInternal.Assert(exprNe, "a != b", "TestFile.cs", 2);
          actionNe.Should().Throw<SharpAssertionException>()
                  .WithMessage("*5*5*");
      ```
   - NEVER add comments that simply reiterate what is obvious from the code
   - PREFER using message argument in assertions to communicate what assertion is doing instead of comment 