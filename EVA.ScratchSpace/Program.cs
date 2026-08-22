using EVA.Attributes;

namespace EVA.ScratchSpace;

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
    [EVA(nameof(MyFun))]
    public enum MyFunV : byte
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
        return MyFunR<int>.None(3);
    }

    static void Main()
    {
        IEVA<int> result = MyFun(); 
        // Make it so that you only need to cast to the full type if IsSuccess == false
        
        if (result.Happy) return;

        MyFunR<int> error = (MyFunR<int>)result;

        MyFunV my_enum = (MyFunR<int>)error;
        
        switch (my_enum)
        {
            case MyFunV.None:
                {
                    var none = error.None();
                }
                break;
            case MyFunV.One or MyFunV.Two:
                {
                    var one = error.One();
                    var two = error.Two();
                }
                break;
            case MyFunV.Three: break;
        }
        
        int my_num = my_enum switch 
        {
            MyFunV.None => error.None(),
            MyFunV.One => error.One().Length,
            MyFunV.Two => ((int)error.Two()),
            MyFunV.Three => 2,
            _ => 0
        };

        Console.WriteLine($"{error.None()}");
    }
}