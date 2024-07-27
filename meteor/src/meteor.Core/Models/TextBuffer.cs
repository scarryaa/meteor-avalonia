using System.Runtime.InteropServices;

namespace meteor.Core.Models;

public class TextBuffer : IDisposable
{
    private const string DllName = "../../../../meteor-rust-core/target/release/libmeteor_rust_core.dylib";

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern UIntPtr create_document();

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void delete_document(UIntPtr docId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void insert_text(UIntPtr docId, int index, string text);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void delete_text(UIntPtr docId, int index, int length);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern IntPtr get_document_slice(UIntPtr docId, int start, int end);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_document_length(UIntPtr docId);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern void free_string(IntPtr s);

    [DllImport(DllName, CallingConvention = CallingConvention.Cdecl)]
    private static extern int get_document_version(UIntPtr docId);

    private readonly UIntPtr _docId;

    public TextBuffer()
    {
        _docId = create_document();
    }

    public int GetVersion()
    {
        return get_document_version(_docId);
    }

    public void InsertText(int index, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        insert_text(_docId, index, text);
    }

    public void DeleteText(int index, int length)
    {
        delete_text(_docId, index, length);
    }

    public string GetDocumentSlice(int start, int end)
    {
        start = Math.Clamp(start, 0, GetDocumentLength());
        end = Math.Clamp(end, start, GetDocumentLength());

        if (start < 0 || end < start)
            throw new ArgumentOutOfRangeException(nameof(start),
                "Specified argument was out of the range of valid values.");

        var documentSlicePtr = get_document_slice(_docId, start, end);
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

    public int GetDocumentLength()
    {
        return get_document_length(_docId);
    }

    public void LoadContent(string content)
    {
        DeleteText(0, GetDocumentLength());
        InsertText(0, content);
    }

    public void Dispose()
    {
        delete_document(_docId);
        GC.SuppressFinalize(this);
    }

    ~TextBuffer()
    {
        Dispose();
    }
}