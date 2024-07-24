using System.Runtime.InteropServices;

namespace meteor.Core.Models;

public class TextBuffer
{
    private const string DllName = "../../../../meteor-rust-core/target/release/libmeteor_rust_core.so";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void initialize_rope();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void insert_text(int index, string text);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void delete_text(int index, int length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_rope_content();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int get_rope_length();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr s);

    public static string GetRopeContent()
    {
        var ptr = get_rope_content();
        var content = Marshal.PtrToStringAnsi(ptr);
        free_string(ptr);
        return content;
    }
}