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
            return Marshal.PtrToStringAuto(documentSlicePtr) ?? string.Empty;
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
        private const string WindowsDllName = "../../../../meteor-rust-core/target/release/meteor_rust_core.dll";
        private const string OsxDllName = "../../../../meteor-rust-core/target/release/libmeteor_rust_core.dylib";
        private const string LinuxDllName = "../../../../meteor-rust-core/target/release/libmeteor_rust_core.so";

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_document")]
        internal static extern UIntPtr CreateDocumentWindows();

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_document")]
        internal static extern UIntPtr CreateDocumentOsx();

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "create_document")]
        internal static extern UIntPtr CreateDocumentLinux();

        internal static UIntPtr CreateDocument()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return CreateDocumentWindows();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return CreateDocumentOsx();
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return CreateDocumentLinux();
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_document")]
        internal static extern void DeleteDocumentWindows(UIntPtr docId);

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_document")]
        internal static extern void DeleteDocumentOsx(UIntPtr docId);

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_document")]
        internal static extern void DeleteDocumentLinux(UIntPtr docId);

        internal static void DeleteDocument(UIntPtr docId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DeleteDocumentWindows(docId);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                DeleteDocumentOsx(docId);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                DeleteDocumentLinux(docId);
            else
                throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "insert_text")]
        internal static extern void InsertTextWindows(UIntPtr docId, int index, string text);

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "insert_text")]
        internal static extern void InsertTextOsx(UIntPtr docId, int index, string text);

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "insert_text")]
        internal static extern void InsertTextLinux(UIntPtr docId, int index, string text);

        internal static void InsertText(UIntPtr docId, int index, string text)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                InsertTextWindows(docId, index, text);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                InsertTextOsx(docId, index, text);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                InsertTextLinux(docId, index, text);
            else
                throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_text")]
        internal static extern void DeleteTextWindows(UIntPtr docId, int index, int length);

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_text")]
        internal static extern void DeleteTextOsx(UIntPtr docId, int index, int length);

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "delete_text")]
        internal static extern void DeleteTextLinux(UIntPtr docId, int index, int length);

        internal static void DeleteText(UIntPtr docId, int index, int length)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                DeleteTextWindows(docId, index, length);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                DeleteTextOsx(docId, index, length);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                DeleteTextLinux(docId, index, length);
            else
                throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_slice")]
        internal static extern IntPtr GetDocumentSliceWindows(UIntPtr docId, int start, int end);

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_slice")]
        internal static extern IntPtr GetDocumentSliceOsx(UIntPtr docId, int start, int end);

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_slice")]
        internal static extern IntPtr GetDocumentSliceLinux(UIntPtr docId, int start, int end);

        internal static IntPtr GetDocumentSlice(UIntPtr docId, int start, int end)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetDocumentSliceWindows(docId, start, end);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetDocumentSliceOsx(docId, start, end);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetDocumentSliceLinux(docId, start, end);
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_length")]
        internal static extern int GetDocumentLengthWindows(UIntPtr docId);

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_length")]
        internal static extern int GetDocumentLengthOsx(UIntPtr docId);

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_length")]
        internal static extern int GetDocumentLengthLinux(UIntPtr docId);

        internal static int GetDocumentLength(UIntPtr docId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetDocumentLengthWindows(docId);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetDocumentLengthOsx(docId);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetDocumentLengthLinux(docId);
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "free_string")]
        internal static extern void FreeStringWindows(IntPtr s);

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "free_string")]
        internal static extern void FreeStringOsx(IntPtr s);

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "free_string")]
        internal static extern void FreeStringLinux(IntPtr s);

        internal static void FreeString(IntPtr s)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                FreeStringWindows(s);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                FreeStringOsx(s);
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                FreeStringLinux(s);
            else
                throw new PlatformNotSupportedException("Unsupported operating system.");
        }

        [DllImport(WindowsDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_version")]
        internal static extern int GetDocumentVersionWindows(UIntPtr docId);

        [DllImport(OsxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_version")]
        internal static extern int GetDocumentVersionOsx(UIntPtr docId);

        [DllImport(LinuxDllName, CallingConvention = CallingConvention.Cdecl, EntryPoint = "get_document_version")]
        internal static extern int GetDocumentVersionLinux(UIntPtr docId);

        internal static int GetDocumentVersion(UIntPtr docId)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                return GetDocumentVersionWindows(docId);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                return GetDocumentVersionOsx(docId);
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                return GetDocumentVersionLinux(docId);
            throw new PlatformNotSupportedException("Unsupported operating system.");
        }
    }
}