using System;
using System.IO;
using System.Linq;
using grasslang.Build;
using System.Diagnostics;
using System.Collections.Generic;

namespace grasslang.Services
{
    public class CppService : Service
    {
        // from interface
        public string SourceDir { set; get; }
        public string OutputDir { set; get; }
        public Project Project { set; get; }

        /// <summary>
        /// "executable" or "shared" or "static"
        /// </summary>
        public string Target = "executable";
        public List<string> IncludePaths = new List<string>();
        public List<string> Libraries = new List<string>();

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
                //cppService.IncludePaths.Add(OutputDir);
                cppService.Libraries.Add(OutputBinary);
            }
            else
            {
                throw new Exception("Unsupported source.");
            }
        }
        public List<string> didTasks { get; set; }
        private void runInOutputDir(string cmd)
        {
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.CreateNoWindow = true;
            psi.Arguments = "-c \"" + cmd+ "\"";
            psi.FileName = "bash";
            psi.WorkingDirectory = OutputDir;
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
                // add include path
                CMakeCommands += "include_directories(" + Project.Parent.OutputDir + ")\n";
                if(IncludePaths.Any())
                {
                    string includePath = "";
                    IncludePaths.ForEach((path) =>
                    {
                        includePath += path + " ";
                    });
                    CMakeCommands += "include_directories(" + includePath + ")\n";
                }
                // get the build command by target
                if (Target == "executable")
                {
                    CMakeCommands += "add_executable(" + ProjectName + " ${sources})\n";
                }
                else if (Target == "static" || Target == "shared")
                {
                    CMakeCommands += "add_library(" + ProjectName + " " + Target.ToUpper() + " ${sources})\n";
                }
                // link libraries
                if(Libraries.Any())
                {
                    string librariesPath = "";
                    Libraries.ForEach((libraryPath) =>
                    {
                        librariesPath += libraryPath + " ";
                    });
                    CMakeCommands += "set(libraries " + librariesPath + ")\n";
                    CMakeCommands += "target_link_libraries(" + ProjectName + " ${libraries})\n";
                }
                // write to cmakelists file
                File.WriteAllText(CMakeLists, CMakeCommands);
                didTasks.Add("genCMakeLists");
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
                    File.Copy(Path.Combine(SourceDir, file), Path.Combine(OutputDir, file));
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
        public CppService()
        {
            didTasks = new List<string>();
        }
    }
}
