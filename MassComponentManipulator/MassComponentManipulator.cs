using Elements.Assets;
using Elements.Core;
using FrooxEngine;
using FrooxEngine.ProtoFlux;
using FrooxEngine.ProtoFlux.CoreNodes;
using FrooxEngine.UIX;
using FrooxEngine.Undo;
using HarmonyLib;
using ResoniteHotReloadLib;
using ResoniteModLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MassComponentManipulator
{
	public class SyncMemberManipulatorMod : ResoniteMod
	{
		public override string Name => "ComponentMemberReplicator";
		public override string Author => "Nytra";
		public override string Version => "1.0.0";
		public override string Link => "https://github.com/Nytra/ResoniteSyncMemberManipulator";

		// initial
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialMinWidth = new ModConfigurationKey<float>("Key_InitialMinWidth", "Key_InitialMinWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialMinHeight = new ModConfigurationKey<float>("Key_InitialMinHeight", "Key_InitialMinHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialPreferredWidth = new ModConfigurationKey<float>("Key_InitialPreferredWidth", "Key_InitialPreferredWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialPreferredHeight = new ModConfigurationKey<float>("Key_InitialPreferredHeight", "Key_InitialPreferredHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialFlexibleWidth = new ModConfigurationKey<float>("Key_InitialFlexibleWidth", "Key_InitialFlexibleWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_InitialFlexibleHeight = new ModConfigurationKey<float>("Key_InitialFlexibleHeight", "Key_InitialFlexibleHeight", () => -1f);

		[AutoRegisterConfigKey]
		static ModConfigurationKey<dummy> Key_Dummy1 = new ModConfigurationKey<dummy>("Key_Dummy1", "<size=0></size>", () => new dummy());

		// fields
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsMinWidth = new ModConfigurationKey<float>("Key_FieldsMinWidth", "Key_FieldsMinWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsMinHeight = new ModConfigurationKey<float>("Key_FieldsMinHeight", "Key_FieldsMinHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsPreferredWidth = new ModConfigurationKey<float>("Key_FieldsPreferredWidth", "Key_FieldsPreferredWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsPreferredHeight = new ModConfigurationKey<float>("Key_FieldsPreferredHeight", "Key_FieldsPreferredHeight", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsFlexibleWidth = new ModConfigurationKey<float>("Key_FieldsFlexibleWidth", "Key_FieldsFlexibleWidth", () => 1000f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_FieldsFlexibleHeight = new ModConfigurationKey<float>("Key_FieldsFlexibleHeight", "Key_FieldsFlexibleHeight", () => -1f);

		[AutoRegisterConfigKey]
		static ModConfigurationKey<dummy> Key_Dummy2 = new ModConfigurationKey<dummy>("Key_Dummy2", "<size=0></size>", () => new dummy());

		// checkbox
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxMinWidth = new ModConfigurationKey<float>("Key_CheckboxMinWidth", "Key_CheckboxMinWidth", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxMinHeight = new ModConfigurationKey<float>("Key_CheckboxMinHeight", "Key_CheckboxMinHeight", () => 24f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxPreferredWidth = new ModConfigurationKey<float>("Key_CheckboxPreferredWidth", "Key_CheckboxPreferredWidth", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxPreferredHeight = new ModConfigurationKey<float>("Key_CheckboxPreferredHeight", "Key_CheckboxPreferredHeight", () => -1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxFlexibleWidth = new ModConfigurationKey<float>("Key_CheckboxFlexibleWidth", "Key_CheckboxFlexibleWidth", () => 1f);
		[AutoRegisterConfigKey]
		static ModConfigurationKey<float> Key_CheckboxFlexibleHeight = new ModConfigurationKey<float>("Key_CheckboxFlexibleHeight", "Key_CheckboxFlexibleHeight", () => -1f);

		//[AutoRegisterConfigKey]
		//static ModConfigurationKey<bool> Key_HandleLists = new ModConfigurationKey<bool>("Key_HandleLists", "Key_HandleLists", () => false);

		static ModConfiguration config;

		static string WIZARD_TITLE
		{
			get
			{
				string s = "Component Member Replicator (Mod)";
				s += " " + HotReloader.GetReloadedCountOfModType(typeof(SyncMemberManipulatorMod)).ToString();
				return s;
			}
		}

		static string wizardActionString => WIZARD_TITLE;

		public override void OnEngineInit()
		{
			config = GetConfiguration();
			HotReloader.RegisterForHotReload(this);
			Engine.Current.RunPostInit(Setup);
		}

		static void BeforeHotReload()
		{
			Msg("In BeforeHotReload!");
			HotReloader.RemoveMenuOption("Editor", wizardActionString);
		}

		static void OnHotReload(ResoniteMod modInstance)
		{
			Msg("In OnHotReload!");
			config = modInstance.GetConfiguration();
			Setup();
		}

		static void AddMenuOption()
		{
			//DateTime utcNow = DateTime.UtcNow;
			//wizardActionString = WIZARD_TITLE + utcNow.ToString();
			DevCreateNewForm.AddAction("Editor", wizardActionString, (slot) => MassComponentManipulator.CreateWizard(slot));
		}

		static void Setup()
		{
			AddMenuOption();
		}

		public class MassComponentManipulator
		{
			public static MassComponentManipulator CreateWizard(Slot x)
			{
				return new MassComponentManipulator(x);
			}

			Slot WizardSlot;
			Slot WizardStaticContentSlot;
			//Slot WizardGeneratedFieldsSlot;
			RectTransform WizardStaticContentRect;
			//RectTransform WizardGeneratedFieldsRect;
			Slot WizardGeneratedContentSlot;
			//RectTransform WizardGeneratedContentRect;
			Slot WizardSearchDataSlot;
			//Slot WizardGeneratedFieldsDataSlot;
			UIBuilder WizardUI;

			ReferenceField<Slot> searchRoot;
			ReferenceField<Component> sourceComponent;

			//ValueField<bool> restoreDrives;
			//ValueField<bool> restoreDrivesRecursively;

			ValueField<int> modeField;

			ValueField<bool> breakExistingDrives;

			bool DriveFromSource => modeField.Value == 1;
			bool RecursiveDeepCopyDrives => modeField.Value == 2 || modeField.Value == 3;
			bool ShouldDrive => DriveFromSource || RecursiveDeepCopyDrives;

			bool ShouldWrite => modeField.Value == 0 || modeField.Value == 3;

			static Dictionary<Component, Component> newCompMappings = new Dictionary<Component, Component>();

			//Button applyButton;

			struct SyncMemberData
			{
				public ISyncMember sourceSyncMember; // the syncMember to copy from
				public IField<bool> enabledField; // the checkbox that determines if the syncMember should be copied out to other components
			}

			// workers with same name?
			// syncMembers with same name?
			// should check syncMemberIndex

			// <workerName, <memberName, SyncMemberWizardFields>>
			// could break if there are nested workers? or workers with same name?
			Dictionary<string, Dictionary<string, SyncMemberData>> workerMemberFields = new Dictionary<string, Dictionary<string, SyncMemberData>>();

			const float CANVAS_WIDTH_DEFAULT = 800f; // 800f
			const float CANVAS_HEIGHT_DEFAULT = 1200f;

			MassComponentManipulator(Slot x)
			{
				WizardSlot = x;
				WizardSlot.Tag = "Developer";
				WizardSlot.PersistentSelf = false;
				WizardSlot.LocalScale *= 0.0006f;

				WizardSearchDataSlot = WizardSlot.AddSlot("SearchData");
				//WizardGeneratedFieldsDataSlot = WizardSlot.AddSlot("FieldsData");

				WizardUI = RadiantUI_Panel.SetupPanel(WizardSlot, WIZARD_TITLE.AsLocaleKey(), new float2(CANVAS_WIDTH_DEFAULT, CANVAS_HEIGHT_DEFAULT));
				RadiantUI_Constants.SetupEditorStyle(WizardUI);

				WizardUI.Canvas.MarkDeveloper();
				WizardUI.Canvas.AcceptPhysicalTouch.Value = false;

				WizardUI.Style.MinWidth = config.GetValue(Key_InitialMinWidth);
				WizardUI.Style.MinHeight = config.GetValue(Key_InitialMinHeight);
				WizardUI.Style.PreferredWidth = config.GetValue(Key_InitialPreferredWidth);
				WizardUI.Style.PreferredHeight = config.GetValue(Key_InitialPreferredHeight);
				WizardUI.Style.FlexibleWidth = config.GetValue(Key_InitialFlexibleWidth);
				WizardUI.Style.FlexibleHeight = config.GetValue(Key_InitialFlexibleHeight);

				WizardSlot.PositionInFrontOfUser(float3.Backward, distance: 1f);

				WizardStaticContentSlot = WizardUI.Root;
				WizardStaticContentRect = WizardUI.CurrentRect;

				RegenerateWizardUI();
			}

			void SetEnabledFields(bool val)
			{
				foreach (Dictionary<string, SyncMemberData> dict in workerMemberFields.Values)
				{
					foreach (SyncMemberData fields in dict.Values)
					{
						fields.enabledField.Value = val;
					}
				}
			}

			//void UpdateApplyButtonEnabled()
			//{
			//	bool b = false;
			//	foreach (Dictionary<string, SyncMemberWizardFields> dict in workerMemberFields.Values)
			//	{
			//		foreach (SyncMemberWizardFields fields in dict.Values)
			//		{
			//			if (fields.enabledField.Value == true)
			//			{
			//				b = true;
			//				break;
			//			}
			//		}
			//	}
			//	Button applyButton = null;
			//	if (applyButton.FilterWorldElement() != null)
			//	{
			//		applyButton.Enabled = b && searchRoot.Reference.Target != null;
			//	}
			//}

			void UpdateCanvasSize()
			{
				// I couldn't get this to work right
				// So now the canvas is constant size
				WizardUI.Canvas.Size.Value = new float2(CANVAS_WIDTH_DEFAULT, CANVAS_HEIGHT_DEFAULT);
				return;

				//WizardSlot.RunInUpdates(30, () => 
				//{
				//	// supposed to be the size of the canvas that is actually used
				//	// 80 is height of panel header. 24 is extra padding to make it stop scrolling when it doesn't need to
				//	float newY = 80 + 24 + 12;
				//	foreach(Slot childSlot in WizardStaticContentSlot.Children)
				//	{
				//		if (childSlot.GetComponent<VerticalLayout>() != null) continue;

				//		RectTransform rectTransform = childSlot.GetComponent<RectTransform>();
				//		if (rectTransform != null && !rectTransform.IsRemoved)
				//		{
				//			newY += rectTransform.LocalComputeRect.size.y;
				//		}
				//	}
				//	if (WizardGeneratedFieldsRect != null && !WizardGeneratedFieldsRect.IsRemoved)
				//	{
				//		newY += WizardGeneratedFieldsRect.LocalComputeRect.size.y;
				//		if (WizardGeneratedContentRect != null && !WizardGeneratedContentRect.IsRemoved)
				//		{
				//			newY += WizardGeneratedFieldsRect.LocalComputeRect.size.y - WizardGeneratedContentRect.LocalComputeRect.size.y;
				//		}
				//	}
				//	WizardUI.Canvas.Size.Value = new float2(CANVAS_WIDTH_DEFAULT, MathX.Min(newY, CANVAS_HEIGHT_DEFAULT));
				//});
			}

			IEnumerable<ISyncMember> EnumerateMembersRecursively(ISyncMember member)
			{
				if (member is Worker nextWorker)
				{
					foreach (var nextWorkerMember in EnumerateMembersRecursively(nextWorker))
					{
						yield return nextWorkerMember;
					}
				}
				else if (member is ISyncList list)
				{
					//Debug($"Found list: {list.Name}");
					var genericArg = list.GetType().GetGenericArguments()[0];
					if (typeof(Worker).IsAssignableFrom(genericArg))
					{
						//Debug("List of workers");
						foreach (var elem in list.Elements)
						{
							foreach (var listMember in EnumerateMembersRecursively((Worker)elem))
							{
								yield return listMember;
							}
						}
						//break;
					}
					else if (typeof(ISyncMember).IsAssignableFrom(genericArg))
					{
						foreach (var elem in list.Elements)
						{
							yield return (ISyncMember)elem;
						}
						//break;
					}
				}
				else if (member is ISyncBag bag)
				{
					//Debug($"Found bag: {bag.Name}");
					var genericArg = bag.GetType().GetGenericArguments()[0];
					if (typeof(Worker).IsAssignableFrom(genericArg))
					{
						//Debug("Bag of workers");
						foreach (var elem in bag.Values)
						{
							foreach (var bagMember in EnumerateMembersRecursively((Worker)elem))
							{
								yield return bagMember;
							}
						}
						//break;
					}
					else if (typeof(ISyncMember).IsAssignableFrom(genericArg))
					{
						foreach (var elem in bag.Values)
						{
							yield return (ISyncMember)elem;
						}
						//break;
					}
				}
			}

			IEnumerable<ISyncMember> EnumerateMembersRecursively(Worker rootWorker)
			{
				//Debug($"Root worker: {rootWorker.Name}");
				foreach (var member in rootWorker.SyncMembers)
				{
					yield return member;
					foreach (var allMember in EnumerateMembersRecursively(member))
					{
						yield return allMember;
					}
					//continue;
					////Debug(member.Name);
					//yield return member;
					//if (member is Worker nextWorker)
					//{
					//	foreach (var nextWorkerMember in EnumerateMembersRecursively(nextWorker))
					//	{
					//		yield return nextWorkerMember;
					//	}
					//}
					//else if (member is ISyncList list)
					//{
					//	//Debug($"Found list: {list.Name}");
					//	var genericArg = list.GetType().GetGenericArguments()[0];
					//	if (typeof(Worker).IsAssignableFrom(genericArg))
					//	{
					//		// Need to do recursion here probably
					//		//Debug("List of workers");
					//		foreach (var elem in list.Elements)
					//		{
					//			foreach (var listMember in EnumerateMembersRecursively((Worker)elem))
					//			{
					//				yield return listMember;
					//			}
					//		}
					//		break;
					//	}
					//	else if (typeof(ISyncMember).IsAssignableFrom(genericArg))
					//	{
					//		foreach (var elem in list.Elements)
					//		{
					//			yield return (ISyncMember)elem;
					//		}
					//		break;
					//	}
					//}
					//else if (member is ISyncBag bag)
					//{
					//	//Debug($"Found bag: {bag.Name}");
					//	var genericArg = bag.GetType().GetGenericArguments()[0];
					//	if (typeof(Worker).IsAssignableFrom(genericArg))
					//	{
					//		// Need to do recursion here probably
					//		//Debug("Bag of workers");
					//		foreach (var elem in bag.Values)
					//		{
					//			foreach (var bagMember in EnumerateMembersRecursively((Worker)elem))
					//			{
					//				yield return bagMember;
					//			}
					//		}
					//		break;
					//	}
					//	else if (typeof(ISyncMember).IsAssignableFrom(genericArg))
					//	{
					//		foreach (var elem in bag.Values)
					//		{
					//			yield return (ISyncMember)elem;
					//		}
					//		break;
					//	}
					//}
				}
			}

			class DriveData
			{
				public Stack<ISyncMember> stackToTargetMember;
				public ISyncMember targetMember = null;
			}

			Stack<ISyncMember> GetMemberStack(ISyncMember member)
			{
				var stack = new Stack<ISyncMember>();
				bool found = false;
				var currentMember = member;
				while (!found)
				{
					stack.Push(currentMember);
					if (currentMember.Parent is ISyncMember parentMember)
					{
						currentMember = parentMember;
					}
					else
					{
						found = true;
					}
				}
				return stack;
			}

			void CollectDriveData(Worker worker, List<DriveData> allDriveData)
			{
				foreach (var member in EnumerateMembersRecursively(worker))
				{
					if (member is IField && (DriveFromSource || (RecursiveDeepCopyDrives && member.IsDriven)))
					{
						var driveData = new DriveData();
						driveData.targetMember = member;
						driveData.stackToTargetMember = GetMemberStack(member);
						allDriveData.Add(driveData);
					}
				}
			}

			string ElementIdentifierString(IWorldElement elem)
			{
				return $"{elem.Name} {elem.ReferenceID}";
			}

			static ValueCopy<T> GetValueCopy<T>(IField fromField, IField toField)
			{
				return ((IField<T>)toField).DriveFrom((IField<T>)fromField);
			}

			static ReferenceCopy<T> GetReferenceCopy<T>(IField fromRef, IField toRef) where T : class, IWorldElement
			{
				return ((SyncRef<T>)toRef).DriveFrom((SyncRef<T>)fromRef);
			}

			void SafeFieldUndoPoint(IField field)
			{
				if (field.ValueType.IsEnginePrimitive() || field.ValueType == typeof(RefID) || field.ValueType == typeof(Type))
				{
					field.CreateUndoPoint();
				}
			}

			bool RestoreDrives(SyncElement fromElement, SyncElement toElement, Dictionary<Component, Component> newCompMappings, bool undoable = false, bool recursive = false, ulong recursionDepth = 0)
			{
				var link = fromElement.ActiveLink;
				var comp = link.FindNearestParent<Component>();

				var toFieldComponent = toElement.FindNearestParent<Component>();
				var targetSlot = toFieldComponent.Slot;

				if (!breakExistingDrives.Value && toElement.IsDriven)
				{
					Debug("Target element is driven, skipping");
					return false;
				}

				Debug($"Recursion depth: {recursionDepth}");

				Debug($"Restoring drive for field {ElementIdentifierString(toElement)} on component {ElementIdentifierString(toFieldComponent)} on slot {ElementIdentifierString(targetSlot)}");

				Debug($"Source field is driven by {ElementIdentifierString(link)} of type {link.GetType().GetNiceName()} on component {ElementIdentifierString(comp)}");

				if (comp is ProtoFluxEngineProxy proxy) // && proxy.GetSyncMember("Drive") is ILinkRef proxyLinkRef && proxyLinkRef.Target != null && typeof(IField).IsAssignableFrom(proxyLinkRef.Target.GetType()))
				{
					Debug("Is ProtoFluxEngineProxy Drive");

					if (proxy.Node.Target is null)
					{
						Debug("Proxy node target is null, skipping");
						return false;
					}

					var newSlot = comp.Slot.Parent.AddSlot(comp.Slot.Name + "_duped");
					var origDriveNode = proxy.Node.Target.FindNearestParent<Component>();
					var dupedDriveNode = newSlot.DuplicateComponent(origDriveNode, breakExternalReferences: true);
					if (undoable)
					{
						newSlot.CreateSpawnUndoPoint();
					}
					if (recursive)
					{
						Debug("Entered recursive part.");

						var allDriveData = new List<DriveData>();
						CollectDriveData(origDriveNode, allDriveData);
						foreach (var driveData in allDriveData)
						{
							Debug($"Found driven member on source component that needs to be restored: {ElementIdentifierString(driveData.targetMember)}");
							var correspondingMember = FindCorrespondingMember(dupedDriveNode, driveData.targetMember, driveData.stackToTargetMember);
							if (RestoreDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: undoable, recursive: recursive, recursionDepth: recursionDepth + 1))
							{
								Debug("Restored drive.");
							}
							else
							{
								Debug("Failed to restore drive.");
							}
							Debug($"Recursion depth: {recursionDepth}");
						}

						Debug("Finished recursive part.");
					}

					if (undoable)
					{
						SafeFieldUndoPoint((IField)toElement);
					}

					if (dupedDriveNode is IDrive driveNode && driveNode.TrySetRootTarget(toElement))
					{
						Debug("ProtoFlux drive node restored.");
						return true;
					}
					else
					{
						Debug("Failed to restore ProtoFlux drive node.");
						return false;
					}
				}

				Debug($"Requires mapping for source component: {ElementIdentifierString(comp)}");

				Component newComp;
				bool newSpawn = false;
				if (newCompMappings.TryGetValue(comp, out var existingComp))
				{
					newComp = existingComp;
					Debug($"Found existing component mapping: {ElementIdentifierString(newComp)}");
				}
				else
				{
					// It breaks the original drive if I don't set breakExternalReferences to true
					newComp = targetSlot.DuplicateComponent(comp, breakExternalReferences: true);
					newCompMappings.Add(comp, newComp);
					Debug($"Duplicated new component, added new component mapping: {ElementIdentifierString(newComp)}");
					newSpawn = true;
				}

				if (undoable)
				{
					// not sure about the correct ordering of these undo things
					if (toElement is IField toField)
					{
						// If it got here then it's either not driven or we should break the drive
						if (breakExistingDrives.Value && toField.IsDriven)
						{
							toField.ActiveLink.ReleaseLink(undoable: true);
						}
						else
						{
							SafeFieldUndoPoint(toField);
						}
					}
					if (newSpawn)
					{
						newComp.CreateSpawnUndoPoint();
					}
				}

				var pathToLink = GetMemberStack(link);

				if (recursive)
				{
					Debug("Entered recursive part.");

					var allDriveData = new List<DriveData>();
					CollectDriveData(comp, allDriveData);
					foreach (var driveData in allDriveData)
					{
						Debug($"Found driven member on source component that needs to be restored: {ElementIdentifierString(driveData.targetMember)}");
						var correspondingMember = FindCorrespondingMember(newComp, driveData.targetMember, driveData.stackToTargetMember);
						if (RestoreDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: undoable, recursive: recursive, recursionDepth: recursionDepth + 1))
						{
							Debug("Restored drive.");
						}
						else
						{
							Debug("Failed to restore drive.");
						}
						Debug($"Recursion depth: {recursionDepth}");
					}

					Debug("Finished recursive part.");
				}

				ISyncMember foundMember = FindCorrespondingMember(newComp, link, pathToLink);

				if (foundMember != null && foundMember.Name == link.Name && foundMember.GetType() == link.GetType())
				{
					var syncRef = (ISyncRef)foundMember;
					syncRef.Target = toElement;
					return true;
				}

				return false;
			}

			ISyncMember FindCorrespondingMember(Worker root, ISyncMember memberToFind, Stack<ISyncMember> pathToMemberToFind)
			{
				var rootWorker = root;
				Debug($"Searching {rootWorker.Name} for {memberToFind.Name} of type {memberToFind.GetType().GetNiceName()}");
				Debug($"Current stack: {string.Join(",", pathToMemberToFind.Select(x => x.Name))}");
				var syncElementListType = typeof(SyncElementList<>).MakeGenericType(memberToFind.GetType());
				while (pathToMemberToFind.Count > 0)
				{
					var member = pathToMemberToFind.Pop();
					Debug($"Looking for: {member.Name}");
					var correspondingMember = rootWorker.GetSyncMember(member.Name);
					if (correspondingMember is Worker nextWorker)
					{
						Debug($"Found next worker: {nextWorker.Name}");
						rootWorker = nextWorker;
					}
					else if (correspondingMember is ISyncList list)
					{
						Debug($"Found list: {list.Name}");
						var genericArg = list.GetType().GetGenericArguments()[0];
						if (syncElementListType.IsAssignableFrom(list.GetType()))
						{
							foreach (var elem in list.Elements)
							{
								if (elem is ISyncMember listMember && listMember.Name == memberToFind.Name)
								{
									Debug($"Found list member: {listMember.Name}");
									return listMember;
								}
							}
						}
						else if (typeof(Worker).IsAssignableFrom(genericArg))
						{
							Debug("List of workers");
							var listWorkerName = pathToMemberToFind.Pop().Name;
							foreach (var elem in list.Elements)
							{
								var listWorker = (Worker)elem;
								if (listWorker.Name == listWorkerName)
								{
									Debug($"Found list worker: {listWorker.Name}");
									var result = FindCorrespondingMember((Worker)elem, memberToFind, pathToMemberToFind);
									if (result != null)
									{
										return result;
									}
									break;
								}
							}
							break;
						}
					}
					else if (correspondingMember is ISyncBag bag)
					{
						Debug($"Found bag: {bag.Name}");
						var genericArg = bag.GetType().GetGenericArguments()[0];
						if (genericArg == memberToFind.GetType())
						{
							foreach (var elem in bag.Values)
							{
								if (elem is ISyncMember bagMember && bagMember.Name == memberToFind.Name)
								{
									Debug($"Found bag member: {bagMember.Name}");
									return bagMember;
								}
							}
						}
						else if (typeof(Worker).IsAssignableFrom(genericArg))
						{
							// Need to do recursion here probably
							Debug("Bag of workers");
							var bagWorkerName = pathToMemberToFind.Pop().Name;
							foreach (var elem in bag.Values)
							{
								var bagWorker = (Worker)elem;
								if (bagWorker.Name == bagWorkerName)
								{
									Debug($"Found bag worker: {bagWorker.Name}");
									var result = FindCorrespondingMember((Worker)elem, memberToFind, pathToMemberToFind);
									if (result != null)
									{
										return result;
									}
									break;
								}
							}
							break;
						}
					}
					else if (correspondingMember != null)
					{
						Debug($"Found: {correspondingMember.Name}");
						return correspondingMember;
					}
					else
					{
						Debug("Could not find the member.");
						break;
					}
				}
				Debug("Failed. Returning null");
				return null;
			}

			bool SetupValueCopy(IField fromField, IField toField, bool undoable = false)
			{
				MethodInfo copyMethod;
				if (toField is ISyncRef syncRef)
				{
					copyMethod = AccessTools.Method(typeof(MassComponentManipulator), "GetReferenceCopy").MakeGenericMethod(syncRef.TargetType);
				}
				else
				{
					copyMethod = AccessTools.Method(typeof(MassComponentManipulator), "GetValueCopy").MakeGenericMethod(toField.ValueType);
				}

				if (toField.IsDriven)
				{
					if (breakExistingDrives.Value)
					{
						toField.ActiveLink.ReleaseLink(undoable: undoable);
					}
					else
					{
						Debug("Field is driven, skipping");
						return false;
					}
				}
				else if (undoable)
				{
					SafeFieldUndoPoint(toField);
				}

				var valueCopy = (Component)copyMethod.Invoke(null, new object[] { fromField, toField });
				if (undoable)
				{
					valueCopy.CreateSpawnUndoPoint();
				}
				return valueCopy != null;
			}

			void RegenerateWizardUI()
			{
				WizardSearchDataSlot.DestroyChildren();
				WizardStaticContentSlot.DestroyChildren();
				//var rect = WizardContentSlot.GetComponent<RectTransform>();
				WizardUI.ForceNext = WizardStaticContentRect;
				WizardStaticContentSlot.RemoveAllComponents((Component c) => c != WizardStaticContentRect);

				searchRoot = WizardSearchDataSlot.FindChildOrAdd("searchRoot").GetComponentOrAttach<ReferenceField<Slot>>();
				//searchRoot.Reference.Target = WizardSlot.World.RootSlot;
				sourceComponent = WizardSearchDataSlot.FindChildOrAdd("sourceComponent").GetComponentOrAttach<ReferenceField<Component>>();

				modeField = WizardSearchDataSlot.FindChildOrAdd("modeField").GetComponentOrAttach<ValueField<int>>();

				breakExistingDrives = WizardSearchDataSlot.FindChildOrAdd("breakExistingDrives").GetComponentOrAttach<ValueField<bool>>();

				//restoreDrives = WizardSearchDataSlot.FindChildOrAdd("restoreDrives").GetComponentOrAttach<ValueField<bool>>();
				//restoreDrivesRecursively = WizardSearchDataSlot.FindChildOrAdd("restoreDrivesRecursively").GetComponentOrAttach<ValueField<bool>>();

				VerticalLayout verticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout.ForceExpandHeight.Value = false;

				SyncMemberEditorBuilder.Build(sourceComponent.Reference, "Source Component", null, WizardUI);
				SyncMemberEditorBuilder.Build(searchRoot.Reference, "Target Hierarchy Slot", null, WizardUI);

				WizardUI.Text("Mode:");
				WizardUI.ValueRadio<int>("Write".AsLocaleKey(), modeField.Value, 0);
				WizardUI.ValueRadio<int>("Drive From Source".AsLocaleKey(), modeField.Value, 1);
				WizardUI.ValueRadio<int>("Deep Copy Existing Drives From Source".AsLocaleKey(), modeField.Value, 2);
				WizardUI.ValueRadio<int>("Write And Deep Copy Existing Drives From Source".AsLocaleKey(), modeField.Value, 3);

				SyncMemberEditorBuilder.Build(breakExistingDrives.Value, "Break Existing Drives On Target", null, WizardUI);

				//SyncMemberEditorBuilder.Build(restoreDrives.Value, "Drive From Source (ValueCopy)", null, WizardUI);
				//SyncMemberEditorBuilder.Build(restoreDrivesRecursively.Value, "Recursive Deep Copy Drives", null, WizardUI);

				WizardUI.Spacer(24f);

				WizardUI.Text("<color=hero.red>WARNING: Effects may not be fully undoable. Proceed with care!</color>");
				WizardUI.Spacer(24f);

				WizardUI.PushStyle();

				WizardUI.Style.MinWidth = -1f;
				WizardUI.Style.MinHeight = -1f;
				WizardUI.Style.PreferredWidth = -1f;
				WizardUI.Style.PreferredHeight = -1f;
				WizardUI.Style.FlexibleWidth = -1f;
				WizardUI.Style.FlexibleHeight = -1f;

				VerticalLayout verticalLayout2 = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
				verticalLayout2.ForceExpandHeight.Value = false;

				WizardGeneratedContentSlot = WizardUI.Root;
				//WizardGeneratedContentRect = WizardUI.CurrentRect;

				WizardUI.PopStyle();

				sourceComponent.Reference.Changed += (reference) =>
				{
					//WizardGeneratedFieldsDataSlot.DestroyChildren();
					WizardGeneratedContentSlot.DestroyChildren();
					//WizardUI.ForceNext = WizardGeneratedFieldsRect;
					WizardUI.NestInto(WizardGeneratedContentSlot);
					//WizardGeneratedFieldsSlot.RemoveAllComponents((Component c) => c != WizardGeneratedFieldsRect);
					if (((ISyncRef)reference).Target != null)
					{

						//WizardUI.Text("Component Members");
						//WizardUI.Spacer(24f);
						WizardUI.Button("Select All").LocalPressed += (btn, data) =>
						{
							SetEnabledFields(true);
						};
						WizardUI.Button("Deselect All").LocalPressed += (btn, data) =>
						{
							SetEnabledFields(false);
						};

						WizardUI.Spacer(24f);

						WizardUI.PushStyle();

						WizardUI.Style.MinWidth = -1f;
						WizardUI.Style.MinHeight = -1f;
						WizardUI.Style.PreferredWidth = -1f;
						WizardUI.Style.PreferredHeight = -1f;
						WizardUI.Style.FlexibleWidth = -1f;
						WizardUI.Style.FlexibleHeight = -1f;

						WizardUI.PushStyle();
						WizardUI.Style.FlexibleHeight = 1f;
						WizardUI.ScrollArea();
						WizardUI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
						WizardUI.PopStyle();

						VerticalLayout fieldsVerticalLayout = WizardUI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
						fieldsVerticalLayout.ForceExpandHeight.Value = false;

						//WizardGeneratedFieldsSlot = WizardUI.Root;
						//WizardGeneratedFieldsRect = WizardUI.CurrentRect;

						WizardUI.PopStyle();

						

						// here!

						WizardUI.PushStyle();

						WizardUI.Style.MinWidth = config.GetValue(Key_FieldsMinWidth);
						WizardUI.Style.MinHeight = config.GetValue(Key_FieldsMinHeight);
						WizardUI.Style.PreferredWidth = config.GetValue(Key_FieldsPreferredWidth);
						WizardUI.Style.PreferredHeight = config.GetValue(Key_FieldsPreferredHeight);
						WizardUI.Style.FlexibleWidth = config.GetValue(Key_FieldsFlexibleWidth);
						WizardUI.Style.FlexibleHeight = config.GetValue(Key_FieldsFlexibleHeight);

						workerMemberFields.Clear();

						//dummyComponent = WizardGeneratedFieldsDataSlot.AddSlot(searchComponent.Reference.Target.Name).AttachComponent(searchComponent.Reference.Target.GetType());

						GenerateWorkerMemberEditors(WizardUI, sourceComponent.Reference.Target);

						WizardUI.PopStyle();

						WizardUI.NestOut(); // Out of GeneratedFieldsSlot, Into ScrollArea slot
						WizardUI.NestOut(); // Out of ScrollArea slot, Into WizardGeneratedContentSlot

						WizardUI.Spacer(24f);

						var applyButton = WizardUI.Button("Copy to Hierarchy (Undoable)");
						applyButton.LocalPressed += (btn, data) =>
						{
							Debug("Apply pressed");
							Apply();
						};

						//var referenceEqualityDriver = WizardUI.Current.AttachComponent<ReferenceEqualityDriver<Slot>>();
						//referenceEqualityDriver.TargetReference.Target = searchRoot.Reference;
						//referenceEqualityDriver.Target.Target = applyButton.EnabledField;
						//referenceEqualityDriver.Invert.Value = true;

						WizardUI.Spacer(24f);
					}
					UpdateCanvasSize();
				};
				UpdateCanvasSize();
			}

			//private void HandleField(Worker worker, ISyncMember syncMember)
			//{
			//	if (!workerMemberFields[worker.Name].ContainsKey(syncMember.Name))
			//	{
			//		Warn("syncMember not in dictionary. Skipping.");
			//		return;
			//	}
			//	//Msg("Is field");
			//	//IField field = ((IField)syncMember);
			//	//SyncMemberWizardFields fieldsStruct = workerMemberFields[worker.Name][syncMember.Name];
			//	//if (fieldsStruct.enabledField.Value == false) return;
			//	//IField sourceMember = (IField)fieldsStruct.sourceSyncMember;
			//	//field.CreateUndoPoint();

			//	//field.BoxedValue = sourceMember.BoxedValue;
			//}

			private void HandleWorker(Worker worker)
			{
				if (!workerMemberFields.ContainsKey(worker.Name))
				{
					Warn($"Worker: {worker.Name}:{worker.GetType().GetNiceName()} does not exist in workerMemberFields");
					return;
				}
				foreach (ISyncMember syncMember in worker.SyncMembers)
				{
					//Debug("syncMember Name: " + syncMember.Name);

					if (syncMember is SyncObject)
					{
						Debug("syncMember Name: " + syncMember.Name);
						Debug("Is SyncObject");
						HandleWorker((Worker)syncMember);
					}
					else if (syncMember is IField)
					{
						if (!workerMemberFields[worker.Name].ContainsKey(syncMember.Name))
						{
							Warn("syncMember not in dictionary. Skipping.");
							return;
						}

						SyncMemberData fieldsStruct = workerMemberFields[worker.Name][syncMember.Name];
						if (fieldsStruct.enabledField.Value == false) continue;

						Debug("syncMember Name: " + syncMember.Name);
						Debug("Is IField");

						ISyncMember sourceMember = fieldsStruct.sourceSyncMember;

						// copy drives
						var sourceField = (IField)sourceMember;
						if (DriveFromSource)
						{
							if (SetupValueCopy(sourceField, (IField)syncMember, undoable: true))
							{
								Debug("Setup value copy.");
							}
							else
							{
								Debug("Failed to setup value copy.");
							}
						}
						else if (RecursiveDeepCopyDrives && sourceField.IsDriven)
						{
							if (RestoreDrives((SyncElement)sourceField, (SyncElement)syncMember, newCompMappings, undoable: true, recursive: RecursiveDeepCopyDrives))
							{
								Debug("Deep copied drives.");
							}
							else
							{
								Debug("Failed to deep copy drives.");
							}
						}
						else if (ShouldWrite)
						{
							var targetField = (IField)syncMember;

							if (targetField.IsDriven)
							{
								if (breakExistingDrives.Value)
								{
									var link = targetField.ActiveLink;
									link.ReleaseLink(undoable: true);
								}
								else if (!targetField.IsHooked)
								{
									Debug("Field is driven and not hooked, skipping");
									continue;
								}
							}
							else
							{
								SafeFieldUndoPoint(targetField);
							}

							syncMember.CopyValues(sourceMember);

							Debug("Values written.");
						}
						else
						{
							Debug("Nothing to do.");
						}
					}
					else if (syncMember is SyncElement)
					{
						if (!workerMemberFields[worker.Name].ContainsKey(syncMember.Name))
						{
							Warn("syncMember not in dictionary. Skipping.");
							continue;
						}

						SyncMemberData fieldsStruct = workerMemberFields[worker.Name][syncMember.Name];
						if (fieldsStruct.enabledField.Value == false) continue;

						Debug("syncMember Name: " + syncMember.Name);
						Debug("Is SyncElement");

						ISyncMember sourceMember = fieldsStruct.sourceSyncMember;

						if (ShouldWrite)
						{
							syncMember.CopyValues(sourceMember);
							Debug("Values written.");
						}

						if (ShouldDrive)
						{
							// Restore drives to list and bag elements
							if (sourceMember is ISyncList sourceList)
							{
								Debug("Is List");
								var allDriveData = new List<DriveData>();
								foreach (var elem in sourceList.Elements)
								{
									if (elem is IField listField)
									{
										if (DriveFromSource || (RecursiveDeepCopyDrives && listField.IsDriven))
										{
											var driveData = new DriveData();
											driveData.targetMember = listField;
											driveData.stackToTargetMember = GetMemberStack(listField);
											allDriveData.Add(driveData);
										}
									}
									else if (elem is Worker listWorker)
									{
										CollectDriveData(listWorker, allDriveData);
									}
								}
								foreach (var driveData in allDriveData)
								{
									Debug($"Drive data member: {ElementIdentifierString(driveData.targetMember)}");
									var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), driveData.targetMember, driveData.stackToTargetMember);
									if (DriveFromSource)
									{
										if (SetupValueCopy((IField)driveData.targetMember, (IField)correspondingMember, undoable: true))
										{
											Debug("Setup value copy.");
										}
										else
										{
											Debug("Failed to setup value copy.");
										}
									}
									else if (RecursiveDeepCopyDrives)
									{
										if (RestoreDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: true, recursive: true))
										{
											Debug("Deep copied drives.");
										}
										else
										{
											Debug("Failed to deep copy drives.");
										}
									}
								}
							}
							else if (sourceMember is ISyncBag sourceBag)
							{
								Debug("Is Bag");
								var allDriveData = new List<DriveData>();
								foreach (var elem in sourceBag.Values)
								{
									if (elem is IField listField)
									{
										if (DriveFromSource || (RecursiveDeepCopyDrives && listField.IsDriven))
										{
											var driveData = new DriveData();
											driveData.targetMember = listField;
											driveData.stackToTargetMember = GetMemberStack(listField);
											allDriveData.Add(driveData);
										}
									}
									else if (elem is Worker listWorker)
									{
										CollectDriveData(listWorker, allDriveData);
									}
								}
								foreach (var driveData in allDriveData)
								{
									Debug($"Drive data member: {ElementIdentifierString(driveData.targetMember)}");
									var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), driveData.targetMember, driveData.stackToTargetMember);
									if (DriveFromSource)
									{
										if (SetupValueCopy((IField)driveData.targetMember, (IField)correspondingMember, undoable: true))
										{
											Debug("Setup value copy.");
										}
										else
										{
											Debug("Failed to setup value copy.");
										}
									}
									else if (RecursiveDeepCopyDrives)
									{
										if (RestoreDrives((SyncElement)driveData.targetMember, (SyncElement)correspondingMember, newCompMappings, undoable: true, recursive: true))
										{
											Debug("Deep copied drives.");
										}
										else
										{
											Debug("Failed to deep copy drives.");
										}
									}
									//Debug($"Driven field to restore: {ElementIdentifierString(driveData.drivenMember)}");
									//var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), driveData.drivenMember, driveData.stackToDrivenMember);
									//if (RestoreDrives((SyncElement)driveData.drivenMember, (SyncElement)correspondingMember, newCompMappings, undoable: true, recursive: RecursiveDeepCopyDrives))
									//{
									//	Debug("Restored drive.");
									//}
									//else
									//{
									//	Debug("Failed to restore drive.");
									//}
								}
							}
							else if (sourceMember is SyncPlayback sourcePlayback)
							{
								if (DriveFromSource)
								{
									var playbackSynchronizer = syncMember.FindNearestParent<Slot>().AttachComponent<PlaybackSynchronizer>();
									var targetPlayback = (SyncPlayback)syncMember;
									playbackSynchronizer.Source.Target = sourcePlayback;
									playbackSynchronizer.Target.Target = targetPlayback;
									playbackSynchronizer.CreateSpawnUndoPoint();
									Debug("Synchronized playbacks.");
								}
								else if (RecursiveDeepCopyDrives)
								{
									if (sourcePlayback.IsDriven)
									{
										Debug($"Driven playback to restore: {ElementIdentifierString(sourcePlayback)}");
										var correspondingMember = FindCorrespondingMember(syncMember.FindNearestParent<Component>(), sourcePlayback, GetMemberStack(sourcePlayback));
										if (RestoreDrives((SyncElement)sourceMember, (SyncElement)correspondingMember, newCompMappings, undoable: true, recursive: true))
										{
											Debug("Deep copied drives.");
										}
										else
										{
											Debug("Failed to deep copy drives.");
										}
									}
								}
							}
						}
					}
					else
					{
						Debug("syncMember is not supported type: " + syncMember.GetType().Name ?? "NULL");
					}
				}
			}

			private void Apply()
			{
				if (searchRoot.Reference.Target == null || sourceComponent.Reference.Target == null)
				{
					Debug("searchRoot or sourceComponent is null!");
					return;
				}

				//if (workerMemberFields.Count == 0 || workerMemberFields.Values.Count == 0) return;

				// it could be an empty undo batch if there are no matching components?
				newCompMappings.Clear();
				WizardSlot.World.BeginUndoBatch("Set component members");

				foreach (Component c in searchRoot.Reference.Target.GetComponentsInChildren((Component c) =>
					c.GetType() == sourceComponent.Reference.Target.GetType() && c != sourceComponent.Reference.Target))
				{
					Debug(c.Name);
					HandleWorker(c);
				}

				WizardSlot.World.EndUndoBatch();
			}

			void GenerateWorkerMemberEditors(UIBuilder UI, Worker targetWorker, bool recursive = true)
			{
				workerMemberFields.Add(targetWorker.Name, new Dictionary<string, SyncMemberData>());

				int i = -1;
				foreach (ISyncMember syncMember in targetWorker.SyncMembers)
				{
					i += 1;

					Type type = null;

					if (syncMember is IField field)
					{
						type = field.ValueType;
					}
					else if (syncMember is ISyncRef syncRef)
					{
						type = syncRef.TargetType;
					}
					else
					{
						type = syncMember.GetType();
					}

					colorX c = type.GetTypeColor().MulRGB(1.5f);
					UI.Style.TextColor = MathX.LerpUnclamped(RadiantUI_Constants.TEXT_COLOR, c, 0.1f); // copying the way field names get colored in the inspector

					if (syncMember is SyncObject)
					{
						UI.PushStyle();
						UI.Style.PreferredHeight = 24f;
						UI.Text($"{syncMember.Name}").HorizontalAlign.Value = TextHorizontalAlignment.Left;
						UI.PopStyle();
						if (recursive)
						{
							UI.PushStyle();
							UI.HorizontalLayout();
							UI.FitContent(SizeFit.Disabled, SizeFit.PreferredSize);
							UI.Style.MinWidth = 16f;
							UI.Style.FlexibleWidth = -1;
							UI.Panel();
							colorX color = colorX.Black;
							UI.Image(in color);
							UI.CurrentRect.OffsetMin.Value = new float2(UI.Style.MinWidth * 0.5f - 2f);
							UI.CurrentRect.OffsetMax.Value = new float2(0f - (UI.Style.MinWidth * 0.5f - 2f));
							UI.NestOut();

							VerticalLayout verticalLayout = UI.VerticalLayout(4f, childAlignment: Alignment.TopCenter);
							verticalLayout.ForceExpandHeight.Value = false;
							verticalLayout.PaddingLeft.Value = 6f;

							UI.PopStyle();

							GenerateWorkerMemberEditors(UI, (Worker)syncMember);

							UI.NestOut();
							UI.NestOut();
						}
						continue;
					}
					//else if (syncMember is ISyncList && config.GetValue(Key_HandleLists))
					//{
					//	Debug("Is ISyncList");
					//	Debug(syncMember.Name ?? "NULL");
					//	Debug(syncMember.GetType().ToString() ?? "NULL");
					//	Debug(syncMember.GetType().GetGenericArguments()[0].ToString() ?? "NULL");
					//}
					else if (!(syncMember is SyncElement))
					{
						UI.PushStyle();
						UI.Style.PreferredHeight = 24f;
						UI.Style.TextColor = colorX.Gray;
						UI.Text($"{syncMember.Name} (not supported)").HorizontalAlign.Value = TextHorizontalAlignment.Left;
						UI.PopStyle();
						continue;
					}

					//Type genericTypeDefinition = null;
					//if (syncMember.GetType().IsGenericType)
					//{
					//	genericTypeDefinition = syncMember.GetType().GetGenericTypeDefinition();
					//}

					FieldInfo fieldInfo = targetWorker.GetSyncMemberFieldInfo(syncMember.Name);

					//Slot s = WizardGeneratedFieldsDataSlot.FindChildOrAdd(targetWorker.Name + ":" + i.ToString() + "_" + syncMember.Name);

					var horizontalLayout = UI.HorizontalLayout(4f, childAlignment: Alignment.MiddleLeft);
					horizontalLayout.ForceExpandWidth.Value = false;
					//horizontalLayout.PaddingLeft.Value = CANVAS_WIDTH_DEFAULT / 4f;

					UI.PushStyle();

					//WizardUI.Style.MinWidth = config.GetValue(Key_CheckboxMinWidth);
					//WizardUI.Style.MinHeight = config.GetValue(Key_CheckboxMinHeight);
					//WizardUI.Style.PreferredWidth = config.GetValue(Key_CheckboxPreferredWidth);
					//WizardUI.Style.PreferredHeight = config.GetValue(Key_CheckboxPreferredHeight);
					//WizardUI.Style.FlexibleWidth = config.GetValue(Key_CheckboxFlexibleWidth);
					//WizardUI.Style.FlexibleHeight = config.GetValue(Key_CheckboxFlexibleHeight);

					UI.Style.MinWidth = config.GetValue(Key_CheckboxMinWidth);
					UI.Style.MinHeight = config.GetValue(Key_CheckboxMinHeight);
					UI.Style.PreferredWidth = config.GetValue(Key_CheckboxPreferredWidth);
					UI.Style.PreferredHeight = config.GetValue(Key_CheckboxPreferredHeight);
					UI.Style.FlexibleWidth = config.GetValue(Key_CheckboxFlexibleWidth);
					UI.Style.FlexibleHeight = config.GetValue(Key_CheckboxFlexibleHeight);

					UI.Style.TextColor = RadiantUI_Constants.Neutrals.LIGHT;

					var checkbox = UI.Checkbox(false);

					UI.PopStyle();

					//SyncMemberEditorBuilder.Build(syncMember, syncMember.Name, fieldInfo, UI);

					UI.PushStyle();
					UI.Style.PreferredHeight = 24f;
					if (!(syncMember is IField))// && syncMember is SyncElement)
					{
						//UI.Text($"{syncMember.GetType().GetGenericTypeDefinition().Name ?? syncMember.GetType().GetNiceName()} {syncMember.Name} (not undoable)").HorizontalAlign.Value = TextHorizontalAlignment.Left;
						UI.Text($"{syncMember.Name} <color=hero.red>(not undoable)</color>").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					}
					else
					{
						UI.Text($"{syncMember.Name}").HorizontalAlign.Value = TextHorizontalAlignment.Left;
					}
					UI.PopStyle();

					//UI.MemberEditor((IField)syncMember, )

					SyncMemberData fieldsStruct = new SyncMemberData();
					fieldsStruct.sourceSyncMember = syncMember;
					fieldsStruct.enabledField = checkbox.State;

					workerMemberFields[targetWorker.Name].Add(syncMember.Name, fieldsStruct);

					UI.NestOut();
				}
			}
		}
	}
}