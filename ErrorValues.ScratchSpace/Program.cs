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
    public enum MyEnum : byte
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
        return RMyFun<int>.None(3);
    }

    public static int Test()
    {
        return 0;
    }

    ref struct Struct : IStruc
    {

    }

    interface IStruc { }

    enum TestEnum
    {
        t1, t2, t3
    }

    static void Main()
    {
        IEVA<int> result = MyFun(); // Make it so that you only need to cast to the full type if IsSuccess == false
        
        if (result.Happy) return;

        RMyFun<int> error = (RMyFun<int>)result;

        MyEnum my_enum = (RMyFun<int>)error;

        switch (my_enum)
        {
            case MyEnum.None:
                {
                    var none = error.None();
                }
                break;
            case MyEnum.One or MyEnum.Two:
                {
                    var one = error.One();
                    var two = error.Two();
                }
                break;
            case MyEnum.Three: break;
        }

        TestEnum t = TestEnum.t1;

        switch (t)
        {
            case TestEnum.t1:
                break;
            case TestEnum.t2 or TestEnum.t3:
                break; 
        }

        int my_num = my_enum switch
        {
            MyEnum.None => error.None(),
            MyEnum.One => error.One().Length,
            MyEnum.Two => ((int)error.Two()),
            MyEnum.Three => 2,
            _ => 0
        };

        Console.WriteLine($"{error.None()}");
    }
}