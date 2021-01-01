using System;
namespace grasslang.Build
{
    public interface Service
    {
        public void InstallDependency(Project sourceProject);
        public bool RunTask(string taskName);
    }
}
