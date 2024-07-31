using System;
using System.Runtime.InteropServices;

namespace meteor.Core.Models;

public class TextBuffer : IDisposable
{
    private readonly UIntPtr _docId;

    public TextBuffer()
    {
        _docId = NativeMethods.CreateDocument();
    }

    public void Dispose()
    {
        NativeMethods.DeleteDocument(_docId);
        GC.SuppressFinalize(this);
    }

    public int GetVersion()
    {
        return NativeMethods.GetDocumentVersion(_docId);
    }

    public void InsertText(int index, string text)
    {
        if (string.IsNullOrEmpty(text)) return;
        NativeMethods.InsertText(_docId, index, text);
    }

    public void DeleteText(int index, int length)
    {
        NativeMethods.DeleteText(_docId, index, length);
    }

    public string GetDocumentSlice(int start, int end)
    {
        start = Math.Clamp(start, 0, GetDocumentLength());
        end = Math.Clamp(end, start, GetDocumentLength());

        if (start < 0 || end < start)
            throw new ArgumentOutOfRangeException(nameof(start),
                "Specified argument was out of the range of valid values.");

        var documentSlicePtr = NativeMethods.GetDocumentSlice(_docId, start, end);
        if (documentSlicePtr == IntPtr.Zero) return string.Empty;

        try
        {
            return Marshal.PtrToStringUTF8(documentSlicePtr) ?? string.Empty;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in GetDocumentSlice: {ex.Message}");
            return string.Empty;
        }
        finally
        {
            NativeMethods.FreeString(documentSlicePtr);
        }
    }

    public int GetDocumentLength()
    {
        return NativeMethods.GetDocumentLength(_docId);
    }

    public void LoadContent(string content)
    {
        DeleteText(0, GetDocumentLength());
        InsertText(0, content);
    }

    ~TextBuffer()
    {
        Dispose();
    }

    private static class NativeMethods
    {
#if NATIVELIBNAME
        private const string DllName = NATIVELIBNAME;
#else
        private const string DllName = "meteor_rust_core";
#endif

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_document")]
        private static extern UIntPtr CreateDocumentNative();

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_document")]
        private static extern void DeleteDocumentNative(UIntPtr docId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "insert_text")]
        private static extern void InsertTextNative(UIntPtr docId, int index, string text);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_text")]
        private static extern void DeleteTextNative(UIntPtr docId, int index, int length);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_slice")]
        private static extern IntPtr GetDocumentSliceNative(UIntPtr docId, int start, int end);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_length")]
        private static extern int GetDocumentLengthNative(UIntPtr docId);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "free_string")]
        private static extern void FreeStringNative(IntPtr s);

        [DllImport(DllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_version")]
        private static extern int GetDocumentVersionNative(UIntPtr docId);

        private static readonly Func<UIntPtr> _createDocument = CreateDocumentNative;
        private static readonly Action<UIntPtr> _deleteDocument = DeleteDocumentNative;
        private static readonly Action<UIntPtr, int, string> _insertText = InsertTextNative;
        private static readonly Action<UIntPtr, int, int> _deleteText = DeleteTextNative;
        private static readonly Func<UIntPtr, int, int, IntPtr> _getDocumentSlice = GetDocumentSliceNative;
        private static readonly Func<UIntPtr, int> _getDocumentLength = GetDocumentLengthNative;
        private static readonly Action<IntPtr> _freeString = FreeStringNative;
        private static readonly Func<UIntPtr, int> _getDocumentVersion = GetDocumentVersionNative;

        internal static UIntPtr CreateDocument() => _createDocument();
        internal static void DeleteDocument(UIntPtr docId) => _deleteDocument(docId);
        internal static void InsertText(UIntPtr docId, int index, string text) => _insertText(docId, index, text);
        internal static void DeleteText(UIntPtr docId, int index, int length) => _deleteText(docId, index, length);
        internal static IntPtr GetDocumentSlice(UIntPtr docId, int start, int end) => _getDocumentSlice(docId, start, end);
        internal static int GetDocumentLength(UIntPtr docId) => _getDocumentLength(docId);
        internal static void FreeString(IntPtr s) => _freeString(s);
        internal static int GetDocumentVersion(UIntPtr docId) => _getDocumentVersion(docId);
    }
}