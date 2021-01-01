namespace grasslang.Build
{
    public interface Service
    {
        public void InstallDependency(grasslang.Build.Project sourceProject);
        public bool RunTask(string taskName);
    }
}
