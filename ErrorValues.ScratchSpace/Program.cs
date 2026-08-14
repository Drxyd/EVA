using System.Collections;
using ErrorValues.Attributes;

namespace ErrorValues.ScratchSpace;

/* Analyzers TODO: 
 * 1. Refactor, repeated work in GetVariantNameFromClause and VariantHasPayload. See AnalyzeSwitchExpressionArm for example reduction. 
 * 2. Design control flow graph analysis pipeline.
 * 3. Add support for partial matching per matching construct whilst enforcing complete matching in function nody.
 * 4. Add support for exhaustive if-else blocks.
 * 5. Add support for disjoint if block matching.
 * 6. Add support for ternary expression matching.
 */

internal class Program 
{
    //public static void Main()
    //{
    //    Console.WriteLine(typeof(IEVA<>).GetCleanName());
    //    Console.WriteLine(typeof(IEVA<>).GetCleanFullName());
    //}
    [ErrorValues(nameof(MyFun))]
    public enum FunErr : byte
    {
        [Payload<int>(happy: true)]
        None = 1 << 0,
        [Payload<byte[]>]
        One = 1 << 1,
        [Payload<FileAccess>]
        Two = 1 << 2,
        Three
    }

    public static IEVA<int> MyFun()
    {
        return RFunErr<int>.None(3);
    }

    static void Main()
    {
        IEVA<int> result = MyFun(); 
        // Make it so that you only need to cast to the full type if IsSuccess == false
        
        if (result.Happy) return;

        RFunErr<int> error = (RFunErr<int>)result;

        FunErr my_enum = (RFunErr<int>)error;
        
        switch (my_enum)
        {
            case FunErr.None:
                {
                    var none = error.None();
                }
                break;
            case FunErr.One or FunErr.Two:
                {
                    var one = error.One();
                    var two = error.Two();
                }
                break;
            case FunErr.Three: break;
        }
        
        int my_num = my_enum switch 
        {
            FunErr.None => error.None(),
            FunErr.One => error.One().Length,
            FunErr.Two => ((int)error.Two()),
            FunErr.Three => 2,
            _ => 0
        };

        Console.WriteLine($"{error.None()}");
    }
}

class Test : IEVA<int>
{
    public bool Happy => throw new NotImplementedException();
}