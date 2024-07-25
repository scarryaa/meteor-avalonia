using System.Runtime.InteropServices;

namespace meteor.Core.Models;

public class TextBuffer
{
    private const string DllName = "../../../../meteor-rust-core/target/release/libmeteor_rust_core.dylib";

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

    public static void Initialize()
    {
        initialize_rope();
    }

    public static void InsertText(int index, string text)
    {
        insert_text(index, text);
    }

    public static void DeleteText(int index, int length)
    {
        delete_text(index, length);
    }

    public static string GetRopeContent()
    {
        var ptr = IntPtr.Zero;
        try
        {
            ptr = get_rope_content();
            if (ptr == IntPtr.Zero) throw new Exception("Failed to get rope content. Pointer is null.");
            var content = Marshal.PtrToStringAnsi(ptr);
            return content;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetRopeContent: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            if (ptr != IntPtr.Zero) free_string(ptr);
        }
    }

    public static int GetRopeLength()
    {
        var length = get_rope_length();
        return length;
    }
}