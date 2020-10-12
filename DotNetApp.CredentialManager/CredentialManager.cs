using System;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;

namespace DotNetApp
{
    /// <summary>
    /// Provides wrapped versions for some of the wincred.h functions.
    /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/"/>
    /// <see cref="https://docs.microsoft.com/en-us/dotnet/framework/interop/marshaling-data-with-platform-invoke"/>
    /// </summary>
    public static class CredentialManager
    {
        /// <summary>
        /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/nf-wincred-credreadw"/>
        /// </summary>
        /// <param name="target"></param>
        /// <param name="type"></param>
        /// <param name="reservedFlag"></param>
        /// <param name="credential"></param>
        /// <returns></returns>
        [DllImport("Advapi32.dll", EntryPoint = "CredReadW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool ReadCredential(
            [In] [MarshalAs(UnmanagedType.LPWStr)] string target,
            [In] CredentialType type,
            [In] int reservedFlag, 
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CredentialMarshaler))] out Credential credential);

        /// <summary>
        /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/nf-wincred-credwritew"/>
        /// </summary>
        /// <param name="credential"></param>
        /// <param name="flags"></param>
        /// <returns></returns>
        [DllImport("Advapi32.dll", EntryPoint = "CredWriteW", CharSet = CharSet.Unicode, SetLastError = true)]
        public static extern bool WriteCredential(
            [MarshalAs(UnmanagedType.CustomMarshaler, MarshalTypeRef = typeof(CredentialMarshaler))] ref Credential credential,
            [In] CredentialWriteFlags flags = CredentialWriteFlags.None);
    }

    /// <summary>
    /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw#members"/>
    /// </summary>
    public enum CredentialType : uint
    {
        Generic = 1,
        DomainPassword = 2,
        DomainCertificate = 3,
        DomainVisiblePassword = 4,
        GenericCertificate = 5,
        DomainExtended = 6,
        Maximum = 7,      // Maximum supported cred type
        MaximumEx = (Maximum + 1000)  // Allow new applications to run on old OSes
    }

    /// <summary>
    /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/nf-wincred-credwritew#members"/>
    /// </summary>
    public enum CredentialWriteFlags : uint
    {
        None = 0,
        PreserveCredentialBlob = 1
    }

    /// <summary>
    /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw#members"/>
    /// </summary>
    public enum CredentialPersist : uint
    {
        Session = 1,
        LocalMachine = 2,
        Enterprise = 3
    }

    /// <summary>
    /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credential_attributew"/>
    /// </summary>
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct CredentialAttribute
    {
        string Keyword;
        uint Flags;
        uint ValueSize;
        IntPtr Value;
    }

    //This type is deliberately not designed to be marshalled.
    public class Credential
    {
        public uint Flags;
        public CredentialType Type;
        public string TargetName;
        public string Comment;
        public FILETIME LastWritten;
        public byte[] CredentialBlob;
        public CredentialPersist Persist;
        public CredentialAttribute[] Attributes;
        public string TargetAlias;
        public string UserName;
    }

    /// <summary>
    /// 
    /// </summary>
    public class CredentialMarshaler : ICustomMarshaler
    {
        [DllImport("Advapi32.dll", SetLastError = true)]
        private static extern bool CredFree([In] IntPtr buffer);

        /// <summary>
        /// <see cref="https://docs.microsoft.com/en-us/windows/win32/api/wincred/ns-wincred-credentialw"/>
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct CREDENTIALW
        {
            public uint Flags;
            public CredentialType Type;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetName;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string Comment;
            public FILETIME LastWritten;
            public uint CredentialBlobSize;
            public IntPtr CredentialBlob;
            public CredentialPersist Persist;
            public uint AttributeCount;
            public IntPtr Attributes;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string TargetAlias;
            [MarshalAs(UnmanagedType.LPWStr)]
            public string UserName;
        }

        private IntPtr ptrAllocated;

        public void CleanUpManagedData(object ManagedObj)
        {
            // Nothing to do since all data can be garbage collected.
        }

        public void CleanUpNativeData(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return;
            }

            if (ptrAllocated == pNativeData)
            {
                Marshal.FreeHGlobal(pNativeData);
                ptrAllocated = IntPtr.Zero;
            }
            else
            {
                CredFree(pNativeData);
            }            
        }

        public int GetNativeDataSize()
        {
            throw new NotImplementedException();
        }

        public IntPtr MarshalManagedToNative(object obj)
        {
            if (obj == null)
            {
                return IntPtr.Zero;
            }

            if (obj is Credential credential)
            {
                int sizeofCredentialStruct = Marshal.SizeOf<CREDENTIALW>();
                int sizeofCredentialBlob = (credential.CredentialBlob?.Length ?? 0) * sizeof(byte);
                int sizeofCredentialAttributes = (credential.Attributes?.Length ?? 0) * Marshal.SizeOf<CredentialAttribute>();

                ptrAllocated = Marshal.AllocHGlobal(sizeofCredentialStruct + sizeofCredentialBlob + sizeofCredentialAttributes);

                IntPtr ptrCredentialStruct = ptrAllocated;
                IntPtr ptrCredentialBlob = IntPtr.Add(ptrCredentialStruct, sizeofCredentialStruct);
                IntPtr ptrCredentialAttributes = IntPtr.Add(ptrCredentialBlob, sizeofCredentialBlob);

                CREDENTIALW nativeCredential = new CREDENTIALW
                {
                    UserName = credential.UserName,
                    TargetName = credential.TargetName,
                    TargetAlias = credential.TargetAlias,
                    Persist = credential.Persist,
                    Comment = credential.Comment,
                    Flags = credential.Flags,
                    LastWritten = credential.LastWritten,
                    Type = credential.Type,
                    CredentialBlobSize = (uint)(credential.CredentialBlob?.Length ?? 0),
                    CredentialBlob = ptrCredentialBlob,
                    AttributeCount = (uint)(credential.Attributes?.Length ?? 0),
                    Attributes = ptrCredentialAttributes
                };

                Marshal.StructureToPtr(nativeCredential, ptrCredentialStruct, false);
                Marshal.Copy(credential.CredentialBlob, 0, ptrCredentialBlob, sizeofCredentialBlob);

                for (int i = 0; i < nativeCredential.AttributeCount; ++i)
                {
                    IntPtr ptrAttribute = IntPtr.Add(nativeCredential.Attributes, Marshal.SizeOf<CredentialAttribute>());
                    Marshal.StructureToPtr(credential.Attributes[i], ptrAttribute, false);
                }

                return ptrCredentialStruct;
            }

            throw new ArgumentException("Argument is not valid Credential object.");
        }

        public object MarshalNativeToManaged(IntPtr pNativeData)
        {
            if (pNativeData == IntPtr.Zero)
            {
                return null;
            }

            CREDENTIALW nativeCredential = (CREDENTIALW)Marshal.PtrToStructure(pNativeData, typeof(CREDENTIALW));

            Credential credential = new Credential
            {
                UserName = nativeCredential.UserName,
                TargetName = nativeCredential.TargetName,
                TargetAlias = nativeCredential.TargetAlias,
                Persist = nativeCredential.Persist,
                Comment = nativeCredential.Comment,
                Flags = nativeCredential.Flags,
                LastWritten = nativeCredential.LastWritten,
                Type = nativeCredential.Type,
                CredentialBlob = new byte[nativeCredential.CredentialBlobSize],
                Attributes = new CredentialAttribute[nativeCredential.AttributeCount]
            };

            Marshal.Copy(nativeCredential.CredentialBlob, credential.CredentialBlob, 0, (int)nativeCredential.CredentialBlobSize);

            for (int i = 0; i < nativeCredential.AttributeCount; ++i)
            {
                IntPtr ptrAttribute = IntPtr.Add(nativeCredential.Attributes, Marshal.SizeOf<CredentialAttribute>());
                Marshal.PtrToStructure(ptrAttribute, credential.Attributes[i]);
            }

            return credential;
        }

        public static ICustomMarshaler GetInstance(string cookie)
        {
            return new CredentialMarshaler();
        }
    }
}
