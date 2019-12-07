//#define DEBUG_GENERATE_CONTEXT_MENU_ITEMS
#define DEBUG_FAIL_GENERATE_CONTEXT_MENU_ITEMS
//#define DEBUG_ADD

#if UNITY_EDITOR
using JetBrains.Annotations;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using Object = UnityEngine.Object;

namespace Sisus
{
	public static class MenuItemAttributeUtility
	{
		private static Dictionary<Type, List<ContextMenuItemInfo>> contextMenuMethodsByType;
		
		private static Dictionary<Type, List<ContextMenuItemInfo>> ContextMenuMethodsByType
		{
			get
			{
				if(contextMenuMethodsByType == null)
				{
					contextMenuMethodsByType = new Dictionary<Type, List<ContextMenuItemInfo>>();

					GenerateContextMenuMethodsByTypeFromTypes(TypeExtensions.AllVisibleTypes);
					GenerateContextMenuMethodsByTypeFromTypes(TypeExtensions.AllInvisibleTypes);
					
					#if DEV_MODE && DEBUG_GENERATE_CONTEXT_MENU_ITEMS
					UnityEngine.Debug.Log(contextMenuMethodsByType.Count + " context menu items discovered.");
					#endif
				}

				return contextMenuMethodsByType;
			}
		}

		private static void GenerateContextMenuMethodsByTypeFromTypes(Type[] types)
		{
			for(int n = types.Length - 1; n >= 0; n--)
			{
				var type = types[n];

				var methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly);
					
				for(int m = methods.Length - 1; m >= 0; m--)
				{
					var method = methods[m];
					var menuItems = method.GetCustomAttributes(typeof(MenuItem), false);
					foreach(var menuItemAttribute in menuItems)
					{
						var menuItem = (MenuItem)menuItemAttribute;
						var itemPath = menuItem.menuItem;
						if(itemPath.StartsWith("CONTEXT/", StringComparison.OrdinalIgnoreCase) && !IsBlackListed(itemPath))
						{
							int targetNameEnd = itemPath.IndexOf('/', 8);
							if(targetNameEnd != -1)
							{
								var typeName = itemPath.Substring(8, targetNameEnd - 8);
								var targetType = TypeExtensions.GetType(typeName, Types.UnityObject);
								if(targetType != null)
								{
									#if DEV_MODE && PI_ASSERTATIONS
									UnityEngine.Debug.Assert(Types.UnityObject.IsAssignableFrom(targetType));
									#endif

									#if DEV_MODE && DEBUG_GENERATE_CONTEXT_MENU_ITEMS
									UnityEngine.Debug.Log("Found context menu item \""+itemPath+"\" for type "+typeName+" in class "+type.FullName);
									#endif
										
									var label = itemPath.Substring(targetNameEnd + 1);

									AddContextMenuMethodForType(targetType, method, label, menuItem.priority);
									var extendingTypes = TypeExtensions.GetExtendingUnityObjectTypes(targetType, true);
									for(int t = extendingTypes.Length - 1; t >= 0; t--)
									{
										AddContextMenuMethodForType(extendingTypes[t], method, label, menuItem.priority);
									}
								}
								#if DEV_MODE && DEBUG_FAIL_GENERATE_CONTEXT_MENU_ITEMS
								else { UnityEngine.Debug.LogWarning("Context menu item \""+itemPath+"\" in class "+type.FullName+": UnityObject of type " + typeName + " was not found."); }
								#endif
							}
						}
					}
				}
			}
		}

		private static bool IsBlackListed(string menuItem)
		{
			switch(menuItem)
			{
				case PowerInspectorMenuItemPaths.ViewInPowerInspector:
				case PowerInspectorMenuItemPaths.PeekInPowerInspector:
					return true;
				default:
					return false;
			}
		}

		private static void AddContextMenuMethodForType(Type targetType, MethodInfo method, string label, int priority)
		{
			List<ContextMenuItemInfo> itemInfos;
			if(!contextMenuMethodsByType.TryGetValue(targetType, out itemInfos))
			{
				itemInfos = new List<ContextMenuItemInfo>(1);
				contextMenuMethodsByType.Add(targetType, itemInfos);
			}
			itemInfos.Add(new ContextMenuItemInfo(label, method, priority));
		}


		public static void AddItemsFromMenuItemAttributesToContextMenu([NotNull]Menu menu, [NotNull]Object[] targets)
		{
			int targetCount = targets.Length;
			if(targetCount == 0)
			{
				return;
			}

			var firstTarget = targets[0];			
			if(firstTarget == null)
			{
				return;
			}

			var targetType = firstTarget.GetType();
			List<ContextMenuItemInfo> itemInfos;
			if(ContextMenuMethodsByType.TryGetValue(targetType, out itemInfos))
			{
				menu.AddSeparatorIfNotRedundant();
				for(int n = 0, count = itemInfos.Count; n < count; n++)
				{
					var itemInfo = itemInfos[n];

					#if DEV_MODE && DEBUG_ADD
					UnityEngine.Debug.Log("Adding context menu item \""+itemInfo.label+"\" for type "+targetType.Name +" via method "+itemInfo.method.Name);
					#endif

					string label = itemInfo.label;
					var item = Menu.Item(label, ()=>
					{
						for(int t = targetCount - 1; t >= 0; t--)
						{
							itemInfo.method.InvokeWithParameter(null, new MenuCommand(targets[t], 0));
						}
					});
					
					#if DEV_MODE
					if(menu.Contains(label))
					{
						#if DEV_MODE
						UnityEngine.Debug.LogWarning("Context menu item conflict \""+itemInfo.label+"\" for type "+targetType.Name +" via method "+itemInfo.method.Name+". Adding with MenuItemAttribute suffix.");
						#endif
						menu.AddEvenIfDuplicate(label + "\tMenuItemAttribute", item.Effect);
					}
					else
					#endif
					{
						menu.Add(item);
					}
				}
			}
		}

		private class ContextMenuItemInfo
		{
			public readonly string label;
			public readonly MethodInfo method;
			public readonly int priority;

			public ContextMenuItemInfo(string setLabel, MethodInfo setMethod, int setPriority)
			{
				label = setLabel;
				method = setMethod;
				priority = setPriority;
			}
		}
	}
}
#endif