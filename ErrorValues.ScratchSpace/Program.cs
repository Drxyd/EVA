using ErrorValues.Attributes;

namespace ErrorValues.ScratchSpace;

internal class Program
{
    [ErrorValues( nameof(MyFun) )]
    public enum MyEnum : byte
    {
        [Payload<int>]
        None = 1 << 0,
        [Payload<byte[]>]
        One = 1 << 1,
        [Payload<FileAccess>]
        Two = 1 << 2, 
        Three
    }

    public static RMyFun<int> MyFun()
    {
        return RMyFun<int>.None(3);
    }

    static void Main()
    {
        RMyFun<int> res = default;
        MyEnum my_enum = res;

        switch (my_enum)
        {
            case MyEnum.None:
                {
                    res.None();
                }
                break;
            case MyEnum.One:
                {
                    res.One();
                }
                break;
            case MyEnum.Two:
                {
                    res.Two();
                }
                break;
            case MyEnum.Three:
                break;
        }

        int my_num = my_enum switch
        {
            MyEnum.None => res.None(),
            MyEnum.One => res.One().Length,
            MyEnum.Two => ((int)res.Two()), 
            MyEnum.Three => 2,
            _ => 0
        };

        if (my_num != 0)
        {
            Console.WriteLine($"{typeof(ErrorValuesAttribute).Namespace}.{typeof(ErrorValuesAttribute).Name}");
            Console.WriteLine($"{typeof(ErrorValuesAttribute).FullName}");
            Console.WriteLine($"{typeof(RMyFun<int>).Name}");
        }
    }
}