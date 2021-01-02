using System.Collections.Generic;
namespace grasslang.Build
{
    public interface Service
    {
        public Project Project { set; get; }
        public string OutputDir { set; get; }
        public string SourceDir { set; get; }
        public List<string> didTasks { get; set; }
        public void InstallDependency(Project sourceProject);
        public bool RunTask(string taskName);
    }
}
