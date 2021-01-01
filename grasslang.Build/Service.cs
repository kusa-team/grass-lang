using System;
namespace grasslang.Build
{
    public interface Service
    {
        public Project Project { set; get; }
        public string OutputDir { set; get; }
        public void InstallDependency(Project sourceProject);
        public bool RunTask(string taskName);
    }
}
