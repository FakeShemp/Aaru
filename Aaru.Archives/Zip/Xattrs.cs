using System.Collections.Generic;
using System.Text;
using Aaru.CommonTypes.Enums;

namespace Aaru.Archives;

public sealed partial class Zip
{
    // XAttr name constants
    const string XATTR_COMMENT             = "comment";
    const string XATTR_APPLE_RESOURCE_FORK = "com.apple.ResourceFork";
    const string XATTR_APPLE_FINDER_INFO   = "com.apple.FinderInfo";
    const string XATTR_APPLE_HFS_TYPE      = "hfs.type";
    const string XATTR_APPLE_HFS_CREATOR   = "hfs.creator";
    const string XATTR_MICROSOFT_NTACL     = "com.microsoft.ntacl";
    const string XATTR_IBM_OS2_ACL         = "com.ibm.os2.acl";
    const string XATTR_VMS_ATTR            = "com.vms.attr";
    const string XATTR_ACORN_LOAD_ADDR     = "riscos.loadaddr";
    const string XATTR_ACORN_EXEC_ADDR     = "riscos.execaddr";
    const string XATTR_ACORN_ATTR          = "riscos.attr";
    const string XATTR_FWKCS_MD5           = "md5";
    const string XATTR_IBM_S390_ATTR       = "com.ibm.s390.attr";
    const string XATTR_IBM_VMCMS_ATTR      = "com.ibm.vmcms.attr";
    const string XATTR_IBM_MVS_ATTR        = "com.ibm.mvs.attr";
    const string XATTR_THEOS_ATTR          = "theos.attr";
    const string XATTR_QDOS_ATTR           = "qdos.attr";
    const string XATTR_TANDEM_ATTR         = "tandem.attr";
    const string XATTR_DG_AOSVS_ATTR       = "dg.aosvs.attr";

#region IArchive Members

    /// <inheritdoc />
    public ErrorNumber ListXAttr(int entryNumber, out List<string> xattrs)
    {
        xattrs = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        xattrs = [];

        Entry entry = _entries[entryNumber];

        if(entry.Comment is not null) xattrs.Add(XATTR_COMMENT);
        if(entry.ResourceFork is not null) xattrs.Add(XATTR_APPLE_RESOURCE_FORK);
        if(entry.FinderInfo is not null) xattrs.Add(XATTR_APPLE_FINDER_INFO);
        if(entry.MacFileType is not null) xattrs.Add(XATTR_APPLE_HFS_TYPE);
        if(entry.MacCreator is not null) xattrs.Add(XATTR_APPLE_HFS_CREATOR);

        if(entry.NtSecurityDescriptor is not null) xattrs.Add(XATTR_MICROSOFT_NTACL);

        // Individual OS/2 EAs
        if(entry.Os2Eas is not null)
        {
            foreach(string key in entry.Os2Eas.Keys) xattrs.Add(key);
        }

        if(entry.Os2Acl is not null) xattrs.Add(XATTR_IBM_OS2_ACL);

        // Individual BeOS attributes
        if(entry.BeOsAttributes is not null)
        {
            foreach(string key in entry.BeOsAttributes.Keys) xattrs.Add(key);
        }

        if(entry.AcornLoadAddr is not null) xattrs.Add(XATTR_ACORN_LOAD_ADDR);
        if(entry.AcornExecAddr is not null) xattrs.Add(XATTR_ACORN_EXEC_ADDR);
        if(entry.AcornAttr is not null) xattrs.Add(XATTR_ACORN_ATTR);

        if(entry.OpenVmsAttributes is not null) xattrs.Add(XATTR_VMS_ATTR);
        if(entry.Md5Hash is not null) xattrs.Add(XATTR_FWKCS_MD5);
        if(entry.S390Attributes is not null) xattrs.Add(XATTR_IBM_S390_ATTR);
        if(entry.VmCmsAttributes is not null) xattrs.Add(XATTR_IBM_VMCMS_ATTR);
        if(entry.MvsAttributes is not null) xattrs.Add(XATTR_IBM_MVS_ATTR);
        if(entry.TheosAttributes is not null) xattrs.Add(XATTR_THEOS_ATTR);
        if(entry.QdosAttributes is not null) xattrs.Add(XATTR_QDOS_ATTR);
        if(entry.TandemAttributes is not null) xattrs.Add(XATTR_TANDEM_ATTR);
        if(entry.AosVsAttributes is not null) xattrs.Add(XATTR_DG_AOSVS_ATTR);

        if(entry.UnicodeComment is not null && entry.Comment is null) xattrs.Add(XATTR_COMMENT);

        return ErrorNumber.NoError;
    }

    /// <inheritdoc />
    public ErrorNumber GetXattr(int entryNumber, string xattr, ref byte[] buffer)
    {
        buffer = null;

        if(!Opened) return ErrorNumber.NotOpened;

        if(entryNumber < 0 || entryNumber >= _entries.Count) return ErrorNumber.OutOfRange;

        Entry entry = _entries[entryNumber];

        switch(xattr)
        {
            case XATTR_COMMENT:
                if(entry.UnicodeComment is not null)
                    buffer = Encoding.UTF8.GetBytes(entry.UnicodeComment);
                else if(entry.Comment is not null)
                    buffer = Encoding.UTF8.GetBytes(entry.Comment);
                else
                    return ErrorNumber.NoSuchExtendedAttribute;

                return ErrorNumber.NoError;

            case XATTR_APPLE_RESOURCE_FORK:
                if(entry.ResourceFork is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.ResourceFork;

                return ErrorNumber.NoError;

            case XATTR_APPLE_FINDER_INFO:
                if(entry.FinderInfo is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.FinderInfo;

                return ErrorNumber.NoError;

            case XATTR_APPLE_HFS_TYPE:
                if(entry.MacFileType is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = Encoding.ASCII.GetBytes(entry.MacFileType);

                return ErrorNumber.NoError;

            case XATTR_APPLE_HFS_CREATOR:
                if(entry.MacCreator is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = Encoding.ASCII.GetBytes(entry.MacCreator);

                return ErrorNumber.NoError;

            case XATTR_MICROSOFT_NTACL:
                if(entry.NtSecurityDescriptor is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.NtSecurityDescriptor;

                return ErrorNumber.NoError;

            case XATTR_IBM_OS2_ACL:
                if(entry.Os2Acl is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.Os2Acl;

                return ErrorNumber.NoError;

            case XATTR_VMS_ATTR:
                if(entry.OpenVmsAttributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.OpenVmsAttributes;

                return ErrorNumber.NoError;

            case XATTR_ACORN_LOAD_ADDR:
                if(entry.AcornLoadAddr is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.AcornLoadAddr;

                return ErrorNumber.NoError;

            case XATTR_ACORN_EXEC_ADDR:
                if(entry.AcornExecAddr is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.AcornExecAddr;

                return ErrorNumber.NoError;

            case XATTR_ACORN_ATTR:
                if(entry.AcornAttr is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.AcornAttr;

                return ErrorNumber.NoError;

            case XATTR_FWKCS_MD5:
                if(entry.Md5Hash is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.Md5Hash;

                return ErrorNumber.NoError;

            case XATTR_IBM_S390_ATTR:
                if(entry.S390Attributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.S390Attributes;

                return ErrorNumber.NoError;

            case XATTR_IBM_VMCMS_ATTR:
                if(entry.VmCmsAttributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.VmCmsAttributes;

                return ErrorNumber.NoError;

            case XATTR_IBM_MVS_ATTR:
                if(entry.MvsAttributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.MvsAttributes;

                return ErrorNumber.NoError;

            case XATTR_THEOS_ATTR:
                if(entry.TheosAttributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.TheosAttributes;

                return ErrorNumber.NoError;

            case XATTR_QDOS_ATTR:
                if(entry.QdosAttributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.QdosAttributes;

                return ErrorNumber.NoError;

            case XATTR_TANDEM_ATTR:
                if(entry.TandemAttributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.TandemAttributes;

                return ErrorNumber.NoError;

            case XATTR_DG_AOSVS_ATTR:
                if(entry.AosVsAttributes is null) return ErrorNumber.NoSuchExtendedAttribute;

                buffer = entry.AosVsAttributes;

                return ErrorNumber.NoError;

            default:
                // Check OS/2 individual EAs
                if(entry.Os2Eas is not null && entry.Os2Eas.TryGetValue(xattr, out byte[] eaData))
                {
                    buffer = eaData;

                    return ErrorNumber.NoError;
                }

                // Check BeOS individual attributes
                if(entry.BeOsAttributes is not null && entry.BeOsAttributes.TryGetValue(xattr, out byte[] beData))
                {
                    buffer = beData;

                    return ErrorNumber.NoError;
                }

                return ErrorNumber.NoSuchExtendedAttribute;
        }
    }

#endregion
}