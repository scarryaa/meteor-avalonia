using System.Runtime.InteropServices;

namespace meteor.Core.Models;

public class SafeStringHandle : SafeHandle
{
    public SafeStringHandle(IntPtr handle, Action<IntPtr> releaseAction)
        : base(IntPtr.Zero, true)
    {
        SetHandle(handle);
        ReleaseAction = releaseAction;
    }

    private Action<IntPtr> ReleaseAction { get; }

    public override bool IsInvalid => handle == IntPtr.Zero;

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid) ReleaseAction(handle);
        return true;
    }
}