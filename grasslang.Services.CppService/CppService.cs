using System;
using System.IO;
using System.Linq;
using grasslang.Build;

namespace grasslang.Services
{
    public class CppService : Service
    {
        /// <summary>
        /// "executable" or "shared" or "static"
        /// </summary>
        public string Type = "executable";
        public string OutputDir { set; get; }
        public Project Project { set; get; }
        public void InstallDependency(Project sourceProject)
        {
            if (sourceProject.Service is CppService cppService)
            {

            }
            else
            {
                throw new Exception("Unsupported source.");
            }
        }
        public bool RunTask(string taskName)
        {
            if (taskName == "genCMakeLists")
            {
                string CMakeLists = Path.Combine(OutputDir, "CMakeLists.txt");
                if ((from file in Directory.GetFiles(OutputDir)
                     where file != ".." && file != "."
                     select file).Any())
                {
                    Directory.Delete(OutputDir, true);
                    Directory.CreateDirectory(OutputDir);
                }
                File.AppendAllText(CMakeLists, "cmake_minimum_required (VERSION 3.12.0)\n");
                if(!Project.Files.Any())
                {
                    return true;
                }
                string sourceFiles = "";
                Project.Files.ForEach((file) =>
                {
                    sourceFiles += file + " ";
                });
                File.AppendAllText(CMakeLists, "set(SOURCES " + sourceFiles + ")\n");

                return true;
            }
            return false;
        }
    }
}
