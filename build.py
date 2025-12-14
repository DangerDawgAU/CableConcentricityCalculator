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
    print(colored(f"✓ {text}", Colors.GREEN))


def print_error(text):
    """Print an error message"""
    print(colored(f"✗ {text}", Colors.RED))


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


def clean_build(output_dir, configuration):
    """Clean previous build artifacts"""
    print_info("Cleaning previous builds...")

    if output_dir.exists():
        shutil.rmtree(output_dir)
        print_success("Removed publish directory")

    success, _ = run_command(f"dotnet clean --configuration {configuration}")
    if success:
        print_success("Clean complete")
    else:
        print_error("Clean failed")

    print()


def build_target(project_path, runtime, name, configuration, output_dir):
    """Build for a specific target platform"""
    print(colored(f"Building for {name} ({runtime})...", Colors.CYAN))

    target_output = output_dir / runtime

    # Build command
    cmd = [
        "dotnet publish",
        f'"{project_path}"',
        f"--configuration {configuration}",
        f"--runtime {runtime}",
        "--self-contained true",
        f'--output "{target_output}"',
        "-p:PublishSingleFile=true",
        "-p:PublishTrimmed=false",
        "-p:IncludeNativeLibrariesForSelfExtract=true",
        "-p:EnableCompressionInSingleFile=true"
    ]

    success, output = run_command(" ".join(cmd))

    if success:
        print_success(f"{name} build successful")

        # Get executable name
        exe_name = "CableConcentricityCalculator.Gui.exe" if runtime.startswith("win") else "CableConcentricityCalculator.Gui"
        exe_path = target_output / exe_name

        # Display size
        if exe_path.exists():
            size_mb = get_file_size_mb(exe_path)
            print(colored(f"  Size: {size_mb:.2f} MB", Colors.WHITE))

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
    output_dir = script_dir / "publish"

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
        clean_build(output_dir, args.configuration)

    # Create output directory
    output_dir.mkdir(exist_ok=True)

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
    for target in targets:
        success = build_target(
            gui_project,
            target["runtime"],
            target["name"],
            args.configuration,
            output_dir
        )

        if not success:
            failed_builds.append(target["name"])

        print()

    # Print summary
    if failed_builds:
        print_error("Build Failed")
        print_info("Failed targets:")
        for name in failed_builds:
            print(f"  - {name}")
        sys.exit(1)
    else:
        print_header("Build Complete")
        print_info(f"Output directory: {output_dir}")
        print()
        print(colored("Built executables:", Colors.CYAN))
        for target in targets:
            print(colored(f"  - {target['name']} : publish/{target['runtime']}", Colors.WHITE))
        print()
        print_success("All builds completed successfully!")


if __name__ == "__main__":
    main()
