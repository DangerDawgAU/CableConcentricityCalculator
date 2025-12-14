#!/usr/bin/env python3
"""
Build script for Cable Concentricity Calculator
Generates self-contained executables for Windows, macOS, and Linux
"""

import argparse
import os
import shutil
import subprocess
import sys
from pathlib import Path


class Colors:
    """ANSI color codes for terminal output"""
    RED = '\033[0;31m'
    GREEN = '\033[0;32m'
    YELLOW = '\033[1;33m'
    CYAN = '\033[0;36m'
    WHITE = '\033[1;37m'
    RESET = '\033[0m'

    @staticmethod
    def supports_color():
        """Check if terminal supports colors"""
        return hasattr(sys.stdout, 'isatty') and sys.stdout.isatty()


def colored(text, color):
    """Print colored text if terminal supports it"""
    if Colors.supports_color() or os.name != 'nt':
        return f"{color}{text}{Colors.RESET}"
    return text


def print_header(text):
    """Print a header message"""
    print(colored(f"\n=== {text} ===", Colors.CYAN))


def print_success(text):
    """Print a success message"""
    print(colored(f"[OK] {text}", Colors.GREEN))


def print_error(text):
    """Print an error message"""
    print(colored(f"[ERROR] {text}", Colors.RED))


def print_info(text):
    """Print an info message"""
    print(colored(text, Colors.YELLOW))


def get_file_size_mb(filepath):
    """Get file size in MB"""
    if filepath.exists():
        size_bytes = filepath.stat().st_size
        return size_bytes / (1024 * 1024)
    return 0


def run_command(cmd, cwd=None):
    """Run a shell command and return success status"""
    try:
        result = subprocess.run(
            cmd,
            cwd=cwd,
            shell=True,
            check=True,
            capture_output=True,
            text=True
        )
        return True, result.stdout
    except subprocess.CalledProcessError as e:
        return False, e.stderr


def clean_build(output_dir, release_dir, configuration):
    """Clean previous build artifacts"""
    print_info("Cleaning previous builds...")

    if output_dir.exists():
        try:
            shutil.rmtree(output_dir, ignore_errors=True)
            # Ensure it's really gone
            if output_dir.exists():
                shutil.rmtree(output_dir, onerror=lambda func, path, exc: os.chmod(path, 0o777) or func(path))
            print_success("Removed publish directory")
        except Exception as e:
            print_error(f"Failed to remove publish directory: {e}")

    if release_dir.exists() and release_dir != output_dir:
        try:
            shutil.rmtree(release_dir, ignore_errors=True)
            print_success("Removed release directory")
        except Exception as e:
            print_error(f"Failed to remove release directory: {e}")

    success, _ = run_command(f"dotnet clean --configuration {configuration}")
    if success:
        print_success("Clean complete")
    else:
        print_error("Clean failed")

    print()


def build_target(project_path, runtime, name, configuration, output_dir, release_dir):
    """Build for a specific target platform"""
    print(colored(f"Building for {name} ({runtime})...", Colors.CYAN))

    # Temporary build directory
    temp_output = output_dir / "temp" / runtime

    # Build command
    cmd = [
        "dotnet publish",
        f'"{project_path}"',
        f"--configuration {configuration}",
        f"--runtime {runtime}",
        "--self-contained true",
        f'--output "{temp_output}"',
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=false",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true"
    ]

    success, output = run_command(" ".join(cmd))

    if success:
        print_success(f"{name} build successful")

        # Get source and target executable names
        source_exe_name = "CableConcentricityCalculator.Gui.exe" if runtime.startswith("win") else "CableConcentricityCalculator.Gui"
        source_exe_path = temp_output / source_exe_name

        # Architecture-specific naming
        arch_name = runtime.replace("-", "_")
        extension = ".exe" if runtime.startswith("win") else ""
        target_exe_name = f"CableConcentricityCalculator_{arch_name}{extension}"
        target_exe_path = release_dir / target_exe_name

        # Copy executable to release directory
        if source_exe_path.exists():
            release_dir.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source_exe_path, target_exe_path)
            size_mb = get_file_size_mb(target_exe_path)
            print(colored(f"  Size: {size_mb:.2f} MB", Colors.WHITE))
            print(colored(f"  Output: {target_exe_name}", Colors.WHITE))
        else:
            print_error(f"Executable not found: {source_exe_path}")
            return False

        return True
    else:
        print_error(f"{name} build failed")
        print(output)
        return False


def main():
    """Main build script entry point"""
    parser = argparse.ArgumentParser(description="Build Cable Concentricity Calculator")
    parser.add_argument("--clean", action="store_true", help="Clean before building")
    parser.add_argument("--configuration", default="Release", choices=["Debug", "Release"], help="Build configuration")
    parser.add_argument("--target", help="Build specific target only (e.g., win-x64, linux-x64)")
    parser.add_argument("--skip-tests", action="store_true", help="Skip running tests")

    args = parser.parse_args()

    # Paths
    script_dir = Path(__file__).parent.absolute()
    gui_project = script_dir / "CableConcentricityCalculator.Gui" / "CableConcentricityCalculator.Gui.csproj"
    output_dir = script_dir / "Publish"
    release_dir = output_dir / "Release"

    # Print header
    print_header("Cable Concentricity Calculator Build Script")
    print_info(f"Configuration: {args.configuration}")
    print()

    # Verify project exists
    if not gui_project.exists():
        print_error(f"Project file not found: {gui_project}")
        sys.exit(1)

    # Clean if requested
    if args.clean:
        clean_build(output_dir, release_dir, args.configuration)

    # Create output directories
    output_dir.mkdir(exist_ok=True)
    release_dir.mkdir(exist_ok=True)

    # Define build targets
    all_targets = [
        {"runtime": "win-x64", "name": "Windows x64"},
        {"runtime": "osx-x64", "name": "macOS x64 (Intel)"},
        {"runtime": "osx-arm64", "name": "macOS ARM64 (Apple Silicon)"},
        {"runtime": "linux-x64", "name": "Linux x64"},
    ]

    # Filter targets if specific target requested
    if args.target:
        targets = [t for t in all_targets if t["runtime"] == args.target]
        if not targets:
            print_error(f"Unknown target: {args.target}")
            print_info("Available targets: " + ", ".join(t["runtime"] for t in all_targets))
            sys.exit(1)
    else:
        targets = all_targets

    # Build each target
    failed_builds = []
    built_executables = []
    for target in targets:
        success = build_target(
            gui_project,
            target["runtime"],
            target["name"],
            args.configuration,
            output_dir,
            release_dir
        )

        if not success:
            failed_builds.append(target["name"])
        else:
            arch_name = target["runtime"].replace("-", "_")
            extension = ".exe" if target["runtime"].startswith("win") else ""
            exe_name = f"CableConcentricityCalculator_{arch_name}{extension}"
            built_executables.append((target["name"], exe_name))

        print()

    # Clean up temporary build directory
    temp_dir = output_dir / "temp"
    if temp_dir.exists():
        try:
            shutil.rmtree(temp_dir, ignore_errors=True)
        except:
            pass  # Ignore cleanup errors

    # Print summary
    if failed_builds:
        print_error("Build Failed")
        print_info("Failed targets:")
        for name in failed_builds:
            print(f"  - {name}")
        sys.exit(1)
    else:
        print_header("Build Complete")
        print_info(f"Output directory: {release_dir}")
        print()
        print(colored("Built executables:", Colors.CYAN))
        for name, exe_name in built_executables:
            exe_path = release_dir / exe_name
            if exe_path.exists():
                size_mb = get_file_size_mb(exe_path)
                print(colored(f"  - {name}: {exe_name} ({size_mb:.2f} MB)", Colors.WHITE))
        print()
        print_success("All builds completed successfully!")


if __name__ == "__main__":
    main()
