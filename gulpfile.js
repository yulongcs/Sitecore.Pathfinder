// 1. Install NodeJS
// 2. Run 'npm update' to install/update node modules
//
// 3. Update version in this file
// 4. Update version in ./src/buildfiles/npm/package.json
// 5. Update version in assemblyinfo.cs files
//
// 6. Run 'gulp' to build Pathfinder without publishing
// 7. Or run 'gulp publish' to publish Pathfinder to npmjs.com, Nuget.org and the zip file directory
//
// 9. Done

var version = "0.11.1";

var gulp = require("gulp");
var del = require("del");
var msbuild = require("gulp-msbuild");
var nugetpack = require("gulp-nuget-pack");
var runSequence = require("run-sequence");
var spawn = require("child_process").spawn;
var zip = require("gulp-zip");

if (process.env.APPVEYOR_BUILD_VERSION) {
    version = process.env.APPVEYOR_BUILD_VERSION;
}

// build project

gulp.task("build-project", function() {
    return gulp.src("./Sitecore.Pathfinder.sln").pipe(msbuild({
        targets: ["Clean", "Build"],
        configuration: "Release",
        logCommand: true,
        verbosity: "minimal",
        maxcpucount: 0,
        toolsVersion: 15.0
    }));
});

// dist

gulp.task("clean-dist-directory", function() {
    return del("./build/dist");
});

gulp.task("build-dist-directory", ["clean-dist-directory"], function() {
    return gulp.src(["./bin/netcoreapp2.0/files/**/*", "./bin/netcoreapp2.0/help/**/*", "./bin/netcoreapp2.0/licenses/**/*", "./bin/netcoreapp2.0/*.dll", "./bin/netcoreapp2.0/*.zip", "./bin/netcoreapp2.0/scconfig.json", "./bin/netcoreapp2.0/scc.cmd", "./bin/netcoreapp2.0/Sitecore.Pathfinder.psm1", "./bin/netcoreapp2.0/Sitecore.Pathfinder.psd1"], { base: "./bin/netcoreapp2.0/" }).
        pipe(gulp.dest("./build/dist"));
});

// npm

gulp.task("clean-npm-directory", function() {
    return del("./build/npm");
});

gulp.task("copy-npm-files", ["clean-npm-directory"], function() {
    return gulp.src(["./src/buildfiles/npm/package.json", "./src/buildfiles/npm/README.md"]).
        pipe(gulp.dest("./build/npm"));
});

gulp.task("copy-npm-publish-files", ["copy-npm-files"], function() {
    return gulp.src(["./build/publish/*.dll"]).
        pipe(gulp.dest("./build/npm/bin/"));
});

gulp.task("copy-npm-directory", ["copy-npm-publish-files"], function() {
    return gulp.src(["./build/dist/**/*"]).
        pipe(gulp.dest("./build/npm/bin/"));
});

gulp.task("build-npm-package", ["copy-npm-directory"], function() {
    return spawn("npm.cmd", ["pack"], { stdio: "inherit", "cwd": "./build/npm/" });
});

gulp.task("publish-npm-package", ["copy-npm-directory"], function() {
    return spawn("npm.cmd", ["publish"], { stdio: "inherit", "cwd": "./build/npm/" });
});

// nuget

gulp.task("clean-nuget-package", function() {
    return del("./build/nuget/Sitecore.Pathfinder.nupkg");
});

gulp.task("build-nuget-package", ["clean-nuget-package"], function(callback) {
    return nugetpack({
            id: "Sitecore.Pathfinder",
            version: version,
            authors: "Jakob Christensen",
            owners: "Sitecore A/S",
            description: "Sitecore Pathfinder toolchain.",
            releaseNotes: "",
            summary: "Get started, get far, get happy.",
            language: "en-us",
            projectUrl: "https://github.com/JakobChristensen/Sitecore.Pathfinder",
            licenseUrl: "https://github.com/JakobChristensen/Sitecore.Pathfinder/blob/master/LICENSE",
            copyright: "Copyright 2016-2017 by Sitecore A/S",
            requireLicenseAcceptance: false,
            dependencies: [
            ],
            excludes: ["./build/publish/Sitecore.Pathfinder.App.dll", "./build/publish/Sitecore.Pathfinder.Core.dll", "./build/publish/Sitecore.Pathfinder.psd1", , "./build/publish/Sitecore.Pathfinder.psm1"],
            tags: "Sitecore, Pathfinder, compilation, nuget, npm",
            outputDir: "./build/nuget"
        },
        [
            { src: "./build/dist/*.psd1", dest: "/" },
            { src: "./build/dist/*.psm1", dest: "/" },
            { src: "./build/dist", dest: "/bin/" },
            { src: "./build/publish/**/*.dll", dest: "/bin/" }
        ],
        callback
    );
});

gulp.task("publish-nuget-package", ["build-nuget-package"], function() {
    return spawn("../bin/nuget.exe", ["push", "Sitecore.Pathfinder." + version + ".nupkg"], { stdio: "inherit", "cwd": "./build/nuget" });
});

gulp.task("publish-powershell-package", ["build-nuget-package"], function() {
    return spawn("../bin/nuget.exe", ["push", "Sitecore-Pathfinder." + version + ".nupkg"], { stdio: "inherit", "cwd": "./build/nuget" });
});

// zip file

gulp.task("clean-zip-file", function() {
    return del("./build/Sitecore.Pathfinder.zip");
});

gulp.task("build-zip-file", ["clean-zip-file"], function() {
    return gulp.src(["build/dist/**/*"]).
        pipe(zip("Sitecore.Pathfinder.zip")).
        pipe(gulp.dest("build"));
});

// tasks

gulp.task("default", ["build"], function() {
});

gulp.task("build", function() {
    runSequence("build-project", "build-dist-directory", ["build-npm-package"]);
});

gulp.task("build-nuget", function() {
    runSequence("build-project", "build-dist-directory", ["build-nuget-package"]);
});

gulp.task("publish", function() {
    runSequence("build-project", "build-dist-directory", ["publish-npm-package"]);
});

gulp.task("appveyor", function() {
    runSequence("build-dist-directory", ["build-npm-package", "build-zip-file"]);
});

gulp.task("generate-factory", function () {
    return spawn("scc.cmd", ["generate-factory"], { stdio: "inherit", "cwd": "./src/Sitecore.Pathfinder.Core/Configuration/" });
});
