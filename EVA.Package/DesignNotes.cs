/* 
Function targeting specializes error cases that might be shared across many functions
Optimal code reuse requires support for error case unions, intersections and subsets triggering generation
This makes the error variant the core object, not the set of errors, though sets can also be shared
e.g. 
User writes:
[EVA.Union(
    "RemoteFileErrors"
    FileErrors.PathNotFound,
    FileErrors.Permission, 
    NetworkErrors.InvalidURL)]

which generates: 
[EVA]
internal enum RemoteFileV
{
    Happy,
    [Payload<PathError>(recoverable: false)]
    PathNotFound,
    [Payload<FilePermission>(recoverable: true)]
    Permission,
    [Payload<FormatError>(recoverable: false)]
    InvalidURL
}
*/

/*
[EVA.New(nameof(MyTest),
    EVA.Variant<FilePermision>("Permission", recoverable: true)]
MyTestR<bool> MyTest()
{
    return MyTestR.Permission(FilePermission.Restricted);
}

vs

[EVA(nameof(MyTest))]
public enum MyTestV
{
    Happy,
    [Payload<FilePermision>(recoverable: true)]
    Permission
} 

MyTestR<bool> MyTest()
{
    return MyTestResult.Permission(FilePermission.Restricted);
}
 */
using System;
public abstract class MyAbstractException : Exception
{

}

public struct PayloadType { }
public class MyException : MyAbstractException
{
    public PayloadType Payload;
}