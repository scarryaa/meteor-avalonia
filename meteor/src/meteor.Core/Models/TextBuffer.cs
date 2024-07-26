using System.Runtime.InteropServices;

namespace meteor.Core.Models;

public abstract class TextBuffer
{
    private const string DllName = "../../../../meteor-rust-core/target/release/libmeteor_rust_core.dylib";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void initialize_document();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void insert_text(int index, string text);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void delete_text(int index, int length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_document_slice(int start, int end);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_document_length();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr s);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_document_version();

    public static int GetVersion()
    {
        return get_document_version();
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
        if (start < 0 || end < start) throw new ArgumentOutOfRangeException(nameof(start));

        var documentSlicePtr = get_document_slice(start, end);
        if (documentSlicePtr == IntPtr.Zero) return string.Empty;

        using var safePtr = new SafeStringHandle(documentSlicePtr, free_string);
        if (safePtr.IsInvalid) return string.Empty;

        try
        {
            return Marshal.PtrToStringAuto(safePtr.DangerousGetHandle()) ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetDocumentSlice: {ex.Message}");
            return string.Empty;
        }
    }

    public static int GetDocumentLength()
    {
        return get_document_length();
    }
}