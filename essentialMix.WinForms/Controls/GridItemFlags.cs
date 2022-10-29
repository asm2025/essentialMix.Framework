using System;

namespace essentialMix.WinForms.Controls;

[Flags]
public enum GridItemFlags
{
	Default = 0,
	Root = 1,
	Category = 2,
	Property = 4,
	Array = 8
}