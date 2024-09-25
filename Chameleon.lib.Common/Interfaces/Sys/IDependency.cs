namespace Chameleon.lib.Common.Interfaces.Systemics;
public interface INotaDependency {
}
public interface IDependency {
}
public interface ISingletonDependency : IDependency {
}
public interface IScopedDependency : IDependency {
}
public interface ITransientDependency : IDependency {
}
