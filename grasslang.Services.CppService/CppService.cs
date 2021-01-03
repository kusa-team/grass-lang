using System;
using System.IO;
using System.Linq;
using grasslang.Build;
using System.Diagnostics;
using System.Collections.Generic;
using System.Text.RegularExpressions;
namespace grasslang.Services
{
    public class CppService : Service
    {
        // from interface
        public string SourceDir { set; get; }
        public string OutputDir { set; get; }
        public Project Project { set; get; }

        // cmake properties
        /// <summary>
        /// "executable" or "shared" or "static"
        /// </summary>
        public string Target = "executable";
        public List<string> ExtraIncludePaths = new List<string>();
        public List<string> ExtraLibraries = new List<string>();
        public List<string> ExtraCommands = new List<string>();
        public List<string> ExtraPackages = new List<string>();

        public void AddPackage(string name)
        {
            ExtraPackages.Add(name);
        }
        public void AddExtraIncludePath(string path)
        {
            ExtraIncludePaths.Add(path);
        }
        public void AddExtraLibrary(string path)
        {
            ExtraLibraries.Add(path);
        }
        public void AddExtraCommand(string command)
        {
            ExtraCommands.Add(command);
        } 

        // project properties

        public string ProjectName
        {
            get
            {
                return Project.Name.Replace('.', '_');
            }
        }
        private string OutputBinary
        {
            get
            {
                var platform = Platform.GetPlatform();
                string result = "";
                if (platform == Platform.PlatformName.Windows)
                {
                    if (Target == "executable")
                    {
                        result = Path.Combine(OutputDir, ProjectName + ".exe");
                    }
                    else if (Target == "shared")
                    {
                        result = Path.Combine(OutputDir, ProjectName + ".dll");
                    }
                    else if (Target == "static")
                    {
                        result = Path.Combine(OutputDir, ProjectName + ".lib");
                    }
                }
                else if (platform == Platform.PlatformName.Linux
                    || platform == Platform.PlatformName.Mac)
                {
                    if (Target == "executable")
                    {
                        result = Path.Combine(OutputDir, ProjectName);
                    }
                    else if (Target == "shared")
                    {
                        result = Path.Combine(OutputDir, "lib" + (platform == Platform.PlatformName.Mac
                            ? ProjectName + ".dylib" : ProjectName + ".so"));
                    }
                    else if (Target == "static")
                    {
                        result = Path.Combine(OutputDir, "lib" + ProjectName + ".a");
                    }
                }
                return result;
            }
        }
        public void InstallDependency(Project sourceProject)
        {
            if (sourceProject.Service is CppService cppService)
            {
                if(Target != "static" && Target != "shared")
                {
                    throw new Exception("Unsupported target.");
                }
                cppService.ExtraLibraries.Add(OutputBinary);
            }
            else
            {
                throw new Exception("Unsupported source.");
            }
        }
        public List<string> didTasks { get; set; }
        private void runInOutputDir(string cmd)
        {
            Console.WriteLine("------------ CMake: " + ProjectName);
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.CreateNoWindow = true;
            psi.Arguments = "-c \"" + cmd+ "\"";
            psi.FileName = "bash";
            psi.WorkingDirectory = OutputDir;
            psi.RedirectStandardOutput = true;
            Process.Start(psi).WaitForExit();
        }
        
        public bool RunTask(string taskName)
        {
            if (didTasks.Contains(taskName))
            {
                return true;
            }
            if (taskName == "genCMakeLists")
            {
                genCMakeLists();
                return true;
            }
            else if (taskName == "copySources")
            {
                // check for the files of this project
                if (!Project.Files.Any())
                {
                    throw new Exception("Empty project!");
                }
                Project.Files.ForEach((file) =>
                {
                    // copy sources
                    var targetFile = Path.Combine(OutputDir, file);
                    Project.createDirTree(Path.GetDirectoryName(targetFile));
                    File.Copy(Path.Combine(SourceDir, file), targetFile);
                });
                didTasks.Add("copySources");
                return true;
            }
            else if (taskName == "build")
            {
                // check for cmakelists
                RunTask("genCMakeLists");
                RunTask("copySources");
                // check for dependencies
                Project.Dependencies.ForEach((project) =>
                {
                    if (project.Service is CppService cppService)
                    {
                        cppService.RunTask("build");
                    }
                });
                // call cmake
                runInOutputDir("cmake .; make");
                didTasks.Add("build");
                return true;
            }
            return false;
        }

        public void genCMakeLists()
        {
            // get the path of cmakelists
            string CMakeLists = Path.Combine(OutputDir, "CMakeLists.txt");
            if (!Project.Files.Any())
            {
                throw new Exception("Empty project!");
            }
            // if the build path is not empty, will delete it and recreate it
            if ((from file in Directory.GetFiles(OutputDir)
                 where file != ".." && file != "."
                 select file).Any())
            {
                Directory.Delete(OutputDir, true);
                Directory.CreateDirectory(OutputDir);
            }
            string CMakeCommands = "";
            // add cmake_minimum_required
            CMakeCommands += "cmake_minimum_required (VERSION 3.12.0)\n";

            // write project name
            CMakeCommands += "project(" + ProjectName + ")\n";

            // get all sources
            string sourceFiles = "";
            Project.Files.ForEach((file) =>
            {
                sourceFiles += file + " ";
            });
            CMakeCommands += "set(sources " + sourceFiles + ")\n";

            // add commands
            string commands = "";
            ExtraCommands.ForEach((command) =>
            {
                commands += command + "\n";
            });
            CMakeCommands += commands;
            // handle dependency
            var dependencyServices = (from dependency in Project.Dependencies
                                      where dependency.Service is CppService
                                      select (dependency.Service as CppService)).ToArray();
            // add packages
            Action<string> packageHandle = (packageName) =>
            {
                CMakeCommands += "find_package(" + packageName + ")\n";
                if(!ExtraPackages.Contains(packageName))
                {
                    AddPackage(packageName);
                }
            };
            ExtraPackages.ForEach(packageHandle);
            (from service in dependencyServices
             select service.ExtraPackages).ToList()
             .ForEach((package) => package.ForEach(packageHandle));

            // add include path
            CMakeCommands += "include_directories(" + Project.Parent.OutputDir + ")\n";
            // add extra include paths
                string includePath = "";
            Action<string> includeDirsHandle = (path) =>
            {
                if (!includePath.Contains(path + " "))
                {
                    includePath += path + " ";
                }
                if(!ExtraIncludePaths.Contains(path))
                {
                    AddExtraIncludePath(path);
                }
            };
            if (ExtraIncludePaths.Any())
            {
                ExtraIncludePaths.ForEach(includeDirsHandle);
            }
            (from service in dependencyServices
             select service.ExtraIncludePaths).ToList()
             .ForEach((paths) => paths.ForEach(includeDirsHandle));
            CMakeCommands += "include_directories(" + includePath + ")\n";
            // get the build command by target
            if (Target == "executable")
            {
                CMakeCommands += "add_executable(" + ProjectName + " ${sources})\n";
            }
            else if (Target == "static" || Target == "shared")
            {
                CMakeCommands += "add_library(" + ProjectName + " " + Target.ToUpper() + " ${sources})\n";
            }
            // link extra libraries
            string librariesPath = "";
            Action<string> librariesHandle = (libraryPath) =>
            {
                librariesPath += libraryPath + " ";
                if(!ExtraLibraries.Contains(libraryPath))
                {
                    AddExtraLibrary(libraryPath);
                }
            };
            if (ExtraLibraries.Any())
            {
                ExtraLibraries.ForEach(librariesHandle);
            }
            (from service in dependencyServices
             select service.ExtraLibraries).ToList()
             .ForEach((libraries) => libraries.ForEach(librariesHandle));
            CMakeCommands += "set(libraries " + librariesPath + ")\n";
            CMakeCommands += "target_link_libraries(" + ProjectName + " ${libraries})\n";
            // write to cmakelists file
            File.WriteAllText(CMakeLists, CMakeCommands);
            didTasks.Add("genCMakeLists");
        }

        public CppService()
        {
            didTasks = new List<string>();
        }
    }
}
