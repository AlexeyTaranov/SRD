using System;
using System.Collections.Generic;
using UnityEditor.IMGUI.Controls;

namespace SerializeReferenceDropdown.Editor
{
    public class SerializeReferenceDropdownAdvancedDropdown : AdvancedDropdown
    {
        private readonly IEnumerable<string> typeNames;

        private readonly Dictionary<AdvancedDropdownItem, int> itemAndIndexes =
            new Dictionary<AdvancedDropdownItem, int>();

        private readonly Action<int> onSelectedTypeIndex;

        public SerializeReferenceDropdownAdvancedDropdown(AdvancedDropdownState state, IEnumerable<string> typeNames,
            Action<int> onSelectedNewType) :
            base(state)
        {
            this.typeNames = typeNames;
            onSelectedTypeIndex = onSelectedNewType;
        }

        protected override AdvancedDropdownItem BuildRoot()
        {
            var root = new AdvancedDropdownItem("Types");
            itemAndIndexes.Clear();

            var index = 0;
            foreach (var typeName in typeNames)
            {
                var item = new AdvancedDropdownItem(typeName);
                itemAndIndexes.Add(item, index);
                root.AddChild(item);
                index++;
            }

            return root;
        }

        protected override void ItemSelected(AdvancedDropdownItem item)
        {
            base.ItemSelected(item);
            if (itemAndIndexes.TryGetValue(item, out var index))
            {
                onSelectedTypeIndex.Invoke(index);
            }
        }
    }
}