namespace Chameleon.lib.Common.Interfaces;
public interface IHaveContainerProvider : ISingletonDependency {
		//
		// Summary:
		//     Resolves a given System.Type
		//
		// Parameters:
		//   type:
		//     The service System.Type
		//
		// Returns:
		//     The resolved Service System.Type
		object Resolve(Type type);
		T? Resolve<T>();
		T Resolve<T>(Type type) where T : class;
}
