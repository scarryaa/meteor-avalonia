using System.Runtime.InteropServices;

namespace meteor.Core.Models;

public class TextBuffer
{
    private const string DllName = "../../../../meteor-rust-core/target/release/libmeteor_rust_core.dylib";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void initialize_document();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void insert_text(int index, string text);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void delete_text(int index, int length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_document_slice(int start, int end);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int get_document_length();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr s);

    public static void Initialize()
    {
        initialize_document();
    }

    public static void InsertText(int index, string text)
    {
        insert_text(index, text);
    }

    public static void DeleteText(int index, int length)
    {
        delete_text(index, length);
    }

    public static string GetDocumentSlice(int start, int end)
    {
        var ptr = IntPtr.Zero;
        try
        {
            ptr = get_document_slice(start, end);
            if (ptr == IntPtr.Zero) return string.Empty;
            var content = Marshal.PtrToStringAnsi(ptr);
            return content ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetDocumentSlice: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            if (ptr != IntPtr.Zero) free_string(ptr);
        }
    }

    public static int GetDocumentLength()
    {
        return get_document_length();
    }
}