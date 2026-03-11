# RPM spec file for Aaru
#
# To build this package, you must first create the source tarball:
#   tar --exclude-vcs --exclude="*/bin" --exclude="*/obj" -czf \
#       ~/rpmbuild/SOURCES/aaru-6.0.0~alpha17.tar.gz \
#       --transform="s,^,aaru-6.0.0~alpha17/," .
#
# Or use the provided scripts which do this automatically:
#   ./build.sh                    (builds all package types)
#   pkg/rpm/build-rpm.sh          (builds only RPM)
#
# Then run: rpmbuild -bb aaru.spec
#

Name:           aaru
Version:        6.0.0~alpha.19
Release:        1%{?dist}
Summary:        Disc image management and creation tool

License:        GPL-3.0-or-later AND LGPL-2.1-or-later AND MIT
URL:            https://www.aaru.app
Source0:        %{name}-%{version}.tar.gz

# Runtime dependencies for self-contained .NET app
Requires:       libicu
Requires:       krb5-libs
Requires:       libunwind
Requires:       openssl-libs
Requires:       zlib

# Desktop integration
Requires:       shared-mime-info
Requires:       desktop-file-utils

# Don't strip .NET binaries
%global __strip /bin/true
%global _build_id_links none
%global debug_package %{nil}

# Skip automatic dependency detection for bundled .NET libraries
%global __requires_exclude ^lib.*\.so.*$
%global __provides_exclude ^lib.*\.so.*$

%description
Aaru (named after the Egyptian paradise where the righteous dwell eternally)
is the ultimate Data Preservation Suite — your all-in-one solution for digital
media preservation and archival.

This package is built for Red Hat Enterprise Linux 9+, Fedora 38+, openSUSE
Leap 15.5+, and compatible distributions.

Aaru is designed to assist you through the entire workflow of digital media
preservation — from the initial creation of disk images (commonly called
"dumping") all the way through long-term archival storage.

Key features include:
* Media dumping from various drives (magnetic, optical, flash, tapes)
* Hardware flexibility with ATA, ATAPI, SCSI, USB, FireWire support
* Image management (identify, compare, convert formats)
* Filesystem analysis and extraction
* Archive and game package support
* Both CLI and GUI interfaces
* AaruFormat archival format with comprehensive metadata

%prep
%setup -q

%build
# Determine architecture and map to .NET Runtime Identifier
%ifarch x86_64
DOTNET_RID=linux-x64
%endif
%ifarch aarch64
DOTNET_RID=linux-arm64
%endif
%ifarch armv7hl
DOTNET_RID=linux-arm
%endif

# Build self-contained .NET application
cd Aaru
dotnet publish -f net10.0 -c Release \
    --self-contained -r ${DOTNET_RID} \
    -p:PublishSingleFile=true \
    -p:IncludeNativeLibrariesForSelfExtract=true \
    -p:EnableCompressionInSingleFile=true

%install
# Determine architecture and map to .NET Runtime Identifier
%ifarch x86_64
DOTNET_RID=linux-x64
%endif
%ifarch aarch64
DOTNET_RID=linux-arm64
%endif
%ifarch armv7hl
DOTNET_RID=linux-arm
%endif

# Install main binary
install -D -m 0755 Aaru/bin/Release/net10.0/${DOTNET_RID}/publish/aaru \
    %{buildroot}/opt/Aaru/aaru

# Install documentation
install -D -m 0644 README.md %{buildroot}/opt/Aaru/README.md
install -D -m 0644 Changelog.md %{buildroot}/opt/Aaru/Changelog.md
install -D -m 0644 CONTRIBUTING.md %{buildroot}/opt/Aaru/CONTRIBUTING.md
install -D -m 0644 LICENSE %{buildroot}/opt/Aaru/LICENSE
install -D -m 0644 LICENSE.MIT %{buildroot}/opt/Aaru/LICENSE.MIT
install -D -m 0644 LICENSE.LGPL %{buildroot}/opt/Aaru/LICENSE.LGPL

# Install MIME type
install -D -m 0644 Aaru/aaruformat.xml \
    %{buildroot}%{_datadir}/mime/packages/aaruformat.xml

# Install desktop file
install -D -m 0644 Aaru/aaru.desktop \
    %{buildroot}%{_datadir}/applications/aaru.desktop

# Install icons
install -D -m 0644 icons/32x32/aaru.png \
    %{buildroot}%{_datadir}/icons/hicolor/32x32/apps/aaru.png
install -D -m 0644 icons/64x64/aaru.png \
    %{buildroot}%{_datadir}/icons/hicolor/64x64/apps/aaru.png
install -D -m 0644 icons/128x128/aaru.png \
    %{buildroot}%{_datadir}/icons/hicolor/128x128/apps/aaru.png
install -D -m 0644 icons/256x256/aaru.png \
    %{buildroot}%{_datadir}/icons/hicolor/256x256/apps/aaru.png
install -D -m 0644 icons/512x512/aaru.png \
    %{buildroot}%{_datadir}/icons/hicolor/512x512/apps/aaru.png

# Create symlink in /usr/bin
install -d %{buildroot}%{_bindir}
ln -sf /opt/Aaru/aaru %{buildroot}%{_bindir}/aaru

%post
# Update icon cache
touch --no-create %{_datadir}/icons/hicolor &>/dev/null || :

# Update MIME database
update-mime-database %{_datadir}/mime &>/dev/null || :

# Update desktop database
update-desktop-database &>/dev/null || :

%postun
# Update icon cache
if [ $1 -eq 0 ] ; then
    touch --no-create %{_datadir}/icons/hicolor &>/dev/null
    gtk-update-icon-cache %{_datadir}/icons/hicolor &>/dev/null || :
fi

# Update MIME database
update-mime-database %{_datadir}/mime &>/dev/null || :

# Update desktop database
update-desktop-database &>/dev/null || :

%posttrans
gtk-update-icon-cache %{_datadir}/icons/hicolor &>/dev/null || :

%files
/opt/Aaru/aaru
/opt/Aaru/README.md
/opt/Aaru/Changelog.md
/opt/Aaru/CONTRIBUTING.md
/opt/Aaru/LICENSE
/opt/Aaru/LICENSE.MIT
/opt/Aaru/LICENSE.LGPL
%{_bindir}/aaru
%{_datadir}/mime/packages/aaruformat.xml
%{_datadir}/applications/aaru.desktop
%{_datadir}/icons/hicolor/32x32/apps/aaru.png
%{_datadir}/icons/hicolor/64x64/apps/aaru.png
%{_datadir}/icons/hicolor/128x128/apps/aaru.png
%{_datadir}/icons/hicolor/256x256/apps/aaru.png
%{_datadir}/icons/hicolor/512x512/apps/aaru.png

%changelog
* Wed Mar 11 2026 Natalia Portillo <claunia@claunia.com> - 6.0.0~alpha.19-1
- New upstream alpha release 6.0.0-alpha.19
- Added command to generate JSON schema for Aaru metadata files
- Added LZO compression algorithms
- Added LZVN compression algorithm with native version
- Added XZ buffer compression and decompression
- Added support for reading DVD lead-in and lead-out
- Added support for OmniDrive DVD reading
- Added PS5 DI/BCA dumping
- Added many fully readable filesystems: ADFS, Acer, AO-DOS, AmigaDOS,
  AtheOS, btrfs, BeFS, Coherent, Cram, DEC Files-11, DEC RT-11, ECMA-67,
  exFAT, ext/ext2/ext3/ext4, F2FS, HFS, HFS+, HPOFS, HP LIF, HPFS, JFS,
  Locus, MicroDOS, Minix, NTFS, NILFS2, Nintendo optical, OS-9 RBF, PFS,
  ProDOS, QNX4, QNX6, Reiser v3/v4, SGI EFS, SmartFileSystem, Squash,
  System V, UDF, UNIX 7th Edition, UNIX boot (bfs), UFS, Veritas, Xia,
  XENIX, XFS
- Added CHD compressed compact disc and CHD v5 image support
- Added Easy CD Creator (.CIF) disc image support
- Added PowerISO / gBurner disc image support
- Added UltraISO disc image support
- Added WinOnCD disc image support
- Added RISC OS system area support for ISO9660
- Added full zisofs support for ISO9660
- Added zlib and lzfse compressed files support for HFS+
- Added support for reading all extended attributes in HFS+
- Added Wii U Disc Key and PS3 related media tags
- Added AIX minidisk partition traversing and enumeration
- Added big endian and PDP endianness swapping source generators
- Changed device reports to try other commands if one fails
- Changed DVD raw reading to dynamically figure out buffer size
- Changed dumping to only re-read pregap sectors on track type changes
- Changed FFS filesystem name to UFS with improved detection
- Changed GUI to show image path on all tabs and negative sectors in viewer
- Enhanced Pascal filesystem with marshalable structures and consistency checks
- Refactored to use source generator based big endian marshaller
- Changed sidecar to disable default SpamSum and use pathless filenames
- Fixed CD-Text processing from images in image info command
- Fixed Spectre.Console exception propagation
- Fixed divide by zero error
- Fixed markup escaping in CD-Text logging
- Fixed sidecar creation for files with brackets or braces
- Fixed listing files with braces
- Fixed Blu-ray disc info logging crash with braces or brackets
- Fixed missing markup end tag
- Fixed handling of invalid ISO9660 dates
- Fixed parallel track path in PFI
- Fixed volume name sanitization replacing '/' and '\' with '_'
- Fixed barcode casting in GUI
- Fixed wrongly using marked up path in image metadata editor
- Built for RHEL 9+, Fedora 38+, openSUSE Leap 15.5+
- Multi-architecture support (x86_64, aarch64, armv7hl)

