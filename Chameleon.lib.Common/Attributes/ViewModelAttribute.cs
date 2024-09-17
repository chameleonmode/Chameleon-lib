namespace Chameleon.lib.Common.Attributes;
[AttributeUsage(AttributeTargets.Class)]
public class ViewModelAttribute(Type type)
				: Attribute {
	public Type Type { get; private set; } = type;
}