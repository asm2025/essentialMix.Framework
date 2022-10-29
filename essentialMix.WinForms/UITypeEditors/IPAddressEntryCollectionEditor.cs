using System;
using essentialMix.Collections.Specialized;

namespace essentialMix.WinForms.UITypeEditors;

public class IPAddressEntryCollectionEditor : CollectionEditor<IPAddressEntry>
{
	public IPAddressEntryCollectionEditor(Type type) 
		: base(type)
	{
	}
}