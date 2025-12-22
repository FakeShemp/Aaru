#!/usr/bin/env bash
AARU_VERSION=6.0.0-alpha.16
OS_NAME=$(uname)

mkdir -p build

# Create standalone versions
cd Aaru
for conf in Debug Release;
do
 for distro in linux-arm64 linux-arm linux-x64 osx-x64 osx-arm64 win-arm64 win-x64 win-x86;
 do
  dotnet publish -f net10.0 -r ${distro} -c ${conf}

# Package the Linux packages (stopped working)
#  if [[ ${distro} == alpine* ]] || [[ ${distro} == linux* ]]; then
#    dotnet tarball -f net10.0 -r ${distro} -c ${conf} -o ../build
#    dotnet rpm -f net10.0 -r ${distro} -c ${conf} -o ../build
#    dotnet deb -f net10.0 -r ${distro} -c ${conf} -o ../build
#  elif [[ ${distro} == win* ]] || [[ ${distro} == osx* ]]; then
#    dotnet zip -f net10.0 -r ${distro} -c ${conf} -o ../build
#  elif [[ ${distro} == rhel* ]] || [[ ${distro} == sles* ]]; then
#    pkg="rpm"
#  else
#    pkg="deb"
#  fi

  done
done

cd ..

# If we are compiling on Linux check if we are on Arch Linux and then create the Arch Linux package as well
if [[ ${OS_NAME} == Linux ]]; then
 OS_RELEASE=`pcregrep -o1 -e "^ID=(?<distro_id>\w+)" /etc/os-release`

 if [[ ${OS_RELEASE} != arch && ${OS_RELEASE} != cachyos ]]; then
  exit 0
 fi

 tar --exclude-vcs --exclude="*/bin" --exclude="*/obj" --exclude="build" --exclude="pkg/pacman/*/*.tar.xz" \
  --exclude="pkg/pacman/*/src" --exclude="pkg/pacman/*/pkg"  --exclude="pkg/pacman/*/*.tar" \
  --exclude="pkg/pacman/*/*.asc" --exclude="*.user" --exclude=".idea" --exclude=".vs" --exclude=".vscode" \
  --exclude="build.iso" --exclude=".DS_Store" -cvf pkg/pacman/stable/aaru-src-${AARU_VERSION}.tar .
 mv .glogalconfig .globalconfig.bak
 cd pkg/pacman/stable
 xz -v9e aaru-src-${AARU_VERSION}.tar
 gpg --armor --detach-sign aaru-src-${AARU_VERSION}.tar.xz
 cp PKGBUILD PKGBUILD.bak
 echo -e \\n >> PKGBUILD
 makepkg -g >> PKGBUILD
 makepkg
 mv PKGBUILD.bak PKGBUILD
 mv aaru-src-${AARU_VERSION}.tar.xz aaru-src-${AARU_VERSION}.tar.xz.asc ../../../build
 cd ../../..
 mv .globalconfig.bak .globalconfig

fi

mv pkg/pacman/stable/*.pkg.tar.zst build/

# Remove stray files from published folders
rm -R Aaru/bin/*/net10.0/*/publish/BuildHost-net* Aaru/bin/*/net10.0/*/publish/*.xml Aaru/bin/*/net10.0/*/publish/*.pdb

# Package tarballs
cd Aaru/bin/Debug/net10.0/linux-arm/publish/
tar cvf ../../../../../../build/aaru-${AARU_VERSION}_linux_armhf-dbg.tar -- *
cd ../../linux-arm64/publish/
tar cvf ../../../../../../build/aaru-${AARU_VERSION}_linux_arm64-dbg.tar -- *
cd ../../linux-x64/publish/
tar cvf ../../../../../../build/aaru-${AARU_VERSION}_linux_amd64-dbg.tar -- *
cd ../../osx-arm64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_macos_aarch64-dbg.zip *
cd ../../osx-x64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_macos-dbg.zip *
cd ../../win-arm64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_windows_aarch64-dbg.zip *
cd ../../win-x64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_windows_x64-dbg.zip *
cd ../../win-x86/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_windows_x86-dbg.zip *
cd ../../../../Release/net10.0/linux-arm/publish/
tar cvf ../../../../../../build/aaru-${AARU_VERSION}_linux_armhf.tar -- *
cd ../../linux-arm64/publish/
tar cvf ../../../../../../build/aaru-${AARU_VERSION}_linux_arm64.tar -- *
cd ../../linux-x64/publish/
tar cvf ../../../../../../build/aaru-${AARU_VERSION}_linux_amd64.tar -- *
cd ../../osx-arm64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_macos_aarch64.zip *
cd ../../osx-x64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_macos.zip *
cd ../../win-arm64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_windows_aarch64.zip *
cd ../../win-x64/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_windows_x64.zip *
cd ../../win-x86/publish/
7z a ../../../../../../build/aaru-${AARU_VERSION}_windows_x86.zip *
cd ../../../../../../

cd build
xz -9e -- *.tar
for i in *.deb *.rpm *.zip *.tar.xz *.pkg.tar.zst;
do
 gpg --armor --detach-sign "$i"
done

cd ..
rm -Rf build/macos/Aaru.app
mkdir -p build/macos/Aaru.app/Contents/Resources
mkdir -p build/macos/Aaru.app/Contents/MacOS
cp Aaru/Aaru.icns build/macos/Aaru.app/Contents/Resources
cp Aaru/Info.plist build/macos/Aaru.app/Contents
cp -r Aaru/bin/Release/net10.0/osx-x64/publish/* build/macos/Aaru.app/Contents/MacOS
rm -Rf build/macos-dbg/Aaru.app
mkdir -p build/macos-dbg/Aaru.app/Contents/Resources
mkdir -p build/macos-dbg/Aaru.app/Contents/MacOS
cp Aaru/Aaru.icns build/macos-dbg/Aaru.app/Contents/Resources
cp Aaru/Info.plist build/macos-dbg/Aaru.app/Contents
cp -r Aaru/bin/Debug/net10.0/osx-x64/publish/* build/macos-dbg/Aaru.app/Contents/MacOS
