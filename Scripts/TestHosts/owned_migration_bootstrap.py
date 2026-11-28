"""Compile one private fixture entry point against the immutable API's own assemblies."""
import io
import os
from pathlib import Path
import re
import shutil
import subprocess
import tarfile


def prepare(root, api, owner):
    if os.environ.get('GITHUB_ACTIONS')!='true' or not re.fullmatch(r'oasis_startup_[0-9]+_[0-9]+',owner) or api!=owner+'_api':
        raise ValueError('Owned cloud migration fixture required.')
    workspace=Path(os.environ['GITHUB_WORKSPACE']).resolve(strict=True)
    runtime=Path(os.environ['OASIS_PROFILE_BROWSER_RUNTIME']).resolve(strict=True)
    expected=workspace/'portal-startup-owned'/'runtime'
    if runtime!=expected or runtime.is_symlink():
        raise ValueError('Exact owned browser runtime required.')
    owned=runtime.parent/'migration-bootstrap'
    if owned.exists() or owned.is_symlink():
        raise ValueError('Native bootstrap directory must be new.')
    label=subprocess.run(['docker','inspect','--format','{{ index .Config.Labels "oasis.startup.owner" }}',api],capture_output=True,text=True,check=True,timeout=15).stdout.strip()
    if label!=owner:
        raise ValueError('Wrong API container owner.')
    owned.mkdir();(owned/'.owner').write_text(owner)
    try:
        refs=owned/'refs';refs.mkdir()
        archive=subprocess.run(['docker','exec',api,'sh','-c','cd /app && tar -cf - -- *.dll'],capture_output=True,check=True,timeout=30).stdout
        with tarfile.open(fileobj=io.BytesIO(archive),mode='r:') as tar:
            members=tar.getmembers()
            if len(members)>1024 or sum(member.size for member in members)>512*1024*1024:
                raise ValueError('Unexpected published assembly inventory.')
            for member in members:
                if not member.isfile() or not re.fullmatch(r'[A-Za-z0-9_.-]+\.dll',member.name):
                    raise ValueError('Only flat published DLL files are allowed.')
                with tar.extractfile(member) as source, (refs/member.name).open('xb') as destination:
                    shutil.copyfileobj(source,destination)
        del archive
        source=root/'Scripts/TestHosts/Fixtures/migration-wizard-bootstrap.cs'
        shutil.copyfile(source,owned/'Program.cs')
        (owned/'NuGet.Config').write_text('<configuration><packageSources><clear /></packageSources></configuration>')
        project=owned/'OwnedMigrationBootstrap.csproj'
        project.write_text('<Project Sdk="Microsoft.NET.Sdk"><PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework><ImplicitUsings>disable</ImplicitUsings><Nullable>disable</Nullable></PropertyGroup><ItemGroup><Reference Include="refs/*.dll"><Private>false</Private></Reference></ItemGroup></Project>')
        # References are frozen private copies from this exact image; no Core/provider build or shared output mutation.
        built=subprocess.run(['dotnet','build',str(project),'-c','Release','-o',str(owned/'out'),'-m:2','-nr:false','-p:UseSharedCompilation=false','-p:ImportDirectoryBuildProps=false','-p:ImportDirectoryBuildTargets=false','-p:BaseIntermediateOutputPath='+str(owned/'obj')+'/', '-p:MSBuildProjectExtensionsPath='+str(owned/'obj')+'/', '-p:RestorePackagesPath='+str(owned/'packages'),'-v:minimal'],capture_output=True,text=True,timeout=120)
        if built.returncode:
            errors=[line for line in (built.stdout+'\n'+built.stderr).splitlines() if re.search(r'error [A-Z]+[0-9]+',line)]
            raise RuntimeError('Owned fixture build failed: '+'\n'.join(errors[-5:]))
        subprocess.run(['docker','cp',str(owned/'out/OwnedMigrationBootstrap.dll'),api+':/app/OwnedMigrationBootstrap.dll'],capture_output=True,check=True,timeout=20)
    finally:
        if owned.resolve()!=runtime.parent/'migration-bootstrap' or owned.is_symlink() or (owned/'.owner').read_text()!=owner:
            raise ValueError('Refusing cleanup outside the owned native bootstrap.')
        shutil.rmtree(owned)
        if owned.exists():
            raise RuntimeError('Owned native bootstrap cleanup incomplete.')
