Properties {
	$base_dir = Split-Path $psake.build_script_file	

	$version_info = Get-VersionInfo "$base_dir\.version"
	$build_number = Get-BuildNumber

	$version = "$($version_info.Version).$build_number"
	$sem_version = $version_info.Version
	$short_version = Get-ShortenedVersion $sem_version
	if(-not [string]::IsNullOrEmpty($version_info.Quality)) {
		$sem_version = "$sem_version-$($version_info.Quality)"
		$short_version = "$short_version-$($version_info.Quality)"
	}
	if($version_info.PreRelease -eq "true") {
		$sem_version = "$sem_version-$build_number"
		$short_version = "$short_version-$build_number"
	}
	$release_notes_version = Get-ShortenedVersion $version_info.Version
	$config = 'Release'

	Write-Host "$config Version $sem_version($version)" -ForegroundColor Yellow

	$git_commit = Get-GitCommit
	$installer_product_id = New-Object System.Guid($git_commit.Hash.SubString(0,32))
	$installer_upgrade_code = New-Object System.Guid($git_commit.Hash.SubString(1,32))

	$src_dir = "$base_dir"
	$tools_dir = "$base_dir\tools"
	$artifacts_dir = "$base_dir\artifacts"
	$bin_dir = "$artifacts_dir\bin"
	$40_bin_dir = "$bin_dir\net40"
	$45_bin_dir = "$bin_dir\net45"
	$test_results_dir = "$artifacts_dir\test_results"
	$docs_dir = "$artifacts_dir\docs"

	$sln_file = "$base_dir\CSharpDriver.sln"
	$asm_file = "$src_dir\GlobalAssemblyInfo.cs"
	$docs_file = "$base_dir\Docs\Api\CSharpDriverDocs.shfbproj"
	$installer_file = "$base_dir\Installer\CSharpDriverInstaller.wixproj"
	$nuspec_file = "$base_dir\mongocsharpdriver.nuspec"
	$chm_file = "$artifacts_dir\CSharpDriverDocs-$short_version.chm"
	$release_notes_file = "$base_dir\Release Notes\Release Notes v$release_notes_version.md"
	$license_file = "$base_dir\License.txt"

	$nuget_tool = "$tools_dir\nuget\nuget.exe"
	$nunit_tool = "$tools_dir\nunit\nunit-console.exe"
	$opencover_tool = "$tools_dir\OpenCover\OpenCover.Console.exe"
	$coberturaconverter_tool = "$tools_dir\OpenCoverToCoberturaConverter\Palmmedia.OpenCoverToCoberturaConverter.exe"
	$reportgenerator_tool = "$tools_dir\ReportGenerator\bin\ReportGenerator.exe"
	$zip_tool = "$tools_dir\7Zip\7za.exe"
}

Framework('4.0')

Include tools\psake\psake-ext.ps1

function BuildHasBeenRun {
	$build_exists = (Test-Path $40_bin_dir) -and (Test-Path $45_bin_dir)
	Assert $build_exists "Build task has not been run"
	$true
}

function DocsHasBeenRun {
	$build_exists = Test-Path $chm_file
	Assert $build_exists "Docs task has not been run"
	$true
}

Task Default -Depends Build

Task Release -Depends Build, Docs, Zip, Installer, NugetPack

Task Clean {
	RemoveDirectory $artifacts_dir
	
	Write-Host "Cleaning $sln_file" -ForegroundColor Green
	Exec { msbuild "$sln_file" /t:Clean /p:Configuration=".NET 4.0 - $config" /v:quiet } 
	Exec { msbuild "$sln_file" /t:Clean /p:Configuration=".NET 4.5 - $config" /v:quiet } 
}

Task Init -Depends Clean {
	Generate-AssemblyInfo `
		-file $asm_file `
		-version $version `
		-config $config `
		-sem_version $sem_version `
}

Task Build -Depends Init {	
	try {
		
	mkdir -path $40_bin_dir | out-null
	Write-Host "Building $sln_file for .NET 4.0" -ForegroundColor Green
	Exec { msbuild "$sln_file" /t:Rebuild /p:Configuration=".NET 4.0 - $config" /p:TargetFrameworkVersion=v4.0 /v:quiet /p:OutDir=$40_bin_dir }

	mkdir -path $45_bin_dir | out-null
	Write-Host "Building $sln_file for .NET 4.5" -ForegroundColor Green
	Exec { msbuild "$sln_file" /t:Rebuild /p:Configuration=".NET 4.5 - $config" /p:TargetFrameworkVersion=v4.5 /v:quiet /p:OutDir=$45_bin_dir }
	}
	finally {
		Reset-AssemblyInfo
	}
}

Task Test -precondition { BuildHasBeenRun } {
	RemoveDirectory $test_results_dir
	mkdir -path $test_results_dir | out-null

	$test_assemblies = ls -rec $40_bin_dir\*Tests*.dll
	Write-Host "Testing $test_assemblies for .NET 4.0" -ForegroundColor Green
	Exec { &$nunit_tool $test_assemblies /timeout=10000 /xml=$test_results_dir\net40-test-results.xml /framework=net-4.0 /nologo /noshadow }

	$test_assemblies = ls -rec $45_bin_dir\*Tests*.dll
	Write-Host "Testing $test_assemblies for .NET 4.5" -ForegroundColor Green
	Exec { &$nunit_tool $test_assemblies /timeout=10000 /xml=$test_results_dir\net45-test-results.xml /framework=net-4.0 /nologo /noshadow }
}

Task TestWithCoverage -precondition { BuildHasBeenRun } {
	RemoveDirectory $test_results_dir
	mkdir -path $test_results_dir | out-null

	$test_assemblies = ls -rec $40_bin_dir\*Tests*.dll
	Write-Host "Testing $test_assemblies for .NET 4.0 with Code Coverage" -ForegroundColor Green
	$nunit_args = "$test_assemblies /timeout=10000 /xml=$test_results_dir\net40-test-results.xml /framework=net-4.0 /nologo /noshadow"
	Exec { &$opencover_tool "-register:user" "-target:$nunit_tool" "-targetargs:$nunit_args" "-filter:+[MongoDB*]* -[MongoDB*Tests*]*" "-output:$test_results_dir\net40-test-coverage.xml" }

	$test_assemblies = ls -rec $45_bin_dir\*Tests*.dll
	Write-Host "Testing $test_assemblies for .NET 4.5 with Code Coverage" -ForegroundColor Green
	$nunit_args = "$test_assemblies /timeout=10000 /xml=$test_results_dir\net45-test-results.xml /framework=net-4.0 /nologo /noshadow"
	Exec { &$opencover_tool "-register:user" "-target:$nunit_tool" "-targetargs:$nunit_args" "-filter:+[MongoDB*]* -[MongoDB*Tests*]*" "-output:$test_results_dir\net45-test-coverage.xml" }

	Exec { &$coberturaconverter_tool "-input:$test_results_dir\net40-test-coverage.xml" "-output:$test_results_dir\net40-cobertura-coverage.xml" "-sources:$src_dir" }

	Exec { &$coberturaconverter_tool "-input:$test_results_dir\net45-test-coverage.xml" "-output:$test_results_dir\net45-covertura-coverage.xml" "-sources:$src_dir" }

	Exec { &$reportgenerator_tool "-reports:$test_results_dir\*-test-coverage.xml" "-targetdir:$test_results_dir\report" "-reporttypes:Html;HtmlSummary" }
}

Task Docs -precondition { BuildHasBeenRun } {
	RemoveDirectory $docs_dir

	mkdir -path $docs_dir | out-null
	Exec { msbuild "$docs_file" /p:Configuration=$config /p:CleanIntermediate=True /p:HelpFileVersion=$version /p:OutputPath=$docs_dir } 

	mv "$docs_dir\CSharpDriverDocs.chm" $chm_file
	mv "$docs_dir\Index.html" "$docs_dir\index.html"
	Exec { &$zip_tool a "$artifacts_dir\CSharpDriverDocs-$short_version-html.zip" "$docs_dir\*" }
	RemoveDirectory $docs_dir
}

Task Zip -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) }{
	$zip_dir = "$artifacts_dir\ziptemp"
	
	RemoveDirectory $zip_dir
	mkdir -path $zip_dir | out-null

	mkdir -path "$zip_dir\net40" | out-null
	$40_items = @("$40_bin_dir\MongoDB.Bson.dll", `
		"$40_bin_dir\MongoDB.Bson.pdb", `
		"$40_bin_dir\MongoDB.Bson.xml", `
		"$40_bin_dir\MongoDB.Driver.Core.dll", `
		"$40_bin_dir\MongoDB.Driver.Core.pdb", `
		"$40_bin_dir\MongoDB.Driver.Core.xml", `
		"$40_bin_dir\MongoDB.Driver.dll", `
		"$40_bin_dir\MongoDB.Driver.pdb", `
		"$40_bin_dir\MongoDB.Driver.xml")
	cp $40_items "$zip_dir\net40"

	mkdir -path "$zip_dir\net45" | out-null
	$45_items = @("$45_bin_dir\MongoDB.Bson.dll", `
		"$45_bin_dir\MongoDB.Bson.pdb", `
		"$45_bin_dir\MongoDB.Bson.xml", `
		"$45_bin_dir\MongoDB.Driver.Core.dll", `
		"$45_bin_dir\MongoDB.Driver.Core.pdb", `
		"$45_bin_dir\MongoDB.Driver.Core.xml", `
		"$45_bin_dir\MongoDB.Driver.dll", `
		"$45_bin_dir\MongoDB.Driver.pdb", `
		"$45_bin_dir\MongoDB.Driver.xml")
	cp $45_items "$zip_dir\net45"

	cp $license_file $zip_dir
	cp "Release Notes\Release Notes v$release_notes_version.md" "$zip_dir\Release Notes.txt"
	cp $chm_file "$zip_dir\CSharpDriverDocs.chm"

	Exec { &$zip_tool a "$artifacts_dir\CSharpDriver-$short_version.zip" "$zip_dir\*" }

	rd $zip_dir -rec -force | out-null
}

Task Installer -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) } {
	$release_notes_relative_path = Get-Item $release_notes_file | Resolve-Path -Relative
	$doc_relative_path = Get-Item $chm_file | Resolve-Path -Relative

	Exec { msbuild "$installer_file" /t:Rebuild /p:Configuration=$config /p:Version=$version /p:SemVersion=$short_version /p:ProductId=$installer_product_id /p:UpgradeCode=$installer_upgrade_code /p:ReleaseNotes=$release_notes_relative_path /p:License="License.rtf" /p:Documentation=$doc_relative_path /p:OutputPath=$artifacts_dir /p:BinDir=$bin_dir}
	
	rm -force $artifacts_dir\*.wixpdb
}

Task NugetPack -precondition { (BuildHasBeenRun) -and (DocsHasBeenRun) }{
	Exec { &$nuget_tool pack $nuspec_file -o $artifacts_dir -Version $sem_version -Symbols -BasePath $base_dir }
}